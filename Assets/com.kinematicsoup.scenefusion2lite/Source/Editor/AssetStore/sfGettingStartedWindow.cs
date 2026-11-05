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
using UnityEditor;
using UnityEngine;
using KS.SF.Unity.Editor;

namespace KS.SceneFusion.Client.Unity.Editor
{
    // Implementation specific to the asset store package.
    public partial class sfGettingStartedWindow
    {
        /// <summary>Draws editing instructions.</summary>
        partial void DrawEditingInstructions()
        {
            EditorGUILayout.LabelField("1. Open the session window by going to <b>Window > Scene Fusion > Session</b>.", m_paragraph);
            EditorGUILayout.LabelField("2. Enter a username.", m_paragraph);
            EditorGUILayout.LabelField("3. Choose your user color. Objects you select will be shaded with this color for other users.", m_paragraph);
            EditorGUILayout.LabelField("4. Editing Sessions", m_paragraph);
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("a. If no LAN session is detected, there will be a <b>New Session</b> button. Click it to start a new editing session.", m_paragraph);
            EditorGUILayout.LabelField("b. If a LAN session is detected, the session information will be displayed. Click the <b>Join</b> button to join the session.", m_paragraph);
            EditorGUI.indentLevel--;
            EditorGUILayout.LabelField("5. A busy spinner will appear in the lower left corner of the scene view while the scene is being prepared. Once the session is joined, green checkmarks will appear next to synced objects in the hierarchy window.", m_paragraph);
        }
    }
}
