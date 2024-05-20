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

            WriteDebug("1");
            searcher.PropertiesToLoad.Clear();
            searcher.PropertiesToLoad.Add("userprincipalname");
            searcher.PropertiesToLoad.Add("badPwdCound");
            searcher.PropertiesToLoad.Add("badpasswordtime");
            searcher.PropertiesToLoad.Add("lastLogon");
            searcher.PropertiesToLoad.Add("enabled");
            searcher.PropertiesToLoad.Add("lockouttime");
            searcher.PropertiesToLoad.Add("msDS-UserPasswordExpiryTimeComputed");
            searcher.PropertiesToLoad.Add("pwdlastpassword");
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            WriteDebug("2");
            SearchResult sr = searcher.FindOne();
            WriteDebug("3");
            DirectoryEntry directoryEntry = sr.GetDirectoryEntry();
            WriteDebug("4");
            WriteObject(directoryEntry);
            //LockoutSet lockoutSet = new LockoutSet("", user.UserPrincipalName, user.BadLogonCount, user.LastBadPasswordAttempt, user.LastLogon, user.Enabled, user.IsAccountLockedOut(), user.AccountLockoutTime, (DirectoryEntry)user.GetUnderlyingObject().);
            //WriteObject(lockoutSet);

        }
    }

    public class LockoutSet
    {
        public LockoutSet(string server, string userPrincipalName, int badLogonCount, int badPwdCound, DateTime lastBadPasswordAttempt, DateTime lastLogonDate, bool enabled, bool lockedOut, DateTime lockoutTime, bool passwordExpired, DateTime passwordLastSet)
        {
            Server = server;
            UserPrincipalName = userPrincipalName;
            BadLogonCount = badLogonCount;
            BadPwdCound = badPwdCound;
            LastBadPasswordAttempt = lastBadPasswordAttempt;
            LastLogonDate = lastLogonDate;
            Enabled = enabled;
            LockedOut = lockedOut;
            LockoutTime = lockoutTime;
            PasswordExpired = passwordExpired;
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
