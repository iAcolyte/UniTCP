﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Kodai100.Tcp {
    internal enum SocketType {
        Server, Client
    }

    internal class UnityTCP: MonoBehaviour {

        [SerializeField] private SocketType socketType = SocketType.Server;
        public SocketType SocketType => socketType;

        [SerializeField] private string host = "127.0.0.1";
        public string Host => host;

        [SerializeField] private ushort port = 7000;
        public int Port => port;

        public IReadOnlyList<TcpClient>? Clients => tcpServer?.Clients;

        public OnMessageEvent OnMessage;
        public OnEstablishedEvent OnEstablished;
        public OnDisconnectedEvent OnDisconnected;

        private TCPServer tcpServer;
        private TcpCommunicator tcpClient;


        private void OnEnable() {
            if (socketType == SocketType.Server) {
                tcpServer = new TCPServer(new IPEndPoint(IPAddress.Any, port), OnMessage, OnEstablished, OnDisconnected, default);
                _ = tcpServer.Listen();
                return;
            }
            tcpClient = new TcpCommunicator(host, port, OnMessage);
            Task.Run(tcpClient.Listen);
        }

        public void BroadcastToClients(string data) {
            if (socketType == SocketType.Server) {
                var msg = BuildMessage(data);
                tcpServer.BroadcastToClients(msg);
            }
        }

        public void SendMessageToClient(TcpClient client, string data) {
            if (socketType == SocketType.Server) {
                var msg = BuildMessage(data);
                tcpServer.SendMessageToClient(client, msg);
            }
        }

        public void SendMessageToServer(string data) {
            if (socketType == SocketType.Client) {
                tcpClient.Send(BuildMessage(data));
            }
        }

        private void OnDisable() {
            if (socketType == SocketType.Server) {
                tcpServer.Dispose();
            }
            tcpClient.Dispose();
        }

        private byte[] BuildMessage(string data) {
            var terminator = "\r\n";
            return Encoding.GetEncoding("UTF-8").GetBytes($"{data}{terminator}");
        }
    }
}