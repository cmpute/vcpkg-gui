using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Vcpkg
{
    /// <summary>
    /// FindPackageDialog.xaml 的交互逻辑
    /// </summary>
    public partial class FindPackageDialog : Window
    {
        public FindPackageDialog(string package)
        {
            InitializeComponent();
            DataContext = this;
            PackageName = package;

            switch (package)
            {
                case EnvironmentChecker.VcpkgName:
                    PackagePath = Properties.Settings.Default.vcpkg_path;
                    break;
            }
        }

        #region Bindings
        public string PackageName
        {
            get { return (string)GetValue(PackageNameProperty); }
            set { SetValue(PackageNameProperty, value); }
        }

        public static readonly DependencyProperty PackageNameProperty =
            DependencyProperty.Register("PackageName", typeof(string), typeof(FindPackageDialog), new PropertyMetadata("Module"));

        public string PackagePath
        {
            get { return (string)GetValue(PackagePathProperty); }
            set { SetValue(PackagePathProperty, value); }
        }

        public static readonly DependencyProperty PackagePathProperty =
            DependencyProperty.Register("PackagePath", typeof(string), typeof(FindPackageDialog), new PropertyMetadata(string.Empty));
        #endregion

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            var result = dialog.ShowDialog(new WindowInteropHelper(this).Handle);
            if (result != CommonFileDialogResult.Ok) return;
            PackagePath = dialog.FileName;
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(PackagePath))
            {
                MessageBox.Show("Specified path doesn't exist! Please select again!",
                                "Path doesn't exist",
                                MessageBoxButton.OK,
                                MessageBoxImage.Stop);
                return;
            }

            switch (PackageName)
            {
                case EnvironmentChecker.VcpkgName:
                    if (!EnvironmentChecker.CheckVcpkg(PackagePath))
                    {
                        MessageBox.Show("vcpkg doesn't exist in specified path! Please select again or press 'Download' to download it !",
                                        "vcpkg doesn't exist",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Stop);
                        return;
                    }
                    Properties.Settings.Default.vcpkg_path = PackagePath;
                    DialogResult = true;
                    Close();
                    break;
            }
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            switch (PackageName)
            {
                case EnvironmentChecker.GitName:
                    Process.Start("https://git-scm.com/download/win");
                    break;
                case EnvironmentChecker.VcpkgName:
                    if(!Directory.Exists(PackagePath))
                    {
                        MessageBox.Show("Specified path is invalid or missing! Please select path to download vcpkg!",
                                        "Select vcpkg download path",
                                        MessageBoxButton.OK,
                                        MessageBoxImage.Stop);
                        return;
                    }

                    var clone = Process.Start("git", "clone https://github.com/Microsoft/vcpkg.git " + PackagePath);
                    clone.WaitForExit();
                    switch(clone.ExitCode)
                    {
                        case 0:
                            if (!EnvironmentChecker.CheckVcpkg(PackagePath))
                            {
                                MessageBox.Show("vcpkg dosen't exist in clone directory! Please check you git configuration",
                                                "vcpkg is not donwloaded",
                                                MessageBoxButton.OK,
                                                MessageBoxImage.Stop);
                                return;
                            }
                            Properties.Settings.Default.vcpkg_path = PackagePath;
                            DialogResult = true;
                            Close();
                            break;
                        case 128:
                            MessageBox.Show("Downloading is terminated because the specified directory is not empty. Please select a null folder to use vcpkg!",
                                            "Specified directory is not empty",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Stop);
                            return;
                        default:
                            MessageBox.Show($"Git exited with code {clone.ExitCode}!",
                                            "Unknown Error",
                                            MessageBoxButton.OK,
                                            MessageBoxImage.Stop);
                            return;
                    }
                    break;
            }
        }
    }
}
