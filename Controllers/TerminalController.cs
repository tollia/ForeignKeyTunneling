using FirebirdSql.Data.FirebirdClient;
using ForeignKeyTunneling.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ForeignKeyTunneling.Controllers
{
    public class TerminalController : Controller
    {
        private readonly ILogger<TerminalController> _logger;
        private DB DB { get; init; }

        public TerminalController(DB db, ILogger<TerminalController> logger)
        {
            DB = db;
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public string Execute(string command)
        {
            command = command.Trim(new char[] {' ', '\n'});
            string firstKeyword = command.Split(new char[] { ' ', ';', '\n' })[0].ToLower();
            string result = string.Empty;
            if (command.EndsWith(';'))
            {
                switch (firstKeyword)
                {
                    case "":
                        // No command to issue
                        break;
                    case "select":
                        using (FbConnection connection = DB.GetConnection())
                        {
                            result = DB.ExecuteSelect(command);
                        }
                        break;
                    default:
                        using (FbConnection connection = DB.GetConnection())
                        {
                            FbCommand dbCommand = connection.CreateCommand();
                            dbCommand.CommandText = command;
                            int numRows = dbCommand.ExecuteNonQuery();
                            result = $"Rows affected: {numRows}";
                        }
                        break;
                }
            }
            else
            {
                result = command;
            }
            return result;
        }
    }
}
