namespace EFFlow.EfThen
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using EFFlow.EFGiven;

    using TechTalk.SpecFlow;

    /// <summary>
    /// The Entity Framework Then clause.
    /// </summary>
    /// <typeparam name="T">
    /// Entity's type.
    /// </typeparam>
    public class EfThen<T> where T : class
    {
        #region < Fields >

        /// <summary>
        /// The database context.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
        private DbContext dbContext;

        /// <summary>
        /// The lookup properties.
        /// </summary>
        private List<LookupProperty> lookupProperties = new List<LookupProperty>();

        /// <summary>
        /// The includes.
        /// </summary>
        private List<Expression<Func<T, object>>> includes = new List<Expression<Func<T, object>>>();

        /// <summary>
        /// The collection includes.
        /// </summary>
        private List<CollectionIncludeInfo> collectionIncludes = new List<CollectionIncludeInfo>();

        #endregion

        #region < Constructors >

        /// <summary>
        /// Initializes a new instance of the <see cref="EfThen{T}"/> class.
        /// </summary>
        /// <param name="dbContext">
        /// The database context.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation", Justification = "Reviewed. Suppression is OK here.")]
        public EfThen(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        #endregion

        #region < Methods >

        /// <summary>
        /// Gets the property info of a given entity..
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="propertyLambda">
        /// The property lambda.
        /// </param>
        /// <returns>
        /// The <see cref="PropertyInfo"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The exception.
        /// </exception>
        public static PropertyInfo GetPropertyInfo(T source, LambdaExpression propertyLambda)
        {
            var type = source.GetType();

            var member = propertyLambda.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a method, not a property.");
            }

            var propInfo = member.Member as PropertyInfo;

            if (propInfo == null)
            {
                throw new ArgumentException($"Expression '{propertyLambda}' refers to a field, not a property.");
            }

            if (type != propInfo.ReflectedType && !type.IsSubclassOf(propInfo.ReflectedType))
            {
                throw new ArgumentException($"Expresion '{propertyLambda}' refers to a property that is not from type {type}.");
            }

            return propInfo;
        }

        /// <summary>
        /// The collection include.
        /// </summary>
        /// <param name="include">
        /// The include.
        /// </param>
        /// <param name="columnPrefix">
        /// The column prefix.
        /// </param>
        /// <param name="hierarchyProperty">
        /// The hierarchy property.
        /// </param>
        /// <param name="cellDelegate">
        /// The cell delegate.
        /// </param>
        /// <typeparam name="T1">
        /// First entity's type.
        /// </typeparam>
        /// <typeparam name="T2">
        /// Second entity's type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="EfThen"/>.
        /// </returns>
        public EfThen<T> CollectionInclude<T1, T2>(
            Expression<Func<T, ICollection<T1>>> include,
            string columnPrefix,
            Expression<Func<T1, T2>> hierarchyProperty,
            Action<T, string, string, T2> cellDelegate)
        {
            this.collectionIncludes.Add(
                new CollectionIncludeInfo
                    {
                        ColumnPrefix = columnPrefix,
                        IncludeEntityCellDelegate = cellDelegate,
                        Include = include,
                        HierarchyProperty = hierarchyProperty,
                    });
            return this;
        }

        /// <summary>
        /// The collection include.
        /// </summary>
        /// <param name="include">
        /// The include.
        /// </param>
        /// <param name="columnPrefix">
        /// The column prefix.
        /// </param>
        /// <param name="cellDelegate">
        /// The cell delegate.
        /// </param>
        /// <typeparam name="T1">
        /// Entity's type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="EfThen"/>.
        /// </returns>
        public EfThen<T> CollectionInclude<T1>(
            Expression<Func<T, ICollection<T1>>> include,
            string columnPrefix,
            Action<T, string, string> cellDelegate)
        {
            this.collectionIncludes.Add(
                new CollectionIncludeInfo
                    {
                        ColumnPrefix = columnPrefix,
                        IncludeEntityCellDelegate = cellDelegate,
                        Include = include,
                        HierarchyProperty = null,
                    });

            return this;
        }

        /// <summary>
        /// The include.
        /// </summary>
        /// <param name="included">
        /// The included.
        /// </param>
        /// <returns>
        /// The <see cref="EfThen"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public EfThen<T> Include(Expression<Func<T, object>> included)
        {
            // Verify the properties are not in lists objects, otherwise an exception should be thrown.
            var propertyInfo = (PropertyInfo)((MemberExpression)included.Body).Member;

            if (typeof(ICollection<>).IsAssignableFrom(propertyInfo.PropertyType))
            {
                throw new Exception("Exception: a list type was found in the properties of the list of includes");
            }

            this.includes.Add(included);

            return this;
        }

        /// <summary>
        /// The lookup property method.
        /// </summary>
        /// <param name="lookupProperty">
        /// The lookup property.
        /// </param>
        /// <typeparam name="T2">
        /// Entity's type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="EfThen"/>.
        /// </returns>
        public EfThen<T> LookupProperty<T2>(Expression<Func<T, T2>> lookupProperty)
        {
            this.lookupProperties.Add(new LookupProperty { LookupExpression = lookupProperty, DefaultValue = null, });

            return this;
        }

        /// <summary>
        /// The lookup property method.
        /// </summary>
        /// <param name="lookupProperty">
        /// The lookup property.
        /// </param>
        /// <param name="defaultValue">
        /// The default value.
        /// </param>
        /// <typeparam name="T2">
        /// Entity's type.
        /// </typeparam>
        /// <returns>
        /// The <see cref="EfThen"/>.
        /// </returns>
        public EfThen<T> LookupProperty<T2>(Expression<Func<T, T2>> lookupProperty, T2 defaultValue = null) where T2 : class
        {
            this.lookupProperties.Add(
                new LookupProperty { LookupExpression = lookupProperty, DefaultValue = defaultValue, });

            return this;
        }

        /// <summary>
        /// Compares the created entities to the entities in the given Table.
        /// </summary>
        /// <param name="table">
        /// The table.
        /// </param>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{T}"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public List<T> Execute(Table table)
        {
            if (this.lookupProperties == null || !this.lookupProperties.Any())
            {
                throw new Exception("No lookup properties provided for Then clause.");
            }

            var parsedTree = new TreeParser(table, this.collectionIncludes.GetHierarchyPropertyNames()).Parse();
            var propertiesToCheck = this.GeneratePropertiesToCheck(table.Header);
            var foundEntities = new List<T>();
            var entityDictionary = new Dictionary<int, object>();

            for (var i = 0; i < table.RowCount; i++)
            {
                if (parsedTree[i].HierarchyLevel != 0)
                {
                    continue;
                }

                var entity = (T)Helpers.CreateInstance(table, table.Rows[i], typeof(T));

                // Set Default values
                foreach (var lookupProperty in this.lookupProperties)
                {
                    if (lookupProperty.DefaultValue != null)
                    {
                        ClausesHelper.SetPropertiesValues(
                            entity,
                            new Tuple<object, object>(lookupProperty.LookupExpression, lookupProperty.DefaultValue),
                            false);
                    }
                }

                var lookupExpression = this.BuildLookupExpression(entity);
                IQueryable<T> query = this.dbContext.Set<T>();
                query = this.AddIncludes(query);
                var foundEntity = query.FirstOrDefault(lookupExpression);

                if (foundEntity == null)
                {
                    throw new Exception("Entity not found at row :" + i);
                }

                this.CheckEntity(entity, foundEntity, propertiesToCheck);

                foundEntities.Add(foundEntity);
                entityDictionary.Add(i, foundEntity);

                this.CheckIncludedCollections1(foundEntity, table, table.Rows[i]);
            }

            // Resto de la jerarquia + collectionincludes
            var maxHierarchyLevel = parsedTree.Max(x => x.Value.HierarchyLevel);

            for (var hierarchyLevel = 1; hierarchyLevel <= maxHierarchyLevel; hierarchyLevel++)
            {
                var indexes = parsedTree.Where(x => x.Value.HierarchyLevel == hierarchyLevel).Select(x => x.Key).ToList();

                for (var i = 0; i < table.RowCount; i++)
                {
                    if (!indexes.Contains(i))
                    {
                        continue;
                    }

                    var rowInfo = parsedTree[i];

                    // Look for the parent entity
                    var parentRowInfo = rowInfo.Parent;
                    var parentIndex = parsedTree.Single(x => x.Value == parentRowInfo).Key;
                    var parentEntity = entityDictionary[parentIndex];

                    this.CheckIncludedCollections2((T)parentEntity, table.Rows[i], table.Header, rowInfo.RowKey);
                }
            }

            return foundEntities;
        }

        /// <summary>
        /// The get entity property names.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{String}"/>.
        /// </returns>
        private static List<string> GetEntityPropertyNames(Type type)
        {
            var properties = type.GetProperties().ToList();

            return properties.Select(property => property.Name).ToList();
        }

        /// <summary>
        /// Build the expression from the lookup properties values.
        /// </summary>
        /// <param name="lookupPropertiesValues">
        /// The lookup properties values.
        /// </param>
        /// <returns>
        /// The <see cref="Expression"/>.
        /// </returns>
        private static Expression<Func<T, bool>> BuildLookupExpression(Dictionary<string, object> lookupPropertiesValues)
        {
            var pe = Expression.Parameter(typeof(T), "x");

            var left = (from lookupPropertyValue in lookupPropertiesValues
                        let property = Expression.Property(pe, lookupPropertyValue.Key)
                        let constant = Expression.Constant(lookupPropertyValue.Value)
                        select Expression.Equal(property, Expression.Convert(constant, property.Type)))
                        .Aggregate<Expression, Expression>(null, (current, equality) => current == null ? equality : Expression.AndAlso(current, equality));

            return Expression.Lambda<Func<T, bool>>(left, pe);
        }

        /// <summary>
        /// The add includes.
        /// </summary>
        /// <param name="query">
        /// The query.
        /// </param>
        /// <returns>
        /// The <see cref="IQueryable"/>.
        /// </returns>
        private IQueryable<T> AddIncludes(IQueryable<T> query)
        {
            var updatedQuery = this.includes.Aggregate(query, (current, include) => current.Include(include));

            return updatedQuery;
        }

        /// <summary>
        /// The check included collections 1.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="table">
        /// The table.
        /// </param>
        /// <param name="tableRow">
        /// The table row.
        /// </param>
        private void CheckIncludedCollections1(T entity, Table table, TableRow tableRow)
        {
            foreach (var collectionInclude in this.collectionIncludes.Where(x => x.HierarchyProperty == null))
            {
                for (var j = 0; j < table.Header.Count; j++)
                {
                    var header = table.Header.ElementAt(j);

                    if (header.StartsWith(collectionInclude.ColumnPrefix))
                    {
                        var cellValue = tableRow[j];

                        collectionInclude.IncludeEntityCellDelegate(entity, header, cellValue);
                    }
                }
            }
        }

        /// <summary>
        /// The check included collections 2.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="tableRow">
        /// The table row.
        /// </param>
        /// <param name="headers">
        /// The headers.
        /// </param>
        /// <param name="rowKey">
        /// The row key.
        /// </param>
        private void CheckIncludedCollections2(T entity, TableRow tableRow, ICollection<string> headers, string[] rowKey)
        {
            var hierarchyIdentifier = this.collectionIncludes.FindMatch(rowKey);

            for (var j = 0; j < headers.Count; j++)
            {
                var header = headers.ElementAt(j);

                if (header.StartsWith(hierarchyIdentifier.Include.ColumnPrefix))
                {
                    var cellValue = tableRow[j];
                    var parameter = hierarchyIdentifier.Include.BuildFunctionParameter(hierarchyIdentifier.Identifier);

                    hierarchyIdentifier.Include.IncludeEntityCellDelegate(entity, header, cellValue, parameter);
                }
            }
        }

        /// <summary>
        /// Build a list of properties to check from the table.
        /// </summary>
        /// <param name="headers">
        /// The headers.
        /// </param>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{LambdaExpression}"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        private List<LambdaExpression> GeneratePropertiesToCheck(ICollection<string> headers)
        {
            var listExpressions = new List<LambdaExpression>();

            foreach (var header in headers)
            {
                if (this.collectionIncludes.Any(x => header.StartsWith(x.ColumnPrefix)))
                {
                    continue;
                }

                if (this.collectionIncludes.Any(x => x.GetHierarchyPropertyName() == header))
                {
                    continue;
                }

                if (GetEntityPropertyNames(typeof(T)).Contains(header))
                {
                    var pe = Expression.Parameter(typeof(T), "x");
                    var property = Expression.Property(pe, header);
                    var expression = Expression.Lambda(property, pe);

                    listExpressions.Add(expression);
                }
                else
                {
                    throw new Exception("Not supported yet");
                }
            }

            return listExpressions;
        }

        /// <summary>
        /// Build the expression from the lookup properties, if the property info is null, an exception is thrown.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <returns>
        /// The <see cref="Expression"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        private Expression<Func<T, bool>> BuildLookupExpression(T entity)
        {
            var lookupPropertiesValues = new Dictionary<string, object>();

            foreach (var lookupProperty in this.lookupProperties)
            {
                var propertyInfo = GetPropertyInfo(entity, lookupProperty.LookupExpression);

                if (propertyInfo == null)
                {
                    throw new Exception("Invalid lookup property");
                }

                lookupPropertiesValues.Add(propertyInfo.Name, propertyInfo.GetValue(entity));
            }

            return BuildLookupExpression(lookupPropertiesValues);
        }

        /// <summary>
        /// Check if all the properties of the given entities are the same.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="entityToCheck">
        /// The entity to check.
        /// </param>
        /// <param name="propertiesToCheck">
        /// The properties to check.
        /// </param>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        private void CheckEntity(T entity, T entityToCheck, List<LambdaExpression> propertiesToCheck)
        {
            if (propertiesToCheck
                .Cast<dynamic>()
                .Select(propertyToCheck => propertyToCheck.Compile())
                .Any(func => func(entity) != func(entityToCheck)))
            {
                throw new Exception("Invalid property value");
            }
        }

        #endregion
    }
}
