using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Controls;

namespace DS1000Viewer
{
    public sealed partial class SettingsDialog : ContentDialog
    {
        public ContentDialogResult Result { get; private set; }

        public SettingsDialog()
        {
            this.InitializeComponent();
            this.Opened += SettingsDialog_Opened;
            this.Closing += SettingsDialog_Closing;
        }

        public string IPAddress { get { return ipAddress.Text; } }
    
        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {         
            this.Result = ContentDialogResult.Primary;
        }

        private void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // User clicked Cancel, ESC, or the system back button.
            this.Result = ContentDialogResult.None;
        }

        void SettingsDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            this.Result = ContentDialogResult.None;

            PackageVersion pkgVersion = Package.Current.Id.Version;
            version.Text = $"DS1000 v{pkgVersion.Major}.{pkgVersion.Minor}.{pkgVersion.Build}";

            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("ipAddress"))
            {
                ipAddress.Text = localSettings.Values["ipAddress"].ToString();
            }
           
        }

        void SettingsDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (this.Result == ContentDialogResult.Primary)
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["ipAddress"] = ipAddress.Text;           
            }
        }      
    }
}