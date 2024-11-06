using Kodai100.Tcp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

namespace UniTCP {
    public class UniTCPServer: MonoBehaviour {
        [SerializeField] private ushort port = 42674;
        public ushort Port {
            get => port;
            set => port = value;
        }

        [SerializeField] private UnityEvent started;
        public UnityEvent Started => started;
        [SerializeField] private UnityEvent stopped;
        public UnityEvent Stopped => stopped;

        [SerializeField] private OnMessageEvent messageReceived;
        public OnMessageEvent MessageReceived => messageReceived;
        [SerializeField] private OnEstablishedEvent established;
        public OnEstablishedEvent Established => established;
        [SerializeField] private OnDisconnectedEvent clientDisconnected;
        public OnDisconnectedEvent ClientDisconnected => clientDisconnected;



        private TCPServer? tcpServer;

        private CancellationTokenSource source;

        private void Start() {
        }

        private void OnEnable() {
            source = new();
            tcpServer = new TCPServer(new IPEndPoint(IPAddress.Any, port),
                messageReceived,
                established,
                clientDisconnected,
                source.Token);
            _ = tcpServer.Listen();
            started.Invoke();
        }

        private void OnDisable() {
            source.Cancel();
            tcpServer?.Dispose();
            tcpServer = null;
            stopped.Invoke();
        }

        public void DisconnectClient(TcpClient client) {
            if (tcpServer is null) throw new InvalidOperationException("Can't disconnect client with disabled server");
            tcpServer.DisconnectClient(client);
        }

        public void BroadcastToClients(string data) {
            if (tcpServer is null) throw new InvalidOperationException("Can't broadcast data with disabled server");
            if (!data.EndsWith('\n')) data += '\n';
            var msg = UniTCPUtilities.BuildMessage(data);
            tcpServer.BroadcastToClients(msg);
        }

        public void SendMessageToClient(TcpClient client, string data) {
            if (tcpServer is null) throw new InvalidOperationException("Can't broadcast data with disabled server");
            if (!data.EndsWith('\n')) data += '\n';
            var msg = UniTCPUtilities.BuildMessage(data);
            tcpServer.SendMessageToClient(client, msg);
        }
        public void SendMessageToClient(TcpClient client, char data) {
            if (tcpServer is null) throw new InvalidOperationException("Can't broadcast data with disabled server");
            //var msg = UniTCPUtilities.BuildMessage(data);
            tcpServer.SendMessageToClient(client, new byte[] { (byte)data });
        }
    }
}