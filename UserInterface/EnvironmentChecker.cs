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
        public const string VcpkgName = "Vcpkg";
        public const string GitName = "Git";

        /// <summary>
        /// Find the path to Git
        /// </summary>
        /// <returns>The path of Git. If not found, then <c>null</c> is returned.</returns>
        public static string GetGit()
        {
            try { return Environment.GetEnvironmentVariable("Path").Split(';').First(CheckGit); }
            catch { return null; }
        }

        /// <summary>
        /// Check whether git exists in given path.
        /// </summary>
        /// <param name="path">path to check git existance</param>
        /// <returns>whether git exist</returns>
        public static bool CheckGit(string path)
        {
            const string gitfile = "git.exe";
            if (path.IndexOf("git", StringComparison.InvariantCultureIgnoreCase) >= 0)
                if (File.Exists(Path.Combine(path, gitfile)))
                    return true;
            return false;
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

        /// <summary>
        /// Check whether vcpkg exists in given path.
        /// </summary>
        /// <param name="path">path to check vcpkg existance</param>
        /// <returns>whether vcpkg exist</returns>
        public static bool CheckVcpkgRoot(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Directory.GetFiles(path).Any(fname => Path.GetFileName(fname) == ".vcpkg-root");
        }

        /// <summary>
        /// Check whether vcpkg is compiled.
        /// </summary>
        /// <param name="path">vcpkg path</param>
        /// <returns>whether vcpkg is compiled</returns>
        public static bool CheckVcpkgCompiled(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Directory.GetFiles(path).Any(fname => Path.GetFileName(fname) == "vcpkg.exe");
        }
    }
}
