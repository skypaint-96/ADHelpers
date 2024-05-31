namespace ADHelpers
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Management.Automation;

    /// <summary>
    /// <para type="synopsis">Used to check what groups are specific to a certain AD Object compared to another.</para>
    /// <example>
    /// <para>Getting groups for two users via their userprincipalname where JSmith is in 2 groups that ARimmer is not.</para>
    /// <code>
    /// 
    /// > $o = Get-UniqueGroups -Identities JSmith@corpo.net, ARimmer@corpo.net -SearchProperty userprincipalname -ADClass User
    /// > $o['JSmith']
    /// CN=Group1ThatOnlyJSmithIsIn,OU=Groups,DC=Corpo,DC=Net
    /// CN=Group2ThatOnlyJSmithIsIn,OU=Groups,DC=Corpo,DC=Net
    /// > $o['ARimmer']
    /// > $o['CommonGroups']
    /// CN=GroupThatBothUsersAreIn,OU=Groups,DC=Corpo,DC=Net
    /// </code>
    /// </example>
    /// <example>
    /// <para>Example 2: Getting groups for two Computers via their canonicalname.</para>
    /// <code>
    /// > $o = Get-UniqueGroups -Identities Workstation52, Laptop1 -SearchProperty cn -ADClass Computer</code>
    /// > $o['Laptop1']
    /// CN=SCLaptopSpecificSoftware,OU=Groups,DC=Corpo,DC=Net
    /// > $o['Workstation52']
    /// CN=SCWorkstationSpecificSoftware,OU=Groups,DC=Corpo,DC=Net
    /// > $o['CommonGroups']
    /// </example>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "UniqueGroups")]
    [OutputType(typeof(Dictionary<string, HashSet<string>>))]
    public class GetUniqueGroupsCommand : PSCmdlet
    {
        /// <summary>
        /// <para type="description">Identifier of AD Object. (accepts '*'s)</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "Identifier of AD Object. (accepts '*'s)")]
        public string[] Identites { get; set; }

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

        /// <summary>
        /// <para type="description">Entry point of command.</para>
        /// </summary>
        protected override void EndProcessing()
        {
            Dictionary<string, HashSet<string>> output = new Dictionary<string, HashSet<string>>();
            HashSet<string> CommonGroups = new HashSet<string>();

            DirectorySearcher s = new DirectorySearcher();
            output.Add(Identites[0], GetUserGroups(Identites[0], s));
            CommonGroups.UnionWith(output[Identites[0]]);
            for (int i = 1; i < Identites.Length; i++)
            {
                output.Add(Identites[i], GetUserGroups(Identites[i], s));
                CommonGroups.IntersectWith(output[Identites[i]]);
            }

            foreach (KeyValuePair<string, HashSet<string>> userGroup in output)
            {
                output[userGroup.Key].ExceptWith(CommonGroups);
            }

            output.Add("CommonGroups", CommonGroups);
            WriteObject(output);
        }

        private HashSet<string> GetUserGroups(string user, DirectorySearcher s)
        {
            HashSet<string> userGroups = new HashSet<string>();
            s.Filter = $"(&(objectclass={ADClass})({SearchProperty}={user}))";
            s.PropertiesToLoad.Clear();
            _ = s.PropertiesToLoad.Add("memberof");
            SearchResult sr = s.FindOne();
            if (sr != null)
            {
                ResultPropertyCollection r = sr.Properties;
                if (r.Contains("memberof"))
                {
                    for (int i = 0; i < r["memberof"].Count; i++)
                    {
                        _ = userGroups.Add((string)r["memberof"][i]);
                    }
                }
            }

            return userGroups;
        }
    }


}
