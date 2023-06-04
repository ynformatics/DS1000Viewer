using System;
using System.Threading;
using Windows.Storage;

namespace DS1000Viewer
{
    internal class Logger
    {
        static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        public static async void Log(string message)
        {
            await _semaphore.WaitAsync();
            try
            {
                var logFile = await ApplicationData.Current.TemporaryFolder
                    .CreateFileAsync($"{DateTime.Now:yyyy-MM-dd}.log", CreationCollisionOption.OpenIfExists);
                await FileIO.AppendTextAsync(logFile, $"{DateTime.Now:yyyy-MM-ddTHH:mm:ss.sss}\t{message}\n");
            }
            finally { _semaphore.Release(); }
        }
    }
}
