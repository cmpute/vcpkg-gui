using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Vcpkg
{
    public static class Paragraph
    {
        public static List<Dictionary<string, string>> ParseParagraph(string filepath)
        {
            var result = new List<Dictionary<string, string>>();
            var paragraph = new Dictionary<string, string>();
            string lastkey = null;
            foreach (var line in File.ReadAllText(filepath).Split(
                new string[] { Environment.NewLine }, StringSplitOptions.None))
            {
                if (line.StartsWith("#")) // comment
                    continue;
                else if (line.StartsWith("  ")) // continuous line
                {
                    if (lastkey == null) continue;
                    paragraph[lastkey] += Environment.NewLine + line.Trim();
                }
                else if (string.IsNullOrWhiteSpace(line)) // paragraph end
                {
                    if (paragraph.Count > 0)
                    {
                        result.Add(paragraph);
                        paragraph = new Dictionary<string, string>();
                    }
                }
                else
                {
                    var lsplit = line.Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                    paragraph.Add(lsplit[0], lsplit.Length < 2 ? string.Empty : lsplit[1]);
                    lastkey = lsplit[0];
                }
            }
            if(paragraph.Count >0) result.Add(paragraph);

            return result;
        }
    }

    [DebuggerDisplay("{Name}")]
    public sealed class Port
    {
        private Port() { }
        public string Name => CoreParagraph?.Name;
        public SourceParagraph CoreParagraph { get; set; }
        public List<FeatureParagraph> FeatureParagraphs { get; set; }

        public static Port ParseControlFile(string filepath)
        {
            const string SourceToken = "Source";
            const string FeatureToken = "Feature";

            var port = new Port();
            foreach (var paragraph in Paragraph.ParseParagraph(filepath))
            {
                if (paragraph.Keys.Contains(SourceToken))
                {
                    port.CoreParagraph = new SourceParagraph() { Name = paragraph[SourceToken] };
                    foreach (var item in paragraph)
                        switch (item.Key)
                        {
                            case "Version":
                                port.CoreParagraph.Version = item.Value;
                                break;
                            case "Build-Depends":
                                port.CoreParagraph.Depends = item.Value.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                                break;
                            case "Description":
                                port.CoreParagraph.Description = item.Value;
                                break;
                            case "Maintainer":
                                port.CoreParagraph.Maintainer = item.Value;
                                break;
                            case "Supports":
                                throw new NotSupportedException();
                            case "Default-Features":
                                throw new NotSupportedException();
                        }
                }
                else if (paragraph.Keys.Contains(FeatureToken))
                {
                    if (port.FeatureParagraphs == null)
                        port.FeatureParagraphs = new List<FeatureParagraph>();
                    if (port.Name == null) System.Diagnostics.Debugger.Break();
                    var feature = new FeatureParagraph(port.Name) { Name = paragraph[FeatureToken] };
                    foreach (var item in paragraph)
                        switch (item.Key)
                        {
                            case "Build-Depends":
                                feature.Depends = item.Value.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                                break;
                            case "Description":
                                feature.Description = item.Value;
                                break;
                        }
                }
                else throw new FormatException("Unknown paragraph type");
            }

            return port;
        }

        public static List<Port> ParsePortsFolder(string folderpath)
        {
            var result = new List<Port>();
            foreach(var dir in Directory.GetDirectories(folderpath))
            {
                var controlFile = Path.Combine(dir, "CONTROL");
                if (!File.Exists(controlFile))
                    throw new FileNotFoundException($"Control file for {Path.GetFileName(dir)} is not found!");
                result.Add(ParseControlFile(controlFile));
            }
            return result;
        }
    }

    [DebuggerDisplay("{Name}")]
    public sealed class SourceParagraph
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Maintainer { get; set; }
        public string[] Supports { get; set; }
        public string[] Depends { get; set; }
        public string[] DefaultFeatures { get; set; }
    }

    [DebuggerDisplay("{CoreName}[{Name}]")]
    public sealed class FeatureParagraph
    {
        public FeatureParagraph(string coreName) => CoreName = coreName;
        public string CoreName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Depends { get; set; }
    }
}
