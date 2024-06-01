﻿namespace ADHelpers
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;
    using System.Management.Automation;

    /// <summary>
    /// <para type="synopsis">Returns an user's last login/failed login/expiry times etc.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LockoutStatus")]
    [OutputType(typeof(IEnumerable<LockoutSet>))]
    public class GetLockoutStatusCommand : ADSearcher
    {
        /// <summary>
        /// <para type="description">Identifier of AD Object, use -SearchProperty to chose which field to search on. (accepts '*'s)</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "Identifier of AD Object. (accepts '*'s)")]
        public string Identity { get; set; }

        private bool AllServers { get; set; } = false;

        /// <summary>
        /// <para type="description">Entry point of command.</para>
        /// </summary>
        protected override void EndProcessing()
        {
            Stack<LockoutSet> output = new Stack<LockoutSet>();
            if (AllServers)
            {
                output = new Stack<LockoutSet>(GetLockoutDataForAllServers());
            }
            else
            {
                DirectorySearcher ds = new DirectorySearcher();
                output.Push(GetLockoutData(ds));
                ds.Dispose();
            }
            foreach (LockoutSet lockoutset in output)
            {
                WriteObject(lockoutset);
            }

        }

        private Stack<LockoutSet> GetLockoutDataForAllServers()
        {
            Domain domain = Domain.GetCurrentDomain();
            Stack<LockoutSet> output = new Stack<LockoutSet>();
            foreach (DomainController dc in domain.FindAllDiscoverableDomainControllers())
            {
                DirectorySearcher searcher = dc.GetDirectorySearcher();
                output.Push(GetLockoutData(searcher));
            }
            return output;

        }

        private static DateTime ADEpoc = new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        private static LockoutSet EmptyLockoutSet = new LockoutSet("null", "null", 0, ADEpoc, ADEpoc, false, ADEpoc, ADEpoc, ADEpoc);

        private LockoutSet GetLockoutData(DirectorySearcher searcher)
        {
            //UserPrincipal user = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), Identity);

            searcher.PropertiesToLoad.Clear();
            _ = searcher.PropertiesToLoad.Add("samaccountname");
            _ = searcher.PropertiesToLoad.Add("badPwdCount");
            _ = searcher.PropertiesToLoad.Add("badpasswordtime");
            _ = searcher.PropertiesToLoad.Add("lastLogon");
            _ = searcher.PropertiesToLoad.Add("useraccountcontrol");
            _ = searcher.PropertiesToLoad.Add("lockouttime");
            _ = searcher.PropertiesToLoad.Add("msDS-UserPasswordExpiryTimeComputed");
            _ = searcher.PropertiesToLoad.Add("pwdlastset");
            searcher.Filter = $"(&(objectclass={ADClass})({SearchProperty}={Identity}))";
            SearchResult sr = searcher.FindOne();
            string server = searcher.SearchRoot.Name;
            LockoutSet ls = EmptyLockoutSet;
            if (sr != null)
            {
                ResultPropertyCollection r = sr.Properties;
                ls = new LockoutSet(server,
                r.Contains("samaccountname") ? (string)r["samaccountname"][0] : EmptyLockoutSet.SamAccountName,
                r.Contains("badPwdCount") ? (int)r["badPwdCount"][0] : EmptyLockoutSet.BadPwdCount,
                r.Contains("badpasswordtime") ? ADEpoc.AddMilliseconds((long)r["badpasswordtime"][0] * 0.0001) : EmptyLockoutSet.LastBadPasswordAttempt,
                r.Contains("lastLogon") ? ADEpoc.AddMilliseconds((long)r["lastLogon"][0] * 0.0001) : EmptyLockoutSet.LastLogonDate,
                r.Contains("useraccountcontrol") ? ((int)r["useraccountcontrol"][0] & 0x2) == 0 : EmptyLockoutSet.IsEnabled,
                r.Contains("lockouttime") ? ADEpoc.AddMilliseconds((long)r["lockouttime"][0] * 0.0001) : EmptyLockoutSet.LockoutTime,
                r.Contains("msDS-UserPasswordExpiryTimeComputed") ? ADEpoc.AddMilliseconds((long)r["msDS-UserPasswordExpiryTimeComputed"][0] * 0.0001) : EmptyLockoutSet.PasswordExpires,
                r.Contains("pwdlastset") ? ADEpoc.AddMilliseconds((long)r["pwdlastset"][0] * 0.0001) : EmptyLockoutSet.PasswordLastSet);
            }
            return ls;

        }
    }

    /// <summary>
    /// Represents data returned from the GetLockoutStatusCommand.
    /// </summary>
    public class LockoutSet
    {
        /// <summary>
        /// Basic ctor for the GetLockoutStatusCommand return type.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="userPrincipalName"></param>
        /// <param name="badPwdCound"></param>
        /// <param name="lastBadPasswordAttempt"></param>
        /// <param name="lastLogonDate"></param>
        /// <param name="enabled"></param>
        /// <param name="lockoutTime"></param>
        /// <param name="passwordExpires"></param>
        /// <param name="passwordLastSet"></param>
        public LockoutSet(string server, string userPrincipalName, int badPwdCound, DateTime lastBadPasswordAttempt, DateTime lastLogonDate, bool enabled, DateTime lockoutTime, DateTime passwordExpires, DateTime passwordLastSet)
        {
            Server = server;
            SamAccountName = userPrincipalName;
            BadPwdCount = badPwdCound;
            LastBadPasswordAttempt = lastBadPasswordAttempt;
            LastLogonDate = lastLogonDate;
            IsEnabled = enabled;
            LockoutTime = lockoutTime;
            PasswordExpires = passwordExpires;
            PasswordLastSet = passwordLastSet;
        }

        /// <summary>
        /// Which server this data was pulled from.
        /// </summary>
        public String Server { get; }
        /// <summary>
        /// User's SAMACCOUNTNAME.
        /// </summary>
        public String SamAccountName { get; }
        /// <summary>
        /// User's bad password attempt count.
        /// </summary>
        public int BadPwdCount { get; }
        /// <summary>
        /// Datetime of last bad password attempt.
        /// </summary>
        public DateTime LastBadPasswordAttempt { get; }
        /// <summary>
        /// Datetime of last successful login.
        /// </summary>
        public DateTime LastLogonDate { get; }
        /// <summary>
        /// Is the account Enabled.
        /// </summary>
        public bool IsEnabled { get; }
        /// <summary>
        /// When the account was locked.
        /// </summary>
        public DateTime LockoutTime { get; }
        /// <summary>
        /// Datetime of when the user's password expires.
        /// </summary>
        public DateTime PasswordExpires { get; }
        /// <summary>
        /// DateTime of when the password was last set.
        /// </summary>
        public DateTime PasswordLastSet { get; }
    }


}
