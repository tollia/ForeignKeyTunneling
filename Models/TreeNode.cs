using ForeignKeyTunneling.Utils;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace ForeignKeyTunneling.Models
{
    public class TreeNode
    {
        public TreeNode(object id, object text, List<TreeNode> children)
        {
            Id = id.ToString();
            Text = text.ToString();
            Children = children;
        }
        [JsonPropertyName("id")]
        public string Id { get; set; }
        [JsonPropertyName("text")]
        public string Text { get; set; }
        [JsonPropertyName("children")]
        public List<TreeNode> Children { get; set; }

        public static List<TreeNode> GetTableNodeList(
            DbConnection connection,
            string tableName,
            string parentPath,
            Dictionary<TableColumnKey, TableColumnKey> auxForeignKeys
        )
        {
            List<TreeNode> nodeList = new();
            Dictionary<TableColumnKey, TableColumnKey> foreignKeys = Schema.ForeignKeyMapForTable(connection, tableName);
            DataTable columnsTable = connection.GetSchema("Columns", new string[] { null, null, tableName });
            foreach (DataRow row in columnsTable.Rows)
            {
                TableColumnKey key = new(tableName, row["COLUMN_NAME"]);
                List<TreeNode> children =
                    foreignKeys.ContainsKey(key) ?
                        GetTableNodeList(connection, foreignKeys[key].TableName, parentPath + "." + row["COLUMN_NAME"], auxForeignKeys) :
                        new();
                nodeList.Add(new TreeNode(parentPath + "." + row["COLUMN_NAME"], row["COLUMN_NAME"], children));
            }
            return nodeList;
        }
    }
}
