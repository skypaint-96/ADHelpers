namespace ADHelpers.Cmdlets
{
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;
    using System.Management.Automation;
    using ADHelpers.DataAccess;

    /// <summary>
    /// <para type="synopsis">Used to get the names of an AD user, useful as it shows if there are any missmatches/misspellings.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "UserNames")]
    [OutputType(typeof(IEnumerable<UserNamesRecord>))]
    public partial class GetADEXUserNamesCommand : Cmdlet
    {
        /// <summary>
        /// <para type="description">Identifier of AD Object, use -IdentityType to chose which field to search on.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "Identifier of AD Object.")]
        public string Identity { get; set; }

        /// <summary>
        /// <para type="description">Which property to search for the identifier. (Accepts tab completion)</para>
        /// </summary>
        [Parameter(
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public IdentityType IdentityType { get; set; } = IdentityType.SamAccountName;

        /// <summary>
        /// <para type="description">Entry point of command.</para>
        /// </summary>
        protected override void ProcessRecord()
        {
            this.WriteVerbose($"Searching for a UserPrincipal using the {this.IdentityType}: {this.Identity}");
            this.WriteObject(((UserPrincipal)Principal.FindByIdentity(new PrincipalContext(ContextType.Domain), this.IdentityType, this.Identity)).GetUserNames());
        }
    }
}
