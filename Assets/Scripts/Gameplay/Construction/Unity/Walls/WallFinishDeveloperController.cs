using System;
using BigRetail.Map.Domain;
using BigRetail.Map.Unity;
using BigRetail.Map.View;
using BigRetail.Map.Walls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Temporary developer control for validating per-wall, per-face finish
    /// changes before the player-facing finish tool exists.
    ///
    /// The currently visible face of the wall beneath the shared construction
    /// pointer is used as the target.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(175)]
    public sealed class WallFinishDeveloperController : MonoBehaviour
    {
        [Header("Targeting")]

        [SerializeField]
        private WallTargetResolver targetResolver;

        [SerializeField]
        private GridMapHost mapHost;


        [Header("Test Finish")]

        [SerializeField]
        private string testFinishId =
            "brick";


        [Header("Keyboard Shortcuts")]

        [SerializeField]
        private bool enableKeyboardShortcuts =
            true;

        [SerializeField]
        private Key applyFinishKey =
            Key.F7;

        [SerializeField]
        private Key resetFinishKey =
            Key.F8;


        [Header("Diagnostics")]

        [SerializeField]
        private bool logResults =
            true;


        private void Awake()
        {
            if (targetResolver == null)
            {
                Debug.LogError(
                    "WallFinishDeveloperController has no "
                    + "WallTargetResolver assigned.",
                    this);

                enabled = false;
                return;
            }

            if (mapHost == null)
            {
                Debug.LogError(
                    "WallFinishDeveloperController has no GridMapHost assigned.",
                    this);

                enabled = false;
            }
        }


        private void Update()
        {
            if (!enableKeyboardShortcuts
                || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current[applyFinishKey]
                .wasPressedThisFrame)
            {
                ApplyTestFinishToVisibleFace();
            }

            if (Keyboard.current[resetFinishKey]
                .wasPressedThisFrame)
            {
                ResetVisibleFace();
            }
        }


        [ContextMenu("Apply Test Finish To Visible Face")]
        public void ApplyTestFinishToVisibleFace()
        {
            if (!TryResolveVisibleFace(
                    out CellEdge edge,
                    out GridPosition facingCell))
            {
                return;
            }

            WallFinishId finishId;

            try
            {
                finishId =
                    new WallFinishId(
                        testFinishId);
            }
            catch (ArgumentException exception)
            {
                Debug.LogException(
                    exception,
                    this);
                return;
            }

            WallFinishChangeResult result =
                mapHost.WallFinishes.TrySetFinish(
                    edge,
                    facingCell,
                    finishId);

            LogResult(
                "apply",
                result);
        }


        [ContextMenu("Reset Visible Face")]
        public void ResetVisibleFace()
        {
            if (!TryResolveVisibleFace(
                    out CellEdge edge,
                    out GridPosition facingCell))
            {
                return;
            }

            WallFinishChangeResult result =
                mapHost.WallFinishes.TryResetFinish(
                    edge,
                    facingCell);

            LogResult(
                "reset",
                result);
        }


        private bool TryResolveVisibleFace(
            out CellEdge edge,
            out GridPosition facingCell)
        {
            edge = default;
            facingCell = default;

            if (!Application.isPlaying)
            {
                Debug.LogWarning(
                    "Wall finish developer controls require Play Mode.",
                    this);
                return false;
            }

            if (!mapHost.IsInitialized
                || mapHost.WallFinishes == null)
            {
                Debug.LogWarning(
                    "The runtime wall-finish service is not initialized.",
                    this);
                return false;
            }

            if (!targetResolver.HasTarget)
            {
                Debug.LogWarning(
                    "No wall target exists beneath the construction pointer.",
                    this);
                return false;
            }

            edge =
                targetResolver.CurrentTarget.Edge;

            if (!mapHost.WallState.HasWall(edge))
            {
                Debug.LogWarning(
                    $"The targeted edge {edge} has no structural wall.",
                    this);
                return false;
            }

            IsometricViewProjection projection =
                targetResolver.ViewProjection;

            if (projection == null)
            {
                Debug.LogWarning(
                    "The wall target has no active isometric projection.",
                    this);
                return false;
            }

            facingCell =
                WallPresentationSelector.Select(
                    edge,
                    projection)
                .ViewerFacingCell;

            return true;
        }


        private void LogResult(
            string operation,
            WallFinishChangeResult result)
        {
            if (!result.Succeeded)
            {
                Debug.LogWarning(
                    $"Wall finish {operation} was rejected for "
                    + $"{result.Edge} facing {result.FacingCell}: "
                    + $"{result.Failure}.",
                    this);
                return;
            }

            if (!logResults)
            {
                return;
            }

            Debug.Log(
                $"Wall finish {operation} "
                + (result.Changed ? "changed" : "kept")
                + $" {result.Edge} facing {result.FacingCell} at "
                + $"'{result.EffectiveFinishId}'.",
                this);
        }


        private void OnValidate()
        {
            if (testFinishId != null)
            {
                testFinishId =
                    testFinishId.Trim();
            }
        }
    }
}
