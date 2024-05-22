namespace ADHelpers
{
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "UniqueGroups")]
    [OutputType(typeof(Dictionary<string, HashSet<string>>))]
    public class GetUniqueGroupsCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string[] Identites { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public ObjectClass Class { get; set; } = ObjectClass.User;


        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
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
            s.Filter = $"(&(objectclass={Class})({SearchProperty}={user}))";
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
