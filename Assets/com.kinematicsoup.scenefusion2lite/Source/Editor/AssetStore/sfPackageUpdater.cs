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
using KS.SF.Unity.Editor;

namespace KS.SceneFusion.Client.Unity.Editor
{
    ///<summary>Checks for Scene Fusion package updates and prompts users to apply the update.</summary>
    public class sfPackageUpdater : ksAssetStorePackageUpdater<sfPackageUpdater>
    {
        /// <summary>Package id</summary>
        protected override string PackageId => sfPackageInfo.PACKAGE_ID;

        /// <summary>Package name</summary>
        protected override string PackageName => "Scene Fusion";
    }
}
