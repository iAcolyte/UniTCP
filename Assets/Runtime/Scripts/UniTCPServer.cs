using Kodai100.Tcp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace UniTCP {
    public class UniTCPServer: MonoBehaviour {
        [SerializeField] private ushort port = 7000;
        public ushort Port {
            get => port;
            set => port = value;
        }

        [SerializeField] private OnMessageEvent messageReceived;
        public OnMessageEvent MessageReceived => messageReceived;
        [SerializeField] private OnEstablishedEvent established;
        public OnEstablishedEvent Established => established;
        [SerializeField] private OnDisconnectedEvent disconnected;
        public OnDisconnectedEvent Disconnected => disconnected;
        private TCPServer? tcpServer;

        private CancellationTokenSource source;

        private void Start() {
            messageReceived.AddListener(Debug.Log);
            established.AddListener(Debug.Log);
            disconnected.AddListener(Debug.Log);
        }

        private void OnEnable() {
            source = new();
            tcpServer = new TCPServer(new IPEndPoint(IPAddress.Any, port),
                messageReceived,
                established,
                disconnected,
                source.Token);
            _ = tcpServer.Listen();
        }

        private void OnDisable() {
            source.Cancel();
            tcpServer?.Dispose();
            tcpServer = null;
        }

        public void BroadcastToClients(string data) {
            if (tcpServer is null) throw new InvalidOperationException("Can't broadcast data with disabled server");
            var msg = UniTCPUtilities.BuildMessage(data);
            tcpServer.BroadcastToClients(msg);
        }

        public void SendMessageToClient(TcpClient client, string data) {
            if (tcpServer is null) throw new InvalidOperationException("Can't broadcast data with disabled server");
            var msg = UniTCPUtilities.BuildMessage(data);
            tcpServer.SendMessageToClient(client, msg);
        }
    }
}