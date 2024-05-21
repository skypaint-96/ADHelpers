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
    using System.Collections;

    [Cmdlet(VerbsCommon.Get, "ADUserNames")]
    [OutputType(typeof(IEnumerable<UserNamesSet>))]
    public class GetADUserNamesCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string Identity { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        static UserNamesSet EmptyUsernameSet = new UserNamesSet("null", "null", "null", "null", "null", "null", "null", "null");

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            DirectorySearcher searcher = new DirectorySearcher();
            searcher.PropertiesToLoad.Add("samaccountname");
            searcher.PropertiesToLoad.Add("userprincipalname");
            searcher.PropertiesToLoad.Add("targetaddress");
            searcher.PropertiesToLoad.Add("name");
            searcher.PropertiesToLoad.Add("displayname");
            searcher.PropertiesToLoad.Add("mail");
            searcher.PropertiesToLoad.Add("Mailnickname");
            searcher.PropertiesToLoad.Add("Proxyaddresses");
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            SearchResult sr = searcher.FindOne();
            searcher.Dispose();
            UserNamesSet uns = EmptyUsernameSet;
            if (sr != null )
            {
                ResultPropertyCollection r = sr.Properties;
                string proxyAddresses = "";
                if (r.Contains("proxyaddresses"))
                {
                    for (int i = 0; i < r["Proxyaddresses"].Count; i++)
                    {
                        proxyAddresses += Environment.NewLine + (string)r["Proxyaddresses"][i];
                    }
                }
                else
                {
                    proxyAddresses = EmptyUsernameSet.ProxyAddresses;
                }

                uns = new UserNamesSet(r.Contains("samaccountname") ? (string)r["samaccountname"][0] : EmptyUsernameSet.SamAccountName,
                    r.Contains("userprincipalname") ? (string)r["userprincipalname"][0] : EmptyUsernameSet.UserPrincipalName,
                    r.Contains("targetaddress") ? (string)r["targetaddress"][0] : EmptyUsernameSet.TargetAddress,
                    r.Contains("name") ? (string)r["name"][0] : EmptyUsernameSet.Name,
                    r.Contains("displayname") ? (string)r["displayname"][0] : EmptyUsernameSet.DisplayName,
                    r.Contains("mail") ? (string)r["mail"][0] : EmptyUsernameSet.EmailAddress,
                    r.Contains("Mailnickname") ? (string)r["Mailnickname"][0] : EmptyUsernameSet.MailNickname,
                    proxyAddresses);
            }

            WriteObject(uns);
            
        }

        public class UserNamesSet
        {
            public UserNamesSet(string samAccountName, string userPrincipalName, string targetAddress, string name, string displayName, string emailAddress, string mailNickname, string proxyAddress)
            {
                SamAccountName = samAccountName;
                UserPrincipalName = userPrincipalName;
                TargetAddress = targetAddress;
                Name = name;
                DisplayName = displayName;
                EmailAddress = emailAddress;
                MailNickname = mailNickname;
                ProxyAddresses = proxyAddress;
            }

            public string SamAccountName { get; }
            public string UserPrincipalName { get; }
            public string TargetAddress { get; }
            public string Name { get; }
            public string DisplayName { get; }
            public string EmailAddress { get; }
            public string MailNickname { get; }
            public string ProxyAddresses { get; set; }
        }
    }
}
