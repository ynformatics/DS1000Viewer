using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Controls;

namespace DS1000Viewer
{
    public enum SignInResult
    {
        SignInOK,
        SignInFail,
        SignInCancel,
        Nothing
    }

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

            // If the user name is saved, get it and populate the user name field.
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (localSettings.Values.ContainsKey("ipAddress"))
            {
                ipAddress.Text = localSettings.Values["ipAddress"].ToString();
            }
           
        }

        void SettingsDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            // If sign in was successful, save or clear the user name based on the user choice.
            if (this.Result == ContentDialogResult.Primary)
            {
                Windows.Storage.ApplicationData.Current.LocalSettings.Values["ipAddress"] = ipAddress.Text;           
            }
        }      
    }
}