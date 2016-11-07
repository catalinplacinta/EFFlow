namespace EfFlow.EfGiven
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    ///     The clauses helper.
    /// </summary>
    public class ClausesHelper
    {
        /// <summary>
        ///     The set properties values.
        /// </summary>
        /// <param name="entity">
        ///     The entity.
        /// </param>
        /// <param name="valueExpression">
        ///     The value expression.
        /// </param>
        /// <param name="overwriteValue">
        ///     The overwrite value.
        /// </param>
        /// <typeparam name="T">
        ///     The entity's type.
        /// </typeparam>
        /// <exception cref="Exception">
        ///     The exception.
        /// </exception>
        public static void SetPropertiesValues<T>(T entity, Tuple<object, object> valueExpression, bool overwriteValue)
        {
            var exp = (LambdaExpression)valueExpression.Item1;
            var memberExpression = (MemberExpression)exp.Body;

            PropertyInfo propertyInfo = null;
            object property = entity;

            var expression = memberExpression.Expression as MemberExpression;
            if (expression != null)
            {
                var memberExpression2 = expression;
                var propertyInfo2 = (PropertyInfo)memberExpression2.Member;
                property = propertyInfo2.GetValue(entity);
                propertyInfo = (PropertyInfo)memberExpression.Member;
            }
            else if (memberExpression.Expression is ParameterExpression)
            {
                propertyInfo = (PropertyInfo)memberExpression.Member;
            }
            else
            {
                throw new Exception();
            }

            if (overwriteValue)
            {
                propertyInfo.SetValue(property, valueExpression.Item2);
            }
            else
            {
                var currentValue = propertyInfo.GetValue(property);

                if (currentValue == Helpers.GetDefault(propertyInfo.PropertyType))
                {
                    propertyInfo.SetValue(property, valueExpression.Item2);
                }
            }
        }
    }
}