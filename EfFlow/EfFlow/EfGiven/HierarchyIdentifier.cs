namespace EfFlow.EfGiven
{
    /// <summary>
    ///     The hierarchy identifier.
    /// </summary>
    public class HierarchyIdentifier
    {
        /// <summary>
        ///     Gets or sets the identifier.
        /// </summary>
        public object[] Identifier { get; set; }

        /// <summary>
        ///     Gets or sets the include.
        /// </summary>
        public CollectionIncludeInfo Include { get; set; }
    }
}