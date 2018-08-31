using RedGate.Shared.ComparisonInterfaces;
using RedGate.Shared.SQL.ExecutionBlock;
using RedGate.SQLCompare.Engine;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CustomWorkBundler
{
    public class SQLCompare : IDisposable
    {

        private Database SourceDb { get; set; }
        private Database TargetDb { get; set; }
        
        public static Options Options { get; private set; } = Options.None.Plus(Options.IgnoreConstraintNames, Options.IgnoreUsers,  Options.DoNotOutputCommentHeader,  Options.IgnoreDatabaseAndServerName, 
                                                                                Options.NoSQLPlumbing,  Options.IgnoreWhiteSpace,  Options.IgnoreFillFactor,  Options.IgnoreUserProperties, 
                                                                                Options.IgnoreWithElementOrder,  Options.IgnoreExtendedProperties,  Options.IgnoreFileGroups);

        private Differences DbDifferences { get; set; }

        public string UpdateScript { get; private set; }

        // Regex to match with "[dbo].[(vw)CustomFields_XXXX]"
        private Regex CustomFieldsRegex { get; set; } = new Regex(@"\[.*\]\.\[(?:vw)?CustomFields_.*\]");

        public SQLCompare(ConnectionProperties dbSourceConnection, ConnectionProperties dbTargetConnection) 
        {
            RegisterDBs(dbSourceConnection, dbTargetConnection);
            
            DbDifferences = GetDifferences();

            UpdateScript = GenerateUpdateScript();
        }

        public SQLCompare(Database dbSource, Database dbTarget)
        {
            SourceDb = dbSource;
            TargetDb = dbTarget;

            DbDifferences = GetDifferences();

            UpdateScript = GenerateUpdateScript();
        }

        private void RegisterDBs(ConnectionProperties dbSourceConnection, ConnectionProperties dbTargetConnection)
        {
            SourceDb = new Database();
            TargetDb = new Database();
            
            SourceDb.Register(dbSourceConnection, Options);
            
            TargetDb.Register(dbTargetConnection, Options);
        }

        private Differences GetDifferences()
        {
            // Create 2 copies of the comparison. A temp one to iterate over, and the real one used to generate the upgrade script.
            var dbDifferencesTemp = SourceDb.CompareWith(TargetDb, Options);
            var dbDifferences = SourceDb.CompareWith(TargetDb, Options);
            
            // Remove any differences we don't care about (Ones that are equal).
            foreach (var difference in dbDifferencesTemp) {
                difference.Selected = false;
                if (IsValidDifference(difference)) {
                    // Don't think it does anything. Just for sanity
                    difference.Selected = true;
                } else {
                    dbDifferences.Remove(difference);
                }
            }

            if (dbDifferences.Count() == 0)
                dbDifferences = null;

            return dbDifferences;
        }

        private string GenerateUpdateScript()
        {
            if (DbDifferences == null)
                return string.Empty;
            
            Work work = new Work();
            
            // Generate the differences using the options
            work.BuildFromDifferences(DbDifferences, Options, false);
            
            using (var block = work.ExecutionBlock as ExecutionBlock) {
                // Remove the print statements and get the string from the Block object
                var queries = block.Where(q => !(q.Contents.StartsWith("PRINT N") || q.Contents.StartsWith("GO")))
                                   .Select(q => $"{q.Contents.Trim() + Environment.NewLine} GO").ToList();

                StringBuilder sb = new StringBuilder();
                queries.ForEach(x => sb.AppendLine(x));
                
                return sb.ToString();
            }
        }
        
        private bool IsValidDifference(Difference difference)
        {
            var difType = difference.DatabaseObjectType;

            // Differences that are not equal and not a user are valid
            bool isValid = difference.Type != DifferenceType.Equal && difType != ObjectType.User;

            // We want to ignore any tables or views called "CustomFields_XXXX"
            if (difType == ObjectType.Table || difType == ObjectType.View)
                isValid = !CustomFieldsRegex.IsMatch(difference.Name);
            return isValid;
        }

        public void WriteDatabaseTo(string filePath, string fileName)
        {
            Directory.CreateDirectory(filePath);
            TargetDb.SaveToDisk(Path.Combine(filePath, fileName));
        }

        public void Dispose()
        {
            SourceDb.Dispose();
            TargetDb.Dispose();
        }

        public static async Task<List<string>> TryConnectToDatabase(string servername, string database, string username, string password) => await TryConnectToDatabase(GetConnectionString(servername, database, username, password));
        public static async Task<List<string>> TryConnectToDatabase(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString)) {
                try {
                    await connection.OpenAsync();
                    return null;
                } catch (SqlException ex) {
                    var errors = new List<string>() { ex.Message };
                    return errors;
                }
            }
        }

        public static string GetConnectionString(string servername, string database, string username, string password)
        {
            return $"Data Source={servername}; Initial Catalog={database}; User id={username}; Password={password};";
        }
    }
}
