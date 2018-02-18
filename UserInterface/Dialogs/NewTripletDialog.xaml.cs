using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;

namespace Vcpkg
{
    /// <summary>
    /// NewTripletDialog.xaml 的交互逻辑
    /// </summary>
    public partial class NewTripletDialog : Window
    {
        public NewTripletDialog()
        {
            InitializeComponent();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            var tripletpath = Path.Combine((Application.Current as App).VcpkgRootPath, "triplets");
            var file = Path.Combine(tripletpath, TripletName.Text) + ".cmake";
            if (TripletName.Text.Intersect(Path.GetInvalidFileNameChars()).Count() > 0)
                MessageBox.Show("Specified triplet name is not valid. Please create a path-friendly name", "Invalid triplet name", MessageBoxButton.OK, MessageBoxImage.Error);
            File.WriteAllText(file, string.Empty);

            if (CheckOpenFolder.IsChecked ?? false) Process.Start(tripletpath);
            if (CheckOpenEditor.IsChecked ?? false) Process.Start(file);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
