namespace EfFlow.EfGiven
{
    /// <summary>
    ///     The row info.
    /// </summary>
    public class RowInfo
    {
        /// <summary>
        ///     Gets or sets the hierarchy level.
        /// </summary>
        public int HierarchyLevel { get; set; }

        /// <summary>
        ///     Gets or sets the parent.
        /// </summary>
        public RowInfo Parent { get; set; }

        /// <summary>
        ///     Gets or sets the row key.
        /// </summary>
        public string[] RowKey { get; set; }
    }
}