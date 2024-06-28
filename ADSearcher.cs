namespace ADHelpers
{
    using System.DirectoryServices;
    using System.Management.Automation;
    using System.Reflection;

    /// <summary>
    /// Base class of helpercmdlets that search AD.
    /// </summary>
    public abstract class ADSearcher : Cmdlet
    {
        /// <summary>
        /// <para type="description">Which property to search for the identifier. (Accepts tab completion)</para>
        /// </summary>
        [Parameter(
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public SearchType SearchProperty { get; set; } = SearchType.SamAccountName;

        /// <summary>
        /// <para type="description">Which type of AD Object to search for. (Accepts tab completion)</para>
        /// </summary>
        [Parameter(
            Mandatory = false,
            ValueFromPipeline = false,
            ValueFromPipelineByPropertyName = false)]
        public ADObjectClass ADClass { get; set; } = ADObjectClass.User;

        //public T GetADObjectData<T>(DirectorySearcher searcher, string identity, string[] props)
        //{
        //    PropertyInfo[] pr = typeof(T).GetProperties();
        //    searcher.PropertiesToLoad.Clear();
        //    foreach (var prop in props)
        //    {
        //        _ = searcher.PropertiesToLoad.Add(prop);
        //    }
        //    searcher.Filter = $"(&(objectclass={ADClass})({SearchProperty}={identity}))";
        //    SearchResult sr = searcher.FindOne();
        //    string server = searcher.SearchRoot.Name;
        //    if (sr != null)
        //    {
        //        ResultPropertyCollection r = sr.Properties;
        //        new T();
        //        foreach (var prop in typeof(T).GetProperties())
        //        {
        //            r.Contains(prop) ?
        //        }
        //    }
        //    return ls;
        //}
    }
}
