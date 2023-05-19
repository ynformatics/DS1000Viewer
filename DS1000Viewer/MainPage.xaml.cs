
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;


namespace DS1000Viewer
{
    class Channel
    {
        public string name;
        public bool on;
        public double yorig;
        public int yref;
        public double yinc;
        public double yscale;
        public string preamble;
        public List<double> trace;
        public double offset = 0;

        public Channel(string name)
        {
            this.name = name;
        }
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly ChartRenderer _chartRenderer;

        readonly DS1054 _ds = new DS1054();

        double _xoffset;
        double _timebase = 1;
        double _timOffs;

        DispatcherTimer timer = new DispatcherTimer();
        DispatcherTimer initTimer = new DispatcherTimer();

        Channel[] channels = { new Channel("CHAN1"), new Channel("CHAN2"), new Channel("CHAN3"), new Channel("CHAN4") };

        DragManager drag;

        int currentSourcedChannel = 0;      

        bool turboMode = true;

        public MainPage()
        {
            this.InitializeComponent();
            _chartRenderer = new ChartRenderer();
            drag = new DragManager(canvas);
            image.LayoutUpdated += Image_LayoutUpdated;

            canvas.PointerPressed += Canvas_PointerPressed;
            canvas.PointerMoved += Canvas_PointerMoved;
            canvas.PointerReleased += Canvas_PointerReleased;

            offset.PointerPressed += Offset_PointerPressed;
            offset.PointerMoved += Offset_PointerMoved;
            offset.PointerReleased += Offset_PointerReleased;

            initTimer.Tick += InitTimer_Tick;
            initTimer.Start();
        }

       

        private async void InitTimer_Tick(object sender, object e)
        {
            initTimer.Stop();

            try
            {
                System.Diagnostics.Debug.WriteLine("DS1000Viewer Starting");
                var localSettings = ApplicationData.Current.LocalSettings;
                if (localSettings.Values["turboMode"] == null)
                    localSettings.Values["turboMode"] = true;
                turboMode = (bool)localSettings.Values["turboMode"];
                SetupTurbo();

                object ipAddress = localSettings.Values["ipAddress"];
                int port = 5555;

                while (ipAddress == null || !_ds.Connect(ipAddress as string, port))
                {
                    var dlg = new SettingsDialog();
                    try { dlg.XamlRoot = grid.XamlRoot; } catch { };
                    await dlg.ShowAsync();

                    if (dlg.Result == ContentDialogResult.None)
                    {
                        Application.Current.Exit();
                    }

                    localSettings.Values["ipAddress"] = dlg.IPAddress;

                    ipAddress = localSettings.Values["ipAddress"];
                }

                System.Diagnostics.Debug.WriteLine(_ds.Identify());

                _ds.Send(":WAV:SOUR CHAN1");
                currentSourcedChannel = 0;
                _ds.Send(":WAV:MODE NORM");
                _ds.Send(":WAV:FORM BYTE");
 
                canvas.Draw += CanvasControl_Draw;
                timer.Interval = TimeSpan.FromMilliseconds(100);
                timer.Tick += Timer_Tick;
                timer.Start();
            }
            catch
            {
                initTimer.Start();
            }
        }
        private async void Timer_Tick(object sender, object e)
        {
            if (turboMode)
                canvas.Invalidate();
            else
                await updateImage();
        }
        private void Offset_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            drag.Start(e);
        }

        private void Offset_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            drag.Update(e, () =>
            {
                _xoffset = (float)drag.Dx;

                canvas.Invalidate();
            });
        }
        private void Offset_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            drag.Stop(e, () =>
            {
                _xoffset = 0;
                _timOffs -= (float)drag.Dx / (float)canvas.ActualWidth * 12 * _timebase;

                _ds.Send($":TIM:OFFS {_timOffs}");

                canvas.Invalidate();
            });
        }
        int GetChannelNearestPoint(PointerPoint point)
        {
            int x = (int)(point.Position.X / (float)canvas.ActualWidth * 1200);
            double y = 1.0 - point.Position.Y / canvas.ActualHeight;

            double near = double.MaxValue;
            int nearest = 0;

            for (int i = 0; i < 4; i++)
            {
                if (channels[i].trace.Count < x)
                {
                    continue;
                }

                var distance = Math.Abs(y - channels[i].trace[x]);
                if (distance < near)
                {
                    nearest = i;
                    near = distance;
                }
            }
            return nearest;
        }

        private void Canvas_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            drag.Start(e);
            drag.Channel = GetChannelNearestPoint(drag.StartPoint);
        }

        private void Canvas_PointerMoved(object sender, PointerRoutedEventArgs e)
        {      
            drag.Update(e, () =>
            {
                channels[drag.Channel].offset = (float)drag.Dy / (float)canvas.ActualHeight;

                canvas.Invalidate();
            });        
        }

      
        private void Canvas_PointerReleased(object sender, PointerRoutedEventArgs e)
        {         
            drag.Stop(e, () =>
            {
                channels[drag.Channel].yorig -= (float)drag.Dy / (float)canvas.ActualHeight * 8f * channels[drag.Channel].yscale;
                channels[drag.Channel].offset = 0;

                _ds.Send($":{channels[drag.Channel].name}:OFFS {channels[drag.Channel].yorig}");

                canvas.Invalidate();
            });          
        }

        private void Image_LayoutUpdated(object sender, object e)
        {
            if (image.ActualWidth > 1 && image.ActualHeight > 1)
            {
                canvas.Margin = new Thickness(
                  image.ActualWidth * 84.0 / 800 + image.ActualOffset.X, //left
                  image.ActualHeight * 37.0 / 480 + image.ActualOffset.Y, //top
                  image.ActualWidth * 116.0 / 800 + image.ActualOffset.X, //right
                  image.ActualHeight * 40.0 / 480 + image.ActualOffset.Y //bottom
                  );

                y.Margin = new Thickness(
                   image.ActualOffset.X, //left
                   image.ActualHeight * 450.0 / 480 + image.ActualOffset.Y, //top
                   image.ActualOffset.X, //right
                   image.ActualOffset.Y //bottom
                   );

                x.Margin = new Thickness(
                   image.ActualOffset.X, //left
                   image.ActualOffset.Y, //top
                   image.ActualOffset.X, //right
                   image.ActualHeight * 450.0 / 480 + image.ActualOffset.Y //bottom
                   );
            }
        }
        void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            try
            {
                if (!drag.IsDragging)
                {
                    for (int chan = 0; chan < 4; chan++)
                    {
                        if (!_ds.ChannelOn(chan + 1))
                        {
                            channels[chan].on = false;
                            channels[chan].trace = new List<double>();
                            continue;
                        }

                        channels[chan].on = true;
                        if (chan != currentSourcedChannel)
                        {
                            _ds.Send($":WAV:SOUR {channels[chan].name}");
                            currentSourcedChannel = chan;
                        }
                        var pre = _ds.GetPreamble();

                        var yref = 127; // always 127 in NORM mode

                        if (pre != channels[chan].preamble)
                        {
                            channels[chan].preamble = pre;
                            _ = updateImage();                    
                        }
                        var preItems = pre.Split(',');

                        channels[chan].yinc = double.Parse(preItems[7]);
                        channels[chan].yorig = double.Parse(preItems[8]) * channels[chan].yinc;
                        channels[chan].yref = yref;
                        _timebase = double.Parse(preItems[4]) * 100;
                        var data = _ds.GetWaveformData();

                        channels[chan].trace = new List<double>();

                        channels[chan].yscale = _ds.GetYScale(chan + 1); // volts
                        for (int i = 0; i < data.Length; i++)
                            channels[chan].trace.Add((data[i] - channels[chan].yref) * channels[chan].yinc / (8 * channels[chan].yscale) + 0.5);
                    }
                }

                args.DrawingSession.Clear(Colors.Black);
                _chartRenderer.RenderGrid(canvas, args);
                _chartRenderer.RenderData(canvas, args, channels.Select(c => c.trace).ToArray(), channels.Select(c => c.offset).ToArray(), _xoffset);
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Reinitialising");
                timer.Stop();
                initTimer.Start();
            }
        }
        async Task updateImage()
        {
            image.Source = await _ds.ScreenCapture();
        }
        private void Chan1Scale_Click(object sender, RoutedEventArgs e)
        {
            string val = (sender as MenuFlyoutItem).Tag as string;
            ScaleChannel(0, val);
        }
        private void Chan2Scale_Click(object sender, RoutedEventArgs e)
        {
            string val = (sender as MenuFlyoutItem).Tag as string;
            ScaleChannel(1, val);
        }
        private void Chan3Scale_Click(object sender, RoutedEventArgs e)
        {
            string val = (sender as MenuFlyoutItem).Tag as string;
            ScaleChannel(2, val);
        }
        private void Chan4Scale_Click(object sender, RoutedEventArgs e)
        {
            string val = (sender as MenuFlyoutItem).Tag as string;
            ScaleChannel(3, val);
        }
        private async void ScaleChannel(int channel, string val)
        {
            channels[channel].yscale = double.Parse(val);
            _ds.Send($":{channels[channel].name}:SCAL {val}");
            await updateImage();
        }
        private async void Timebase_Click(object sender, RoutedEventArgs e)
        {
            string val = (sender as MenuFlyoutItem).Tag as string;
            _timebase = double.Parse(val);
            _ds.Send($"TIM:SCAL {val}");
            await updateImage();
        }
        private async void Settings_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsDialog();
          
            dlg.XamlRoot = grid.XamlRoot;
            
            await dlg.ShowAsync();
        }
        private async void Channel_Click(object sender, RoutedEventArgs e)
        {
            string channel = (sender as Button).Tag as string;
            int chan = int.Parse(channel) - 1;
            string on = channels[chan].on ? "0" : "1";

            _ds.Send($":{channels[chan].name}:DISP {on}");
            await updateImage();
        }
      
        private async void Scope_SaveImage(object sender, object e)
        {
            FileSavePicker save = new FileSavePicker();
         
            save.FileTypeChoices.Add("png", new List<string> { ".png" });
            save.FileTypeChoices.Add("jpeg", new List<string> { ".jpeg", ".jpg" });
            save.FileTypeChoices.Add("tiff", new List<string> { ".tif", ".tiff" });
            // Open a stream for the selected file 
            StorageFile file = await save.PickSaveFileAsync();
            // Ensure a file was selected 
            if (file != null)
            {
                string format = string.Empty;
                switch (file.FileType)
                {
                    case ".png":
                        format = "PNG";
                        break;
                    case ".jpeg":
                    case ".jpg":
                        format = "JPEG";
                        break;
                    case ".tif":
                    case ".tiff":
                        format = "TIFF";
                        break;
                }
                if (string.IsNullOrEmpty(format))
                    return;

                using (var fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.ReadWrite))
                {
                    var os = fileStream.AsStreamForWrite();
                    var bytes = _ds.SendRaw("DISP:DATA? ON,0,{format}");
                    await os.WriteAsync(bytes, 0, bytes.Length);
                    await os.FlushAsync();
                }
            }
        }

        private async void Scope_CopyImage(object sender, object e)
        {
            var bytes = _ds.SendRaw("DISP:DATA? ON,0,PNG");

            var ms = new MemoryStream(bytes);
            var imageDecoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
            var imras = new InMemoryRandomAccessStream();
            var imageEncoder = await BitmapEncoder.CreateForTranscodingAsync(imras, imageDecoder);
            await imageEncoder.FlushAsync();

            DataPackage dataPackage = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(imras));

            Clipboard.SetContent(dataPackage);
        }

        private  void Scope_Turbo(object sender, object e)
        {
            turboMode = turbo.Text == "Turbo Mode On";
         
            SetupTurbo();
        }

        private void SetupTurbo()
        {
            if (turboMode)
            {
                turbo.Text = "Turbo Mode Off";
                timer.Interval = TimeSpan.FromMilliseconds(100);
                canvas.Visibility = Visibility.Visible;
                ApplicationData.Current.LocalSettings.Values["turboMode"] = true;
            }
            else
            {
                turbo.Text = "Turbo Mode On";
                timer.Interval = TimeSpan.FromMilliseconds(1000);
                canvas.Visibility = Visibility.Collapsed;
                ApplicationData.Current.LocalSettings.Values["turboMode"] = false;
            }
        }
    }
}
