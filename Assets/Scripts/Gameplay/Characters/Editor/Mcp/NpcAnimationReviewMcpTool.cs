using System;
using BigRetail.Characters.Rigging;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEngine;

namespace BigRetail.Characters.Editor
{
    public sealed class NpcAnimationReviewMcpParameters
    {
        [McpDescription(
            "AnimationClip asset path, for example "
            + "Assets/Animations/Characters/Core/Person_Walk_NorthFacing.anim.",
            Required = true)]
        public string ClipAssetPath { get; set; }

        [McpDescription(
            "Compass facing used for the disposable review render.",
            Required = true,
            EnumType = typeof(NpcFacing))]
        public string Facing { get; set; }
    }


    public static class NpcAnimationReviewMcpTool
    {
        public const string ToolName =
            "BigRetail_CaptureNpcAnimationReview";

        private const string PersonPrefabPath =
            "Assets/Prefabs/Characters/Core/Person.prefab";


        [McpTool(
            ToolName,
            "Render an NPC animation as an 11-frame contact sheet and pose "
            + "manifest for a requested compass facing. The review uses a "
            + "hidden disposable Person copy and does not alter scene objects "
            + "or animation assets.",
            "Capture BIG-RETAIL NPC Animation Review",
            Groups = new[] { "editor", "validation" },
            EnabledByDefault = true)]
        public static object Capture(
            NpcAnimationReviewMcpParameters parameters)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                throw new InvalidOperationException(
                    "NPC animation reviews are available only in Edit Mode.");
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                throw new InvalidOperationException(
                    "Unity is compiling or importing assets. Try the capture "
                    + "again after the Editor becomes idle.");
            }

            if (string.IsNullOrWhiteSpace(parameters.ClipAssetPath))
            {
                throw new ArgumentException(
                    "An AnimationClip asset path is required.",
                    nameof(parameters));
            }

            if (!Enum.TryParse(
                    parameters.Facing,
                    true,
                    out NpcFacing facing)
                || !Enum.IsDefined(typeof(NpcFacing), facing))
            {
                throw new ArgumentException(
                    $"Unknown NPC facing '{parameters.Facing}'. Use one of: "
                    + string.Join(", ", Enum.GetNames(typeof(NpcFacing)))
                    + ".",
                    nameof(parameters));
            }

            AnimationClip clip =
                AssetDatabase.LoadAssetAtPath<AnimationClip>(
                    parameters.ClipAssetPath);

            if (clip == null)
            {
                throw new ArgumentException(
                    $"No AnimationClip exists at "
                    + $"'{parameters.ClipAssetPath}'.",
                    nameof(parameters));
            }

            if (!NpcAnimationReviewCapture.IsFacingCompatible(clip, facing))
            {
                throw new ArgumentException(
                    $"The {clip.name} clip does not match the {facing} "
                    + "animation family.",
                    nameof(parameters));
            }

            GameObject personPrefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(PersonPrefabPath);
            NpcCutoutRig sourceRig = personPrefab == null
                ? null
                : personPrefab.GetComponentInChildren<NpcCutoutRig>(true);

            if (sourceRig == null)
            {
                throw new InvalidOperationException(
                    $"The canonical Person prefab at '{PersonPrefabPath}' "
                    + "is missing or has no NPC cutout rig.");
            }

            NpcAnimationReviewCaptureResult result =
                NpcAnimationReviewCapture.Capture(
                    sourceRig,
                    clip,
                    facing);

            return new
            {
                success = true,
                clip = clip.name,
                facing = facing.ToString(),
                sampleCount = NpcAnimationReviewCapture.SampleCount,
                reviewFolder = result.ReviewFolder,
                imagePath = result.ImagePath,
                poseDataPath = result.DataPath
            };
        }
    }
}
