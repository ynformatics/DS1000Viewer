using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace DS1000Viewer
{
    class Device : IDisposable
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        bool connected = false;

        TcpClient tcpClient;
        NetworkStream stream;

        public Device()
        {
                    
        }
        public Device(string address, int port)
        {
            Connect(address, port);
            Console.Write(Identify());
        }

        
        public bool Connect(string address, int port)
        {
            try
            {
                Logger.Log($"connecting to {address}:{port}");
                this.IPAddress = address;
                this.Port = port;
                if (tcpClient != null && tcpClient.Connected)
                    tcpClient.Close();

                tcpClient = new TcpClient();// { SendTimeout = 200, ReceiveTimeout = 200 };
                if (!tcpClient.ConnectAsync(address, port).Wait(2000))
                {
                    Logger.Log("connect timeout");
                    connected = false;
                    return false;
                }
                stream = tcpClient.GetStream();
                connected = true;
                return true;
            }
            catch (Exception ex)
            {                
                Logger.Log(ex.ToString());
                connected = false;
                return false;
            }
        }

        public bool Connected { get { return connected; } }

        public string Identify()
        {
            return this.Send("*IDN?");
        }
        public string Send(string command, bool header = false)
        {
            string ret;
            try
            {
                var bytes = SendRaw(command, header);
                ret = System.Text.Encoding.ASCII.GetString(bytes);

               // if(command.IndexOf('?') >= 0)
                //    System.Diagnostics.Debug.WriteLine($"ret {ret}");
                return ret;
            }
            catch(Exception ex)
            { 
                System.Diagnostics.Debug.WriteLine("exception");
                Logger.Log(ex.ToString());
            }
            return string.Empty;
        }
       
        public byte[] SendRaw(string command, bool header = true)
        {
            long t0 = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (!connected)
                return null;

            bool hasResponse = command.IndexOf('?') >= 0;

            byte[] cmd = System.Text.Encoding.ASCII.GetBytes(command + "\n");
            stream.Write(cmd, 0, cmd.Length);

            byte[] output = new byte[250000];

            if (!hasResponse)
            {
                //System.Diagnostics.Debug.WriteLine($"{DateTimeOffset.Now.ToUnixTimeMilliseconds() - t0,4} {command}");
                return new byte[0];
            }

            var response = new List<byte>();
            bool firstChunk = true;
            int length = 0;
            int totalRead = 0;

            bool done = false;
            do
            {
                int timeout = 50;
                while (!stream.DataAvailable && !header && timeout-- > 0)
                {
                    Thread.Sleep(10);
                }
                if (timeout < 0)
                    throw new Exception("Timeout");

                while (stream.DataAvailable)
                {
                    int byteCount = stream.Read(output, 0, output.Length);

                    if (header)
                    {
                        if (firstChunk)
                        {
                            firstChunk = false;
                            var str = System.Text.Encoding.ASCII.GetString(output.Skip(2).Take(9).ToArray());

                            if (!int.TryParse(str, out length))
                            {
                                System.Diagnostics.Debug.WriteLine($"{DateTimeOffset.Now.ToUnixTimeMilliseconds() - t0} ERR {command}");
                                return new byte[0];
                            }
                            response.AddRange(output.Skip(11).Take(byteCount - 11));
                            totalRead += byteCount - 11;
                        }
                        else
                        {
                            response.AddRange(output.Take(byteCount));
                            totalRead += byteCount;
                        }
                        done = totalRead >= length;
                    }
                    else
                    {
                        done = output[byteCount - 1] == '\n';
                        response.AddRange(output.Take(byteCount));
                    }
                }
            }
            while (!done);

            //System.Diagnostics.Debug.WriteLine($"{DateTimeOffset.Now.ToUnixTimeMilliseconds() - t0,4} {command}");
            return response.Take(response.Count - 1).ToArray();
        }

        
        public void Close()
        {
            try
            {
                tcpClient.Close();
                connected = false;
            }
            catch { }
        }

        bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~Device()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // free other managed objects that implement
                // IDisposable only
            }

            // release any unmanaged objects
            // set the object references to null
            Close();

            _disposed = true;
        }
    }
}
