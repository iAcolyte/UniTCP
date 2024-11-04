using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniTCP;

namespace Kodai100.Tcp {
    internal class TcpCommunicator: IDisposable {

        public string Name { get; }

        public bool IsConnected {
            get {
                try {
                    if ((TcpClient == null) || !TcpClient.Connected) return false;
                    if (Socket == null) return false;

                    return !(Socket.Poll(1, SelectMode.SelectRead) && (Socket.Available <= 0));
                } catch {
                    return false;
                }
            }
        }


        private TcpClient TcpClient { get; }

        private NetworkStream? stream;
        public NetworkStream? Stream => stream ??= TcpClient?.GetStream();

        private Socket Socket => TcpClient?.Client;

        private SynchronizationContext mainContext;
        private OnMessageEvent OnMessage;

        private bool running = false;


        public TcpCommunicator(TcpClient tcpClient, OnMessageEvent onMessage) {
            this.TcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            this.Name = $"[{Socket.RemoteEndPoint}]";

            this.mainContext = SynchronizationContext.Current;
            this.OnMessage = onMessage;
        }

        public TcpCommunicator(string host, int port, OnMessageEvent onMessage) : this(new TcpClient(host, port), onMessage) {
        }

        public void Dispose() {
            if (TcpClient != null) {
                running = false;

                TcpClient.Close();
                (TcpClient as IDisposable).Dispose();

            }
        }



        public void Send(byte[] data) {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (!IsConnected) throw new InvalidOperationException();

            try {
                var stream = TcpClient.GetStream();
                stream.Write(data, 0, data.Length);
            } catch (Exception ex) {
                throw new ApplicationException("Attempt to send failed.", ex);
            }
        }


        public async Task Listen() {

            if (TcpClient == null) return;

            running = true;

            while (running) {
                await Receive();
            }
        }
        public async Task Receive() {
            if (!IsConnected) {
                throw new InvalidOperationException();
            }

            try {

                while (Stream.DataAvailable) {
                    using var reader = new StreamReader(Stream, Encoding.UTF8, false, 4096, leaveOpen: true);
                    await Task.Run(() => {
                        var bytes = new System.Collections.Generic.List<byte>(1024);
                        int next = -1;
                        char prev = '\0';
                        while (true) {
                            next = reader.Read();


                            if (next == 10) {
                                var r1 = Encoding.UTF8.GetString(bytes.ToArray());
                                bytes.Clear();
                                mainContext.Post(_ => OnMessage.Invoke(r1), null);
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
                            mainContext.Post(_ => OnMessage.Invoke(res), null);
                        }

                    });
                }

            } catch (Exception ex) {
                throw new ApplicationException("Attempt to receive failed.", ex);
            }
        }

    }
}