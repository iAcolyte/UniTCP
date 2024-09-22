using UnityEngine;
using UnityEditor;
using EGL = UnityEditor.EditorGUILayout;
using System.Net;

namespace UniTCP.Editor {


    [CustomEditor(typeof(UniTCPClient))]
    public class UniTCPClientInspector: UnityEditor.Editor {
        private SerializedProperty? _script;
        private SerializedProperty Script => _script ??= serializedObject.FindProperty("m_Script");

        private SerializedProperty? _host;
        private SerializedProperty Host => _host ??= serializedObject.FindProperty("host");

        private SerializedProperty? _port;
        private SerializedProperty Port => _port ??= serializedObject.FindProperty("port");

        private SerializedProperty? _autoReconnect;
        private SerializedProperty AutoReconnect => _autoReconnect ??= serializedObject.FindProperty("autoReconnect");

        private SerializedProperty? _reconnectInterval;
        private SerializedProperty ReconnectInterval => _reconnectInterval ??= serializedObject.FindProperty("reconnectInterval");

        private SerializedProperty? _availabilityCheck;
        private SerializedProperty AvailabilityCheck => _availabilityCheck ??= serializedObject.FindProperty("availabilityCheck");

        private SerializedProperty? _availabilityCheckInterval;
        private SerializedProperty AvailabilityCheckInterval => _availabilityCheckInterval ??= serializedObject.FindProperty("availabilityCheckInterval");

        private SerializedProperty? _messageReceived;
        private SerializedProperty MessageReceived => _messageReceived ??= serializedObject.FindProperty("messageReceived");

        private SerializedProperty? _connected;
        private SerializedProperty Connected => _connected ??= serializedObject.FindProperty("connected");

        private SerializedProperty? _disconnected;
        private SerializedProperty Disconnected => _disconnected ??= serializedObject.FindProperty("disconnected");

        private UniTCPClient? _instance;
        private UniTCPClient Instance => _instance ??= (UniTCPClient)target;

        private bool eventsExpanded = false;

        public override void OnInspectorGUI() {

            GUI.enabled = false;
            EGL.PropertyField(Script);
            GUI.enabled = true;
            EGL.LabelField("Network", EditorStyles.boldLabel);

            EGL.BeginHorizontal();
            EGL.PrefixLabel("Endpoint");
            GUI.enabled = !Instance.enabled;
            var endpoint = EGL.DelayedTextField(Host.stringValue);
            if (IPAddress.TryParse(endpoint, out _)) Host.stringValue = endpoint;
            EGL.LabelField(":", GUILayout.Width(5));
            Port.intValue = EGL.IntField(Port.intValue, GUILayout.MinWidth(40), GUILayout.MaxWidth(50));
            GUI.enabled = true;
            EGL.EndHorizontal();

            EGL.LabelField("Settings", EditorStyles.boldLabel);

            EGL.BeginHorizontal();
            EGL.PropertyField(AutoReconnect);
            if (AutoReconnect.boolValue) {
                EGL.LabelField("Interval (s)", GUILayout.Width(65));
                EGL.PropertyField(ReconnectInterval, GUIContent.none, GUILayout.MinWidth(10));
            }
            EGL.EndHorizontal();

            EGL.BeginHorizontal();
            EGL.PropertyField(AvailabilityCheck);
            if (AvailabilityCheck.boolValue) {
                EGL.LabelField("Interval (s)", GUILayout.Width(65));
                EGL.PropertyField(AvailabilityCheckInterval, GUIContent.none, GUILayout.MinWidth(10));
            }
            EGL.EndHorizontal();

            eventsExpanded = EGL.BeginFoldoutHeaderGroup(eventsExpanded, new GUIContent("Events"));
            if (eventsExpanded) {
                EGL.PropertyField(MessageReceived);
                EGL.PropertyField(Connected);
                EGL.PropertyField(Disconnected);
            }

            if (!Application.isPlaying) GUI.enabled = false;
            if (!Instance.enabled && GUILayout.Button(new GUIContent("Connect", "Playmode only"))) {
                Instance.enabled = true;
            } else if (Instance.enabled && GUILayout.Button("Disconnect")) {
                Instance.enabled = false;
            }
            if (!Application.isPlaying) GUI.enabled = true;


            if (serializedObject.hasModifiedProperties)
                serializedObject.ApplyModifiedProperties();
        }

    }
}
