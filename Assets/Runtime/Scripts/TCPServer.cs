using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UniTCP;
using System.Diagnostics;

namespace Kodai100.Tcp {

    internal class TCPServer: IDisposable {

        private IPEndPoint endpoint;

        private TcpListener? listener;
        private List<TcpClient> clients = new List<TcpClient>();

        private SynchronizationContext mainContext;
        private volatile bool acceptLoop = true;

        private OnMessageEvent OnMessage;
        private OnEstablishedEvent? OnEstablished;
        private OnDisconnectedEvent? OnDisconnected;

        public IReadOnlyList<TcpClient> Clients => clients;

        private CancellationToken token;

        public TCPServer(IPEndPoint endpoint, OnMessageEvent onMessage, CancellationToken token) {
            this.token = token;
            this.endpoint = endpoint ?? throw new ArgumentNullException("endpoint should not be null");

            // Set Unity main thread
            mainContext = SynchronizationContext.Current;

            OnMessage = onMessage;

        }

        public TCPServer(IPEndPoint endpoint, OnMessageEvent onMessage, OnEstablishedEvent onEstablished, OnDisconnectedEvent onDisconnected, CancellationToken token) : this(endpoint, onMessage, token) {
            OnEstablished = onEstablished;
            OnDisconnected = onDisconnected;
        }

        public async Task Listen() {
            lock (this) {
                if (listener != null)
                    throw new InvalidOperationException("Already started");

                acceptLoop = true;
                listener = new TcpListener(endpoint);
            }

            UnityEngine.Debug.Log("Starting server...");
            listener.Start();
            UnityEngine.Debug.Log("Server started");

            while (acceptLoop) {
                try {
                    var client = await UniTCPUtilities.AcceptTcpClientAsync(listener, token).ConfigureAwait(false);
                    var _ = Task.Run(() => OnConnectClient(client));
                } catch (ObjectDisposedException ex) {
                    UnityEngine.Debug.LogError(ex.Message);
                    // thrown if the listener socket is closed
                } catch (SocketException ex) {
                    UnityEngine.Debug.LogError(ex.Message);
                    // Some socket error
                }
            }
        }

        /*
        using (cancellationToken.Register(() => tcpListener.Stop()))
{
    try
    {
        var tcpClient = await tcpListener.AcceptTcpClientAsync();
        // … carry on …
    }
    catch (InvalidOperationException)
    {
        // Either tcpListener.Start wasn't called (a bug!)
        // or the CancellationToken was cancelled before
        // we started accepting (giving an InvalidOperationException),
        // or the CancellationToken was cancelled after
        // we started accepting (giving an ObjectDisposedException).
        //
        // In the latter two cases we should surface the cancellation
        // exception, or otherwise rethrow the original exception.
        cancellationToken.ThrowIfCancellationRequested();
        throw;
    }
}
        */



        public void Stop() {

            lock (this) {

                if (listener == null)
                    throw new InvalidOperationException("Not started");

                acceptLoop = false;

                listener.Stop();
                listener = null;

            }


            lock (clients) {
                foreach (var c in clients) {
                    c.Close();
                }
            }

        }

        private async Task OnConnectClient(TcpClient client) {
            var clientEndpoint = client.Client.RemoteEndPoint;

            if (OnEstablished is not null) mainContext.Post(_ => OnEstablished.Invoke(client), null);
            clients.Add(client);

            await NetworkStreamHandler(client);

            if (OnDisconnected is not null) mainContext.Post(_ => OnDisconnected.Invoke(clientEndpoint), null);
            clients.Remove(client);
        }

        public void DisconnectClient(TcpClient client) {
            clients.Remove(client);
            client.Close();
        }


        private async Task NetworkStreamHandler(TcpClient client) {

            while (client.Connected) {
                using (var stream = client.GetStream()) {
                    var reader = new StreamReader(stream, Encoding.UTF8);

                    while (!reader.EndOfStream) {
                        await Task.Run(() => {
                            var bytes = new System.Collections.Generic.List<byte>(1024);
                            int next = -1;
                            char prev = '\0';
                            while (true) {
                                next = reader.Read();
                                if (next == 10) {
                                    var r1 = Encoding.UTF8.GetString(bytes.ToArray());
                                    bytes.Clear();
                                    mainContext.Post(_ => OnMessage.Invoke(r1, client), null);
                                    continue;
                                }
                                prev = (char)next;
                                bytes.Add((byte)next);
                                if ((char)next == '\0' || next == -1) {
                                    break;
                                }
                            };
                            if (bytes.Count > 0) {
                                var res = Encoding.UTF8.GetString(bytes.ToArray());
                                mainContext.Post(_ => OnMessage.Invoke(res, client), null);
                            }
                        });
                    }

                }

            }
            // Disconnected
        }


        public void BroadcastToClients(byte[] data) {
            if (token.IsCancellationRequested) return;
            foreach (var c in Clients) {
                c.GetStream().Write(data, 0, data.Length);
                c.GetStream().Flush();
            }
        }


        public void SendMessageToClient(TcpClient c, byte[] data) {
            if (token.IsCancellationRequested) return;
            c.GetStream().Write(data, 0, data.Length);
            c.GetStream().Flush();
        }


        public void Dispose() {
            Stop();
        }
    }

}