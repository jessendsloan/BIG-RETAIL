using System;
using System.Collections.Generic;
using BigRetail.Map.Unity;
using BigRetail.Map.Unity.Doors;
using BigRetail.Map.Walls;
using UnityEngine;

namespace BigRetail.Construction.Unity.Doors
{
    /// <summary>
    /// Owns the door model currently selected by the player-facing door tool.
    /// This is transient tool state, not placed-map state.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(125)]
    public sealed class DoorDefinitionSelectionHost : MonoBehaviour
    {
        [SerializeField]
        private GridMapHost mapHost;

        [Tooltip(
            "Door selected when Gameplay begins. When empty, the catalog "
            + "default is selected.")]
        [SerializeField]
        private DoorDefinitionAsset startingDefinition;

        [SerializeField]
        private bool logSelectionChanges = true;


        public bool IsInitialized { get; private set; }

        public DoorDefinitionId SelectedDefinitionId { get; private set; }

        public DoorDefinitionAsset SelectedDefinitionAsset
        {
            get;
            private set;
        }


        public event Action<DoorDefinitionId> SelectedDefinitionChanged;


        private void Awake()
        {
            if (mapHost == null)
            {
                Debug.LogError(
                    "DoorDefinitionSelectionHost has no GridMapHost assigned.",
                    this);

                enabled = false;
            }
        }


        private void OnEnable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized +=
                    HandleMapInitialized;
            }
        }


        private void Start()
        {
            if (mapHost != null
                && mapHost.IsInitialized)
            {
                InitializeSelection();
            }
        }


        public IEnumerable<DoorDefinitionAsset>
            EnumerateAvailableDefinitions()
        {
            if (!IsInitialized
                || mapHost == null
                || mapHost.DoorDefinitionAssets == null)
            {
                return Array.Empty<DoorDefinitionAsset>();
            }

            return mapHost.DoorDefinitionAssets.EnumerateAssets();
        }


        public bool SelectDefinition(
            DoorDefinitionAsset definitionAsset)
        {
            if (!IsInitialized
                || definitionAsset == null)
            {
                return false;
            }

            DoorDefinitionId definitionId;

            try
            {
                definitionId =
                    definitionAsset.Id;
            }
            catch (Exception exception)
            {
                Debug.LogException(
                    exception,
                    definitionAsset);
                return false;
            }

            return SelectDefinition(
                definitionId);
        }


        public bool SelectDefinition(
            DoorDefinitionId definitionId)
        {
            if (!IsInitialized
                || mapHost.DoorDefinitions == null
                || !mapHost.DoorDefinitions.Contains(
                    definitionId)
                || !mapHost.DoorDefinitionAssets.TryGetAsset(
                    definitionId,
                    out DoorDefinitionAsset asset))
            {
                return false;
            }

            if (SelectedDefinitionId == definitionId)
            {
                return true;
            }

            SelectedDefinitionId =
                definitionId;

            SelectedDefinitionAsset =
                asset;

            SelectedDefinitionChanged?.Invoke(
                definitionId);

            if (logSelectionChanges)
            {
                Debug.Log(
                    $"Door tool selected '{definitionId}'.",
                    this);
            }

            return true;
        }


        private void HandleMapInitialized(
            GridMapHost initializedHost)
        {
            InitializeSelection();
        }


        private void InitializeSelection()
        {
            if (IsInitialized
                || mapHost == null
                || !mapHost.IsInitialized
                || mapHost.DoorDefinitions == null
                || mapHost.DoorDefinitionAssets == null)
            {
                return;
            }

            IsInitialized = true;

            DoorDefinitionAsset initial =
                startingDefinition != null
                    ? startingDefinition
                    : mapHost.DoorDefinitionAssets.DefaultDefinition;

            if (!SelectDefinition(initial))
            {
                IsInitialized = false;
                enabled = false;

                Debug.LogError(
                    "DoorDefinitionSelectionHost could not select its "
                    + "starting door definition.",
                    this);
            }
        }


        private void OnDisable()
        {
            if (mapHost != null)
            {
                mapHost.Initialized -=
                    HandleMapInitialized;
            }
        }
    }
}
