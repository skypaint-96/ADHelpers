namespace ADHelpers
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "LockoutStatus")]
    [OutputType(typeof(IEnumerable<LockoutSet>))]
    public class GetLockoutStatusCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string Identity { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public bool AllServers { get; set; } = false;

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
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
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
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
                r.Contains("useraccountcontrol") ? ((int)r["useraccountcontrol"][0] & 0x2) == 0 : EmptyLockoutSet.Enabled,
                r.Contains("lockouttime") ? ADEpoc.AddMilliseconds((long)r["lockouttime"][0] * 0.0001) : EmptyLockoutSet.LockoutTime,
                r.Contains("msDS-UserPasswordExpiryTimeComputed") ? ADEpoc.AddMilliseconds((long)r["msDS-UserPasswordExpiryTimeComputed"][0] * 0.0001) : EmptyLockoutSet.PasswordExpires,
                r.Contains("pwdlastset") ? ADEpoc.AddMilliseconds((long)r["pwdlastset"][0] * 0.0001) : EmptyLockoutSet.PasswordLastSet);
            }
            return ls;

        }
    }

    public class LockoutSet
    {
        public LockoutSet(string server, string userPrincipalName, int badPwdCound, DateTime lastBadPasswordAttempt, DateTime lastLogonDate, bool enabled, DateTime lockoutTime, DateTime passwordExpires, DateTime passwordLastSet)
        {
            Server = server;
            SamAccountName = userPrincipalName;
            BadPwdCount = badPwdCound;
            LastBadPasswordAttempt = lastBadPasswordAttempt;
            LastLogonDate = lastLogonDate;
            Enabled = enabled;
            LockoutTime = lockoutTime;
            PasswordExpires = passwordExpires;
            PasswordLastSet = passwordLastSet;
        }

        public String Server { get; }
        public String SamAccountName { get; }
        public int BadPwdCount { get; }
        public DateTime LastBadPasswordAttempt { get; }
        public DateTime LastLogonDate { get; }
        public bool Enabled { get; }
        public DateTime LockoutTime { get; }
        public DateTime PasswordExpires { get; }
        public DateTime PasswordLastSet { get; }
    }


}
