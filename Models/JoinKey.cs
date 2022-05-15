namespace ForeignKeyTunneling.Models
{
    public class JoinKey
    {
        public TableColumnKey From { get; }
        public TableColumnKey To { get; }
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
