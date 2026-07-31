using System.IO;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    /// <summary>
    /// Headless entry points used by Codex and CI to generate and
    /// visually inspect the NPC rig without opening an Editor window.
    /// </summary>
    public static class NpcRigLabBatch
    {
        private const string RowanPrefabPath =
            "Assets/Prefabs/Characters/Prototype/"
            + "RoundedEmployeeRowan.prefab";

        private const string PreviewRelativePath =
            "Logs/RowanWalkPreview.png";

        private const int PanelSize = 384;


        public static void GenerateAndRenderRowan()
        {
            NpcRigLabGenerator.CreateRoundedEmployeeRowan();

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(
                    RowanPrefabPath);

            AnimationClip walkClip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    NpcRigLabAnimationGenerator.WalkClipPath);

            if (prefab == null
                || walkClip == null)
            {
                throw new UnityException(
                    "Rowan preview requires both the generated "
                    + "prefab and walk clip.");
            }

            GameObject instance =
                Object.Instantiate(
                    prefab);

            instance.name = "Rowan Preview";

            Animator animator =
                instance.GetComponent<Animator>();

            if (animator != null)
            {
                animator.enabled = false;
            }

            GameObject cameraObject =
                new GameObject(
                    "Rowan Preview Camera");

            Camera camera =
                cameraObject.AddComponent<Camera>();

            ConfigureCamera(
                camera);

            Texture2D preview =
                new Texture2D(
                    PanelSize * 2,
                    PanelSize * 2,
                    TextureFormat.RGBA32,
                    false);

            try
            {
                float[] sampleTimes =
                    {
                        0f,
                        0.20f,
                        0.40f,
                        0.60f
                    };

                for (int index = 0;
                     index < sampleTimes.Length;
                     index++)
                {
                    walkClip.SampleAnimation(
                        instance,
                        sampleTimes[index]);

                    Texture2D panel =
                        RenderPanel(
                            camera);

                    int x =
                        index % 2 * PanelSize;

                    int y =
                        (1 - index / 2) * PanelSize;

                    preview.SetPixels(
                        x,
                        y,
                        PanelSize,
                        PanelSize,
                        panel.GetPixels());

                    Object.DestroyImmediate(
                        panel);
                }

                preview.Apply();

                string projectRoot =
                    Directory.GetParent(
                        Application.dataPath)?.FullName
                    ?? Application.dataPath;

                string previewPath =
                    Path.Combine(
                        projectRoot,
                        PreviewRelativePath);

                Directory.CreateDirectory(
                    Path.GetDirectoryName(
                        previewPath)
                    ?? projectRoot);

                File.WriteAllBytes(
                    previewPath,
                    preview.EncodeToPNG());

                Debug.Log(
                    $"Rendered Rowan walk preview to "
                    + $"'{previewPath}'.");
            }
            finally
            {
                Object.DestroyImmediate(
                    preview);

                Object.DestroyImmediate(
                    cameraObject);

                Object.DestroyImmediate(
                    instance);
            }
        }


        private static void ConfigureCamera(
            Camera camera)
        {
            camera.orthographic = true;
            camera.orthographicSize = 1.28f;
            camera.transform.position =
                new Vector3(
                    0f,
                    1.08f,
                    -10f);
            camera.clearFlags =
                CameraClearFlags.SolidColor;
            camera.backgroundColor =
                new Color(
                    0.105f,
                    0.12f,
                    0.14f,
                    1f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 20f;
        }


        private static Texture2D RenderPanel(
            Camera camera)
        {
            RenderTexture renderTexture =
                RenderTexture.GetTemporary(
                    PanelSize,
                    PanelSize,
                    24,
                    RenderTextureFormat.ARGB32);

            RenderTexture previousActive =
                RenderTexture.active;

            RenderTexture previousTarget =
                camera.targetTexture;

            try
            {
                camera.targetTexture =
                    renderTexture;

                camera.Render();

                RenderTexture.active =
                    renderTexture;

                Texture2D panel =
                    new Texture2D(
                        PanelSize,
                        PanelSize,
                        TextureFormat.RGBA32,
                        false);

                panel.ReadPixels(
                    new Rect(
                        0f,
                        0f,
                        PanelSize,
                        PanelSize),
                    0,
                    0);

                panel.Apply();

                return panel;
            }
            finally
            {
                camera.targetTexture =
                    previousTarget;

                RenderTexture.active =
                    previousActive;

                RenderTexture.ReleaseTemporary(
                    renderTexture);
            }
        }
    }
}
