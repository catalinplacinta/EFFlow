namespace EFFlow.EFGiven
{
    using System;
    using System.Linq;
    using System.Reflection;

    using TechTalk.SpecFlow;

    /// <summary>
    /// The helpers.
    /// </summary>
    public static class Helpers
    {
        /// <summary>
        /// The create instance.
        /// </summary>
        /// <param name="table">
        /// The table.
        /// </param>
        /// <param name="tableRow">
        /// The table row.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public static object CreateInstance(Table table, TableRow tableRow, Type type)
        {
            var index = table.Rows.ToList().IndexOf(tableRow);
            var includedEntities = table.CreateSet(type);

            return includedEntities[index];
        }

        /// <summary>
        /// The get default.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public static object GetDefault(Type type)
        {
            if (type == typeof(DateTime))
            {
                return DateTime.Parse("1900-01-01 00:00:00");
            }

            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }

            return null;
        }

        /// <summary>
        /// The replace date time minimum values.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        public static void ReplaceDateTimeMinimumValues(object entity)
        {
            foreach (var propertyInfo in entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                // If not writable then cannot null it; if not readable then cannot check it's value
                if (!propertyInfo.CanWrite || !propertyInfo.CanRead)
                {
                    continue;
                }

                var mset = propertyInfo.GetSetMethod(false);

                // Get and set methods have to be public
                if (mset == null)
                {
                    continue;
                }

                if (propertyInfo.PropertyType == typeof(DateTime))
                {
                    var dateTime = (DateTime)propertyInfo.GetValue(entity);

                    if (dateTime == DateTime.MinValue)
                    {
                        propertyInfo.SetValue(entity, DateTime.Parse("1900-01-01 00:00:00"));
                    }
                }
            }
        }
    }
}
