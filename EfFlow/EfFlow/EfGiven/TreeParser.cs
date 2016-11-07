namespace EfFlow.EfGiven
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using TechTalk.SpecFlow;

    /// <summary>
    ///     The tree parser.
    /// </summary>
    public class TreeParser
    {
        /// <summary>
        ///     The columns.
        /// </summary>
        private readonly List<string> columns;

        /// <summary>
        ///     The table.
        /// </summary>
        private readonly Table table;

        /// <summary>
        ///     Initializes a new instance of the <see cref="TreeParser" /> class.
        /// </summary>
        /// <param name="table">
        ///     The table.
        /// </param>
        /// <param name="columns">
        ///     The columns.
        /// </param>
        /// <exception cref="Exception">
        ///     The exception.
        /// </exception>
        public TreeParser(Table table, List<string> columns)
        {
            this.columns = columns;
            this.table = table;

            foreach (var column in columns)
            {
                if (!table.Header.Contains(column))
                {
                    throw new Exception($"Missing hierarchy column {column} in the table");
                }
            }
        }

        /// <summary>
        ///     The parse.
        /// </summary>
        /// <returns>
        ///     The <see cref="System.Collections.Generic.Dictionary{Int, RowInfo}" />.
        /// </returns>
        /// <exception cref="Exception">
        ///     The exception.
        /// </exception>
        public Dictionary<int, RowInfo> Parse()
        {
            var rowInfos = new Dictionary<int, RowInfo>();
            var rowKeys = this.GetRowKeys();

            if (this.columns.Count == 0)
            {
                for (var i = 0; i < this.table.RowCount; i++)
                {
                    rowInfos.Add(i, new RowInfo { HierarchyLevel = 0, Parent = null, RowKey = rowKeys[i] });
                }

                return rowInfos;
            }

            for (var i = 0; i < rowKeys.Count; i++)
            {
                if (i == 0)
                {
                    var firstLineOk = this.CheckFirstLine(rowKeys[i]);

                    if (!firstLineOk)
                    {
                        throw new Exception("Error parsing first line");
                    }

                    rowInfos.Add(0, new RowInfo { HierarchyLevel = 0, Parent = null, RowKey = rowKeys[0] });

                    continue;
                }

                var lastValueFound = false;
                var nullValueFoundBeforeLastValue = false;

                for (var j = rowKeys[i].Length - 1; j >= 0; j--)
                {
                    if (string.IsNullOrEmpty(rowKeys[i][j]))
                    {
                        var valueToCopy = rowKeys[i - 1][j];
                        if (lastValueFound)
                        {
                            nullValueFoundBeforeLastValue = true;

                            if (string.IsNullOrEmpty(valueToCopy))
                            {
                                throw new Exception();
                            }

                            rowKeys[i][j] = rowKeys[i - 1][j];
                        }

                        continue;
                    }

                    if (nullValueFoundBeforeLastValue)
                    {
                        throw new Exception();
                    }

                    lastValueFound = true;
                }

                // Look for the parent
                var hierarchyLevel = rowKeys[i].Count(x => x != null) - 1;
                RowInfo parentRowInfo = null;

                for (var k = i - 1; k >= 0; k--)
                {
                    if (rowInfos[k].HierarchyLevel == hierarchyLevel - 1)
                    {
                        parentRowInfo = rowInfos[k];
                    }
                }

                if (parentRowInfo == null)
                {
                    throw new Exception("Parent not found");
                }

                rowInfos.Add(
                    i,
                    new RowInfo { HierarchyLevel = hierarchyLevel, Parent = parentRowInfo, RowKey = rowKeys[i] });
            }

            return rowInfos;
        }

        /// <summary>
        ///     The check first line.
        /// </summary>
        /// <param name="rowKey">
        ///     The row key.
        /// </param>
        /// <returns>
        ///     The <see cref="bool" />.
        /// </returns>
        private bool CheckFirstLine(string[] rowKey)
        {
            if (rowKey.Length < 1)
            {
                return false;
            }

            if (rowKey[0] == null)
            {
                return false;
            }

            var nullValueFound = false;

            for (var i = 1; i < rowKey.Length; i++)
            {
                if (rowKey[i] == null)
                {
                    nullValueFound = true;
                }
                else
                {
                    if (nullValueFound)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        ///     The get row keys.
        /// </summary>
        /// <returns>
        ///     The <see cref="System.Collections.Generic.List{String}" />.
        /// </returns>
        private List<string[]> GetRowKeys()
        {
            var rowKeys = new List<string[]>();

            foreach (var tableRow in this.table.Rows)
            {
                var rowKey = new string[this.columns.Count];

                rowKeys.Add(rowKey);

                for (var j = 0; j < this.columns.Count; j++)
                {
                    var headerName = this.columns[j];
                    rowKey[j] = tableRow[headerName];
                }
            }

            return rowKeys;
        }
    }
}