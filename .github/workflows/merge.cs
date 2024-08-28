using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

class Program
{
    static void Main(string[] args)
    {
        string inputFile = args[0];
        string outputFile = args[1];

        SortedSet<string> versions = new();
        SortedSet<string> authors = new();

        foreach (string fontFile in File.ReadAllLines(inputFile))
        {
            XDocument fontXml = XDocument.Load(Path.ChangeExtension(fontFile, ".ttx"));
            foreach (var namerecord in fontXml.Descendants("namerecord"))
                switch ((int)namerecord.Attribute("nameID"))
                {
                    case 3:
                        versions.Add(namerecord.Value.Trim().Replace(";GOOG;", ";UJCA;"));
                        break;

                    case 9:
                        authors.Add(namerecord.Value.Trim());
                        break;
                }
        }

        XDocument outputXml = XDocument.Load(outputFile, LoadOptions.PreserveWhitespace);
        string newVersion = Environment.GetEnvironmentVariable("RELEASE_VERSION");

        foreach (var namerecord in outputXml.Descendants("namerecord"))
            switch ((int)namerecord.Attribute("nameID"))
            {
                case 3:
                    string[] versionTokens = namerecord.Value.TrimStart().Split(';');
                    Version version = Version.Parse(versionTokens[0]);
                    newVersion ??= new Version(1, version.Minor + 1, version.Build, version.Revision).ToString(2);
                    namerecord.Value = string.Join(";", versionTokens);
                    break;

                case 5:
                    namerecord.Value = "Version " + newVersion;
                    break;

                case 9:
                    namerecord.Value = string.Join("; ", authors);
                    break;

                case 10:
                    namerecord.Value = string.Join("; ", versions);
                    break;
            }

        Environment.SetEnvironmentVariable("RELEASE_VERSION", newVersion, EnvironmentVariableTarget.User);
        outputXml.Save(outputFile);

        StringBuilder release = new StringBuilder();
        release.AppendLine("## " + args[2]);
        foreach (string v in versions)
            release.AppendLine("* " + v);
        release.AppendLine();

        File.AppendAllText("release.md", release.ToString());
    }
}