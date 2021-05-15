using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace DS1000Viewer
{
    class DS1054 : Device
    {
        public DS1054() : base() { }
        public string GetPreamble()
        {
            return Send(":WAV:PRE?");
        }
        public double GetYScale(int chan)
        {
            return double.Parse(Send($":CHAN{chan}:SCAL?"));
        }
        public bool ChannelOn(int chan)
        {
            var ret = SendRaw($":CHAN{chan}:DISP?", false);

            return ret[0] == '1';
        }
        public byte[] GetWaveformData()
        {
            return SendRaw(":WAV:DATA?");
        }

        public async Task<BitmapImage> ScreenCapture()
        {
            var bytes = this.SendRaw("DISP:DATA? ON,0,PNG");

            var bitmapImage = new BitmapImage();

            using (var stream = new InMemoryRandomAccessStream())
            {
                using (var writer = new DataWriter(stream))
                {
                    writer.WriteBytes(bytes);
                    await writer.StoreAsync();
                    await writer.FlushAsync();
                    writer.DetachStream();
                }

                stream.Seek(0);
                bitmapImage.SetSource(stream);
            }

            return bitmapImage;
        }
    }
}
