using ForeignKeyTunneling.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text.RegularExpressions;

namespace ForeignKeyTunneling.Utils
{
    public static class Schema
    {
        public static string GenerateSelect(DbConnection connection, List<string> colStrings, int? numberOfRows = null)
        {
            // Transform colStrings to colPaths, making sure that all entries has the same base table.
            // Perform rudimentary format checks and refuse paths with invalid/malicious characters.
            Regex validPathRegex = new Regex(@"^(?!.*[.*]{2})[a-z0-9_]+(?:\.[a-z0-9_]+)+?$", RegexOptions.IgnoreCase);
            List<List<string>> colPaths = new();
            string baseTableName = null;
            foreach (string colString in colStrings)
            {
                if (!validPathRegex.Match(colString).Success)
                {
                    throw new Exception($"Invalid column string {colString}");
                }

                List<string> colPath = colString.Split(".").ToList();
                if (baseTableName != null && colPath[0] != baseTableName)
                {
                    throw new Exception("All column paths must originate from same table");
                }
                if (colPath.Count < 2)
                {
                    throw new Exception("All column paths must reference at least a column from base table");
                }
                colPaths.Add(colPath);
                baseTableName = colPath[0];
            }

            // Derive join and select lists based on baseTableName and colPaths from previous step.
            // Note that referenced tables that have foreign keys to other tables can occur multiple times in the schema.
            List<string> selectList = new();
            Dictionary<TableColumnKey, TableColumnKey> foreignKeys = ForeignKeyMapFromTable(connection, baseTableName);
            Dictionary<string, JoinKey> pathJoinKeys = new();
            Dictionary<string, HashSet<JoinKey>> tableJoinKeySets = new();
            foreach (List<string> colPath in colPaths)
            {
                TableColumnKey currentKey = new(baseTableName, null);
                for (int c = 1; c < colPath.Count; c++)
                {
                    string colName = colPath[c];
                    if (c == colPath.Count - 1)
                    {
                        selectList.Add($"{currentKey.TableAlias}.{colName} as {currentKey.TableAlias}_{colName}");
                    }
                    else
                    {
                        string path = string.Join('.', colPath.GetRange(1, c));
                        JoinKey joinKey;
                        if (pathJoinKeys.ContainsKey(path))
                        {
                            joinKey = pathJoinKeys[path];
                        }
                        else
                        {
                            TableColumnKey joinFromKey = new(currentKey.TableName, colName);
                            TableColumnKey joinToKey = new(foreignKeys[joinFromKey]);

                            // If table has been joined to then adjust Alias.
                            if (!tableJoinKeySets.TryAdd(joinToKey.TableName, new()))
                            {
                                joinToKey.TableAlias = joinToKey.TableName + tableJoinKeySets[joinToKey.TableName].Count;
                            }

                            joinKey = new(joinFromKey, joinToKey);
                            tableJoinKeySets[joinToKey.TableName].Add(joinKey);
                            pathJoinKeys.Add(path, joinKey);
                        }
                        currentKey = joinKey.To;
                    }
                }
            }

            // Transform join and select lists to clauses suitable for insertion into select statement.
            string clauseSelect = string.Join(",\n", selectList);
            string clauseJoin = $"from {baseTableName} as {baseTableName}";
            foreach (JoinKey key in pathJoinKeys.Values)
            {
                clauseJoin += $"\njoin {key.To.TableName} as {key.To.TableAlias}";
                clauseJoin += $" on {key.From.TableAlias}.{key.From.ColumnName}={key.To.TableAlias}.{key.To.ColumnName}";
            }

            string firstClause = numberOfRows == null ? "" : " first " + numberOfRows;
            return $"select{firstClause}\n{clauseSelect}\n{clauseJoin}";
        }

        public static Dictionary<TableColumnKey, TableColumnKey> ForeignKeyMapFromTable(DbConnection connection, string tableName, HashSet<TableColumnKey> foreignKeySet = null)
        {
            if (foreignKeySet == null) foreignKeySet = new();
            Dictionary<TableColumnKey, TableColumnKey> foreignKeyMap = ForeignKeyMapForTable(connection, tableName);
            Dictionary<TableColumnKey, TableColumnKey> resultKeyMap = new();
            foreach (TableColumnKey key in foreignKeyMap.Keys)
            {
                if (!foreignKeySet.Contains(key))
                {
                    foreignKeySet.Add(key);
                    Dictionary<TableColumnKey, TableColumnKey> additionalKeyMap = ForeignKeyMapFromTable(connection, foreignKeyMap[key].TableName, foreignKeySet);
                    foreach (TableColumnKey additionalKey in additionalKeyMap.Keys)
                    {
                        resultKeyMap.TryAdd(additionalKey, additionalKeyMap[additionalKey]);
                    }
                }
            }
            foreach (TableColumnKey key in foreignKeyMap.Keys)
            {
                resultKeyMap.TryAdd(key, foreignKeyMap[key]);
            }
            return resultKeyMap;
        }

        public static Dictionary<TableColumnKey, TableColumnKey> ForeignKeyMapForTable(DbConnection connection, string tableName)
        {
            DataTable foreignKeyDT = connection.GetSchema("ForeignKeyColumns", new string[] { null, null, tableName });
            Dictionary<TableColumnKey, TableColumnKey> foreignKeyMap = new();
            foreach (DataRow row in foreignKeyDT.Rows)
            {
                foreignKeyMap.Add(new TableColumnKey(row["TABLE_NAME"], row["COLUMN_NAME"]), new TableColumnKey(row["REFERENCED_TABLE_NAME"], row["REFERENCED_COLUMN_NAME"]));
            }
            return foreignKeyMap;
        }
    }
}
