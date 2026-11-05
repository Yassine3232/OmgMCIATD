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
using UnityEngine;
using UnityEditor;
using KS.SF.Unity.Editor;
using KS.SF.Reactor;
using KS.SF.Reactor.Client;

namespace KS.SceneFusion.Client.Unity.Editor
{
    // Implementation specific to the asset store package.
    public partial class SceneFusion
    {
        /// <summary>Creates the Scene Fusion service.</summary>
        /// <returns>Scene Fusion service.</returns>
        private sfService CreateService()
        {
            return new sfService();
        }

        /// <summary>Loads the user's username and color from <see cref="EditorPrefs"/>.</summary>
        partial void LoadUserSettings()
        {
            m_service.LocalUsername = EditorPrefs.GetString(USERNAME_KEY);
            if (string.IsNullOrEmpty(m_service.LocalUsername))
            {
                EditorApplication.update += SetDefaultUsername;
            }
            if (EditorPrefs.HasKey(USER_COLOR_KEY))
            {
                try
                {
                    m_service.LocalUserColor = new ksColor(EditorPrefs.GetString(USER_COLOR_KEY));
                    return;
                }
                catch (Exception e)
                {
                    ksLog.LogException(this, e);
                }
            }

            // If there is no saved color, choose a random color.
            ksRandom rand = new ksRandom();
            m_service.LocalUserColor = ksColor.FromHSV(
                rand.NextFloat(0f, 1f),
                rand.NextFloat(0.6f, 1f),
                rand.NextFloat(0.8f, 1f)
            );
        }

        /// <summary>
        /// Sets the username to the Unity user display name if the user name is not already set and the display name is
        /// available (not anonymous).
        /// </summary>
        private void SetDefaultUsername()
        {
            if (string.IsNullOrEmpty(m_service.LocalUsername))
            {
                string displayName = ksEditorUtils.GetUnityUserDisplayName();
                if (string.IsNullOrEmpty(displayName) || displayName == "anonymous")
                {
                    return;
                }
                m_service.LocalUsername = displayName;
            }
            EditorApplication.update -= SetDefaultUsername;
        }
    }
}
