using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Vcpkg
{
    [DebuggerDisplay("{Name}")]
    public sealed class Port
    {
        private Port() { }
        public string Name => CoreParagraph.Name;
        public SourceParagraph CoreParagraph { get; set; }
        public List<FeatureParagraph> FeatureParagraphs { get; set; }

        public static Port ParseControlFile(string filepath)
        {
            const string SourceToken = "Source";
            const string FeatureToken = "Feature";

            var port = new Port();
            string token = null;
            FeatureParagraph current = null;
            foreach (var line in File.ReadAllText(filepath).Split(
                new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (line.StartsWith("#"))
                    continue;
                // FIXME: Description with multiple lines is ignored here, need fix
                var lsplit = line.Split(new string[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                switch (lsplit[0])
                {
                    case SourceToken:
                        token = SourceToken;
                        port.CoreParagraph = new SourceParagraph() { Name = lsplit[1] };
                        break;
                    case FeatureToken:
                        token = FeatureToken;
                        if (current != null)
                        {
                            if (port.FeatureParagraphs == null)
                                port.FeatureParagraphs = new List<Vcpkg.FeatureParagraph>();
                            port.FeatureParagraphs.Add(current);
                        }
                        current = new FeatureParagraph(port.Name) { Name = lsplit[1] };
                        break;
                    
                    // Fields
                    case "Version":
                        port.CoreParagraph.Version = lsplit[1];
                        break;
                    case "Build-Depends":
                        var depends = lsplit[1].Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
                        if (token == SourceToken)
                            port.CoreParagraph.Depends = depends;
                        else if (token == FeatureToken)
                            current.Depends = depends;
                        break;
                    case "Description":
                        if (token == SourceToken)
                            port.CoreParagraph.Description = lsplit[1];
                        else if (token == FeatureToken)
                            current.Description = lsplit[1];
                        break;
                    case "Maintainer":
                        port.CoreParagraph.Maintainer = lsplit[1];
                        break;
                    case "Supports":
                        throw new NotImplementedException();
                    case "Default-Features":
                        throw new NotImplementedException();
                }
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
    public class SourceParagraph
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string Maintainer { get; set; }
        public string[] Supports { get; set; }
        public string[] Depends { get; set; }
        public string[] DefaultFeatures { get; set; }
    }

    [DebuggerDisplay("{Name}")]
    public class FeatureParagraph
    {
        public FeatureParagraph(string coreName) => CoreName = coreName;
        public string CoreName { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string[] Depends { get; set; }
    }
}
