using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TakojsnjeSporocanje
{
    public enum NetworkMode { None, Server, Client }

    public class ChatNetworkService : IDisposable
    {
        private TcpListener listener;
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private CancellationTokenSource cancellation;
        private bool disposed;

        public NetworkMode Mode { get; private set; } = NetworkMode.None;
        public bool IsConnected => client != null && client.Connected && stream != null;

        public event Action<string> MessageReceived;
        public event Action Connected;
        public event Action<string> Disconnected;

        public string LocalIPAddress
        {
            get
            {
                string localIP = "127.0.0.1";
                try
                {
                    using Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    socket.Connect("8.8.8.8", 80);
                    localIP = (socket.LocalEndPoint as IPEndPoint)?.Address.ToString() ?? localIP;
                }
                catch { }
                return localIP;
            }
        }

        public async Task StartServerAsync(int port)
        {
            Mode = NetworkMode.Server;
            cancellation = new CancellationTokenSource();

            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            try
            {
                client = await listener.AcceptTcpClientAsync();
                SetupStreams();
                Connected?.Invoke();
                await ReceiveLoopAsync(cancellation.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (!disposed)
            {
                Disconnected?.Invoke(ex.Message);
            }
            finally
            {
                listener.Stop();
            }
        }

        public async Task ConnectAsync(string host, int port)
        {
            Mode = NetworkMode.Client;
            cancellation = new CancellationTokenSource();

            client = new TcpClient();
            await client.ConnectAsync(host, port);
            SetupStreams();
            Connected?.Invoke();

            try
            {
                await ReceiveLoopAsync(cancellation.Token);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) when (!disposed)
            {
                Disconnected?.Invoke(ex.Message);
            }
        }

        public void SendMessage(string message)
        {
            if (!IsConnected || writer == null)
            {
                return;
            }

            try
            {
                writer.WriteLine(message);
                writer.Flush();
            }
            catch { }
        }

        public void Disconnect()
        {
            cancellation?.Cancel();
            DisposeResources();
            Mode = NetworkMode.None;
        }

        private void SetupStreams()
        {
            stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = false };
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                string line = await reader.ReadLineAsync();
                if (line == null)
                {
                    Disconnected?.Invoke("Povezava je bila prekinjena.");
                    break;
                }

                MessageReceived?.Invoke(line);
            }
        }

        private void DisposeResources()
        {
            try { writer?.Close(); } catch { }
            try { reader?.Close(); } catch { }
            try { stream?.Close(); } catch { }
            try { client?.Close(); } catch { }
            try { listener?.Stop(); } catch { }

            writer = null;
            reader = null;
            stream = null;
            client = null;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            cancellation?.Cancel();
            DisposeResources();
        }
    }
}
