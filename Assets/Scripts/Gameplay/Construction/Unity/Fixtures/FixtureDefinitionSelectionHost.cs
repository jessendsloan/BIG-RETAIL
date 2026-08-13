using System;
using System.Collections.Generic;
using BigRetail.Map.Fixtures;
using BigRetail.Map.Unity.Fixtures;
using UnityEngine;

namespace BigRetail.Construction.Unity.Fixtures
{
    /// <summary>
    /// Owns the fixture model and world-space orientation currently selected
    /// by the player-facing fixture tool.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(125)]
    public sealed class FixtureDefinitionSelectionHost : MonoBehaviour
    {
        [SerializeField]
        private FixtureRuntimeHost runtimeHost;

        [SerializeField]
        private FixtureDefinitionAsset startingDefinition;

        [SerializeField]
        private FixtureOrientation startingOrientation =
            FixtureOrientation.North;

        [SerializeField]
        private bool logSelectionChanges = true;


        public bool IsInitialized { get; private set; }

        public FixtureDefinitionId SelectedDefinitionId { get; private set; }

        public FixtureDefinitionAsset SelectedDefinitionAsset { get; private set; }

        public FixtureOrientation Orientation { get; private set; }


        public event Action<FixtureDefinitionId> SelectedDefinitionChanged;

        public event Action<FixtureOrientation> OrientationChanged;


        private void Awake()
        {
            if (runtimeHost == null)
            {
                Debug.LogError(
                    "FixtureDefinitionSelectionHost has no FixtureRuntimeHost assigned.",
                    this);
                enabled = false;
            }
        }


        private void OnEnable()
        {
            if (runtimeHost != null)
            {
                runtimeHost.Initialized += HandleRuntimeInitialized;
            }
        }


        private void Start()
        {
            if (runtimeHost != null && runtimeHost.IsInitialized)
            {
                InitializeSelection();
            }
        }


        public IEnumerable<FixtureDefinitionAsset> EnumerateAvailableDefinitions()
        {
            if (!IsInitialized
                || runtimeHost == null
                || runtimeHost.DefinitionAssets == null)
            {
                return Array.Empty<FixtureDefinitionAsset>();
            }

            return runtimeHost.DefinitionAssets.EnumerateAssets();
        }


        public bool SelectDefinition(FixtureDefinitionAsset definitionAsset)
        {
            return definitionAsset != null
                && SelectDefinition(definitionAsset.Id);
        }


        public bool SelectDefinition(FixtureDefinitionId definitionId)
        {
            if (!IsInitialized
                || runtimeHost.Definitions == null
                || !runtimeHost.Definitions.Contains(definitionId)
                || !runtimeHost.DefinitionAssets.TryGetAsset(
                    definitionId,
                    out FixtureDefinitionAsset asset))
            {
                return false;
            }

            if (SelectedDefinitionId == definitionId)
            {
                return true;
            }

            SelectedDefinitionId = definitionId;
            SelectedDefinitionAsset = asset;
            SelectedDefinitionChanged?.Invoke(definitionId);

            if (logSelectionChanges)
            {
                Debug.Log($"Fixture tool selected '{definitionId}'.", this);
            }

            return true;
        }


        public void RotateClockwise()
        {
            SetOrientation(Orientation.RotateClockwise());
        }


        public void RotateCounterClockwise()
        {
            SetOrientation(Orientation.RotateCounterClockwise());
        }


        public bool SetOrientation(FixtureOrientation orientation)
        {
            if (!orientation.IsSupported() || Orientation == orientation)
            {
                return false;
            }

            Orientation = orientation;
            OrientationChanged?.Invoke(orientation);
            return true;
        }


        private void HandleRuntimeInitialized(FixtureRuntimeHost initializedHost)
        {
            InitializeSelection();
        }


        private void InitializeSelection()
        {
            if (IsInitialized
                || runtimeHost == null
                || !runtimeHost.IsInitialized
                || runtimeHost.Definitions == null
                || runtimeHost.DefinitionAssets == null)
            {
                return;
            }

            Orientation =
                startingOrientation.IsSupported()
                    ? startingOrientation
                    : FixtureOrientation.North;

            IsInitialized = true;

            FixtureDefinitionAsset initial =
                startingDefinition != null
                    ? startingDefinition
                    : runtimeHost.DefinitionAssets.DefaultDefinition;

            if (!SelectDefinition(initial))
            {
                IsInitialized = false;
                enabled = false;

                Debug.LogError(
                    "FixtureDefinitionSelectionHost could not select its starting fixture definition.",
                    this);
            }
        }


        private void OnDisable()
        {
            if (runtimeHost != null)
            {
                runtimeHost.Initialized -= HandleRuntimeInitialized;
            }
        }
    }
}
