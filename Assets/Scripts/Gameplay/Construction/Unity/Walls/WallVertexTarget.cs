using System;
using BigRetail.Map.Domain;
using UnityEngine;

namespace BigRetail.Construction.Unity.Walls
{
    /// <summary>
    /// Describes the logical grid vertex currently selected by the
    /// construction pointer and its projected Unity-world position.
    /// </summary>
    public readonly struct WallVertexTarget :
        IEquatable<WallVertexTarget>
    {
        public GridPosition RequestedCell { get; }

        public GridVertex Vertex { get; }

        public Vector3 WorldPosition { get; }


        public WallVertexTarget(
            GridPosition requestedCell,
            GridVertex vertex,
            Vector3 worldPosition)
        {
            RequestedCell = requestedCell;
            Vertex = vertex;
            WorldPosition = worldPosition;
        }


        public bool Equals(
            WallVertexTarget other)
        {
            return RequestedCell == other.RequestedCell
                && Vertex == other.Vertex
                && WorldPosition == other.WorldPosition;
        }


        public override bool Equals(
            object obj)
        {
            return obj is WallVertexTarget other
                && Equals(other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;

                hash =
                    (hash * 31)
                    + RequestedCell.GetHashCode();

                hash =
                    (hash * 31)
                    + Vertex.GetHashCode();

                hash =
                    (hash * 31)
                    + WorldPosition.GetHashCode();

                return hash;
            }
        }


        public override string ToString()
        {
            return
                $"Requested cell {RequestedCell}. "
                + $"Selected {Vertex}.";
        }


        public static bool operator ==(
            WallVertexTarget left,
            WallVertexTarget right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(
            WallVertexTarget left,
            WallVertexTarget right)
        {
            return !left.Equals(right);
        }
    }
}
