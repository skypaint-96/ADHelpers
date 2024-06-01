namespace ADHelpers
{
    using System.Management.Automation;

    /// <summary>
    /// Base class of helpercmdlets that search AD.
    /// </summary>
    public abstract class ADSearcher : Cmdlet
    {
        /// <summary>
        /// <para type="description">Which property to search for the identifier. (Accepts tab completion)</para>
        /// </summary>
        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        /// <summary>
        /// <para type="description">Which type of AD Object to search for. (Accepts tab completion)</para>
        /// </summary>
        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public ADObjectClass ADClass { get; set; } = ADObjectClass.User;
    }
}
