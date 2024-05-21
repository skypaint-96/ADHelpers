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
            Dictionary<string, HashSet<string>> output = new Dictionary<string, HashSet<string>>
            {
                { "CommonGroups", new HashSet<string>() }
            };
            WriteDebug("1");

            DirectorySearcher s = new DirectorySearcher();
            WriteDebug("2");
            output.Add(Identites[0], GetUserGroups(Identites[0], s));
            output["CommonGroups"].UnionWith(output[Identites[0]]);
            for (int i = 1; i < Identites.Length; i++)
            {
                WriteDebug("3");
                output.Add(Identites[i], GetUserGroups(Identites[i], s));
                output["CommonGroups"].IntersectWith(output[Identites[i]]);
            }

            foreach (KeyValuePair<string, HashSet<string>> userGroup in output)
            {
                WriteDebug("4");
                output[userGroup.Key].ExceptWith(output["CommonGroups"]);
            }

            WriteObject(output);
        }

        private HashSet<string> GetUserGroups(string user, DirectorySearcher s)
        {
            WriteDebug("5");
            HashSet<string> userGroups = new HashSet<string>();
            s.Filter = $"(&(objectclass={Class})({SearchProperty}={user}))";
            s.PropertiesToLoad.Clear();
            s.PropertiesToLoad.Add("memberof");
            SearchResult sr = s.FindOne();
            WriteDebug("6");
            if (sr != null)
            {
                ResultPropertyCollection r = sr.Properties;
                if (r.Contains("memberof"))
                {
                    for (int i = 0; i < r["memberof"].Count; i++)
                    {
                        userGroups.Add((string)r["memberof"][i]);
                    }
                }
            }

            return userGroups;
        }
    }

    
}
