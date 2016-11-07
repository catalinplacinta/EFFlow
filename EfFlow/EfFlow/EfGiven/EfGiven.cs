namespace EfFlow.EfGiven
{
    using System;
    using System.Collections.Generic;
    using System.Data.Entity;
    using System.Data.Entity.Validation;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using EFFlow;

    using TechTalk.SpecFlow;

    /// <summary>
    ///     The EF given.
    /// </summary>
    /// <typeparam name="T">
    ///     Entity's type.
    /// </typeparam>
    public class EfGiven<T>
        where T : class
    {
        /// <summary>
        ///     The calculated value expressions.
        /// </summary>
        private readonly List<Tuple<object, object>> calculatedValueExpressions = new List<Tuple<object, object>>();

        /// <summary>
        ///     The collection includes.
        /// </summary>
        private readonly List<CollectionIncludeInfo> collectionIncludes = new List<CollectionIncludeInfo>();

        /// <summary>
        ///     The database context.
        /// </summary>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
             Justification = "Reviewed. Suppression is OK here.")]
        private readonly DbContext dbContext;

        /// <summary>
        ///     The default value expressions.
        /// </summary>
        private readonly List<Tuple<object, object>> defaultValueExpressions = new List<Tuple<object, object>>();

        /// <summary>
        ///     The fixed value expressions.
        /// </summary>
        private readonly List<Tuple<object, object>> fixedValueExpressions = new List<Tuple<object, object>>();

        /// <summary>
        ///     The identity insertion.
        /// </summary>
        private bool identityInsertion;

        /// <summary>
        ///     The includes.
        /// </summary>
        private readonly List<Expression<Func<T, object>>> includes = new List<Expression<Func<T, object>>>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="EfGiven{T}" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The database context.
        /// </param>
        [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:FieldNamesMustNotUseHungarianNotation",
             Justification = "Reviewed. Suppression is OK here.")]
        public EfGiven(DbContext dbContext)
        {
            this.dbContext = dbContext;
        }

        /// <summary>
        ///     The build entities.
        /// </summary>
        /// <param name="table">
        ///     The table.
        /// </param>
        /// <returns>
        ///     The <see cref="System.Collections.Generic.List{T}" />.
        /// </returns>
        public List<T> BuildEntities(Table table)
        {
            var parsedTree = new TreeParser(table, this.collectionIncludes.GetHierarchyPropertyNames()).Parse();
            var entityDictionary = new Dictionary<int, object>();

            // Entidades de primer nivel
            var entities = new List<T>();

            for (var i = 0; i < table.RowCount; i++)
            {
                if (parsedTree[i].HierarchyLevel != 0)
                {
                    continue;
                }

                var entity = (T)Helpers.CreateInstance(table, table.Rows[i], typeof(T));
                this.CreateIncludedEntities(entity, table, table.Rows[i]);

                Helpers.ReplaceDateTimeMinimumValues(entity);

                // Set Fixed values
                foreach (var fixedValueExpression in this.fixedValueExpressions)
                {
                    ClausesHelper.SetPropertiesValues(entity, fixedValueExpression, true);
                }

                // Set Default values
                foreach (var defaultValueExpression in this.defaultValueExpressions)
                {
                    ClausesHelper.SetPropertiesValues(entity, defaultValueExpression, false);
                }

                // Set calculated values
                foreach (var calculatedValueExpression in this.calculatedValueExpressions)
                {
                    this.SetCalculatedPropertiesValues(entity, calculatedValueExpression);
                }

                this.dbContext.Set<T>().Add(entity);
                this.CreateIncludedCollections1(entity, table.Rows[i], table.Header);

                entities.Add(entity);

                entityDictionary.Add(i, entity);
            }

            // Resto de la jerarquia + collectionincludes
            var maxHierarchyLevel = parsedTree.Max(x => x.Value.HierarchyLevel);

            for (var hierarchyLevel = 1; hierarchyLevel <= maxHierarchyLevel; hierarchyLevel++)
            {
                var indexes =
                    parsedTree.Where(x => x.Value.HierarchyLevel == hierarchyLevel).Select(x => x.Key).ToList();

                for (var i = 0; i < table.RowCount; i++)
                {
                    if (!indexes.Contains(i))
                    {
                        continue;
                    }

                    var rowInfo = parsedTree[i];

                    // Look for the parent and add the entity to the parent
                    var parentRowInfo = rowInfo.Parent;
                    var parentIndex = parsedTree.Single(x => x.Value == parentRowInfo).Key;
                    var parentEntity = entityDictionary[parentIndex];

                    // Add the child entity or included collection
                    this.CreateIncludedCollections2((T)parentEntity, table.Rows[i], table.Header, rowInfo.RowKey);
                }
            }

            return entities;
        }

        /// <summary>
        ///     The calculated value.
        /// </summary>
        /// <param name="propertyExpression">
        ///     The property expression.
        /// </param>
        /// <param name="calculationExpression">
        ///     The calculation expression.
        /// </param>
        /// <typeparam name="T2">
        ///     Entity's type.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        public EfGiven<T> CalculatedValue<T2>(
            Expression<Func<T, T2>> propertyExpression,
            Func<T, T2> calculationExpression)
        {
            this.calculatedValueExpressions.Add(new Tuple<object, object>(propertyExpression, calculationExpression));

            return this;
        }

        /// <summary>
        ///     The collection include.
        /// </summary>
        /// <param name="include">
        ///     The include.
        /// </param>
        /// <param name="columnPrefix">
        ///     The column prefix.
        /// </param>
        /// <param name="entityBuilder">
        ///     The entity builder.
        /// </param>
        /// <typeparam name="T1">
        ///     Entity's type.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        public EfGiven<T> CollectionInclude<T1>(
            Expression<Func<T, ICollection<T1>>> include,
            string columnPrefix,
            Func<T, string, string, T1> entityBuilder)
        {
            this.collectionIncludes.Add(
                new CollectionIncludeInfo
                    {
                        ColumnPrefix = columnPrefix,
                        IncludeEntityCellDelegate = entityBuilder,
                        Include = include,
                        HierarchyProperty = null
                    });

            return this;
        }

        /// <summary>
        ///     The collection include.
        /// </summary>
        /// <param name="include">
        ///     The include.
        /// </param>
        /// <param name="columnPrefix">
        ///     The column prefix.
        /// </param>
        /// <param name="hierarchyProperty">
        ///     The hierarchy property.
        /// </param>
        /// <param name="entityBuilder">
        ///     The entity builder.
        /// </param>
        /// <typeparam name="T1">
        ///     First entity's type.
        /// </typeparam>
        /// <typeparam name="T2">
        ///     Second entity's type.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        public EfGiven<T> CollectionInclude<T1, T2>(
            Expression<Func<T, ICollection<T1>>> include,
            string columnPrefix,
            Expression<Func<T1, T2>> hierarchyProperty,
            Func<T, string, string, T2, T1> entityBuilder)
        {
            this.collectionIncludes.Add(
                new CollectionIncludeInfo
                    {
                        ColumnPrefix = columnPrefix,
                        IncludeEntityCellDelegate = entityBuilder,
                        Include = include,
                        HierarchyProperty = hierarchyProperty
                    });

            return this;
        }

        /// <summary>
        ///     The default value.
        /// </summary>
        /// <param name="propertyExpression">
        ///     The property expression.
        /// </param>
        /// <param name="propertyValue">
        ///     The property value.
        /// </param>
        /// <typeparam name="T2">
        ///     The entity type.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        public EfGiven<T> DefaultValue<T2>(Expression<Func<T, T2>> propertyExpression, T2 propertyValue)
        {
            this.defaultValueExpressions.Add(new Tuple<object, object>(propertyExpression, propertyValue));

            return this;
        }

        /// <summary>
        ///     Create the entities from the given Table, add fixed and default values to the entity and save the entities in the
        ///     database.
        /// </summary>
        /// <param name="table">
        ///     The table.
        /// </param>
        /// <returns>
        ///     The <see cref="System.Collections.Generic.List{T}" />.
        /// </returns>
        public List<T> Execute(Table table)
        {
            var entities = this.BuildEntities(table);

            this.Execute(entities);

            return entities;
        }

        /// <summary>
        ///     The execute.
        /// </summary>
        /// <param name="entities">
        ///     The entities.
        /// </param>
        /// <returns>
        ///     The <see cref="System.Collections.Generic.List{T}" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     The exception.
        /// </exception>
        public List<T> Execute(List<T> entities)
        {
            if (this.identityInsertion)
            {
                // main entity insertion
                this.dbContext.InsertWithIdentityInsertion(entities);

                foreach (var entity in entities)
                {
                    this.dbContext.Set<T>().Attach(entity);
                }
            }
            else
            {
                foreach (var entity in entities)
                {
                    this.dbContext.Set<T>().Add(entity);
                }
            }

            try
            {
                this.dbContext.SaveChanges();
            }
            catch (DbEntityValidationException e)
            {
                var errorMessages = string.Join(
                    "\r\n",
                    e.EntityValidationErrors.SelectMany(x => x.ValidationErrors).Select(x => x.ErrorMessage));

                throw new Exception(errorMessages);
            }

            return entities;
        }

        /// <summary>
        ///     The fixed value.
        /// </summary>
        /// <param name="propertyExpression">
        ///     The property expression.
        /// </param>
        /// <param name="propertyValue">
        ///     The property value.
        /// </param>
        /// <typeparam name="T2">
        ///     Entity's type.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        public EfGiven<T> FixedValue<T2>(Expression<Func<T, T2>> propertyExpression, T2 propertyValue)
        {
            this.fixedValueExpressions.Add(new Tuple<object, object>(propertyExpression, propertyValue));

            return this;
        }

        /// <summary>
        ///     The identity insertion.
        /// </summary>
        /// <param name="identityInsertion">
        ///     True if identity insertion, false otherwise.
        /// </param>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        public EfGiven<T> IdentityInsertion(bool identityInsertion)
        {
            this.identityInsertion = identityInsertion;

            return this;
        }

        /// <summary>
        ///     The include.
        /// </summary>
        /// <param name="included">
        ///     The included.
        /// </param>
        /// <returns>
        ///     The <see cref="EFFlow.EFGiven" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     The exception.
        /// </exception>
        public EfGiven<T> Include(Expression<Func<T, object>> included)
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
        ///     The create included collections 1.
        /// </summary>
        /// <param name="entity">
        ///     The entity.
        /// </param>
        /// <param name="tableRow">
        ///     The table row.
        /// </param>
        /// <param name="headers">
        ///     The headers.
        /// </param>
        private void CreateIncludedCollections1(T entity, TableRow tableRow, ICollection<string> headers)
        {
            foreach (var collectionInclude in this.collectionIncludes.Where(x => x.HierarchyProperty == null))
            {
                var includedEntities = new List<object>();

                for (var j = 0; j < headers.Count; j++)
                {
                    var header = headers.ElementAt(j);

                    if (header.StartsWith(collectionInclude.ColumnPrefix))
                    {
                        var cellValue = tableRow[j];

                        object includedEntity = collectionInclude.IncludeEntityCellDelegate(entity, header, cellValue);

                        if (includedEntity == null)
                        {
                            continue;
                        }

                        includedEntities.Add(includedEntity);
                    }
                }

                collectionInclude.AddIncludedEntities(entity, includedEntities);
            }
        }

        /// <summary>
        ///     The create included collections 2.
        /// </summary>
        /// <param name="entity">
        ///     The entity.
        /// </param>
        /// <param name="tableRow">
        ///     The table row.
        /// </param>
        /// <param name="headers">
        ///     The headers.
        /// </param>
        /// <param name="rowKey">
        ///     The row key.
        /// </param>
        private void CreateIncludedCollections2(
            T entity,
            TableRow tableRow,
            ICollection<string> headers,
            string[] rowKey)
        {
            var hierarchyIdentifier = this.collectionIncludes.FindMatch(rowKey);
            var includedEntities = new List<object>();

            for (var j = 0; j < headers.Count; j++)
            {
                var header = headers.ElementAt(j);

                if (header.StartsWith(hierarchyIdentifier.Include.ColumnPrefix))
                {
                    var cellValue = tableRow[j];
                    var parameter = hierarchyIdentifier.Include.BuildFunctionParameter(hierarchyIdentifier.Identifier);

                    object includedEntity = hierarchyIdentifier.Include.IncludeEntityCellDelegate(
                        entity,
                        header,
                        cellValue,
                        parameter);

                    if (includedEntity == null)
                    {
                        continue;
                    }

                    includedEntities.Add(includedEntity);
                }
            }

            hierarchyIdentifier.Include.AddIncludedEntities(entity, includedEntities);
        }

        /// <summary>
        ///     The create included entities.
        /// </summary>
        /// <param name="entity">
        ///     The entity.
        /// </param>
        /// <param name="table">
        ///     The table.
        /// </param>
        /// <param name="tableRow">
        ///     The table row.
        /// </param>
        private void CreateIncludedEntities(T entity, Table table, TableRow tableRow)
        {
            foreach (var include in this.includes)
            {
                var propertyInfo = (PropertyInfo)((MemberExpression)include.Body).Member;

                // Build included entities
                var includedEntity = Helpers.CreateInstance(table, tableRow, propertyInfo.PropertyType);

                Helpers.ReplaceDateTimeMinimumValues(includedEntity);

                propertyInfo.SetValue(entity, includedEntity);
            }
        }

        /// <summary>
        ///     The set calculated properties values.
        /// </summary>
        /// <param name="entity">
        ///     The entity.
        /// </param>
        /// <param name="calculatedValueExpression">
        ///     The calculated value expression.
        /// </param>
        private void SetCalculatedPropertiesValues(T entity, Tuple<object, object> calculatedValueExpression)
        {
            dynamic func = calculatedValueExpression.Item2;
            object funcResult = func.Invoke(entity);
            var valueExpression = new Tuple<object, object>(calculatedValueExpression.Item1, funcResult);

            ClausesHelper.SetPropertiesValues(entity, valueExpression, false);
        }
    }
}