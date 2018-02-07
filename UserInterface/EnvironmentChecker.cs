using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Vcpkg
{
    /// <summary>
    /// Environment checkers to locate essential programs
    /// </summary>
    public class EnvironmentChecker
    {
        /// <summary>
        /// Find the path to Git
        /// </summary>
        /// <returns>The path of Git. If not found, then <c>null</c> is returned.</returns>
        public static string GetGit()
        {
            foreach (var val in Environment.GetEnvironmentVariable("Path").Split(';'))
            {
                const string gitfile = "git.exe";
                if (val.IndexOf("git", StringComparison.InvariantCultureIgnoreCase) >= 0)
                {
                    var gitpath = val.Last() == '\\' ? val + gitfile : val + '\\' + gitfile;
                    if (File.Exists(gitpath))
                        return gitpath;
                }
            }
            return null;
        }

        /// <summary>
        /// Find the path to vcpkg
        /// </summary>
        /// <returns>The path of vcpkg. If not found, then <c>null</c> is returned.</returns>
        public static string GetVcpkgRoot()
        {
            string[] paths = new string[]
            {
                Environment.CurrentDirectory,
                AppDomain.CurrentDomain.BaseDirectory,
                AppDomain.CurrentDomain.SetupInformation?.ApplicationBase,
                Directory.GetCurrentDirectory()
            };

            try { return paths.First(CheckVcpkgRoot); }
            catch { return null; }
        }

        public static bool CheckVcpkgRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Directory.GetFiles(path).Any(fname => fname.EndsWith("\\.vcpkg-root"));
        }
    }
}
