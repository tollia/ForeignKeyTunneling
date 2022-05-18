using FirebirdSql.Data.FirebirdClient;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;

namespace ForeignKeyTunneling.Utils
{
    // !!! Ugly quick fix. Needs a serious rejig and Singleton...
    public class DB
    {
        public string ConnectionString { get; set; }
        public bool ShowTiming { get; set; }

        public DB()
        {
            ConnectionString = new FbConnectionStringBuilder
            {
                Database = Path.Combine(Directory.GetCurrentDirectory(), @"Data\examples.fdb"),
                ServerType = FbServerType.Embedded,
                UserID = "SYSDBA",
                Password = "masterkey"
            }.ToString();
            ShowTiming = true;
        }

        public FbConnection GetConnection()
        {
            FbConnection connection = new(ConnectionString);
            if (connection.State == ConnectionState.Closed) connection.Open();
            return connection;
        }

        public object ExecuteScalar(string command)
        {
            using (FbConnection connection = GetConnection())
            {
                FbCommand dbCommand = connection.CreateCommand();
                dbCommand.CommandText = command;
                return dbCommand.ExecuteScalar();
            }
        }

        public int ExecuteNonQuery(string command)
        {
            using (FbConnection connection = GetConnection())
            {
                FbCommand dbCommand = connection.CreateCommand();
                dbCommand.CommandText = command;
                return dbCommand.ExecuteNonQuery();
            }
        }

        public string ExecuteSelect(string command)
        {
            DataTable dt = new DataTable("Result");
            using (FbConnection connection = GetConnection())
            {
                FbDataAdapter da = new FbDataAdapter(command, connection);
                da.Fill(dt);
            }
            StringBuilder result = new();
            Dictionary<string, int> colMaxLengthMap = new();
            // Capture maximum length for each column and format name for header.
            foreach (DataColumn col in dt.Columns)
            {
                int measuredMaxLenght = col.ColumnName.Length;
                foreach(DataRow row in dt.Rows)
                {
                    string val = row[col.ColumnName].ToString();
                    if (measuredMaxLenght < val.Length) measuredMaxLenght = val.Length;
                }
                colMaxLengthMap.Add(col.ColumnName, measuredMaxLenght);
                result.Append(col.ColumnName.PadRight(measuredMaxLenght) + " | ");
            }
            result.AppendLine();

            // Format all column values in a left aligned fashion rigt padded matching max width.
            foreach (DataRow row in dt.Rows)
            {
                foreach (DataColumn col in dt.Columns)
                {
                    result.Append(row[col.ColumnName].ToString().PadRight(colMaxLengthMap[col.ColumnName]) + " | ");
                }
                result.AppendLine();
            }

            return result.ToString();
        }

    }
}
