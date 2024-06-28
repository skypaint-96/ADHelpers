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
    [Cmdlet(VerbsData.Update, "ADSIUserNames")]
    [OutputType(typeof(IEnumerable<UserNamesSet>))]
    public class UpdateADSIUserNamesCommand : ADSearcher
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
        /// <summary>
        /// <para type="description">New firstname.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "New firstname")]
        public string Givenname { get; set; }
        /// <summary>
        /// <para type="description">New lastname.</para>
        /// </summary>
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false,
            HelpMessage = "New lastname.")]
        public string Surname { get; set; }

        /// <summary>
        /// Entry point of command.
        /// </summary>
        protected override void EndProcessing()
        {
            string collisions = "";
            DirectorySearcher searcher = new DirectorySearcher();
            _ = searcher.PropertiesToLoad.Add("samaccountname");
            WriteVerbose("checking for collisions...");
            searcher.Filter = $"(&(objectclass=user)(userprincipalname={Givenname}.{Surname}@*))";
            SearchResult sr = searcher.FindOne();
            collisions += sr != null ? "upn already exists with these names;" : "";
            WriteVerbose("current collisions:" + collisions);
            searcher.Filter = $"(&(objectclass=user)(targetaddress=SMTP:{Givenname}.{Surname}@*))";
            sr = searcher.FindOne();
            collisions += sr != null ? "targetaddress already exists with these names;" : "";
            WriteVerbose("current collisions:" + collisions);
            searcher.Filter = $"(&(objectclass=user)(givenname={Givenname})(surname={Surname}))";
            sr = searcher.FindOne();
            collisions += sr != null ? "an user already exists with these names;" : "";
            WriteVerbose("current collisions:" + collisions);
            searcher.Filter = $"(&(objectclass=user)(mail={Givenname}.{Surname}@*))";
            sr = searcher.FindOne();
            collisions += sr != null ? "user with this mail address already exists with these names;" : "";
            WriteVerbose("current collisions:" + collisions);
            searcher.Filter = $"(&(objectclass=user)(mailnickname={Givenname}.{Surname}))";
            sr = searcher.FindOne();
            collisions += sr != null ? "user with this mailnickname already exists with these names;" : "";
            WriteVerbose("current collisions:" + collisions);
            if (collisions.Length > 0)
            {
                throw new Exception(collisions);
            }

            searcher.Filter = $"(&(objectclass=user)({SearchProperty}={Identity}))";
            sr = searcher.FindOne();
            searcher.Dispose();
            if (sr != null)
            {
                ResultPropertyCollection r = sr.Properties;
                string fqdn = ((string)r["userprincipalname"][0]).Split('@')[1];
                WriteVerbose($"fqdn found is:{fqdn}");
                List<string> proxyAddresses = new List<string>();
                if (r.Contains("proxyaddresses"))
                {
                    WriteVerbose("found proxyaddresses, creating local copy of new addresses");
                    for (int i = 0; i < r["Proxyaddresses"].Count; i++)
                    {
                        string pa = (string)r["Proxyaddresses"][i];
                        string[] paParts = pa.Split('@');
                        proxyAddresses.Add(Environment.NewLine + (pa.Contains("@") ? $"{paParts[0].Split(':')[0]}:{Givenname}.{Surname}@{paParts[1]}" : pa));
                    }
                }
                else
                {
                    WriteVerbose("no proxyaddresses found to be replaced");
                    proxyAddresses = null;
                }

                if (r.Contains("targetaddress"))
                {
                    WriteVerbose("found target address");
                    string ta = (string)r["targetaddress"][0];
                    string[] taParts = ta.Split('@');
                    proxyAddresses.Add($"{taParts[0].Split(':')[0]}:{Givenname}.{Surname}@{taParts[1]}");
                }
                else
                {
                    WriteVerbose("no targetaddressfound");
                }
            }
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
