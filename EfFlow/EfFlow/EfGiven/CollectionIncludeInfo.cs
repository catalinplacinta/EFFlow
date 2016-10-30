namespace EFFlow.EFGiven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// The collection include info.
    /// </summary>
    public class CollectionIncludeInfo
    {
        #region < Properties >

        /// <summary>
        /// Gets or sets the include.
        /// </summary>
        public dynamic Include { get; set; }

        /// <summary>
        /// Gets or sets the column prefix.
        /// </summary>
        public string ColumnPrefix { get; set; }

        /// <summary>
        /// Gets or sets the include entity cell delegate.
        /// </summary>
        public dynamic IncludeEntityCellDelegate { get; set; }

        /// <summary>
        /// Gets or sets the hierarchy property.
        /// </summary>
        public dynamic HierarchyProperty { get; set; }

        #endregion

        #region < Methods >

        /// <summary>
        /// The build function parameter.
        /// </summary>
        /// <param name="identifier">
        /// The identifier.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public dynamic BuildFunctionParameter(object[] identifier)
        {
            if (this.HierarchyProperty == null)
            {
                throw new Exception();
            }

            if (this.HierarchyProperty.Body is MemberExpression)
            {
                return identifier[0];
            }

            if (this.HierarchyProperty.Body is NewExpression)
            {
                var arguments = ((NewExpression)this.HierarchyProperty.Body).Arguments.ToList();
                var values = new List<object>();
                var types = new List<Type>();

                for (var i = 0; i < arguments.Count; i++)
                {
                    var id = identifier[i];

                    if (id == null)
                    {
                        throw new Exception();
                    }

                    if (id.GetType().IsGenericType && id.GetType().GetGenericTypeDefinition() == typeof(Nullable<>))
                    {
                        dynamic kk = id;

                        values.Add(kk.Value);
                    }
                    else
                    {
                        values.Add(identifier[i]);
                    }

                    types.Add(identifier[i].GetType());
                }

                var tupleType = GetTupleType(values.Count);
                var genericType = tupleType.MakeGenericType(types.ToArray());
                var constructor = genericType.GetConstructor(types.ToArray());

                dynamic result = constructor.Invoke(values.ToArray());

                return result;
            }

            throw new Exception();
        }

        /// <summary>
        /// The build identifier.
        /// </summary>
        /// <param name="rowKey">
        /// The row key.
        /// </param>
        /// <returns>
        /// The <see cref="object"/> array.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public object[] BuildIdentifier(string[] rowKey)
        {
            if (this.HierarchyProperty == null)
            {
                throw new Exception();
            }

            if (this.HierarchyProperty.Body is MemberExpression)
            {
                var propertyInfo = (PropertyInfo)((MemberExpression)this.HierarchyProperty.Body).Member;

                return new object[] { ParseValue(MakeNullable(propertyInfo.PropertyType), rowKey[0]) };
            }

            if (this.HierarchyProperty.Body is NewExpression)
            {
                var arguments = ((NewExpression)this.HierarchyProperty.Body).Arguments.ToList();

                return arguments
                    .Select(t => (PropertyInfo)(t as MemberExpression).Member)
                    .Select((propertyInfo, i) => ParseValue(MakeNullable(propertyInfo.PropertyType), rowKey[i]))
                    .Cast<object>()
                    .ToArray();
            }

            throw new Exception();
        }

        /// <summary>
        /// The get hierarchy property type.
        /// </summary>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public Type GetHierarchyPropertyType()
        {
            if (this.HierarchyProperty == null)
            {
                return null;
            }

            if (this.HierarchyProperty.Body is MemberExpression)
            {
                var propertyInfo = (PropertyInfo)((MemberExpression)this.HierarchyProperty.Body).Member;

                return propertyInfo.PropertyType;
            }

            if (this.HierarchyProperty.Body is NewExpression)
            {
                var arguments = ((NewExpression)this.HierarchyProperty.Body).Arguments.ToList();
                var propertyInfo = (PropertyInfo)(arguments[arguments.Count - 1] as MemberExpression).Member;

                return propertyInfo.PropertyType;
            }

            throw new Exception();
        }

        /// <summary>
        /// The get hierarchy level.
        /// </summary>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        [Obsolete("Improve this")]
        public int GetHierarchyLevel()
        {
            if (this.HierarchyProperty == null)
            {
                return 0;
            }

            if (this.HierarchyProperty.Body is MemberExpression)
            {
                return 1;
            }

            if (this.HierarchyProperty.Body is NewExpression)
            {
                var arguments = ((NewExpression)this.HierarchyProperty.Body).Arguments.ToList();

                return arguments.Count;
            }

            throw new Exception();
        }

        /// <summary>
        /// The get hierarchy property name.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        public string GetHierarchyPropertyName()
        {
            if (this.HierarchyProperty == null)
            {
                return null;
            }

            if (this.HierarchyProperty.Body is MemberExpression)
            {
                var propertyInfo = (PropertyInfo)((MemberExpression)this.HierarchyProperty.Body).Member;

                return propertyInfo.Name;
            }

            if (this.HierarchyProperty.Body is NewExpression)
            {
                var arguments = ((NewExpression)this.HierarchyProperty.Body).Arguments.ToList();
                var propertyInfo = (arguments[arguments.Count - 1] as MemberExpression).Member;

                return propertyInfo.Name;
            }

            throw new Exception();
        }

        /// <summary>
        /// The add included entities.
        /// </summary>
        /// <param name="entity">
        /// The entity.
        /// </param>
        /// <param name="includedEntities">
        /// The included entities.
        /// </param>
        public void AddIncludedEntities(object entity, List<object> includedEntities)
        {
            var memberExpression = (MemberExpression)this.Include.Body;
            var targetObject = entity;
            var propertyInfo = (PropertyInfo)memberExpression.Member;

            var expression = memberExpression.Expression as MemberExpression;
            if (expression != null)
            {
                var memberExpression2 = expression;
                var propertyInfo2 = (PropertyInfo)memberExpression2.Member;
                targetObject = propertyInfo2.GetValue(entity);
            }

            var collectionType = propertyInfo.PropertyType.GenericTypeArguments[0];
            var listType = typeof(List<>).MakeGenericType(collectionType);
            var listConstructor = listType.GetConstructor(new Type[] { });
            var list = listConstructor.Invoke(new object[] { });

            propertyInfo.SetValue(targetObject, list);

            var addMethod = listType.GetMethod("Add");

            foreach (var includedEntity in includedEntities)
            {
                addMethod.Invoke(list, new[] { includedEntity });
            }
        }

        /// <summary>
        /// The parse value.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        private static dynamic ParseValue(Type type, string value)
        {
            if (type == typeof(int?))
            {
                try
                {
                    return int.Parse(value);
                }
                catch
                {
                    return null;
                }
            }

            if (type == typeof(string))
            {
                try
                {
                    return value;
                }
                catch
                {
                    return null;
                }
            }

            if (type == typeof(short?))
            {
                try
                {
                    return short.Parse(value);
                }
                catch
                {
                    return null;
                }
            }

            throw new Exception();
        }

        /// <summary>
        /// The make null-able.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        private static Type MakeNullable(Type type)
        {
            var nullableType = typeof(Nullable<>);

            return nullableType.MakeGenericType(type);
        }

        /// <summary>
        /// The get tuple type.
        /// </summary>
        /// <param name="numberOfParameters">
        /// The number of parameters.
        /// </param>
        /// <returns>
        /// The <see cref="Type"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// The exception.
        /// </exception>
        private static Type GetTupleType(int numberOfParameters)
        {
            switch (numberOfParameters)
            {
                case 1:
                    return typeof(Tuple<>);
                case 2:
                    return typeof(Tuple<,>);
                case 3:
                    return typeof(Tuple<,,>);
                case 4:
                    return typeof(Tuple<,,,>);
                case 5:
                    return typeof(Tuple<,,,,>);
                case 6:
                    return typeof(Tuple<,,,,,>);
                default:
                    throw new Exception();
            }
        }

        #endregion
    }
}
