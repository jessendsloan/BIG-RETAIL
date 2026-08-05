using System;
using BigRetail.Map.Unity.Walls;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Editor.Walls
{
    /// <summary>
    /// One-time setup for the replace-in-place wall-view art handoff.
    /// It mirrors each full wall's sprite-import settings onto its low variant
    /// and stores the low sprite references on the matching finish asset.
    /// </summary>
    public static class WallViewArtworkSetupMenu
    {
        private const string MenuPath =
            "Big Retail/Walls/Refresh Wall View Artwork";

        private const string WallIconImportTemplatePath =
            "Assets/Art/UI/Construction/Icons/Icon_Walls.png";

        private static readonly string[] WallViewIconPaths =
        {
            "Assets/Art/UI/WallView/Icons/WallView_WallsUp.png",
            "Assets/Art/UI/WallView/Icons/WallView_Cutaway.png",
            "Assets/Art/UI/WallView/Icons/WallView_WallsDown.png"
        };

        private static readonly WallArtworkPair[] ArtworkPairs =
        {
            new WallArtworkPair(
                "Assets/Design/Walls/Finishes/DefaultWallFinish.asset",
                "Assets/Art/WallSegmentArt/Finishes/Default_RisingLeft.png",
                "Assets/Art/WallSegmentArt/Low/Default_Low_RisingLeft.png",
                "Assets/Art/WallSegmentArt/Finishes/Default_RisingRight.png",
                "Assets/Art/WallSegmentArt/Low/Default_Low_RisingRight.png"),

            new WallArtworkPair(
                "Assets/Design/Walls/Finishes/BrickWallFinish.asset",
                "Assets/Art/WallSegmentArt/Finishes/Brick_RisingLeft.png",
                "Assets/Art/WallSegmentArt/Low/Brick_Low_RisingLeft.png",
                "Assets/Art/WallSegmentArt/Finishes/Brick_RisingRight.png",
                "Assets/Art/WallSegmentArt/Low/Brick_Low_RisingRight.png"),

            new WallArtworkPair(
                "Assets/Design/Walls/Finishes/WhiteWallFinish.asset",
                "Assets/Art/WallSegmentArt/Finishes/WhiteWallRisingLeft.png",
                "Assets/Art/WallSegmentArt/Low/White_Low_RisingLeft.png",
                "Assets/Art/WallSegmentArt/Finishes/WhiteWallRisingRight.png",
                "Assets/Art/WallSegmentArt/Low/White_Low_RisingRight.png"),

            new WallArtworkPair(
                "Assets/Design/Walls/Finishes/WoodWallFinish.asset",
                "Assets/Art/WallSegmentArt/Finishes/WoodWallRisingLeft.png",
                "Assets/Art/WallSegmentArt/Low/Wood_Low_RisingLeft.png",
                "Assets/Art/WallSegmentArt/Finishes/WoodWallRisingRight.png",
                "Assets/Art/WallSegmentArt/Low/Wood_Low_RisingRight.png")
        };


        [MenuItem(MenuPath)]
        public static void RefreshWallViewArtwork()
        {
            AssetDatabase.Refresh();

            foreach (WallArtworkPair pair in ArtworkPairs)
            {
                SyncSpriteImportSettings(
                    pair.FullRisingLeftPath,
                    pair.LowRisingLeftPath);

                SyncSpriteImportSettings(
                    pair.FullRisingRightPath,
                    pair.LowRisingRightPath);

                AssignLowSprites(pair);
            }

            foreach (string iconPath in WallViewIconPaths)
            {
                SyncSpriteImportSettings(
                    WallIconImportTemplatePath,
                    iconPath);
            }

            AssetDatabase.SaveAssets();

            Debug.Log(
                "Wall-view artwork is ready. Low wall sprites are wired to "
                + "all four wall finishes.");
        }


        private static void SyncSpriteImportSettings(
            string sourcePath,
            string destinationPath)
        {
            TextureImporter sourceImporter =
                AssetImporter.GetAtPath(sourcePath)
                as TextureImporter;

            TextureImporter destinationImporter =
                AssetImporter.GetAtPath(destinationPath)
                as TextureImporter;

            if (sourceImporter == null
                || destinationImporter == null)
            {
                throw new InvalidOperationException(
                    "Could not load wall sprite importers for '"
                    + sourcePath
                    + "' and '"
                    + destinationPath
                    + "'.");
            }

            TextureImporterSettings settings =
                new TextureImporterSettings();

            sourceImporter.ReadTextureSettings(settings);
            destinationImporter.SetTextureSettings(settings);
            destinationImporter.maxTextureSize =
                sourceImporter.maxTextureSize;
            destinationImporter.textureCompression =
                sourceImporter.textureCompression;
            destinationImporter.SaveAndReimport();
        }


        private static void AssignLowSprites(
            WallArtworkPair pair)
        {
            WallFinishAsset finish =
                AssetDatabase.LoadAssetAtPath<WallFinishAsset>(
                    pair.FinishAssetPath);

            Sprite lowRisingLeft =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    pair.LowRisingLeftPath);

            Sprite lowRisingRight =
                AssetDatabase.LoadAssetAtPath<Sprite>(
                    pair.LowRisingRightPath);

            if (finish == null
                || lowRisingLeft == null
                || lowRisingRight == null)
            {
                throw new InvalidOperationException(
                    "Could not load all wall-view artwork for '"
                    + pair.FinishAssetPath
                    + "'.");
            }

            Undo.RecordObject(
                finish,
                "Refresh Wall View Artwork");

            SerializedObject serializedFinish =
                new SerializedObject(finish);

            serializedFinish.FindProperty("lowRisingLeft")
                .objectReferenceValue = lowRisingLeft;

            serializedFinish.FindProperty("lowRisingRight")
                .objectReferenceValue = lowRisingRight;

            serializedFinish.ApplyModifiedProperties();
            EditorUtility.SetDirty(finish);
        }


        private readonly struct WallArtworkPair
        {
            public string FinishAssetPath { get; }
            public string FullRisingLeftPath { get; }
            public string LowRisingLeftPath { get; }
            public string FullRisingRightPath { get; }
            public string LowRisingRightPath { get; }


            public WallArtworkPair(
                string finishAssetPath,
                string fullRisingLeftPath,
                string lowRisingLeftPath,
                string fullRisingRightPath,
                string lowRisingRightPath)
            {
                FinishAssetPath = finishAssetPath;
                FullRisingLeftPath = fullRisingLeftPath;
                LowRisingLeftPath = lowRisingLeftPath;
                FullRisingRightPath = fullRisingRightPath;
                LowRisingRightPath = lowRisingRightPath;
            }
        }
    }
}
