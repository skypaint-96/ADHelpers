namespace ADHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;
    using System.DirectoryServices.AccountManagement;
    using System.Threading.Tasks;

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
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public bool AllServers { get; set; } = false;

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            if (AllServers)
            {
                GetLockoutDataForAllServers();
            }
            else
            {
                GetLockoutData(new DirectorySearcher());
            }

        }

        private void GetLockoutDataForAllServers()
        {
            Domain domain = Domain.GetCurrentDomain();
            Parallel.ForEach((IEnumerable<DomainController>)domain.FindAllDiscoverableDomainControllers(), (DomainController dc) => {
                DirectorySearcher searcher = dc.GetDirectorySearcher();
                GetLockoutData(searcher);
            });

        }

        private void GetLockoutData(DirectorySearcher searcher)
        {
            //UserPrincipal user = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), Identity);
            DateTime epoc = new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            searcher.PropertiesToLoad.Clear();
            searcher.PropertiesToLoad.Add("samaccountname");
            searcher.PropertiesToLoad.Add("badPwdCount");
            searcher.PropertiesToLoad.Add("badpasswordtime");
            searcher.PropertiesToLoad.Add("lastLogon");
            searcher.PropertiesToLoad.Add("useraccountcontrol");
            searcher.PropertiesToLoad.Add("lockouttime");
            searcher.PropertiesToLoad.Add("msDS-UserPasswordExpiryTimeComputed");
            searcher.PropertiesToLoad.Add("pwdlastset");
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            SearchResult sr = searcher.FindOne();
            WriteObject(sr.Properties);

            WriteObject(new LockoutSet(searcher.SearchRoot.Name,
                (string)sr.Properties["samaccountname"][0], 
                (int)sr.Properties["badPwdCount"][0], 
                epoc.AddMilliseconds((long)sr.Properties["badpasswordtime"][0] * 0.0001), 
                epoc.AddMilliseconds((long)sr.Properties["lastLogon"][0] * 0.0001), 
                ((int)sr.Properties["useraccountcontrol"][0] & 0x2) == 0x2,
                epoc.AddMilliseconds((long)sr.Properties["lockouttime"][0] * 0.0001), 
                epoc.AddMilliseconds((long)sr.Properties["msDS-UserPasswordExpiryTimeComputed"][0] * 0.0001), 
                epoc.AddMilliseconds((long)sr.Properties["pwdlastset"][0] * 0.0001)));

            //WriteObject(sr.Properties);
            //LockoutSet lockoutSet = new LockoutSet("", user.UserPrincipalName, user.BadLogonCount, user.LastBadPasswordAttempt, user.LastLogon, user.Enabled, user.IsAccountLockedOut(), user.AccountLockoutTime, (DirectoryEntry)user.GetUnderlyingObject().);
            //WriteObject(lockoutSet);

        }
    }

    public class LockoutSet
    {
        public LockoutSet(string server, string userPrincipalName, int badPwdCound, DateTime lastBadPasswordAttempt, DateTime lastLogonDate, bool enabled, DateTime lockoutTime, DateTime passwordExpires, DateTime passwordLastSet)
        {
            Server = server;
            UserPrincipalName = userPrincipalName;
            BadPwdCound = badPwdCound;
            LastBadPasswordAttempt = lastBadPasswordAttempt;
            LastLogonDate = lastLogonDate;
            Enabled = enabled;
            LockoutTime = lockoutTime;
            PasswordLastSet = passwordLastSet;
        }

        public String Server { get; }
        public String UserPrincipalName { get; }
        public int BadLogonCount { get; }
        public int BadPwdCound { get; }
        public DateTime LastBadPasswordAttempt { get; }
        public DateTime LastLogonDate { get; }
        public bool Enabled { get; }
        public bool LockedOut { get; }
        public DateTime LockoutTime { get; }
        public bool PasswordExpired { get; }
        public DateTime PasswordLastSet { get; }
    }

    public enum SearchType
    {
        SamAccountName,
        UserPrincipalName,
        CN
    }
}
