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

            DirectorySearcher s = new DirectorySearcher();
            for (int i = 0; i < Identites.Length; i++)
            {
                output.Add(Identites[i], GetUserGroups(Identites[i], s));
            }
            
            foreach (KeyValuePair<string, HashSet<string>> userGroup in output)
            {
                output["CommonGroups"].UnionWith(userGroup.Value);
            }

            foreach (KeyValuePair<string, HashSet<string>> userGroup in output)
            {
                output[userGroup.Key].UnionWith(output["CommonGroups"]);
            }
        }

        private HashSet<string> GetUserGroups(string user, DirectorySearcher s)
        {
            HashSet<string> userGroups = new HashSet<string>();
            s.Filter = $"(&(objectclass={Class})({SearchProperty}={user}";
            s.PropertiesToLoad.Clear();
            s.PropertiesToLoad.Add("memberof");
            SearchResult sr = s.FindOne();
            if (sr != null)
            {
                if (sr.Properties.Contains("memberof"))
                {
                    string[] members = ((string)sr.Properties["memberof"][0]).Split('\n');
                    foreach (string member in members)
                    {
                        s.Filter = $"(&(objectclass=group)(distinguishedName={member}";
                        s.PropertiesToLoad.Clear();
                        s.PropertiesToLoad.Add("name");
                        sr = s.FindOne();
                        if (sr != null)
                        {
                            userGroups.Add(member);
                        }
                    }
                }
            }
            
            return userGroups;
        }
    }

    
}
