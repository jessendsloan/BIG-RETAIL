using System;
using BigRetail.Map.Domain;
using BigRetail.Map.View;
using BigRetail.Map.Walls;

namespace BigRetail.Map.Unity.Doors
{
    /// <summary>
    /// Resolves a placed door edge into its authored panel presentation.
    /// Door state remains authoritative; missing art safely falls back to the
    /// ordinary wall sprite at the caller.
    /// </summary>
    public sealed class DoorPresentationResolver
    {
        private readonly DoorAssemblyState assemblyState;
        private readonly DoorDefinitionAssetCatalog assetCatalog;


        public DoorPresentationResolver(
            DoorAssemblyState assemblyState,
            DoorDefinitionAssetCatalog assetCatalog)
        {
            this.assemblyState =
                assemblyState
                ?? throw new ArgumentNullException(
                    nameof(assemblyState));

            this.assetCatalog =
                assetCatalog
                ?? throw new ArgumentNullException(
                    nameof(assetCatalog));
        }


        public bool TryResolvePanel(
            CellEdge edge,
            out DoorAssembly assembly,
            out DoorDefinitionAsset definitionAsset,
            out int panelIndex)
        {
            if (!assemblyState.TryGetAssemblyAtEdge(
                    edge,
                    out assembly)
                || !assembly.TryGetSegmentIndex(
                    edge,
                    out panelIndex)
                || !assetCatalog.TryGetAsset(
                    assembly.DefinitionId,
                    out definitionAsset))
            {
                assembly = null;
                definitionAsset = null;
                panelIndex = -1;
                return false;
            }

            return true;
        }


        public bool TryResolveDefinitionAsset(
            DoorAssembly assembly,
            out DoorDefinitionAsset definitionAsset)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(
                    nameof(assembly));
            }

            return assetCatalog.TryGetAsset(
                assembly.DefinitionId,
                out definitionAsset);
        }


        public bool TryResolveSprites(
            DoorAssembly assembly,
            WallDisplaySlope displaySlope,
            out DoorAssemblySprites sprites)
        {
            if (!TryResolveDefinitionAsset(
                    assembly,
                    out DoorDefinitionAsset definitionAsset))
            {
                sprites = default;
                return false;
            }

            return definitionAsset.TryGetAssemblySprites(
                displaySlope,
                out sprites);
        }


        public bool TryResolveHingedSprites(
            DoorAssembly assembly,
            WallDisplaySlope displaySlope,
            out HingedDoorSprites sprites)
        {
            if (!TryResolveDefinitionAsset(
                    assembly,
                    out DoorDefinitionAsset definitionAsset))
            {
                sprites = default;
                return false;
            }

            return definitionAsset.TryGetHingedSprites(
                displaySlope,
                out sprites);
        }


        public bool TryResolveDoorwaySprites(
            DoorAssembly assembly,
            WallDisplaySlope displaySlope,
            out DoorwaySprites sprites)
        {
            if (!TryResolveDefinitionAsset(
                    assembly,
                    out DoorDefinitionAsset definitionAsset))
            {
                sprites = default;
                return false;
            }

            return definitionAsset.TryGetDoorwaySprites(
                displaySlope,
                out sprites);
        }
    }
}
