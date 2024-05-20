﻿namespace ADHelpers
{
    using System;
    using System.Collections.Generic;
    using System.Management.Automation;
    using System.Management.Automation.Runspaces;
    using System.DirectoryServices;
    using System.DirectoryServices.ActiveDirectory;

    [Cmdlet(VerbsCommon.Get, "LockoutStatus")]
    [OutputType(typeof(IEnumerable<LockoutSet>))]
    public class GetLockoutStatusCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string Identity { get; set; }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            //DirectorySearcher searcher = new DirectorySearcher();
            Domain domain = Domain.GetCurrentDomain();
            foreach (DomainController dc in domain.FindAllDiscoverableDomainControllers())
            {
                DirectorySearcher searcher = dc.GetDirectorySearcher();
                searcher.Filter = $"(cn={Identity})";
                SearchResult sr = searcher.FindOne();
                sr.GetDirectoryEntry();
            
            }
            
            
        }

    }

    public class LockoutSet
    {
        public String Server { get; }
        public String UserPrincipalName { get; }
        public int BadLogonCount { get; }
        public int BadPwdCound { get; }
        public DateTime LastBadPasswordAttempt { get; }
        public DateTime LastLogonDate { get; }
        public bool Enabled { get; }
        public bool LockedOut { get; }
        public DateTime LockoutTime { get; }
        public bool PasswordExpired { get; }
        public DateTime PasswordLastSet { get; }
    }
}
