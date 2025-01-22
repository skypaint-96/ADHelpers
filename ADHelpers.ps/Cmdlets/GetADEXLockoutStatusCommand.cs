namespace ADHelpers.Cmdlets
{
    using ADHelpers.DataAccess;
    using System.Data;
    using System.DirectoryServices.AccountManagement;
    using System.Management.Automation;

    /// <summary>
    /// <para type="synopsis">Returns an user's last login/failed login/expiry times etc.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "LockoutStatus")]
    [OutputType(typeof(DataTable))]
    public class GetADEXLockoutStatusCommand : Cmdlet //: ADSearcher
    {
        /// <summary>
        /// <para type="description">Identifier of AD Object.</para>
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
            this.WriteVerbose($"Searching for a AuthenticablePrincipal using the {this.IdentityType}: {this.Identity}");
            this.WriteObject(((AuthenticablePrincipal)Principal.FindByIdentity(new PrincipalContext(ContextType.Domain), this.IdentityType, this.Identity)).GetLockoutStatus());
        }
    }
}
