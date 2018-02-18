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
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            Environment.Exit(0); // dispose dummy window here
        }

        #region Fields & Bindings

        public List<FeatureParagraph> CheckedFeatures = new List<FeatureParagraph>();
        public Dictionary<string, MenuItem> TripletMenuItems = new Dictionary<string, MenuItem>();
        const string DefaultTriplet = "x86-windows";
        private bool MenuTripletSet = false;

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { Dispatcher.Invoke(new SetValueDelegate(SetValue), VersionProperty, value); }
        }
        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(MainWindow), new PropertyMetadata("0.0.0"));

        public string BuildDate
        {
            get { return (string)GetValue(BuildDateProperty); }
            set { Dispatcher.Invoke(new SetValueDelegate(SetValue), BuildDateProperty, value); }
        }
        public static readonly DependencyProperty BuildDateProperty =
            DependencyProperty.Register("BuildDate", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public string BuildHash
        {
            get { return (string)GetValue(BuildHashProperty); }
            set { Dispatcher.Invoke(new SetValueDelegate(SetValue), BuildHashProperty, value); }
        }
        public static readonly DependencyProperty BuildHashProperty =
            DependencyProperty.Register("BuildHash", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public List<Port> AllPorts
        {
            get { return (List<Port>)GetValue(AllPortsProperty); }
            set { Dispatcher.Invoke(new SetValueDelegate(SetValue), AllPortsProperty, value); }
        }
        public static readonly DependencyProperty AllPortsProperty =
            DependencyProperty.Register("AllPorts", typeof(List<Port>), typeof(MainWindow), new PropertyMetadata(null));

        public double DescriptionHeight
        {
            get { return (double)GetValue(DescriptionHeightProperty); }
            set { SetValue(DescriptionHeightProperty, value); }
        }
        public static readonly DependencyProperty DescriptionHeightProperty =
            DependencyProperty.Register("DescriptionHeight", typeof(double), typeof(MainWindow), new PropertyMetadata((double)40));

        public string CheckedTriplet
        {
            get { return (string)GetValue(CheckedTripletProperty); }
            set
            {
                if (CheckedTriplet != value)
                {
                    MenuTripletSet = true;
                    TripletMenuItems[CheckedTriplet].IsChecked = false;
                    TripletMenuItems[value].IsChecked = true;
                    MenuTripletSet = false;
                }
                SetValue(CheckedTripletProperty, value);
            }
        }
        public static readonly DependencyProperty CheckedTripletProperty =
            DependencyProperty.Register("CheckedTriplet", typeof(string), typeof(MainWindow), new PropertyMetadata(DefaultTriplet));

        public List<StatusParagraph> PackageStatus
        {
            get { return (List<StatusParagraph>)GetValue(PackageStatusProperty); }
            set { Dispatcher.Invoke(new SetValueDelegate(SetValue), PackageStatusProperty, value); }
        }
        public static readonly DependencyProperty PackageStatusProperty =
            DependencyProperty.Register("PackageStatus", typeof(List<StatusParagraph>), typeof(MainWindow), new PropertyMetadata(null));

        public Visibility ShowLoading
        {
            get { return (Visibility)GetValue(ShowLoadingProperty); }
            set { Dispatcher.Invoke(new SetValueDelegate(SetValue), ShowLoadingProperty, value); }
        }
        public static readonly DependencyProperty ShowLoadingProperty =
            DependencyProperty.Register("ShowLoading", typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Visible));

        #endregion
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

        private void RefreshPortsView()
        {
            var source = (CollectionViewSource)FindResource("PortsSource");
            source.View.Refresh();
        }

        private void RefreshPackagesView()
        {
            var source = (CollectionViewSource)FindResource("PackagesSource");
            source.View.Refresh();
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
        #region Ports Page Event Handlers

        private void PortsSource_Filter(object sender, FilterEventArgs e)
        {
            var keyword = SearchBox.Text.Trim();
            if (string.IsNullOrEmpty(keyword))
            {
                e.Accepted = true;
                return;
            }

            Port port = e.Item as Port;
            if (port != null)
            {
                e.Accepted = port.Name.Contains(keyword);
                if (!NameOnlyCheckBox.IsChecked.Value)
                    e.Accepted = e.Accepted || (port.CoreParagraph.Description?.Contains(keyword) ?? false);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
            => RefreshPortsView();

        private void SearchClear_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
        }

        private void NameOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
            => RefreshPortsView();

        private void MenuInstall_Click(object sender, RoutedEventArgs e)
        {
            List<string> pkgs = new List<string>();
            foreach(var item in PortsList.SelectedItems)
            {
                var port = item as Port;
                var features = CheckedFeatures?.Where(feat => feat.CoreName == port.Name)
                               ?? Enumerable.Empty<FeatureParagraph>();
                if(features.Count() == 0)
                    pkgs.Add(port.Name);
                else
                {
                    var featstr = string.Join(",", features.Select(feat => feat.Name));
                    pkgs.Add($"{port.Name}[core,{featstr}]");
                }
            }

            if (MessageBox.Show("Installing following packages:\n" + string.Join("\n", pkgs) + "\nAre you sure?", "Confirm",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                // TODO: rebuild check. "--recursive" flag is needed for rebuild
                var code = RunVcpkg("install " + string.Join(" ", pkgs), out string result, true);
            }
        }

        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PortsList.SelectedItem as Port).Name;
            var code = RunVcpkg("edit " + pkg, out string _, wait: false);
        }

        private void Feature_Checked(object sender, RoutedEventArgs e)
            =>CheckedFeatures.Add((sender as CheckBox).DataContext as FeatureParagraph);

        private void Feature_Unchecked(object sender, RoutedEventArgs e)
            => CheckedFeatures.Remove((sender as CheckBox).DataContext as FeatureParagraph);

        #endregion
        #region Packages Page Event Handlers

        private void SearchInstalledBox_TextChanged(object sender, TextChangedEventArgs e)
            => RefreshPackagesView();

        private void SearchInstalledClear_Click(object sender, RoutedEventArgs e)
        {
            SearchInstalledBox.Text = string.Empty;
        }

        private void PackagesSource_Filter(object sender, FilterEventArgs e)
        {
            var keyword = SearchInstalledBox.Text.Trim();
            StatusParagraph status = e.Item as StatusParagraph;
            if (string.IsNullOrEmpty(keyword))
            {
                e.Accepted = status.State == InstallState.Installed;
                return;
            }

            if (status != null)
            {
                bool contain = status.Package.Contains(keyword);
                if (!PackageNameOnlyCheckBox.IsChecked.Value)
                    contain = contain || (status.Description?.Contains(keyword) ?? false);
                e.Accepted = (status.State == InstallState.Installed) && contain;
            }
        }

        private void PackageNameOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
            => RefreshPackagesView();

        private void MenuRemove_Click(object sender, RoutedEventArgs e)
        {
            List<string> pkgs = new List<string>();
            foreach (var item in PackagesList.SelectedItems)
            {
                var status = item as StatusParagraph;
                if (string.IsNullOrEmpty(status.Feature))
                    pkgs.Add($"{status.Package}:{status.Architecture}");
                else
                    pkgs.Add($"{status.Package}[{status.Feature}]:{status.Architecture}");
            }

            if (MessageBox.Show("Installing following packages:\n" + string.Join("\n", pkgs) + "\nAre you sure?", "Confirm",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                var code = RunVcpkg("remove " + string.Join(" ", pkgs), out string result, true);
            }
        }

        private void MenuPackageEdit_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PackagesList.SelectedItem as StatusParagraph).Package;
            var code = RunVcpkg("edit " + pkg, out string _, wait: false);
        }

        private void MenuPackageEditBT_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PackagesList.SelectedItem as StatusParagraph).Package;
            var code = RunVcpkg("edit " + pkg + " --buildtrees", out string _, wait: false);
        }

        #endregion
    }

    delegate void SetValueDelegate(DependencyProperty obj, object val);
}
