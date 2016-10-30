namespace EFFlow.EFGiven
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using TechTalk.SpecFlow;
    using TechTalk.SpecFlow.Assist;

    /// <summary>
    /// The spec flow extensions.
    /// </summary>
    public static class SpecFlowExtensions
    {
        /// <summary>
        /// The create set.
        /// </summary>
        /// <param name="table">
        /// The table.
        /// </param>
        /// <param name="type">
        /// The type.
        /// </param>
        /// <returns>
        /// The <see cref="System.Collections.Generic.List{Object}"/>.
        /// </returns>
        public static List<object> CreateSet(this Table table, Type type)
        {
            var methodInfo = typeof(TableHelperExtensionMethods).GetMethods().Single(x => x.Name == "CreateSet" && x.GetParameters().Length == 1);
            var results = methodInfo.MakeGenericMethod(type).Invoke(null, new object[] { table });

            return ((IEnumerable)results).Cast<object>().ToList();
        }
    }
}
