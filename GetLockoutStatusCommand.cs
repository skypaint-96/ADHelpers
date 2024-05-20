namespace ADHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;
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
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            SearchResult sr = searcher.FindOne();
            DirectoryEntry directoryEntry = sr.GetDirectoryEntry();
            LockoutSet lockoutSet = new LockoutSet(searcher.SearchRoot.Name,
                (string)directoryEntry.Properties["userPrincipalName"].Value,
                (int)directoryEntry.Properties["badLogonCount"].Value,
                (int)directoryEntry.Properties["badPwdCound"].Value,
                (DateTime)directoryEntry.Properties["lastBadPasswordAttempt"].Value,
                (DateTime)directoryEntry.Properties["lastLogonDate"].Value,
                (bool)directoryEntry.Properties["enabled"].Value,
                (bool)directoryEntry.Properties["lockedOut"].Value,
                (DateTime)directoryEntry.Properties["lockoutTime"].Value,
                (bool)directoryEntry.Properties["passwordExpired"].Value,
                (DateTime)directoryEntry.Properties["passwordLastSet"].Value);
            WriteObject(lockoutSet);

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
