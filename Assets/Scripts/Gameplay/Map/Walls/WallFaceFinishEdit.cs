using System;

namespace BigRetail.Map.Walls
{
    /// <summary>
    /// Records one exact before-and-after finish change for a physical wall face.
    /// </summary>
    public readonly struct WallFaceFinishEdit :
        IEquatable<WallFaceFinishEdit>
    {
        public WallFaceKey Face { get; }

        public WallFinishId BeforeFinishId { get; }

        public WallFinishId AfterFinishId { get; }


        public WallFaceFinishEdit(
            WallFaceKey face,
            WallFinishId beforeFinishId,
            WallFinishId afterFinishId)
        {
            if (!beforeFinishId.IsValid)
            {
                throw new ArgumentException(
                    "A wall-face finish edit requires a valid previous finish.",
                    nameof(beforeFinishId));
            }

            if (!afterFinishId.IsValid)
            {
                throw new ArgumentException(
                    "A wall-face finish edit requires a valid next finish.",
                    nameof(afterFinishId));
            }

            if (beforeFinishId == afterFinishId)
            {
                throw new ArgumentException(
                    "A wall-face finish edit must represent a real change.",
                    nameof(afterFinishId));
            }

            Face = face;
            BeforeFinishId = beforeFinishId;
            AfterFinishId = afterFinishId;
        }


        public WallFaceFinishEdit Inverse()
        {
            return new WallFaceFinishEdit(
                Face,
                AfterFinishId,
                BeforeFinishId);
        }


        public bool Equals(
            WallFaceFinishEdit other)
        {
            return Face.Equals(other.Face)
                && BeforeFinishId.Equals(other.BeforeFinishId)
                && AfterFinishId.Equals(other.AfterFinishId);
        }


        public override bool Equals(
            object obj)
        {
            return obj is WallFaceFinishEdit other
                && Equals(other);
        }


        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 31) + Face.GetHashCode();
                hash = (hash * 31) + BeforeFinishId.GetHashCode();
                hash = (hash * 31) + AfterFinishId.GetHashCode();
                return hash;
            }
        }


        public override string ToString()
        {
            return $"{Face}: '{BeforeFinishId}' -> '{AfterFinishId}'";
        }


        public static bool operator ==(
            WallFaceFinishEdit left,
            WallFaceFinishEdit right)
        {
            return left.Equals(right);
        }


        public static bool operator !=(
            WallFaceFinishEdit left,
            WallFaceFinishEdit right)
        {
            return !left.Equals(right);
        }
    }
}
