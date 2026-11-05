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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KS.SF.Unity.Editor;

namespace KS.SceneFusion.Client.Unity.Editor
{
    /// <summary>
    /// Unauthenticated base class to use instead of <see cref="ksAuthenticatedMenu"/> to disable authentication. This
    /// class has the same properties and methods as <see cref="ksAuthenticatedMenu"/> except it is missing 
    /// <see cref="ksAuthenticatedMenu.SetLoginMenu(Type)"/>.
    /// </summary>
    public class sfUnauthenticatedMenu : ScriptableObject, ksIMenu
    {
        /// <summary>Session menus are destroyed on close</summary>
        public bool DestroyOnClose
        {
            get { return true; }
        }

        /// <summary>Icon</summary>
        public virtual Texture Icon
        {
            get { return null; }
        }

        public void Draw(ksWindow window)
        {
            OnDraw(window);
        }

        /// <summary>Called when the menu is closed.</summary>
        /// <param name="window">gui window</param>
        public virtual void OnClose(ksWindow window)
        {

        }

        /// <summary>Called when the menu is opened.</summary>
        /// <param name="window">gui window.</param>
        public virtual void OnOpen(ksWindow window)
        {

        }

        /// <summary>Draws the GUI.</summary>
        /// <param name="window">gui window</param>
        public virtual void OnDraw(ksWindow window)
        {
        }
    }
}
