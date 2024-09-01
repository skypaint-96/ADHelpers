namespace ADHelpers.DataAccess
{
    using System;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.DirectoryServices.AccountManagement;
    using System.Linq;

    /// <summary>
    /// Extention and helper methods for when using System.DirectoryServices.AccountManagement.Principal.
    /// </summary>
    public static class PrincipalExtentions
    {
        /// <summary>
        /// Takes a list of AD principals and lists the groups common to all, and lists each principal's groups where they are not common to all.
        /// </summary>
        /// <param name="principals">AD principals to compare.</param>
        /// <returns>A Tuple: +Item1 will be each compared principal, and their AD groups that are not common to all those compared. +Item2 will be a set of common AD groups amongst all principals.</returns>
        public static Tuple<Dictionary<Principal, HashSet<Principal>>, HashSet<Principal>> CompareGroups(IEnumerable<Principal> principals)
        {
            HashSet<Principal> commonGroups = GetCommonGroups(principals);

            Dictionary<Principal, HashSet<Principal>> uniqueGroups = new Dictionary<Principal, HashSet<Principal>>();
            foreach (Principal principal in principals)
            {
                HashSet<Principal> principalGroups = new HashSet<Principal>(principal.GetGroups(), new ComparablePrincipal());
                principalGroups.ExceptWith(commonGroups);
                uniqueGroups.Add(principal, principalGroups);
            }

            return new Tuple<Dictionary<Principal, HashSet<Principal>>, HashSet<Principal>>(uniqueGroups, commonGroups);
        }

        /// <summary>
        /// Gets the requested properties from this/the given principal's DirectoryEntry.
        /// </summary>
        /// <param name="principal">This/the principal to check for the data on.</param>
        /// <param name="properties">The names of the properties to look for.</param>
        /// <returns>A dictionary of the requested with property names as keys and returned objects as values.</returns>
        public static Dictionary<string, Stack<object>> ExtentionGet(this Principal principal, string[] properties)
        {
            Dictionary<string, Stack<object>> output = new Dictionary<string, Stack<object>>();
            using (DirectoryEntry up_de = (DirectoryEntry)principal.GetUnderlyingObject())
            {
                using (DirectorySearcher deSearch = new DirectorySearcher(up_de))
                {
                    foreach (string property in properties)
                    {
                        _ = deSearch.PropertiesToLoad.Add(property);
                    }

                    using (SearchResultCollection results = deSearch.FindAll())
                    {

                        foreach (string property in properties)
                        {
                            Stack<object> result = new Stack<object>();
                            foreach (object obj in results[0].Properties[property])
                            {
                                result.Push(obj);
                            }

                            output.Add(property, result);
                        }
                    }
                }
            }

            return output;
        }

        /// <summary>
        /// Gets a valid DateTime from an Int64 assuming FileTimeUTC format.
        /// </summary>
        /// <param name="rawDate">The Int64 to convert to a DateTime.</param>
        /// <returns>The Datetime given or the minimum date if it fails to parse.</returns>
        public static DateTime ParseDateFromLong(long rawDate) => rawDate > DateTime.MaxValue.ToFileTimeUtc() ? default(DateTime) : DateTime.FromFileTimeUtc(rawDate);

        /// <summary>
        /// Retrieves the lockout information of the given/this AuthenticablePrincipal.
        /// </summary>
        /// <param name="authenticablePrincipal">The AuthenticablePrincipal to retrieve lockout information from.</param>
        /// <returns>A LockoutStatusRecord in relation to the given AuthenticablePrincipal.</returns>
        public static LockoutStatusRecord GetLockoutStatus(this AuthenticablePrincipal authenticablePrincipal) => new LockoutStatusRecord(authenticablePrincipal);

        /// <summary>
        /// Retrieves the DateTime of given/this AuthenticablePrincipal's password expiration.
        /// </summary>
        /// <param name="authenticablePrincipal">The AuthenticablePrincipal to retrieve the time of password expiration from.</param>
        /// <returns>A DateTime retrieved from the given AuthenticablePrincipal's "msDS-UserPasswordExpiryTimeComputed" attribute.</returns>
        public static DateTime GetPasswordExpiry(this AuthenticablePrincipal authenticablePrincipal) => ParseDateFromLong((long)authenticablePrincipal.ExtentionGet(new string[] { "msDS-UserPasswordExpiryTimeComputed" }).FirstOrDefault().Value.FirstOrDefault());

        /// <summary>
        /// Retrieves the attributes holding the UserPrincipal's names.
        /// </summary>
        /// <param name="userPrincipal">The UserPrincipal to retrieve the names of.</param>
        /// <returns>A UserNamesRecord in relation to the given UserPrincipal.</returns>
        public static UserNamesRecord GetUserNames(this UserPrincipal userPrincipal) => new UserNamesRecord(userPrincipal);

        /// <summary>
        /// Takes a collection of strings and finds AD Principals that match the identifiers.
        /// </summary>
        /// <param name="identities">The set of Principal's identifiers.</param>
        /// <param name="identityType">Which attribute is the identifiers to be checked against.</param>
        /// <returns>A set of principals found from the given identifiers.</returns>
        public static HashSet<Principal> GetPrincipals(IEnumerable<string> identities, IdentityType identityType = IdentityType.Name)
        {
            HashSet<Principal> result = new HashSet<Principal>();
            foreach (string identity in identities)
            {
                _ = result.Add(Principal.FindByIdentity(new PrincipalContext(ContextType.Domain), identityType, identity));
            }

            return result;
        }

        /// <summary>
        /// Gets this principal's groups that are not common in the collection of other principals..
        /// </summary>
        /// <param name="principal">This principal.</param>
        /// <param name="otherPrincipals">The collection of other principals to check against.</param>
        /// <returns>A set of groups where this principal is not common amongst the given other principals.</returns>
        public static HashSet<Principal> UniqueGroups(this Principal principal, IEnumerable<Principal> otherPrincipals)
        {
            HashSet<Principal> principalGroups = new HashSet<Principal>(principal.GetGroups());
            principalGroups.ExceptWith(GetCommonGroups(otherPrincipals));
            return principalGroups;
        }

        private static HashSet<Principal> GetCommonGroups(IEnumerable<Principal> principals)
        {
            HashSet<Principal> commonGroups = new HashSet<Principal>(principals, new ComparablePrincipal());
            commonGroups.UnionWith(principals.FirstOrDefault().GetGroups());
            foreach (Principal principal in principals)
            {
                commonGroups.IntersectWith(principal.GetGroups());
            }

            return commonGroups;
        }
    }
}
