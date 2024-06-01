namespace ADHelpers
{
    /// <summary>
    /// Used to allow quick selection of field search properties.
    /// </summary>
    public enum SearchType
    {
        /// <summary>
        /// Search by samaccountname.
        /// </summary>
        SamAccountName,
        /// <summary>
        /// Search by userprincipalname.
        /// </summary>
        UserPrincipalName,
        /// <summary>
        /// Search by the user's canonicalname.
        /// </summary>
        CN
    }
}
