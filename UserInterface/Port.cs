using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Vcpkg
{
    public static class Parser
    {
        private static string[] DotSplit(string str)
            => str.Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);

        public static List<Dictionary<string, string>> ParseParagraph(string filepath)
        {
            var result = new List<Dictionary<string, string>>();
            var paragraph = new Dictionary<string, string>();
            string lastkey = null;

            var content = File.ReadAllText(filepath);
            string sep = content.IndexOf(Environment.NewLine) < 0 ? "\n" : Environment.NewLine;
            foreach (var line in content.Split(new string[] { sep }, StringSplitOptions.None))
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

        public static Port ParseControlFile(string filepath)
        {
            const string SourceToken = "Source";
            const string FeatureToken = "Feature";

            var port = new Port();
            foreach (var paragraph in Parser.ParseParagraph(filepath))
            {
                if (paragraph.Keys.Contains(SourceToken))
                {
                    port.CoreParagraph = new SourceParagraph() { Name = paragraph[SourceToken] };
                    foreach (var item in paragraph)
                        switch (item.Key)
                        {
                            case "Version": port.CoreParagraph.Version = item.Value; break;
                            case "Build-Depends": port.CoreParagraph.Depends = DotSplit(item.Value); break;
                            case "Description": port.CoreParagraph.Description = item.Value; break;
                            case "Maintainer": port.CoreParagraph.Maintainer = item.Value; break;
                            case "Supports": throw new NotSupportedException();
                            case "Default-Features": throw new NotSupportedException();
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
                            case "Build-Depends": feature.Depends = DotSplit(item.Value); break;
                            case "Description": feature.Description = item.Value; break;
                        }
                }
                else throw new FormatException("Unknown paragraph type");
            }

            return port;
        }

        public static List<Port> ParsePortsFolder(string folderpath)
        {
            var result = new List<Port>();
            foreach (var dir in Directory.GetDirectories(folderpath))
            {
                var controlFile = Path.Combine(dir, "CONTROL");
                if (!File.Exists(controlFile))
                    throw new FileNotFoundException($"Control file for {Path.GetFileName(dir)} is not found!");
                result.Add(ParseControlFile(controlFile));
            }
            return result;
        }

        private readonly static Dictionary<string, InstallState> InstallStateParser = new Dictionary<string, InstallState>()
        {
            {"not-installed", InstallState.NotInstalled },
            {"half-installed", InstallState.HalfInstalled },
            {"installed", InstallState.Installed }
        };

        public static List<StatusParagraph> ParseStatus(string statusFile)
        {
            var result = new List<StatusParagraph>();
            foreach (var paragraph in ParseParagraph(statusFile))
            {
                var status = new StatusParagraph();
                foreach (var item in paragraph)
                {
                    switch (item.Key)
                    {
                        case "Package": status.Package = item.Value; break;
                        case "Version": status.Version = item.Value; break;
                        case "Architecture": status.Architecture = item.Value; break;
                        case "Multi-Arch": status.MultiArch = item.Value; break;
                        case "Depends": status.Depends = DotSplit(item.Value); break;
                        case "Description": status.Description = item.Value; break;
                        case "Status":
                            var strs = item.Value.Split(' ');
                            status.Want = (Want)Enum.Parse(typeof(Want), strs[0], true);
                            status.State = InstallStateParser[strs[2]];
                            break;
                    }
                }
                result.Add(status);
            }
            return result;
        }
    }


    [DebuggerDisplay("{Name}")]
    public sealed class Port
    {
        public string Name => CoreParagraph?.Name;
        public SourceParagraph CoreParagraph { get; set; }
        public List<FeatureParagraph> FeatureParagraphs { get; set; }
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

    [DebuggerDisplay("{Package}:{Architecture}")]
    public sealed class StatusParagraph
    {
        public string Package { get; set; }
        public string Version { get; set; }
        public string Architecture { get; set; }
        public string MultiArch { get; set; }
        public string Description { get; set; }
        public string[] Depends { get; set; }
        public Want Want { get; set; }
        public InstallState State { get; set; }
    }

    public enum InstallState
    {
        ErrorState = 0,
        NotInstalled,
        HalfInstalled,
        Installed
    }

    public enum Want
    {
        ErrorState = 0,
        Unknown,
        Install,
        Hold,
        Deinstall,
        Purge
    };
}
