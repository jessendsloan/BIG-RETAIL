using System;
using BigRetail.Map.Fixtures;
using UnityEngine;

namespace BigRetail.Map.Unity.Fixtures
{
    /// <summary>
    /// Owns the player's current fixture-merchandising selection.
    /// Selection keys remain logical and survive camera rotation.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(130)]
    public sealed class FixtureMerchandisingSelectionHost : MonoBehaviour
    {
        [SerializeField]
        private FixtureRuntimeHost fixtureRuntimeHost;


        public bool HasSelectedFixture { get; private set; }

        public FixtureInstanceId SelectedFixtureId { get; private set; }

        public bool IsEditing { get; private set; }

        public bool HasSelectedFrontageUnit { get; private set; }

        public FixtureShelfRunKey SelectedShelfRun { get; private set; }

        public int SelectedFrontageUnitIndex { get; private set; }

        public int RequestedFrontageUnitCount { get; private set; } = 1;

        public event Action SelectionChanged;


        private void OnEnable()
        {
            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized += HandleFixtureRuntimeInitialized;
            }

            TryAttachToState();
        }

        private void OnDisable()
        {
            if (fixtureRuntimeHost != null)
            {
                fixtureRuntimeHost.Initialized -= HandleFixtureRuntimeInitialized;

                if (fixtureRuntimeHost.FixtureState != null)
                {
                    fixtureRuntimeHost.FixtureState.FixtureRemoved -=
                        HandleFixtureRemoved;
                }
            }
        }


        public bool SelectFixture(FixtureInstanceId fixtureId)
        {
            if (fixtureRuntimeHost == null
                || fixtureRuntimeHost.FixtureState == null
                || !fixtureRuntimeHost.FixtureState.TryGetFixture(
                    fixtureId,
                    out FixtureInstance fixture)
                || (!fixture.Definition.MerchandisingProfile.HasDisplayFaces
                    && !fixture.Definition.StorageProfile
                        .ProvidesBackstockStorage))
            {
                return false;
            }

            bool changed =
                !HasSelectedFixture
                || SelectedFixtureId != fixtureId
                || IsEditing
                || HasSelectedFrontageUnit;

            HasSelectedFixture = true;
            SelectedFixtureId = fixtureId;
            IsEditing = false;
            ClearFrontageSelectionWithoutNotification();

            if (changed)
            {
                SelectionChanged?.Invoke();
            }

            return true;
        }

        public bool BeginEditing()
        {
            if (!HasSelectedFixture
                || fixtureRuntimeHost == null
                || fixtureRuntimeHost.FixtureState == null
                || !fixtureRuntimeHost.FixtureState.TryGetFixture(
                    SelectedFixtureId,
                    out FixtureInstance fixture)
                || !fixture.Definition.MerchandisingProfile.HasDisplayFaces)
            {
                return false;
            }

            if (IsEditing)
            {
                return true;
            }

            IsEditing = true;
            SelectionChanged?.Invoke();
            return true;
        }

        public void EndEditing()
        {
            if (!IsEditing && !HasSelectedFrontageUnit)
            {
                return;
            }

            IsEditing = false;
            ClearFrontageSelectionWithoutNotification();
            SelectionChanged?.Invoke();
        }

        public bool SelectFrontageUnit(
            FixtureShelfRunKey shelfRun,
            int frontageUnitIndex)
        {
            if (!IsEditing
                || !HasSelectedFixture
                || shelfRun.FixtureId != SelectedFixtureId
                || fixtureRuntimeHost == null
                || fixtureRuntimeHost.FixtureState == null
                || !fixtureRuntimeHost.FixtureState.TryGetFixture(
                    SelectedFixtureId,
                    out FixtureInstance fixture)
                || !fixture.Definition.MerchandisingProfile.TryGetDisplayFace(
                    shelfRun.LocalDisplaySide,
                    out FixtureDisplayFaceDefinition displayFace)
                || shelfRun.ShelfRunIndex >= displayFace.ShelfRunCount
                || frontageUnitIndex < 0
                || frontageUnitIndex >= displayFace.FrontageUnitsPerRun)
            {
                return false;
            }

            HasSelectedFrontageUnit = true;
            SelectedShelfRun = shelfRun;
            SelectedFrontageUnitIndex = frontageUnitIndex;
            RequestedFrontageUnitCount = 1;
            SelectionChanged?.Invoke();
            return true;
        }

        public void SetRequestedFrontageUnitCount(int frontageUnitCount)
        {
            int clampedCount = Mathf.Clamp(frontageUnitCount, 1, 4);

            if (RequestedFrontageUnitCount == clampedCount)
            {
                return;
            }

            RequestedFrontageUnitCount = clampedCount;
            SelectionChanged?.Invoke();
        }

        public void ClearFrontageSelection()
        {
            if (!HasSelectedFrontageUnit)
            {
                return;
            }

            ClearFrontageSelectionWithoutNotification();
            SelectionChanged?.Invoke();
        }

        public void ClearSelection()
        {
            if (!HasSelectedFixture
                && !IsEditing
                && !HasSelectedFrontageUnit)
            {
                return;
            }

            HasSelectedFixture = false;
            SelectedFixtureId = default;
            IsEditing = false;
            ClearFrontageSelectionWithoutNotification();
            SelectionChanged?.Invoke();
        }


        private void ClearFrontageSelectionWithoutNotification()
        {
            HasSelectedFrontageUnit = false;
            SelectedShelfRun = default;
            SelectedFrontageUnitIndex = 0;
            RequestedFrontageUnitCount = 1;
        }

        private void TryAttachToState()
        {
            if (fixtureRuntimeHost == null
                || !fixtureRuntimeHost.IsInitialized
                || fixtureRuntimeHost.FixtureState == null)
            {
                return;
            }

            fixtureRuntimeHost.FixtureState.FixtureRemoved -=
                HandleFixtureRemoved;
            fixtureRuntimeHost.FixtureState.FixtureRemoved +=
                HandleFixtureRemoved;
        }

        private void HandleFixtureRuntimeInitialized(
            FixtureRuntimeHost initializedHost)
        {
            TryAttachToState();
        }

        private void HandleFixtureRemoved(FixtureInstance fixture)
        {
            if (HasSelectedFixture
                && fixture.Id == SelectedFixtureId)
            {
                ClearSelection();
            }
        }
    }
}
