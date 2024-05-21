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

    [Cmdlet(VerbsCommon.Get, "UniqueGroups")]
    [OutputType(typeof(IEnumerable<LockoutSet>))]
    public class GetUniqueGroupsCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string Identity1 { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string Identity2 { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;


        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            WriteObject("not yet implemented");

        }
    }

    
}
