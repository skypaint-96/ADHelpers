namespace ADHelpers
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Management.Automation;

    /// <summary>
    /// <para type="synopsis">Outputs a dictionary of the groups unique to the accounts specified in the identities parameter as well as a "CommonGroups" entry for groups that all objects are in.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsData.Compare, "ADSIObjectMemberships")]
    [OutputType(typeof(Dictionary<string, HashSet<string>>))]
    public class CompareADSIObjectMembershipsCommand : ADSearcher
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
        public string[] Identites { get; set; }

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
