using System;
using System.Collections.Generic;
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
            this.Close();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog() {
                IsFolderPicker = true
            };
            var result = dialog.ShowDialog(new WindowInteropHelper(this).Handle);
        }

        private void Confirm_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
