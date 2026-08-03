using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Map.Unity.Tests
{
    public sealed class CameraControlIconImportTests
    {
        [TestCase(
            "Assets/Art/UI/Camera/Icons/Camera_ViewNorth.png")]
        [TestCase(
            "Assets/Art/UI/Camera/Icons/Camera_ViewEast.png")]
        [TestCase(
            "Assets/Art/UI/Camera/Icons/Camera_ViewSouth.png")]
        [TestCase(
            "Assets/Art/UI/Camera/Icons/Camera_ViewWest.png")]
        public void RotationIcon_UsesSingleFullRectSprite(
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
