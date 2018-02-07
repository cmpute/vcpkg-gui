using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
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

            // Check if Vcpkg is downloaded
            var vcpkg_path = EnvironmentChecker.GetVcpkgRoot();
            if (string.IsNullOrEmpty(vcpkg_path))
                new FindPackageDialog("vcpkg") { WindowStartupLocation = WindowStartupLocation.CenterScreen }
                    .ShowDialog();

            // Check
        }
    }
}
