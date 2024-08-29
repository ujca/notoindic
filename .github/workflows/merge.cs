using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

class Program
{
    class NameComparer : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            int ix = x.LastIndexOf(';') + 1;
            int iy = y.LastIndexOf(';') + 1;
            return string.CompareOrdinal(x.Substring(ix), y.Substring(iy));
        }
    }

    static void Main(string[] args)
    {
        string inputFile = args[0];
        string outputFile = args[1];
        string header = args[2];
        string notes = args[3];

        SortedSet<string> versions = new(new NameComparer());
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

        StringBuilder release = new StringBuilder();
        release.AppendLine("## " + header);
        foreach (string v in versions)
            release.AppendLine("* " + v);
        release.AppendLine();

        bool minorUpdate = File.Exists(notes) && File.ReadAllText(notes).TrimEnd() == release.ToString().TrimEnd();
        File.WriteAllText(notes, release.ToString());

        XDocument outputXml = XDocument.Load(outputFile, LoadOptions.PreserveWhitespace);
        string newVersion = null;

        foreach (var namerecord in outputXml.Descendants("namerecord"))
            switch ((int)namerecord.Attribute("nameID"))
            {
                case 3:
                    string[] versionTokens = namerecord.Value.Trim().Split(';');
                    Version version = Version.Parse(versionTokens[0]);
                    newVersion ??= minorUpdate ? new Version(1, version.Minor, Math.Max(1, version.Build + 0)).ToString(3) : new Version(1, version.Minor + 1).ToString(2);
                    versionTokens[0] = newVersion;
                    namerecord.Value = PreserveWhitespace(namerecord.Value, string.Join(";", versionTokens));
                    break;

                case 5:
                    namerecord.Value = PreserveWhitespace(namerecord.Value, "Version " + newVersion);
                    break;

                case 9:
                    namerecord.Value = PreserveWhitespace(namerecord.Value, string.Join("; ", authors));
                    break;

                case 10:
                    namerecord.Value = PreserveWhitespace(namerecord.Value, string.Join("; ", versions));
                    break;
            }

        outputXml.Save(outputFile);

        Console.WriteLine("RELEASE_VERSION=" + DateTime.Now.ToString("yyyy-MM-dd"));
    }

    static string PreserveWhitespace(string whitespace, string value)
    {
        if (string.IsNullOrEmpty(whitespace))
            return value;

        int valueStart = 0;
        for (; valueStart < whitespace.Length; valueStart++)
            if (!char.IsWhiteSpace(whitespace, valueStart))
                break;

        int valueLast = whitespace.Length - 1;
        for (; valueLast >= valueStart; valueLast--)
            if (!char.IsWhiteSpace(whitespace, valueLast))
                break;

        return whitespace.Substring(0, valueStart) + value + whitespace.Substring(valueLast + 1);
    }
}