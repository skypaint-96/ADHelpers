namespace ADHelpers
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Management.Automation;
    using System.Runtime.InteropServices;
    using System.Security;

    /// <summary>
    /// <para type="synopsis">Outputs a dictionary of the groups unique to the accounts specified in the identities parameter as well as a "CommonGroups" entry for groups that all objects are in.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsData.Update, "ADSIUserPassword")]
    [OutputType(typeof(Dictionary<string, HashSet<string>>))]
    public class UpdateADSIUserPasswordCommand : ADSearcher
    {
        /// <summary>
        /// <para type="description">Identifier of AD Object, use -SearchProperty to chose which field to search on. (accepts '*'s)</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "Identifier of AD Object. (accepts '*'s)")]
        public string Identity { get; }

        /// <summary>
        /// <para type="description">New password for the specified AD object.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "New password for the specified AD object.")]
        public SecureString NewPassword { get; }

        /// <summary>
        /// <para type="description">Specifies if the user will need to set their password at next logon.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "Specifies if the user will need to set their password at next logon.")]
        public bool ForceChange { get; }

        /// <summary>
        /// <para type="description">Entry point of command.</para>
        /// </summary>
        protected override void EndProcessing()
        {
            DirectorySearcher searcher = new DirectorySearcher();
            searcher.PropertiesToLoad.Clear();
            _ = searcher.PropertiesToLoad.Add("samaccountname");
            searcher.Filter = $"(&(objectclass={this.ADClass})({this.SearchProperty}={this.Identity}))";
            SearchResult sr = searcher.FindOne();
            string plaintextPassword = Marshal.PtrToStringUni(Marshal.SecureStringToGlobalAllocUnicode(this.NewPassword));
            DirectoryEntry r = sr.GetDirectoryEntry();
            r.Invoke("SetPassword", plaintextPassword);
            if (ForceChange)
            {
                r.Properties["pwdlastset"].Value = 0;
            }
        }
    }


}
