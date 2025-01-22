namespace ADHelpers.DataAccess
{
    using System.Collections.Generic;
    using System.DirectoryServices.AccountManagement;

    /// <summary>
    /// To allow for comparing .net princpal objects as if they were in AD.
    /// </summary>
    public class ComparablePrincipal : EqualityComparer<Principal>
    {
        /// <summary>
        /// Determines if the two .net principal objects are actualy holding the same AD object.
        /// </summary>
        /// <param name="x">First principal to compare.</param>
        /// <param name="y">Second principal to compare.</param>
        /// <returns>True if both hold the same DistinguishedName.</returns>
        public override bool Equals(Principal x, Principal y)
        {
            bool result = false;
            if (x is null || y is null)
            {
                result = true;
            }
            else if (x.DistinguishedName == y.DistinguishedName)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Get's an identifier for the given principal object this method will return the same value for 2 objects with the same distinguishedname.
        /// </summary>
        /// <param name="obj">Object to get a hashcode from.</param>
        /// <returns>The hashcode of the inner distinguishedname of the given principal.</returns>
        public override int GetHashCode(Principal obj)
        {
            int result = 0;
            if (!(obj is null))
            {
                result = obj.DistinguishedName.GetHashCode();
            }

            return result;
        }
    }
}
