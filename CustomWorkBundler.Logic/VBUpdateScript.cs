using System;
using System.IO;
using System.Text;

namespace CustomWorkBundler
{
    public class VBUpdateScript
    {
        public string VBString { get; private set; }

        private string SQLString { get; set; }
        private string BuildName { get; set; }
        private double BuildNumber { get; set; }

        public VBUpdateScript(double buildNumber, string buildName, string sqlUpdateScript)
        {
            SQLString = sqlUpdateScript;

            BuildNumber = buildNumber;
            BuildName = buildName;
            VBString = GenerateVBString();
        }

        private string GenerateVBString()
        {
            var vbString = Resourcer.Resource.AsString("./VBTemplate.vb");

            // Replace the bundle name
            vbString = vbString.Replace("CustomBundle", BuildName);

            // Update Web Config Build
            var buildTag = "Settings(\"CurrentBuild\").Value =";
            var index = vbString.IndexOf(buildTag) + buildTag.Length;
            vbString = vbString.Insert(index, " \"" + BuildNumber.ToString() + "\"");

            // Add sql scripts
            var sqlTag = @"<![CDATA[";
            index = vbString.IndexOf(sqlTag) + sqlTag.Length;
            SQLString += Environment.NewLine + "UPDATE SystemOption SET DatabaseBuild = '" + BuildNumber + "'";
            vbString = vbString.Insert(index, Environment.NewLine + SQLString);

            return vbString;
        }

        public void WriteVbScriptTo(string output)
        {
            File.WriteAllText(output, VBString);
        }
    }
}
