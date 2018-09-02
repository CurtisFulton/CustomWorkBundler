using RedGate.Shared.ComparisonInterfaces;
using RedGate.Shared.SQL.ExecutionBlock;
using RedGate.SQLCompare.Engine;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CustomWorkBundler.Logic
{
    public class DatabaseCompare : IDisposable
    {
        private Database SourceDB { get; set; }
        private Database TargetDB { get; set; }
        
        // Regex to match with "[dbo].[(vw)CustomFields_XXXX]"
        private Regex CustomFieldsRegex { get; set; } = new Regex(@"\[.*\]\.\[(?:vw)?CustomFields_.*\]");

        public static Options Options { get; private set; } = Options.None.Plus(Options.IgnoreConstraintNames, Options.IgnoreUsers, Options.DoNotOutputCommentHeader, Options.IgnoreDatabaseAndServerName,
                                                                                Options.NoSQLPlumbing, Options.IgnoreWhiteSpace, Options.IgnoreFillFactor, Options.IgnoreUserProperties,
                                                                                Options.IgnoreWithElementOrder, Options.IgnoreExtendedProperties, Options.IgnoreFileGroups, Options.CaseSensitiveObjectDefinition);

        public DatabaseCompare(Database sourceDB, Database targetDB)
        {
            this.SourceDB = sourceDB;
            this.TargetDB = targetDB;
        }

        public async Task<string> GenerateUpdateScriptAsync()
        {
            // Need both databases to not be null to compare
            if (this.SourceDB == null || this.TargetDB == null)
                return null;

            return await Task.Run(() => {
                var differences = GetDifferences();

                if (differences == null)
                    return string.Empty;

                Work work = new Work();
                work.BuildFromDifferences(differences, Options, false);

                using (var block = work.ExecutionBlock as ExecutionBlock) {
                    // Remove the print statements and get the string from the Block object
                    var queries = block.Where(q => !(q.Contents.StartsWith("PRINT N") || q.Contents.StartsWith("GO")))
                                       .Select(q => $"{q.Contents.Trim() + Environment.NewLine} GO").ToList();

                    StringBuilder sb = new StringBuilder();
                    queries.ForEach(x => sb.AppendLine(x));
                    
                    return sb.ToString();
                }
            });
        }

        private Differences GetDifferences()
        {
            // Just accept the magic
            var differencesTemp = this.SourceDB.CompareWith(this.TargetDB, Options);
            var differencesFinal = this.SourceDB.CompareWith(this.TargetDB, Options);

            foreach (var difference in differencesTemp) {
                difference.Selected = false;

                if (IsValidDifference(difference)) {
                    difference.Selected = true;
                } else {
                    // If it wasn't a valid difference (Or it wasn't a difference at all) remove it from the final differences collection
                    differencesFinal.Remove(difference);
                }
            }

            if (differencesFinal.Count() == 0)
                differencesFinal = null;

            return differencesFinal;
        }

        private bool IsValidDifference(Difference difference)
        {
            var difType = difference.DatabaseObjectType;

            // Differences that are not equal and not a user are valid
            bool isValid = difference.Type != DifferenceType.Equal && difType != ObjectType.User;

            // We want to ignore any tables or views called "CustomFields_XXXX"
            if (difType == ObjectType.Table || difType == ObjectType.View)
                isValid = !this.CustomFieldsRegex.IsMatch(difference.Name);

            return isValid;
        }

        public void Dispose() {
            this.SourceDB?.Dispose();
            this.TargetDB?.Dispose();
        }
    }
}