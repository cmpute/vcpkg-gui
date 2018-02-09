using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;

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

            // Load Data
            ParseVersion();
            ParsePorts();
            ParseTriplets();
        }

        #region Fields & Bindings
        
        public List<FeatureParagraph> CheckedFeatures = new List<FeatureParagraph>();
        public ObservableCollection<string> CheckedTriplets = new ObservableCollection<string>();

        public string Version
        {
            get { return (string)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public static readonly DependencyProperty VersionProperty =
            DependencyProperty.Register("Version", typeof(string), typeof(MainWindow), new PropertyMetadata("0.0.0"));

        public string BuildDate
        {
            get { return (string)GetValue(BuildDateProperty); }
            set { SetValue(BuildDateProperty, value); }
        }
        public static readonly DependencyProperty BuildDateProperty =
            DependencyProperty.Register("BuildDate", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public string BuildHash
        {
            get { return (string)GetValue(BuildHashProperty); }
            set { SetValue(BuildHashProperty, value); }
        }
        public static readonly DependencyProperty BuildHashProperty =
            DependencyProperty.Register("BuildHash", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

        public List<Port> AllPorts
        {
            get { return (List<Port>)GetValue(AllPortsProperty); }
            set { SetValue(AllPortsProperty, value); }
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

        public string CheckedTripletsString
        {
            get { return (string)GetValue(CheckedTripletsStringProperty); }
            set { SetValue(CheckedTripletsStringProperty, value); }
        }
        public static readonly DependencyProperty CheckedTripletsStringProperty =
            DependencyProperty.Register("CheckedTripletsString", typeof(string), typeof(MainWindow), new PropertyMetadata(""));

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
            AllPorts = Port.ParsePortsFolder(Path.Combine(Properties.Settings.Default.vcpkg_path, "ports"));
        }

        private void ParseTriplets()
        {
            CheckedTriplets.CollectionChanged += (sender, e) =>
                CheckedTripletsString = string.Join(", ", CheckedTriplets);

            RunVcpkg("help triplet", out string output);
            foreach(var line in output.Split(new string[] { Environment.NewLine },
                                             StringSplitOptions.RemoveEmptyEntries).Skip(1))
            {
                var tline = line.Trim();
                var newitem = new MenuItem()
                {
                    Header = tline,
                    IsCheckable = true,
                };
                newitem.Checked += MenuTriplet_Checked;
                newitem.Unchecked += MenuTriplet_UnChecked;
                if (tline == "x86-windows") newitem.IsChecked = true;
                TripletsMenu.Items.Add(newitem);
            }
        }

        private void RefreshView()
        {
            var source = (CollectionViewSource)FindResource("PortsSource");
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
            => RefreshView();

        private void SearchClear_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
        }

        private void NameOnlyCheckBox_Checked(object sender, RoutedEventArgs e)
            => RefreshView();

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
            var pkgstr = string.Join(" ", pkgs);

            if (MessageBox.Show("Installing following packages:\n" + pkgstr + "\nAre you sure?", "Confirm",
                                MessageBoxButton.OKCancel,
                                MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                // TODO: rebuild check. "--recursive" flag is needed for rebuild
                var code = RunVcpkg("install " + pkgstr, out string result, true);
            }
        }

        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PortsList.SelectedItem as Port).Name;
            var code = RunVcpkg("edit " + pkg, out string _, wait: false);
        }

        private void MenuShowFullDescription_Checked(object sender, RoutedEventArgs e)
            => DescriptionHeight = double.PositiveInfinity;

        private void MenuShowFullDescription_Unchecked(object sender, RoutedEventArgs e)
            => DescriptionHeight = 40;

        private void MenuTriplet_Checked(object sender, RoutedEventArgs e)
            => CheckedTriplets.Add((sender as MenuItem).Header.ToString());

        private void MenuTriplet_UnChecked(object sender, RoutedEventArgs e)
            => CheckedTriplets.Remove((sender as MenuItem).Header.ToString());

        private void Feature_Checked(object sender, RoutedEventArgs e)
            =>CheckedFeatures.Add((sender as CheckBox).DataContext as FeatureParagraph);

        private void Feature_Unchecked(object sender, RoutedEventArgs e)
            => CheckedFeatures.Remove((sender as CheckBox).DataContext as FeatureParagraph);

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

        #endregion
    }
}
