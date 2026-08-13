using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace BigRetail.EditorTools
{
    /// <summary>
    /// Requests a clean dependency resolution from Unity's Package Manager.
    /// This deliberately leaves the project manifest and package files alone.
    /// </summary>
    internal static class UnityPackageRepairTool
    {
        [MenuItem("Big Retail/Maintenance/Repair Unity Packages")]
        private static void RepairPackages()
        {
            Client.Resolve();

            Debug.Log(
                "Unity Package Manager repair started. " +
                "Let package resolution and script compilation finish before closing the Editor.");
        }
    }
}
