using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class WallViewIconImportTests
    {
        [TestCase(
            "Assets/Art/UI/WallView/Icons/WallView_WallsUp.png")]
        [TestCase(
            "Assets/Art/UI/WallView/Icons/WallView_Cutaway.png")]
        [TestCase(
            "Assets/Art/UI/WallView/Icons/WallView_WallsDown.png")]
        public void VisibilityIcon_UsesSingleFullRectSprite(
            string assetPath)
        {
            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath)
                as TextureImporter;

            Assert.That(
                importer,
                Is.Not.Null,
                $"Could not load TextureImporter for '{assetPath}'.");

            Assert.That(
                importer.textureType,
                Is.EqualTo(TextureImporterType.Sprite));

            Assert.That(
                importer.spriteImportMode,
                Is.EqualTo(SpriteImportMode.Single));

            TextureImporterSettings settings =
                new TextureImporterSettings();

            importer.ReadTextureSettings(settings);

            Assert.That(
                settings.spriteMeshType,
                Is.EqualTo(SpriteMeshType.FullRect));
        }
    }
}
