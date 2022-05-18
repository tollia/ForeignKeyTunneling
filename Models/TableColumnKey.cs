using Newtonsoft.Json;

namespace ForeignKeyTunneling.Models
{
    [JsonObject]
    public class TableColumnKey
    {
        [JsonProperty]
        public string TableName { get; set; }
        [JsonProperty]
        public string ColumnName { get; set; }
        // This does not take part in Equals and HashCode evaluation. It defaults to TableName.
        [JsonProperty]
        public string TableAlias { get; set; }

        public TableColumnKey() { }
        public TableColumnKey(TableColumnKey key) : this(key.TableName, key.ColumnName) { }

        public TableColumnKey(object tableName, object columnName) : this(tableName.ToString(), columnName.ToString()) { }

        public TableColumnKey(string tableName, string columnName)
        {
            TableName = tableName;
            TableAlias = tableName;
            ColumnName = columnName;
        }

        public override bool Equals(object obj)
        {
            TableColumnKey tck = obj as TableColumnKey;
            return tck != null && TableName.Equals(tck.TableName) && ColumnName.Equals(tck.ColumnName);
        }

        public override int GetHashCode()
        {
            return TableName.GetHashCode() ^ ColumnName.GetHashCode();
        }

        public bool ShouldSerializeTableAlias()
        {
            // don't serialize the Manager property if an employee is their own manager
            return (TableName != TableAlias);
        }

        //public override string ToString()
        //{
        //    string aliasString = TableAlias == TableName ? "" : $"~{TableAlias}";
        //    return $"{TableName}{aliasString}({ColumnName})";
        //}
    }
}
