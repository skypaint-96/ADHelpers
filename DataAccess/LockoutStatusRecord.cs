namespace ADHelpers.DataAccess
{
    using System;
    using System.DirectoryServices.AccountManagement;

    /// <summary>
    /// Container for the general lockout information of a authenticablePrincipal to be used/displayed.
    /// </summary>
    public class LockoutStatusRecord
    {
        /// <summary>
        /// Unique Identifier of the authenticablePrincipal this record is based off of.
        /// </summary>
        public string DistinguishedName { get; }

        /// <summary>
        /// Unique Identifier of the authenticablePrincipal this record is based off of.
        /// </summary>
        public string SamAccountName { get; }

        /// <summary>
        /// Number of failed logon attempts.
        /// </summary>
        public int BadLogonCount { get; }

        /// <summary>
        /// Time of last failed logon attempt.
        /// </summary>
        public DateTime LastBadPasswordAttempt { get; }

        /// <summary>
        /// Time of last successful logon.
        /// </summary>
        public DateTime LastLogon { get; }

        /// <summary>
        /// Whether or nothe the related authenticablePrincipal is enabled in the domain.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Time that the authenticablePrincipal will expire.
        /// </summary>
        public DateTime AccountExpirationDate { get; }

        /// <summary>
        /// Time that the authenticablePrincipal was locked out.
        /// </summary>
        public DateTime AccountLockoutTime { get; }

        /// <summary>
        /// Time the authenticablePrincipal's password expires
        /// </summary>
        public DateTime PwdExpires { get; }

        /// <summary>
        /// Time the authenticablePrincipal's password was last set.
        /// </summary>
        public DateTime LastPasswordSet { get; }

        internal LockoutStatusRecord(AuthenticablePrincipal authenticablePrincipal)
        {
            this.DistinguishedName = authenticablePrincipal.DistinguishedName;
            this.SamAccountName = authenticablePrincipal.SamAccountName;
            this.BadLogonCount = authenticablePrincipal.BadLogonCount;
            this.LastBadPasswordAttempt = authenticablePrincipal.LastBadPasswordAttempt.GetValueOrDefault();
            this.LastLogon = authenticablePrincipal.LastLogon.GetValueOrDefault();
            this.Enabled = authenticablePrincipal.Enabled.GetValueOrDefault();
            this.AccountExpirationDate = authenticablePrincipal.AccountExpirationDate.GetValueOrDefault();
            this.AccountLockoutTime = authenticablePrincipal.AccountLockoutTime.GetValueOrDefault();
            this.PwdExpires = authenticablePrincipal.GetPasswordExpiry();
            this.LastPasswordSet = authenticablePrincipal.LastPasswordSet.GetValueOrDefault();
        }
    }


}
