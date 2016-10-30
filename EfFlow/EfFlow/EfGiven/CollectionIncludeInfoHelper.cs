namespace EFFlow.EFGiven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The collection include info helper.
    /// </summary>
    public static class CollectionIncludeInfoHelper
    {
        /// <summary>
        /// The get hierarchy property names.
        /// </summary>
        /// <param name="collectionIncludes">
        /// The collection includes.
        /// </param>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{String}"/>.
        /// </returns>
        public static List<string> GetHierarchyPropertyNames(this List<CollectionIncludeInfo> collectionIncludes)
        {
            return collectionIncludes
                .Where(collectionInclude => collectionInclude.HierarchyProperty != null)
                .Select(collectionInclude => collectionInclude.GetHierarchyPropertyName())
                .ToList();
        }

        /// <summary>
        /// The get row key level.
        /// </summary>
        /// <param name="rowKey">
        /// The row key.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int GetRowKeyLevel(string[] rowKey)
        {
            return rowKey.Length - rowKey.Count(string.IsNullOrEmpty);
        }

        /// <summary>
        /// The find match.
        /// </summary>
        /// <param name="collectionIncludes">
        /// The collection includes.
        /// </param>
        /// <param name="rowKey">
        /// The row key.
        /// </param>
        /// <returns>
        /// The <see cref="HierarchyIdentifier"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public static HierarchyIdentifier FindMatch(this List<CollectionIncludeInfo> collectionIncludes, string[] rowKey)
        {
            if (rowKey.Length == 0)
            {
                throw new Exception();
            }

            foreach (var collectionInclude in collectionIncludes)
            {
                if (collectionInclude.GetHierarchyLevel() == rowKey.Count(x => !string.IsNullOrEmpty(x)))
                {
                    return new HierarchyIdentifier()
                    {
                        Identifier = collectionInclude.BuildIdentifier(rowKey),
                        Include = collectionInclude,
                    };
                }
            }

            throw new Exception();
        }

        /// <summary>
        /// The get hierarchy properties.
        /// </summary>
        /// <param name="collectionIncludes">
        /// The collection includes.
        /// </param>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{Object}"/>.
        /// </returns>
        public static List<dynamic> GetHierarchyProperties(this List<CollectionIncludeInfo> collectionIncludes)
        {
            return collectionIncludes.Where(x => x.HierarchyProperty != null).Select(x => x.HierarchyProperty).ToList();
        }
    }
}
