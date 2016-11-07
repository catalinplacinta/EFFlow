namespace EfFlow.EfThen
{
    using System.Linq.Expressions;

    /// <summary>
    ///     The lookup property.
    /// </summary>
    public class LookupProperty
    {
        /// <summary>
        ///     Gets or sets the default value.
        /// </summary>
        public object DefaultValue { get; set; }

        /// <summary>
        ///     Gets or sets the lookup expression.
        /// </summary>
        public LambdaExpression LookupExpression { get; set; }
    }
}