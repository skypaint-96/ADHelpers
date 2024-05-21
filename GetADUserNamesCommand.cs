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

    [Cmdlet(VerbsCommon.Get, "ADUserNames")]
    [OutputType(typeof(IEnumerable<LockoutSet>))]
    public class GetADUserNamesCommand : PSCmdlet
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

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            DirectorySearcher searcher = new DirectorySearcher();
            searcher.PropertiesToLoad.Add("samaccountname");
            searcher.PropertiesToLoad.Add("userprincipalname");
            searcher.PropertiesToLoad.Add("targetaddress");
            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("displayname");
            searcher.PropertiesToLoad.Add("Emailaddress");
            searcher.PropertiesToLoad.Add("Mailnickname");
            searcher.PropertiesToLoad.Add("Proxyaddresses");
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            SearchResult sr = searcher.FindOne();
            WriteObject(sr.Properties);

        }

        //public user
    }
}
