namespace EFFlow
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Core.Metadata.Edm;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Infrastructure;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The data context extensions.
    /// </summary>
    public static class DataContextExtensions
    {
        #region < Properties >

        /// <summary>
        /// The get entity mapping.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="EntityMapping"/>.
        /// </returns>
        public static EntityMapping GetEntityMapping<TEntity>(this DbContext context) where TEntity : class
        {
            var tableName = context.GetTableName<TEntity>();
            var objectContext = ((IObjectContextAdapter)context).ObjectContext;
            var objectSet = objectContext.CreateObjectSet<TEntity>();

            var keys = objectSet.EntitySet.ElementType.KeyMembers.Select(x => x.Name).ToList();
            var hasIdentity = objectSet.EntitySet.ElementType.KeyMembers.Any(x => ((EdmProperty)x).IsIdentity());

            return new EntityMapping
                       {
                           EntityType = typeof(TEntity),
                           TableName = tableName,
                           Keys = keys,
                           Properties = context.GetPropertiesMappings<TEntity>(),
                           HasIdentity = hasIdentity,
                       };
        }

        /// <summary>
        /// The is identity.
        /// </summary>
        /// <param name="edmProperty">
        /// The property.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public static bool IsIdentity(this EdmProperty edmProperty)
        {
            // Note the attribute may be set to Computed even though edmProperty.IsStoreGeneratedComputed == false
            var storeGeneratedPatternAttribute = edmProperty.MetadataProperties.SingleOrDefault(_ => _.Name == "http://schemas.microsoft.com/ado/2009/02/edm/annotation:StoreGeneratedPattern");

            return storeGeneratedPatternAttribute != null
                   && storeGeneratedPatternAttribute.Value.ToString() == "Identity";
        }

        /// <summary>
        /// The get entity properties.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{String}"/>.
        /// </returns>
        public static List<string> GetEntityProperties<TEntity>(this ObjectContext context) where TEntity : class
        {
            var objectSet = context.CreateObjectSet<TEntity>();

            return objectSet.EntitySet.ElementType.Properties.Select(x => x.Name).ToList();
        }

        /// <summary>
        /// The insert with identity insertion.
        /// </summary>
        /// <param name="dbContext">
        /// The database context.
        /// </param>
        /// <param name="entities">
        /// The entities.
        /// </param>
        /// <typeparam name="T">
        /// The type.
        /// </typeparam>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
        public static void InsertWithIdentityInsertion<T>(this DbContext dbContext, List<T> entities) where T : class
        {
            var entityMapping = dbContext.GetEntityMapping<T>();
            var sql = string.Empty;
            if (entityMapping.HasIdentity)
            {
                var identityInsertOnSql = $"SET IDENTITY_INSERT {entityMapping.TableName} ON";
                sql += identityInsertOnSql + ";";
            }

            entities.ToList().ForEach(
                entity =>
                    {
                        var insertSql = dbContext.GenerateInsert(entity);

                        sql += insertSql + ";";
                    });

            if (entityMapping.HasIdentity)
            {
                var identityInsertOffSql = $"SET IDENTITY_INSERT {entityMapping.TableName} OFF";
                sql += identityInsertOffSql + ";";
            }

            dbContext.Database.ExecuteSqlCommand(sql);
        }

        /// <summary>
        /// The SQL representation.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public static string ToSqlRepresentation(this object value)
        {
            if (value == null)
            {
                return "NULL";
            }

            switch (value.GetType().Name)
            {
                case "String":
                    return $"'{value}'";
                case "Int16":
                    return $"{(short)value}";
                case "Int32":
                    return $"{(int)value}";
                case "Int64":
                    return $"{(long)value}";
                case "Double":
                    return $"{(double)value}";
                case "Decimal":
                    return $"{(decimal)value}";
                case "Boolean":
                    return $"{Convert.ToInt32((bool)value)}";
                case "Byte":
                    return $"{(byte)value}";
                case "DateTime":
                    return $"'{(DateTime)value:yyyy-MM-dd HH:mm:ss}'";
                default:
                    throw new Exception($"The type {value.GetType().Name} is not handled by code.");
            }
        }

        /// <summary>
        /// The get table name.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private static string GetTableName<TEntity>(this DbContext context) where TEntity : class
        {
            var sql = context.Set<TEntity>().ToString();
            var regex = new Regex("FROM (?<Table>.*) AS");
            var match = regex.Match(sql);

            return match.Groups["Table"].Value;
        }

        /// <summary>
        /// The get property.
        /// </summary>
        /// <param name="property">
        /// The property.
        /// </param>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        private static dynamic GetProperty(string property, object instance)
        {
            var type = instance.GetType();
            return type.InvokeMember(
                property,
                BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                instance,
                null);
        }

        /// <summary>
        /// The get array list property.
        /// </summary>
        /// <param name="property">
        /// The property.
        /// </param>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <returns>
        /// The <see cref="ArrayList"/>.
        /// </returns>
        private static ArrayList GetArrayListProperty(string property, object instance)
        {
            var type = instance.GetType();
            var objects = (IEnumerable)type.InvokeMember(
                property,
                BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance,
                null,
                instance,
                null);
            var array = new ArrayList();

            foreach (var obj in objects)
            {
                array.Add(obj);
            }

            return array;
        }

        /// <summary>
        /// The get properties mappings.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="System.Collections.Generic.Dictionary{String, String}"/>.
        /// </returns>
        private static Dictionary<string, string> GetPropertiesMappings<TEntity>(this DbContext context)
        {
            var entityType = typeof(TEntity);
            var objectContext = (context as IObjectContextAdapter).ObjectContext;

            var workspace = objectContext.MetadataWorkspace;

            var mappings = new Dictionary<string, string>();
            var storageMapping = workspace.GetItem<GlobalItem>(objectContext.DefaultContainerName, DataSpace.CSSpace);

            dynamic entitySetMaps = storageMapping.GetType()
                .InvokeMember(
                    "EntitySetMaps",
                    BindingFlags.GetProperty | BindingFlags.NonPublic | BindingFlags.Instance,
                    null,
                    storageMapping,
                    null);

            foreach (var entitySetMap in entitySetMaps)
            {
                var typeMappings = GetArrayListProperty("TypeMappings", entitySetMap);
                dynamic typeMapping = typeMappings[0];
                dynamic types = GetArrayListProperty("Types", typeMapping);

                if (types[0].Name == entityType.Name)
                {
                    var fragments = GetArrayListProperty("MappingFragments", typeMapping);
                    var fragment = fragments[0];
                    var properties = GetArrayListProperty("AllProperties", fragment);

                    foreach (var property in properties)
                    {
                        mappings.Add(property.Property.Name, property.Column.Name);
                    }
                }
            }

            return mappings;
        }

        /// <summary>
        /// The generate insert.
        /// </summary>
        /// <param name="dbContext">
        /// The database context.
        /// </param>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <typeparam name="TEntity">
        /// The entity type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
        private static string GenerateInsert<TEntity>(this DbContext dbContext, TEntity entity) where TEntity : class
        {
            var mapping = dbContext.GetEntityMapping<TEntity>();

            var sql = $"INSERT INTO {mapping.TableName}";

            var propertyNames = (dbContext as IObjectContextAdapter).ObjectContext.GetEntityProperties<TEntity>();

            var propertyInfos =
                mapping.EntityType.GetProperties()
                    .Where(p => !p.GetGetMethod().IsVirtual && propertyNames.Contains(p.Name))
                    .ToArray();

            var columnsPart = new List<string>();

            var valuesPart = new List<string>();

            foreach (var propertyName in propertyNames)
            {
                var propertyInfo = propertyInfos.Single(x => x.Name == propertyName);

                columnsPart.Add(mapping.Properties[propertyName]);
                valuesPart.Add(propertyInfo.GetValue(entity, null).ToSqlRepresentation());
            }

            sql += $"({string.Join(",", columnsPart)})";

            sql += $" VALUES ({string.Join(",", valuesPart)});";

            return sql;
        }

        #endregion
    }
}
