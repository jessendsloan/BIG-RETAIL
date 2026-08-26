using System;
using System.IO;
using BigRetail.StoreLayouts.Unity;
using UnityEditor;
using UnityEngine;

namespace BigRetail.StoreLayouts.Editor
{
    /// <summary>
    /// Owns deliberate StoreLayoutAsset persistence. Capture and validation
    /// happen before this boundary so a rejected draft never touches an asset.
    /// </summary>
    public static class StoreLayoutAssetWriter
    {
        public const string DefaultAssetFolder =
            "Assets/Design/StoreLayouts";


        public static StoreLayoutAsset CreateNew(
            string assetPath,
            StoreLayoutData layout)
        {
            string normalizedPath = ValidateAssetPath(assetPath);

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (AssetDatabase.LoadMainAssetAtPath(normalizedPath) != null)
            {
                throw new InvalidOperationException(
                    $"An asset already exists at '{normalizedPath}'. Use the "
                    + "explicit update command to replace a layout.");
            }

            string folder =
                Path.GetDirectoryName(normalizedPath)
                    ?.Replace('\\', '/');

            if (string.IsNullOrEmpty(folder)
                || !AssetDatabase.IsValidFolder(folder))
            {
                throw new InvalidOperationException(
                    $"The destination folder '{folder}' does not exist.");
            }

            StoreLayoutAsset asset =
                ScriptableObject.CreateInstance<StoreLayoutAsset>();
            asset.ReplaceData(layout);

            AssetDatabase.CreateAsset(asset, normalizedPath);
            Undo.RegisterCreatedObjectUndo(
                asset,
                "Create Store Layout");
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);

            return asset;
        }


        public static void UpdateExisting(
            StoreLayoutAsset asset,
            StoreLayoutData layout)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            string assetPath = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(assetPath))
            {
                throw new InvalidOperationException(
                    "The selected StoreLayoutAsset is not a saved project "
                    + "asset.");
            }

            Undo.RecordObject(asset, "Update Store Layout");
            asset.ReplaceData(layout);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssetIfDirty(asset);
        }


        public static bool Matches(
            StoreLayoutAsset asset,
            StoreLayoutData runtimeLayout)
        {
            if (asset == null || runtimeLayout == null)
            {
                return false;
            }

            StoreDataCanonicalizer canonicalizer =
                new StoreDataCanonicalizer();
            string assetJson =
                JsonUtility.ToJson(asset.CreateRuntimeCopy());
            string runtimeJson =
                JsonUtility.ToJson(
                    canonicalizer.CreateCanonicalCopy(runtimeLayout));

            return string.Equals(
                assetJson,
                runtimeJson,
                StringComparison.Ordinal);
        }


        public static void EnsureDefaultFolder()
        {
            EnsureFolder(DefaultAssetFolder);
        }


        private static string ValidateAssetPath(
            string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                throw new ArgumentException(
                    "An asset path is required.",
                    nameof(assetPath));
            }

            string normalized = assetPath.Trim().Replace('\\', '/');

            if (!normalized.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal)
                || !string.Equals(
                    Path.GetExtension(normalized),
                    ".asset",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Store layouts must be saved as .asset files inside the "
                    + "project Assets folder.",
                    nameof(assetPath));
            }

            return normalized;
        }


        private static void EnsureFolder(
            string folderPath)
        {
            string normalized = folderPath.Replace('\\', '/').TrimEnd('/');

            if (AssetDatabase.IsValidFolder(normalized))
            {
                return;
            }

            string parent =
                Path.GetDirectoryName(normalized)
                    ?.Replace('\\', '/');

            if (string.IsNullOrEmpty(parent))
            {
                throw new InvalidOperationException(
                    $"Cannot create asset folder '{normalized}'.");
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(
                parent,
                Path.GetFileName(normalized));
        }
    }
}
