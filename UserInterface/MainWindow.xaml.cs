using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
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
            ParseVersion();
            AllPorts = Port.ParsePortsFolder(Path.Combine(Properties.Settings.Default.vcpkg_path, "ports"));
        }

        #region Bindings

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

        #endregion

        public static int RunVcpkg(string arguments, out string output)
        {
            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = Path.Combine(Properties.Settings.Default.vcpkg_path, "vcpkg.exe"),
                Arguments = arguments,
                WorkingDirectory = Properties.Settings.Default.vcpkg_path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };
            var process = Process.Start(info);
            process.WaitForExit();
            output = process.StandardOutput.ReadToEnd();
            return process.ExitCode;
        }

        public void ParseVersion()
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

        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var addr = (sender as Hyperlink).NavigateUri.ToString();
            if (addr.IndexOf("://") < 0)
                addr = Path.Combine(Properties.Settings.Default.vcpkg_path, addr);
            Process.Start(addr);
        }
    }
}
