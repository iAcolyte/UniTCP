using Kodai100.Tcp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

namespace UniTCP {
    public class UniTCPClient: MonoBehaviour {
        [SerializeField] private string host = "127.0.0.1";
        public IPAddress Host {
            get => IPAddress.Parse(host);
            set => host = value.ToString();
        }

        [SerializeField] private ushort port = 7000;
        public ushort Port {
            get => port;
            set => port = value;
        }

        public IPEndPoint EndPoint {
            get => new IPEndPoint(Host, port);
            set {
                Host = value.Address;
                port = (ushort)value.Port;
            }
        }


        [SerializeField] private bool autoReconnect;
        [Min(0.1f)]
        [SerializeField] private float reconnectInterval = 1.0f;

        [SerializeField] private bool availabilityCheck;
        [Min(0.1f)]
        [SerializeField] private float availabilityCheckInterval = 1.0f;

        [SerializeField] private OnMessageEvent messageReceived;
        public OnMessageEvent MessageReceived => messageReceived;

        [SerializeField] private UnityEvent connected = new();
        public UnityEvent Connected => connected;
        [SerializeField] private UnityEvent disconnected = new();
        public UnityEvent Disconnected => disconnected;

        public bool AvailabilityCheck {
            get => availabilityCheck;
            set {
                if (availabilityCheck == value) return;
                availabilityCheck = value;
                if (value) availabilityRoutine = StartCoroutine(CheckAliveLoop());
                else StopCoroutine(availabilityRoutine);
            }
        }

        public bool IsConnected => tcpClient?.IsConnected ?? false;

        private TcpCommunicator? tcpClient;
        private Coroutine? availabilityRoutine;

        private IEnumerator Start() {
            messageReceived.AddListener(Debug.Log);
            yield return new WaitForSeconds(5);
            SendMessageToServer("Hello");

        }

        private void OnEnable() {
            try {
                tcpClient = new TcpCommunicator(host, port, messageReceived);
                _ = tcpClient.Listen();
            } catch (SocketException ex) {
                Debug.LogError($"SocketException : {ex.Message}");
                if (!autoReconnect) {
                    enabled = false;
                    return;
                }
                StartCoroutine(ReconnectAttempt());
                return;
            }
            availabilityRoutine = StartCoroutine(CheckAliveLoop());
        }

        private IEnumerator ReconnectAttempt() {
            yield return new WaitForSeconds(reconnectInterval);
            OnEnable();
        }

        private IEnumerator CheckAliveLoop() {
            yield return new WaitForSeconds(availabilityCheckInterval);
            while (IsConnected) {
                yield return new WaitForSeconds(availabilityCheckInterval);
            }
            availabilityRoutine = null;
            LostConnectionBehaviour();
        }

        private void LostConnectionBehaviour() {
            disconnected.Invoke();
            if (autoReconnect) {
                tcpClient?.Dispose();
                OnEnable();
                return;
            }
            enabled = false;
        }

        private void OnDisable() {
            tcpClient?.Dispose();
        }

        public void SendMessageToServer(string data) {
            if (!IsConnected) {
                LostConnectionBehaviour();
                throw new InvalidOperationException("Client not connected");
            }
            try {
                tcpClient?.Send(UniTCPUtilities.BuildMessage(data));
            } catch (InvalidOperationException) {
                LostConnectionBehaviour();
            }
        }

        private void OnValidate() {
            if (!Application.isPlaying) enabled = false;
        }
    }
}
