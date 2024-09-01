namespace ADHelpers.Cmdlets
{
    using ADHelpers.DataAccess;
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;
    using System.Management.Automation;

    /// <summary>
    /// <para type="synopsis">Outputs a dictionary of the groups unique to the accounts specified in the identities parameter as well as a "CommonGroups" entry for groups that all objects are in.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsData.Compare, "ObjectMemberships")]
    [OutputType(typeof(Dictionary<string, HashSet<string>>))]
    public class CompareADEXObjectMembershipsCommand : Cmdlet
    {
        /// <summary>
        /// <para type="description">Identifiers of AD Objects to compare between.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "Identifier of AD Object.")]
        public string[] Identites { get; set; }

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
        protected override void EndProcessing()
        {
            this.WriteObject(PrincipalExtentions.CompareGroups(PrincipalExtentions.GetPrincipals(this.Identites, this.IdentityType)));
        }
    }


}
