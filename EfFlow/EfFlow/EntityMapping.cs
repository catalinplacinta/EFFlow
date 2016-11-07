namespace EFFlow
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     The entity mapping.
    /// </summary>
    public class EntityMapping
    {
        /// <summary>
        ///     Gets or sets the entity type.
        /// </summary>
        public Type EntityType { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether has identity.
        /// </summary>
        public bool HasIdentity { get; set; }

        /// <summary>
        ///     Gets or sets the list of primary keys.
        /// </summary>
        public List<string> Keys { get; set; }

        /// <summary>
        ///     Gets or sets the properties mapping (Key = Entity property name; Value = DB column name).
        /// </summary>
        public Dictionary<string, string> Properties { get; set; }

        /// <summary>
        ///     Gets or sets the name of the DB table.
        /// </summary>
        public string TableName { get; set; }
    }
}