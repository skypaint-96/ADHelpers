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
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        //[Parameter(
        //    Mandatory = false,
        //    Position = 2,
        //    ValueFromPipeline = true,
        //    ValueFromPipelineByPropertyName = true)]
        //public bool AllServers { get; set; } = false;

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            //Stack<LockoutSet> output = new Stack<LockoutSet>();
            //if (AllServers)
            //{
            //    output = new Stack<LockoutSet>(GetLockoutDataForAllServers());
            //}
            //else
            //{
                //output.Push(
                WriteObject(GetLockoutData(new DirectorySearcher()));
                //);
            //}
            //foreach (LockoutSet lockoutSet in output)
            //{
                //WriteObject(lockoutSet);
            //}

        }

        private Stack<LockoutSet> GetLockoutDataForAllServers()
        {
            throw new NotImplementedException();
            Domain domain = Domain.GetCurrentDomain();
            Stack<LockoutSet> output = new Stack<LockoutSet>();
            Parallel.ForEach((IEnumerable<DomainController>)domain.FindAllDiscoverableDomainControllers(), (DomainController dc) => {
                DirectorySearcher searcher = dc.GetDirectorySearcher();
                output.Push(GetLockoutData(searcher));
            });
            return output;

        }

        static DateTime ADEpoc = new DateTime(1601, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        static LockoutSet EmptyLockoutSet = new LockoutSet("null", "null", 0, ADEpoc, ADEpoc, false, ADEpoc, ADEpoc, ADEpoc);

        private LockoutSet GetLockoutData(DirectorySearcher searcher)
        {
            //UserPrincipal user = UserPrincipal.FindByIdentity(new PrincipalContext(ContextType.Domain), Identity);
            
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
            
            return new LockoutSet(searcher.SearchRoot.Name,
                sr.Properties.Contains("samaccountname") ? (string)sr.Properties["samaccountname"][0] : EmptyLockoutSet.SamAccountName,
                sr.Properties.Contains("badPwdCount") ? (int)sr.Properties["badPwdCount"][0] : EmptyLockoutSet.BadPwdCount,
                sr.Properties.Contains("badpasswordtime") ? ADEpoc.AddMilliseconds((long)sr.Properties["badpasswordtime"][0] * 0.0001) : EmptyLockoutSet.LastBadPasswordAttempt,
                sr.Properties.Contains("lastLogon") ? ADEpoc.AddMilliseconds((long)sr.Properties["lastLogon"][0] * 0.0001) : EmptyLockoutSet.LastLogonDate,
                sr.Properties.Contains("useraccountcontrol") ? ((int)sr.Properties["useraccountcontrol"][0] & 0x2) == 0 : EmptyLockoutSet.Enabled,
                sr.Properties.Contains("lockouttime") ? ADEpoc.AddMilliseconds((long)sr.Properties["lockouttime"][0] * 0.0001) : EmptyLockoutSet.LockoutTime,
                sr.Properties.Contains("msDS-UserPasswordExpiryTimeComputed") ? ADEpoc.AddMilliseconds((long)sr.Properties["msDS-UserPasswordExpiryTimeComputed"][0] * 0.0001) : EmptyLockoutSet.PasswordExpires,
                sr.Properties.Contains("pwdlastset") ? ADEpoc.AddMilliseconds((long)sr.Properties["pwdlastset"][0] * 0.0001) : EmptyLockoutSet.PasswordLastSet);

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
