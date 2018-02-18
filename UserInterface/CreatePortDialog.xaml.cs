using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Vcpkg
{
    /// <summary>
    /// CreatePortDialog.xaml 的交互逻辑
    /// </summary>
    public partial class CreatePortDialog : Window
    {
        public CreatePortDialog()
        {
            InitializeComponent();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var code = ExecutionDialog.RunVcpkg($"create {PortName.Text} {ArchiveLink.Text} {ArchiveName.Text}", out string message);
            if (code == 0)
            {
                MessageBox.Show("Successfully create port " + PortName.Text, "Success",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                Close();
                // TODO: Refresh port list and add prompt to open editor.
            }
            else MessageBox.Show($"Failed to create port {PortName.Text}, message:\r\n{message}", "Failed",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
