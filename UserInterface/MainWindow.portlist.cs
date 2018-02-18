using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Vcpkg
{
    public partial class MainWindow
    {
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
            foreach (var item in PortsList.SelectedItems)
            {
                var port = item as Port;
                var features = CheckedFeatures?.Where(feat => feat.CoreName == port.Name)
                               ?? Enumerable.Empty<FeatureParagraph>();
                if (features.Count() == 0)
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
                var code = ExecutionDialog.RunVcpkg("install " + string.Join(" ", pkgs), out string result, true);
            }
        }

        private void MenuEdit_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PortsList.SelectedItem as Port).Name;
            var code = ExecutionDialog.RunVcpkg("edit " + pkg, out string _, wait: false);
        }

        private void Feature_Checked(object sender, RoutedEventArgs e)
            => CheckedFeatures.Add((sender as CheckBox).DataContext as FeatureParagraph);

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
                var code = ExecutionDialog.RunVcpkg("remove " + string.Join(" ", pkgs), out string result, true);
            }
        }

        private void MenuPackageEdit_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PackagesList.SelectedItem as StatusParagraph).Package;
            var code = ExecutionDialog.RunVcpkg("edit " + pkg, out string _, wait: false);
        }

        private void MenuPackageEditBT_Click(object sender, RoutedEventArgs e)
        {
            var pkg = (PackagesList.SelectedItem as StatusParagraph).Package;
            var code = ExecutionDialog.RunVcpkg("edit " + pkg + " --buildtrees", out string _, wait: false);
        }

        #endregion
    }
}
