using Newtonsoft.Json;

namespace ForeignKeyTunneling.Models
{
    [JsonObject]
    public class JoinKey
    {
        [JsonProperty]
        public TableColumnKey From { get; set; }
        [JsonProperty]
        public TableColumnKey To { get; set; }

        public JoinKey() { }
        public JoinKey(TableColumnKey from, TableColumnKey to)
        {
            From = from;
            To = to;
        }

        public override bool Equals(object obj)
        {
            JoinKey jk = obj as JoinKey;
            return jk != null && From.Equals(jk.From) && To.Equals(jk.To);
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() ^ To.GetHashCode();
        }
    }
}
