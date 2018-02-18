using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Vcpkg
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Window dummy; // Prevent application exit when FindpackageDialog is closed
            var settings = Vcpkg.Properties.Settings.Default;
#if DEBUG
            settings.Reset();
#endif

            // Check if Git is installed
            var gitPath = EnvironmentChecker.GetGit();
            if (string.IsNullOrEmpty(gitPath))
            {
                dummy = new Window();
                if (!new FindPackageDialog(EnvironmentChecker.GitName).ShowDialog() ?? false)
                    Environment.Exit(1);
            }

            // Check if Vcpkg is downloaded
            var getVcpkg = string.IsNullOrEmpty(settings.vcpkg_path);
            if (!getVcpkg && !EnvironmentChecker.CheckVcpkg(settings.vcpkg_path))
                getVcpkg = true;

            if (getVcpkg)
            {
                var vcpkgPath = EnvironmentChecker.GetVcpkg();
                if (string.IsNullOrEmpty(vcpkgPath))
                {
                    dummy = new Window();
                    if (!new FindPackageDialog(EnvironmentChecker.VcpkgName).ShowDialog() ?? false)
                        Environment.Exit(1);
                    vcpkgPath = Vcpkg.Properties.Settings.Default.vcpkg_path;
                }
                else Vcpkg.Properties.Settings.Default.vcpkg_path = vcpkgPath;
            }

            // Check if Vcpkg is updated and compiled
            bool compile = false;
            var git = new Process();
            git.StartInfo.FileName = "git";
            git.StartInfo.WorkingDirectory = settings.vcpkg_path;
            git.StartInfo.UseShellExecute = false;
            git.StartInfo.CreateNoWindow = true;
            git.StartInfo.RedirectStandardOutput = true;

            git.StartInfo.Arguments = "status";
            git.Start(); git.WaitForExit();
            var output = git.StandardOutput.ReadToEnd();
            if(output.IndexOf("branch is behind") >= 0)
            {
                if(MessageBox.Show("Your vcpkg repository in not up-to-date, would you like to update and recompile vcpkg?",
                                    "Update vcpkg",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    git.StartInfo.Arguments = "pull";
                    git.Start(); git.WaitForExit();
                    compile = true;
                }
            }

            if(!compile && !EnvironmentChecker.CheckVcpkgCompiled(settings.vcpkg_path))
            {
                MessageBox.Show("Your vcpkg is not initialized. Compiling is going to be executed.",
                                "Initialize vcpkg",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                compile = true;
            }

            while (compile)
            {
                // Parsed from boostrap.bat
                var pspath = Path.Combine(settings.vcpkg_path, "scripts\\bootstrap.ps1");
                var initialize = Process.Start("powershell.exe", "-NoProfile -ExecutionPolicy Bypass \"& {& '" + pspath + "'}\"");
                initialize.WaitForExit();

                if (!EnvironmentChecker.CheckVcpkgCompiled(settings.vcpkg_path))
                {
                    if (MessageBox.Show("vcpkg is not initialized successfully. Would you like to retry? If not, then this application will be terminated.",
                                    "Initialize unsuccessfully",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning) == MessageBoxResult.No)
                        compile = false;
                }
                else compile = false;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            Vcpkg.Properties.Settings.Default.Save();
        }
    }
}
