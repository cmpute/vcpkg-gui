using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Vcpkg
{
    public partial class MainWindow
    {
        public List<FeatureParagraph> CheckedFeatures = new List<FeatureParagraph>();
        public Dictionary<string, MenuItem> TripletMenuItems = new Dictionary<string, MenuItem>();
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
                (Application.Current as App).Triplet = value;
            }
        }
        public static readonly DependencyProperty CheckedTripletProperty =
            DependencyProperty.Register("CheckedTriplet", typeof(string), typeof(MainWindow), new PropertyMetadata(App.DefaultTriplet));

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
        
        public string VcpkgPath
        {
            get { return (string)GetValue(VcpkgPathProperty); }
            set { SetValue(VcpkgPathProperty, value); }
        }
        public static readonly DependencyProperty VcpkgPathProperty =
            DependencyProperty.Register("VcpkgPath", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));

        public string VcpkgRootPath
        {
            get { return (string)GetValue(VcpkgRootPathProperty); }
            set { SetValue(VcpkgRootPathProperty, value); }
        }
        public static readonly DependencyProperty VcpkgRootPathProperty =
            DependencyProperty.Register("VcpkgRootPath", typeof(string), typeof(MainWindow), new PropertyMetadata(string.Empty));
    }

    delegate void SetValueDelegate(DependencyProperty obj, object val);
}
