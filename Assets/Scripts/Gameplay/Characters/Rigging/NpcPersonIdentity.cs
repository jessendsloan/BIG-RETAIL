using System;
using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    /// <summary>
    /// Connects one live Person rig to a repeatable population-generated
    /// appearance. Customers may use only a seed; persistent people such as
    /// employees can additionally keep a simulation-owned identifier.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NpcCutoutRig))]
    public sealed class NpcPersonIdentity : MonoBehaviour
    {
        [SerializeField]
        private NpcCutoutRig rig;

        [SerializeField]
        private NpcPopulationDefinition populationDefinition;

        [SerializeField]
        private int appearanceSeed;

        [Tooltip(
            "Stable simulation identifier for a persistent person, such " +
            "as an employee. Transient customers may leave this empty.")]
        [SerializeField]
        private string persistentId = string.Empty;

        [Tooltip(
            "Optional prefab/scene convenience. Runtime population systems " +
            "should normally initialize people explicitly.")]
        [SerializeField]
        private bool initializeOnAwake;

        [NonSerialized]
        private NpcAppearanceSelection currentAppearance;


        public NpcPopulationDefinition PopulationDefinition =>
            populationDefinition;

        public int AppearanceSeed => appearanceSeed;

        public string PersistentId => persistentId;

        public NpcAppearanceSelection CurrentAppearance =>
            currentAppearance?.Copy();

        public bool HasGeneratedAppearance =>
            currentAppearance != null;

        public bool InitializeOnAwake => initializeOnAwake;


        private void Awake()
        {
            ResolveRig();

            if (!initializeOnAwake)
            {
                return;
            }

            if (!TryRegenerate(out string failureReason))
            {
                Debug.LogError(
                    $"Could not initialize {name}'s population appearance: " +
                    failureReason,
                    this);
            }
        }


        private void OnValidate()
        {
            ResolveRig();
        }


        /// <summary>
        /// Generates and applies a repeatable appearance for this live Person.
        /// Existing identity state is preserved if generation or application
        /// fails.
        /// </summary>
        public bool TryInitialize(
            NpcPopulationDefinition definition,
            int seed,
            string newPersistentId,
            out string failureReason)
        {
            ResolveRig();

            if (rig == null)
            {
                failureReason = "The Person has no NPC cutout rig.";
                return false;
            }

            if (!NpcAppearanceGenerator.TryGenerate(
                    definition,
                    seed,
                    null,
                    null,
                    out NpcAppearanceSelection generatedAppearance,
                    out failureReason))
            {
                return false;
            }

            if (!rig.TrySetAppearanceSelection(
                    generatedAppearance,
                    out failureReason))
            {
                return false;
            }

            populationDefinition = definition;
            appearanceSeed = seed;
            persistentId = newPersistentId ?? string.Empty;
            currentAppearance = generatedAppearance.Copy();
            failureReason = string.Empty;
            return true;
        }


        /// <summary>
        /// Rebuilds the exact appearance from the stored population definition
        /// and seed. This is the handoff point for a future save/load system.
        /// </summary>
        public bool TryRegenerate(
            out string failureReason)
        {
            return TryInitialize(
                populationDefinition,
                appearanceSeed,
                persistentId,
                out failureReason);
        }


        public void ClearIdentity()
        {
            populationDefinition = null;
            appearanceSeed = 0;
            persistentId = string.Empty;
            currentAppearance = null;

            ResolveRig();
            rig?.ClearAppearanceSelection();
        }


        private void ResolveRig()
        {
            if (rig == null)
            {
                rig = GetComponent<NpcCutoutRig>();
            }
        }
    }
}
