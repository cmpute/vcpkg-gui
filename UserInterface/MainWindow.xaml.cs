using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Interop;

namespace Vcpkg
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        private bool needInit = true;
        protected override void OnActivated(EventArgs e)
        {
            if (needInit)
            {
                new Thread(new ThreadStart(() =>
                {
                    // Load Data
                    ShowLoading = Visibility.Visible;
                    ParseVersion();
                    ParsePorts();
                    ParseTriplets();
                    ParseInstalledPackages();
                    ShowLoading = Visibility.Collapsed;
                })).Start();
                needInit = false;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            Environment.Exit(0); // dispose dummy window here
        }

        #region Integration

        public static int RunVcpkg(string arguments, out string output, bool useShell = false, bool wait = true)
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = Path.Combine(Properties.Settings.Default.vcpkg_path, "vcpkg.exe"),
                Arguments = arguments,
                WorkingDirectory = Properties.Settings.Default.vcpkg_path,
                UseShellExecute = useShell,
                CreateNoWindow = !useShell,
                RedirectStandardOutput = !useShell
            };
            var process = Process.Start(info);
            output = null;
            if (wait)
            {
                process.WaitForExit();
                if(!useShell)
                    output = process.StandardOutput.ReadToEnd();
                return process.ExitCode;
            }
            else { return 0; }
        }

        private void ParseVersion()
        {
            RunVcpkg("version", out string output);
            var vEnd = output.IndexOf(Environment.NewLine);
            var vStart = output.LastIndexOf(' ', vEnd);
            var vstr = output.Substring(vStart, vEnd - vStart).Trim();
            var splitHead = vstr.IndexOf('-');
            var splitEnd = vstr.LastIndexOf('-');
            Version = vstr.Substring(0, splitHead);
            BuildDate = vstr.Substring(splitHead + 1, splitEnd - splitHead - 1);
            BuildHash = vstr.Substring(splitEnd + 1);
        }

        private void ParsePorts()
        {
            AllPorts = Parser.ParsePortsFolder(Path.Combine(Properties.Settings.Default.vcpkg_path, "ports"));
        }

        private void ParseTriplets()
        {
            RunVcpkg("help triplet", out string output);
            foreach(var line in output.Split(new string[] { Environment.NewLine },
                                             StringSplitOptions.RemoveEmptyEntries).Skip(1))
                Dispatcher.Invoke(new Action<string>(AddTriplet), line.Trim());
        }
        private void AddTriplet(string triplet)
        {
            var newitem = new MenuItem()
            {
                Header = triplet,
                IsCheckable = true,
            };
            newitem.Checked += MenuTriplet_Checked;
            newitem.Unchecked += MenuTriplet_UnChecked;
            if (triplet == DefaultTriplet) newitem.IsChecked = true;
            TripletsMenu.Items.Insert(0, newitem);
            TripletMenuItems.Add(triplet, newitem);
        }

        private void ParseInstalledPackages()
        {
            RunVcpkg("list png", out string _); // Run vcpkg list to execute database_load_check() method in order to update list.
            PackageStatus = Parser.ParseStatus(Path.Combine(Properties.Settings.Default.vcpkg_path, "installed", "vcpkg", "status"));
        }

        #endregion
        #region Event Handlers

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var addr = (sender as Hyperlink).NavigateUri.ToString();
            if (addr.IndexOf("://") < 0)
                addr = Path.Combine(Properties.Settings.Default.vcpkg_path, addr);
            Process.Start(addr);
        }

        private void MenuTriplet_Checked(object sender, RoutedEventArgs e)
            => CheckedTriplet = (sender as MenuItem).Header.ToString();

        private void MenuTriplet_UnChecked(object sender, RoutedEventArgs e)
        {
            if(!MenuTripletSet)
                // disable manual unchecking
                (sender as MenuItem).IsChecked = true;
        }

        private void IntegrateInstall_Click(object sender, RoutedEventArgs e)
        {
            if (RunVcpkg("integrate install", out string output) == 0)
                MessageBox.Show(output, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("User-wide integration for this vcpkg root is failed!", "Failure",
                                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void IntegrateRemove_Click(object sender, RoutedEventArgs e)
        {
            if (RunVcpkg("integrate remove", out string output) == 0)
                MessageBox.Show(output, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Failed to remove user-wide integration for this vcpkg root!", "Failure",
                                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void IntegratePowerShell_Click(object sender, RoutedEventArgs e)
        {
            if (RunVcpkg("integrate powershell", out string output) == 0)
                MessageBox.Show(output, "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            else
                MessageBox.Show("Failed to integrate PowerShell tab completion for this vcpkg root!", "Failure",
                                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void MenuShowFullDescription_Checked(object sender, RoutedEventArgs e)
            => DescriptionHeight = double.PositiveInfinity;

        private void MenuShowFullDescription_Unchecked(object sender, RoutedEventArgs e)
            => DescriptionHeight = 40;

        private void MenuNewtriplet_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Show window to set name and then create file and open the editor.
        }

        private void MenuHash_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog()
            {
                EnsureFileExists = true,
                Title = "Select file to hash"
            };
            var result = dialog.ShowDialog(new WindowInteropHelper(this).Handle);
            if (result != CommonFileDialogResult.Ok) return;
            RunVcpkg("hash " + dialog.FileName.Replace('\\', '/'), out string hash);
            Clipboard.SetDataObject(hash, true);
            MessageBox.Show("SHA512 Hash result is copied to clipboard:\n" + hash, "Hash Result", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion
    }
}
