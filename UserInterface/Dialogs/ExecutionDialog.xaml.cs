using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Path = System.IO.Path;

namespace Vcpkg
{
    /// <summary>
    /// ExecutionDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ExecutionDialog : Window
    {
        Process _process;
        bool _isRunning;
        int closeCounter = 10;
        DispatcherTimer closeTimer;
        public int ExitCode = 0;

        public ExecutionDialog(Process command, bool wait, bool parseProgress = false)
        {
            InitializeComponent();

            CommandBox.Text = "vcpkg " + command.StartInfo.Arguments;
            command.OutputDataReceived += Process_OutputDataReceived;
            command.ErrorDataReceived += Process_ErrorDataReceived;
            command.Start(); _isRunning = true;
            command.BeginOutputReadLine();
            command.BeginErrorReadLine();
            _process = command;

            if(wait) new Thread(new ThreadStart(WaitforExit)).Start();
            if (parseProgress) ProgressBar.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Run vcpkg tool with certain arguments
        /// </summary>
        /// <param name="arguments">The arguments passed to vcpkg</param>
        /// <param name="output">The output of the execution (<c>null</c> if wait is <c>false</c>)</param>
        /// <param name="useShell">Whether the output of the exection is catched into a shell</param>
        /// <param name="wait">Whether wait until the exection ends</param>
        /// <returns>The exit code of the execution</returns>
        public static int RunVcpkg(string arguments, out string output, bool useShell = false, bool wait = true)
        {
            if ((Application.Current as App).DebugVcpkg)
                arguments += " --debug";

            ProcessStartInfo info = new ProcessStartInfo()
            {
                FileName = Path.Combine(Properties.Settings.Default.vcpkg_path, "vcpkg.exe"),
                Arguments = arguments,
                WorkingDirectory = Properties.Settings.Default.vcpkg_path,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var process = new Process() { StartInfo = info };
            if (wait)
            {
                if (useShell)
                {
                    var dialog = new ExecutionDialog(process, true);
                    dialog.ShowDialog();
                    output = null;
                    return dialog.ExitCode;
                }
                else
                {
                    process.Start();
                    process.WaitForExit();
                    output = process.StandardOutput.ReadToEnd();
                    return process.ExitCode;
                }
            }
            else 
            {
                if (useShell)
                    new ExecutionDialog(process, false).Show();
                else process.Start();
                output = null;
                return 0;
            }
        }

        private void Process_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action<string>(AppendText), e.Data);
        }

        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action<string>(AppendText), e.Data);
            // TODO: Add regex to find "packages x/x" and "...done" to compute the progress
        }

        private void AppendText(string text) => Output.Text += text + Environment.NewLine;

        private void WaitforExit()
        {
            _process.WaitForExit();
            _isRunning = false;
            Dispatcher.BeginInvoke(new Action<string>(AppendText), "------------------------------\r\nExecution Finished");

            // Close after 10 seconds
            closeTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, new EventHandler(DownCountClose), Dispatcher);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                CancelButton.Width = 160;
                CancelButton.Content = $"Close after {closeCounter} seconds";
            }));
            closeTimer.Start();
        }

        private void DownCountClose(object obj, EventArgs e)
        {
            if (closeCounter > 0)
                CancelButton.Content = $"Close after {closeCounter--} seconds";
            else { closeTimer.Stop(); Close(); }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if(_isRunning) _process.Kill();
            // TODO: Refresh packages list when command finished (or manually refresh)
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CancelButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if(closeTimer != null)
            {
                closeTimer.Stop();
                CancelButton.Width = 60;
                CancelButton.Content = "Close";
            }
        }
    }
}
