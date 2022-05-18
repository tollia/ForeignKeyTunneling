using FirebirdSql.Data.FirebirdClient;
using ForeignKeyTunneling.Models;
using ForeignKeyTunneling.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace ForeignKeyTunneling.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private DB DB { get; init; }

        public HomeController(DB db, ILogger<HomeController> logger)
        {
            DB = db;
            _logger = logger;
        }

        private string ConnectionString
        {
            get
            {
                return new FbConnectionStringBuilder
                {
                    Database = Path.Combine(Directory.GetCurrentDirectory(), @"Data\examples.fdb"),
                    ServerType = FbServerType.Embedded,
                    UserID = "SYSDBA",
                    Password = "masterkey"
                }.ToString();
            }
        }

        //Dictionary<TableColumnKey, TableColumnKey> foreignKeys = Schema.ForeignKeyMapFromTable(connection, tableName);
        //JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
        //{
        //    Formatting = Formatting.Indented,
        //    ContractResolver = new DictionaryAsArrayResolver()
        //};
        //Dictionary<TableColumnKey, TableColumnKey> pmet = JsonConvert.DeserializeObject<Dictionary<TableColumnKey, TableColumnKey>>(temp, jsonSettings);
        //string temp = JsonConvert.SerializeObject(foreignKeys, jsonSettings);

        public IActionResult Index(string tableName)
        {
            using (FbConnection connection = DB.GetConnection())
            {
                ViewData["Tables"] = connection.GetSchema("Tables", new string[] { null, null, null, "TABLE" });
            }
            ViewBag.TableName = tableName;
            return View();
        }

        public IActionResult Tree(string tableName, string auxForeignKeyJson)
        {
            // Setup AUX ForeignKeys if provided.
            Dictionary<TableColumnKey, TableColumnKey> auxForeignKeys;
            if (string.IsNullOrEmpty(auxForeignKeyJson))
            {
                auxForeignKeys = new();
            }
            else
            {
                JsonSerializerSettings jsonSettings = new JsonSerializerSettings()
                {
                    Formatting = Formatting.Indented,
                    ContractResolver = new DictionaryAsArrayResolver()
                };
                auxForeignKeys = JsonConvert.DeserializeObject<Dictionary<TableColumnKey, TableColumnKey>>(auxForeignKeyJson, jsonSettings);
            }

            // Prepare Json for 
            List<TreeNode> nodeList;
            using (FbConnection connection = DB.GetConnection())
            {
                if (connection.State == ConnectionState.Closed) connection.Open();
                nodeList = TreeNode.GetTableNodeList(connection, tableName, tableName, auxForeignKeys);
            }
            ViewBag.NodeListJSON = System.Text.Json.JsonSerializer.Serialize(nodeList, new JsonSerializerOptions() { WriteIndented = true });
            return View();
        }


        public IActionResult Select(string colPathCSV, int? maxRecords)
        {
            List<DataTable> tableList = new();
            using (FbConnection connection = DB.GetConnection())
            {
                if (connection.State == ConnectionState.Closed) connection.Open();

                string select = Schema.GenerateSelect(connection, colPathCSV.Split(",").ToList<string>(), maxRecords);
                DataTable dt = new DataTable("FK_TUNNELING_RESULT");
                FbDataAdapter da = new FbDataAdapter(select, connection);
                da.Fill(dt);
                tableList.Add(dt);
            }

            return View(tableList);
        }

            public IActionResult MetaData(string tableName = null)
        {
            List<DataTable> tableList = new();
            using (FbConnection connection = DB.GetConnection())
            {
                if (connection.State == ConnectionState.Closed) connection.Open();

                tableList.Add(connection.GetSchema("MetaDataCollections"));
                tableList.Add(connection.GetSchema("Tables", new string[] { null, null, tableName, "TABLE" }));
                tableList.Add(connection.GetSchema("PrimaryKeys", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("Columns", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("Views", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("ViewColumns", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("Indexes", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("IndexColumns", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("ForeignKeys", new string[] { null, null, tableName }));
                tableList.Add(connection.GetSchema("ForeignKeyColumns", new string[] { null, null, tableName }));

                DataTable dt = new DataTable("INVOICE_LINE");
                FbDataAdapter da = new FbDataAdapter(
                    @"
                    select first 10
                    INVOICE_LINE.QUANTITY as INVOICE_LINE_QUANTITY,
                    INVOICE_LINE.SALE_PRICE as INVOICE_LINE_SALE_PRICE,
                    INVOICE.TOTAL_SALE as INVOICE_TOTAL_SALE,
                    CUSTOMER.NAME as CUSTOMER_NAME,
                    PRODUCT.NAME as PRODUCT_NAME,
                    PRODUCT.PRICE as PRODUCT_PRICE
                    from INVOICE_LINE as INVOICE_LINE
                    join INVOICE as INVOICE on INVOICE_LINE.INVOICE_ID=INVOICE.INVOICE_ID
                    join CUSTOMER as CUSTOMER on INVOICE.CUSTOMER_ID=CUSTOMER.CUSTOMER_ID
                    join PRODUCT as PRODUCT on INVOICE_LINE.PRODUCT_ID=PRODUCT.PRODUCT_ID
                    ", 
                    connection
                );
                da.Fill(dt);
                tableList.Add(dt);
            }

            return View(tableList);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
