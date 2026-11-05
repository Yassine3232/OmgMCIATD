/*
KINEMATICSOUP CONFIDENTIAL
 Copyright(c) 2014-2026 KinematicSoup Technologies Incorporated 
 All Rights Reserved.

NOTICE:  All information contained herein is, and remains the property of 
KinematicSoup Technologies Incorporated and its suppliers, if any. The 
intellectual and technical concepts contained herein are proprietary to 
KinematicSoup Technologies Incorporated and its suppliers and may be covered by
U.S. and Foreign Patents, patents in process, and are protected by trade secret
or copyright law. Dissemination of this information or reproduction of this
material is strictly forbidden unless prior written permission is obtained from
KinematicSoup Technologies Incorporated.
*/
using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using UnityEngine;
using UnityEditor;
using KS.SF.Unity.Editor;
using KS.SF.Reactor;

namespace KS.SceneFusion.Client.Unity.Editor
{
    /// <summary>
    /// Unauthenticated version of the sessions panel. Displays the LAN session with a "Join" button if a LAN session is
    /// detected, otherwise displays a button to start a LAN session.
    /// </summary>
    public class sfSessionsMenu : sfUnauthenticatedMenu
    {
        /// <summary>
        /// Callback for checking if a game object can be synced. Used for counting the number of syncable game objects
        /// in open scenes.
        /// </summary>
        /// <param name="gameObject">gameObject to check.</param>
        /// <returns>true if the game object can be synced.</returns>
        public delegate bool SyncChecker(GameObject gameObject);

        /// <summary>Callback before starting or joining a session.</summary>
        /// <param name="sessionInfo">sessionInfo for the session to join, or null if starting a new session.</param>
        /// <returns>false to cancel joining or starting the session.</returns>
        public delegate bool PreSessionChecker(sfSessionInfo sessionInfo);

        /// <summary>
        /// Callback for checking if a game object can be synced. Used for counting the number of syncable game objects
        /// in open scenes. This is unused. It is here because the authenticated sessions menu uses it and its needed
        /// for open-source code that can work with both sfSessionsMenu implementations to compile without ifdefs.
        /// </summary>
        public static SyncChecker CanSync;

        /// <summary>
        /// Callback before a session is joined or started. Return false to prevent session from starting or being
        /// joined.
        /// </summary>
        public static PreSessionChecker PreSessionCheck;

        /// <summary>Scene Fusion service.</summary>
        private static sfService Service
        {
            get { return SceneFusion.Get().Service; }
        }

        /// <summary>Icon</summary>
        public override Texture Icon
        {
            get { return sfTextures.Logo; }
        }

        // Width in pixels of buttons that appear on the same line
        private const float BUTTON_WIDTH = 120;
        private const float LABEL_PADDING = 5;
        private const float PORT_LABEL_WIDTH = 30;
        private const float PORT_INPUT_WIDTH = 75;
        private const ushort LAN_BROADCAST_PORT = 21312;

        private const string HOST_KEY = "com.kinematicsoup.scenefusion.host";
        private const string HOST_PORT_KEY = "com.kinematicsoup.scenefusion.host_port";
        private const string SERVER_PORT_KEY = "com.kinematicsoup.scenefusion.server_port";

        [NonSerialized]
        private string m_errorMessage = null;
        [NonSerialized]
        private string m_newErrorMessage = null;
        [NonSerialized]
        private string m_lanWarningMessage = null;
        [NonSerialized]
        private string m_newLanWarningMessage = null;

        private ksWindow m_window;
        private Vector2 m_scrollPosition;
        private bool m_canUseInterface = true;
        private string m_cannotUseInterfaceReason = null;
        private MessageType m_reasonType = MessageType.None;

        private sfUndoableColor m_userColor;

        private UdpClient m_broadcastClient;
        [NonSerialized]
        private sfSessionInfo m_lanSession;
        [NonSerialized]
        private sfSessionInfo m_uiSession;
        private ushort m_lanServerPort = 8000;
        private string m_lanHostAddress = "";
        private ushort m_lanHostPort = 8000;
        private sfLocalSession.ServerState m_lanServerState = sfLocalSession.ServerState.MISSING;
        private long m_setupBroadcastReceiverTimer;
        private long m_firstReceiveTimer;
        private const long SETUP_BROADCAST_RECEIVER_INTERVAL = 3 * TimeSpan.TicksPerSecond;
        // When first listening for LAN server broadcasts, wait this long in seconds to enable the UI if we don't
        // receive anything.
        private const long FIRST_RECEIVE_TIMEOUT = 2 * TimeSpan.TicksPerSecond;
        private const string UNABLE_TO_RECEIVE_LAN_SESSION_WARNING =
            "Unable to receive LAN session information. Use direct connections instead.";

        private bool m_expandManualConnect = false;
        [NonSerialized]
        private bool m_isCompiling = false;
        [NonSerialized]
        private bool m_isInitialized = false;
        [NonSerialized]
        private bool m_isStale = false;
        [NonSerialized]
        private bool m_firstSessionCheck = true;

        /// <summary>
        /// Called when the menu is opened. Does initialization and begins listing for LAN session broadcasts.
        /// </summary>
        /// <param name="window">window the opened the menu.</param>
        public override void OnOpen(ksWindow window)
        {
            m_window = window;
            m_newErrorMessage = null;
            m_lanServerState = sfLocalSession.ServerCheck();
            m_setupBroadcastReceiverTimer = 0;
            m_firstReceiveTimer = DateTime.Now.Ticks + FIRST_RECEIVE_TIMEOUT;
            EditorApplication.update += Update;

            m_lanHostAddress = EditorPrefs.GetString(HOST_KEY, m_lanHostAddress);
            m_lanHostPort = (ushort)EditorPrefs.GetInt(HOST_PORT_KEY, m_lanHostPort);
            m_lanServerPort = (ushort)EditorPrefs.GetInt(SERVER_PORT_KEY, m_lanServerPort);

            if (Service != null)
            {
                Update();
            }
        }

        /// <summary>Performs cleanup and stops listening for LAN session broadcasts.</summary>
        /// <param name="window">window that closed the menu.</param>
        public override void OnClose(ksWindow window)
        {
            EditorPrefs.SetString(HOST_KEY, m_lanHostAddress);
            EditorPrefs.SetInt(HOST_PORT_KEY, m_lanHostPort);
            EditorPrefs.SetInt(SERVER_PORT_KEY, m_lanServerPort);

            EditorApplication.update -= Update;
            CloseBroadcastReceiver();
        }

        /// <summary>Draws the GUI.</summary>
        /// <param name="window."></param>
        public override void OnDraw(ksWindow window)
        {
            if (Event.current.type == EventType.Layout && !UpdateLayout(window))
            {
                return;
            }

            if (m_isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling scripts...", MessageType.Warning);
                return;
            }

            if (!m_isInitialized)
            {
                EditorGUILayout.HelpBox("Initializing service...", MessageType.Info);
                return;
            }

            m_scrollPosition = EditorGUILayout.BeginScrollView(m_scrollPosition);

            if (!m_canUseInterface)
            {
                EditorGUILayout.HelpBox(m_cannotUseInterfaceReason, m_reasonType);
            }
            else
            {
                if (m_userColor == null)
                {
                    m_userColor = CreateInstance<sfUndoableColor>();
                    m_userColor.Color = Service.LocalUserColor;
                }

                EditorGUIUtility.labelWidth = sfUI.Get().GetInspectorLabelWidth();
                string userName = EditorGUILayout.TextField("Username", Service.LocalUsername);
                if (userName != Service.LocalUsername)
                {
                    Service.LocalUsername = userName;
                    EditorPrefs.SetString(SceneFusion.USERNAME_KEY, userName);
                }
                m_userColor.Draw(new GUIContent("User Color",
                    "Your user color. Other users will see objects you select shaded with this color."));
                if (Service.LocalUserColor != m_userColor.Color)
                {
                    Service.LocalUserColor = m_userColor.Color;
                    EditorPrefs.SetString(SceneFusion.USER_COLOR_KEY, Service.LocalUserColor.ToHexString());
                }

                EditorGUILayout.Space();
                if (!string.IsNullOrEmpty(m_errorMessage))
                {
                    EditorGUILayout.HelpBox(m_errorMessage, MessageType.Error);
                    EditorGUILayout.Space();
                }

                if (m_firstSessionCheck)
                {
                    EditorGUILayout.HelpBox("Checking for LAN session...", MessageType.Info);
                }
                else
                {
                    DrawSession();

                    if (m_uiSession == null)
                    {
                        EditorGUILayout.Space();
                        DrawLANSessionButtons();
                        DrawManualLanConnect();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            sfSessionFooterUI.Get().DrawFooter();
        }

        /// <summary>Called every frame. Updates the GUI.</summary>
        private void Update()
        {
            CheckLanSessions();
            string cannotUseInterfaceReason;
            m_canUseInterface = CanUseInterface(out cannotUseInterfaceReason);
            if (m_cannotUseInterfaceReason != cannotUseInterfaceReason)
            {
                m_cannotUseInterfaceReason = cannotUseInterfaceReason;
                m_isStale = true;
            }

            bool firstSessionCheck = DateTime.Now.Ticks < m_firstReceiveTimer;
            if (firstSessionCheck != m_firstSessionCheck)
            {
                m_firstSessionCheck = firstSessionCheck;
                m_isStale = true;
            }

            SetupBroadcastReceiver();

            if (Service.ConnectionError != null)
            {
                m_newErrorMessage = Service.ConnectionError;
                Service.ConnectionError = null;
                m_isStale = true;
            }

            if (m_isStale)
            {
                m_isStale = false;
                m_window.Repaint();
            }
        }

        /// <summary>Check if the editor is in a state that we should stop the user from using the interface.</summary>
        /// <param name="reson">reson why the result returned false.</param>
        /// <returns>false if the interface should be disabled.</returns>
        private bool CanUseInterface(out string reason)
        {
            if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                reason = "Cannot create or join sessions while the editor is in play mode.";
                m_reasonType = MessageType.Warning;
                return false;
            }

            if (Service.IsConnecting)
            {
                reason = "Connecting to session.";
                m_reasonType = MessageType.Info;
                return false;
            }

            if (Service.IsStartingSession)
            {
                reason = "Starting session.";
                m_reasonType = MessageType.Info;
                return false;
            }

            reason = null;
            return true;
        }

        /// <summary>
        /// Updates the GUI layout with new data, possibly switching to a different menu
        /// if we've logged out or connected to a room.
        /// </summary>
        /// <param name="window">window the GUI is for.</param>
        /// <returns>false if we switched menus.</returns>
        private bool UpdateLayout(ksWindow window)
        {
            m_isInitialized = Service != null;
            m_isCompiling = EditorApplication.isCompiling;
            if (!m_isInitialized || m_isCompiling)
            {
                return true;
            }

            if (Service.IsConnected)
            {
                window.Menu = ScriptableObject.CreateInstance<sfOnlineMenu>();
                window.Menu.Draw(window);
                return false;
            }

            m_errorMessage = m_newErrorMessage;
            m_lanWarningMessage = m_newLanWarningMessage;
            m_uiSession = m_lanSession;
            return true;
        }

        /// <summary>
        /// Draws the LAN session with a "Join" button if a LAN sessions was detected. Otherwise draws a lan warning
        /// message if one is set, or draws a message saying the LAN server wasn't found.
        /// </summary>
        private void DrawSession()
        {
            if (!string.IsNullOrEmpty(m_lanWarningMessage))
            {
                EditorGUILayout.HelpBox(m_lanWarningMessage, MessageType.Warning);
                return;
            }

            sfSessionTableGUI.Instance.DrawHeader("Project", "Scene", "Creator");
            if (m_uiSession == null)
            {
                EditorGUILayout.LabelField("LAN session not found.");
                return;
            }

            string currentUnityVersion = "Unity " + Application.unityVersion.Substring(0,
                Application.unityVersion.LastIndexOf("."));
            string expectedUnityVersion = m_uiSession.LaunchApplication.Substring(0,
                m_uiSession.LaunchApplication.LastIndexOf("."));
            string joinError = null;
            if (m_uiSession.RequiredVersion != sfConfig.Get().Version.ToString())
            {
                joinError = "Requires Scene Fusion " + m_uiSession.RequiredVersion;
            }
            else if (expectedUnityVersion != currentUnityVersion)
            {
                joinError = "Unity versions do not match; Expected = " + expectedUnityVersion +
                    ", Current = " + currentUnityVersion;
            }
            sfSessionTableGUI.Instance.DrawRow(m_uiSession, joinError, () =>
            {
                if (PreSessionCheck == null || PreSessionCheck(m_uiSession))
                {
                    Service.JoinSession(m_uiSession);
                    GUIUtility.ExitGUI();
                }
            });
        }

        /// <summary>Draw the buttons used to start new LAN sessions.</summary>
        private void DrawLANSessionButtons()
        {
            switch (m_lanServerState)
            {
                case sfLocalSession.ServerState.INCOMPATIBLE_VERSION:
                {
                    EditorGUILayout.HelpBox("Found an incompatible version of the Scene Fusion LAN server. " +
                        "LAN sessions cannot be started.", MessageType.Warning);
                    return;
                }
                case sfLocalSession.ServerState.INCOMPATIBLE_OS:
                {
                    EditorGUILayout.HelpBox("LAN sessions can only be started on Windows.", MessageType.Warning);
                    return;
                }
                case sfLocalSession.ServerState.MISSING:
                {
                    EditorGUILayout.HelpBox("Cannot find Scene Fusion LAN server at '" +
                        sfPaths.ExternalServer + "'. LAN sessions cannot be started.", MessageType.Warning);
                    return;
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("New Session", GUILayout.MaxWidth(BUTTON_WIDTH)) &&
                (PreSessionCheck == null || PreSessionCheck(null)))
            {
                ksLog.Info("Launching LAN Session: " + sfPaths.SceneName);
                m_newErrorMessage = null;

                sfSessionInfo sessionInfo = WriteLANConfig();
                if (sessionInfo != null)
                {
                    CloseBroadcastReceiver();
                    string sessionId = "sf_" + DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                    Service.StartLocalSession(sessionId, sessionInfo);
                }
                else
                {
                    m_newErrorMessage = "An unexpected error occured while launching and joining the LAN session.";
                }
            }

            Rect r = EditorGUILayout.GetControlRect(
                GUILayout.Width(PORT_LABEL_WIDTH + PORT_INPUT_WIDTH + LABEL_PADDING));
            //r.x = r.xMax + LABEL_PADDING;
            r.width = PORT_LABEL_WIDTH;
            EditorGUI.LabelField(r, "Port");

            r.x = r.xMax + LABEL_PADDING;
            r.width = PORT_INPUT_WIDTH;
            m_lanServerPort = (ushort)EditorGUI.IntField(r, m_lanServerPort);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>Draw a manual connection foldout</summary>
        private void DrawManualLanConnect()
        {
            EditorGUILayout.Space();

            m_expandManualConnect = EditorGUILayout.Foldout(m_expandManualConnect, "Manual Connect", true);
            if (!m_expandManualConnect)
            {
                return;
            }
            EditorGUI.indentLevel++;

            m_lanHostAddress = EditorGUILayout.TextField("Host", m_lanHostAddress);
            m_lanHostPort = (ushort)EditorGUILayout.IntField("Port", m_lanHostPort);

            if (ksStyle.Button("Manual Connect"))
            {
                sfSessionInfo session = sfSessionInfo.CreateLANInfo(
                    1,
                    Application.productName,
                    Application.productName,
                    "Unity " + Application.unityVersion,
                    sfPaths.SceneName,
                    sfConfig.Get().Version.ToString(),
                    "",
                    new ksRoomInfo(m_lanHostAddress, m_lanServerPort));
                if (PreSessionCheck == null || PreSessionCheck(session))
                {
                    Service.JoinSession(session);
                    GUIUtility.ExitGUI();
                }
            }
            EditorGUI.indentLevel--;
        }

        /// <summary>
        /// Write a config file to the server directory for a new LAN session then
        /// return the project and session information for the new session.
        /// </summary>
        /// <returns>Local session info</returns>
        private sfSessionInfo WriteLANConfig()
        {
            ksRoomInfo roomInfo = new ksRoomInfo("localhost", m_lanServerPort);
            roomInfo.Scene = "SceneFusion";
            roomInfo.Id = 0;
            sfSessionInfo sessionInfo = sfSessionInfo.CreateLANInfo(
                1,
                Application.productName,
                Application.productName,
                "Unity " + Application.unityVersion,
                sfPaths.SceneName,
                sfConfig.Get().Version.ToString(),
                Service.LocalUsername,
                roomInfo);

            // Write LAN config
            ksJSON jsonConfig = new ksJSON();
            jsonConfig["projectId"] = 1;
            jsonConfig["projectName"] = sessionInfo.ProjectName;
            jsonConfig["scene"] = sessionInfo.SceneName;
            jsonConfig["creator"] = sessionInfo.Creator;
            jsonConfig["version"] = sessionInfo.RequiredVersion;
            jsonConfig["port"] = (int)m_lanServerPort;
            jsonConfig["application"] = sessionInfo.LaunchApplication;
            if (!ksAnalytics.Enabled)
            {
                jsonConfig["analytics"] = false;
            }

            try
            {
                ksPathUtils.Create(sfPaths.External, true);
                File.WriteAllText(sfPaths.External + "config.json", jsonConfig.Print(true));
                return sessionInfo;
            }
            catch (Exception ex)
            {
                ksLog.Error(this, "Error writing LAN configs.", ex);
            }
            return null;
        }

        /// <summary>Stops listening for LAN session broadcasts.</summary>
        private void CloseBroadcastReceiver()
        {
            if (m_broadcastClient != null)
            {
                m_broadcastClient.Close();
                m_broadcastClient = null;
            }
        }

        /// <summary>
        /// Setup broadcast receiver if the broadcast client is null.
        /// Show a warning message on the sessions panel if it failed.
        /// </summary>
        private void SetupBroadcastReceiver()
        {
            if (m_broadcastClient == null && m_setupBroadcastReceiverTimer <= DateTime.Now.Ticks && 
                !EditorApplication.isPlayingOrWillChangePlaymode && !Service.IsStartingSession)
            {
                try
                {
                    IPEndPoint ep = new IPEndPoint(IPAddress.Any, LAN_BROADCAST_PORT);
                    m_broadcastClient = new UdpClient(ep);
                    m_broadcastClient.BeginReceive(OnBroadcastReceived, new object());
                    ksLog.Info(this, "Listening for Session broadcasts on port " + LAN_BROADCAST_PORT);
                    if (m_newLanWarningMessage == UNABLE_TO_RECEIVE_LAN_SESSION_WARNING)
                    {
                        m_newLanWarningMessage = null;
                    }
                }
                catch
                {
                    m_firstReceiveTimer = 0;
                    m_newLanWarningMessage = UNABLE_TO_RECEIVE_LAN_SESSION_WARNING;
                    m_setupBroadcastReceiverTimer = DateTime.Now.Ticks + SETUP_BROADCAST_RECEIVER_INTERVAL;
                    // Do nothing, we will try to setup the UDP socket next update.
                }
            }
        }

        /// <summary>Parse session information from a broadcast message.</summary>
        /// <param name="-">async result</param>
        private void OnBroadcastReceived(IAsyncResult ar)
        {
            m_firstReceiveTimer = 0;
            if (m_broadcastClient == null)
            {
                return;
            }
            try
            {
                IPEndPoint ip = new IPEndPoint(IPAddress.Any, LAN_BROADCAST_PORT);
                byte[] bytes = m_broadcastClient.EndReceive(ar, ref ip);
                // Parse the message data
                int offset = 0;
                long startTime = BitConverter.ToInt64(bytes, offset);
                offset += sizeof(long);
                uint id = BitConverter.ToUInt32(bytes, offset);
                offset += sizeof(uint);
                ushort port = BitConverter.ToUInt16(bytes, offset);
                offset += sizeof(ushort);
                int strlen = (int)bytes[offset++];
                string projectName = System.Text.Encoding.ASCII.GetString(bytes, offset, strlen);
                offset += strlen;
                strlen = (int)bytes[offset++];
                string sceneName = System.Text.Encoding.ASCII.GetString(bytes, offset, strlen);
                offset += strlen;
                strlen = (int)bytes[offset++];
                string creator = System.Text.Encoding.ASCII.GetString(bytes, offset, strlen);
                offset += strlen;
                strlen = (int)bytes[offset++];
                string version = System.Text.Encoding.ASCII.GetString(bytes, offset, strlen);
                offset += strlen;
                strlen = (int)bytes[offset++];
                string application = System.Text.Encoding.ASCII.GetString(bytes, offset, strlen);
                offset += strlen;

                // Create session info
                ksRoomInfo roomInfo = new ksRoomInfo(ip.Address.ToString(), port);
                roomInfo.Id = id;
                roomInfo.Scene = "Scene Fusion";
                roomInfo.Type = "Scene Fusion";
                sfSessionInfo info = sfSessionInfo.CreateLANInfo(
                    1,
                    projectName,
                    projectName,
                    application,
                    sceneName,
                    version,
                    creator,
                    roomInfo,
                    new DateTime(startTime));

                if (m_lanSession == null || m_lanSession.RoomInfo.Id != info.RoomInfo.Id)
                {
                    m_isStale = true;
                }
                m_lanSession = info;

                // Receive again!
                m_broadcastClient.BeginReceive(OnBroadcastReceived, new object());
            }
            catch (ObjectDisposedException)
            {
                // Ignore object disposed exceptions.
            }
            catch (Exception e)
            {
                ksLog.Error(this, "Error parsing broadcast packet", e);
            }
        }

        /// <summary>Clear the LAN sessionif it is older than 5 seconds.</summary>
        private void CheckLanSessions()
        {
            if (m_lanSession != null && m_lanSession.Time < DateTime.Now - TimeSpan.FromSeconds(5.0))
            {
                m_lanSession = null;
                m_isStale = true;
            }
        }
    }
}
