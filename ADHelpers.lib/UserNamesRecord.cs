namespace ADHelpers.DataAccess
{
    using System.DirectoryServices.AccountManagement;
    using System.Collections.Generic;
    using System.Linq;
    using System;

    /// <summary>
    /// 
    /// </summary>
    public class UserNamesRecord
    {
        /// <summary>
        /// Basic constructor for creating a UserNamesSet object.
        /// </summary>
        internal UserNamesRecord(UserPrincipal userPrincipal)
        {
            Dictionary<string, Stack<object>> dictionary = userPrincipal.ExtentionGet(new string[]{"targetAddress", "mailNickname", "proxyaddresses" });
            this.SamAccountName = userPrincipal.SamAccountName;
            this.UserPrincipalName = userPrincipal.UserPrincipalName;
            this.TargetAddress = (string)dictionary["targetAddress"].FirstOrDefault();
            this.Name = userPrincipal.Name;
            this.DisplayName = userPrincipal.DisplayName;
            this.EmailAddress = userPrincipal.EmailAddress;
            this.MailNickname = (string)dictionary["mailNickname"].FirstOrDefault();
            foreach (string proxyAddress in dictionary["proxyaddresses"].Cast<string>())
            {
                this.ProxyAddresses += proxyAddress + Environment.NewLine;
            }
            
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
