namespace ADHelpers
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Management.Automation;

    /// <summary>
    /// <para type="synopsis">Used to get the names of an AD user, useful as it shows if there are any missmatches/misspellings.</para>
    /// <para type="link" uri="(https://github.com/skypaint-96/ADHelpers)">[Project Source]</para>
    /// </summary>
    [Cmdlet(VerbsCommon.Get, "ADUserNames")]
    [OutputType(typeof(IEnumerable<UserNamesSet>))]
    public class GetADUserNamesCommand : ADSearcher
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
        public string Identity { get; set; }

        private static UserNamesSet EmptyUsernameSet = new UserNamesSet("null", "null", "null", "null", "null", "null", "null", "null");

        /// <summary>
        /// Entry point of command.
        /// </summary>
        protected override void EndProcessing()
        {
            DirectorySearcher searcher = new DirectorySearcher();
            _ = searcher.PropertiesToLoad.Add("samaccountname");
            _ = searcher.PropertiesToLoad.Add("userprincipalname");
            _ = searcher.PropertiesToLoad.Add("targetaddress");
            _ = searcher.PropertiesToLoad.Add("name");
            _ = searcher.PropertiesToLoad.Add("displayname");
            _ = searcher.PropertiesToLoad.Add("mail");
            _ = searcher.PropertiesToLoad.Add("Mailnickname");
            _ = searcher.PropertiesToLoad.Add("Proxyaddresses");
            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            SearchResult sr = searcher.FindOne();
            searcher.Dispose();
            UserNamesSet uns = EmptyUsernameSet;
            if (sr != null)
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

        /// <summary>
        /// A class to represent the data returned by the GetADUserNamesCommand, should hold all data containing references to the user's actual name.
        /// </summary>
        public class UserNamesSet
        {
            /// <summary>
            /// Basic constructor for creating a UserNamesSet object.
            /// </summary>
            /// <param name="samAccountName">User's samAccountName.</param>
            /// <param name="userPrincipalName">User's userPrincipalName.</param>
            /// <param name="targetAddress">User's targetAddress.</param>
            /// <param name="name">User's name/CN.</param>
            /// <param name="displayName">User's displayName.</param>
            /// <param name="emailAddress">User's Mail property.</param>
            /// <param name="mailNickname">User's mailNickname.</param>
            /// <param name="proxyAddress">User's proxyAddresses.</param>
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

            /// <summary>
            /// User's samAccountName.
            /// </summary>
            public string SamAccountName { get; }
            /// <summary>
            /// User's userPrincipalName.
            /// </summary>
            public string UserPrincipalName { get; }
            /// <summary>
            /// User's targetAddress.
            /// </summary>
            public string TargetAddress { get; }
            /// <summary>
            /// User's name/CN.
            /// </summary>
            public string Name { get; }
            /// <summary>
            /// User's displayName.
            /// </summary>
            public string DisplayName { get; }
            /// <summary>
            /// User's Mail property.
            /// </summary>
            public string EmailAddress { get; }
            /// <summary>
            /// User's mailNickname.
            /// </summary>
            public string MailNickname { get; }
            /// <summary>
            /// User's proxyAddresses, easiest if outputted as whole string, powershell by default tends to only show a section of this.
            /// </summary>
            public string ProxyAddresses { get; set; }
        }
    }
}
