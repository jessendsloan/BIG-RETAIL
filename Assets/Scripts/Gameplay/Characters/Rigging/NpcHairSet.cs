using UnityEngine;

namespace BigRetail.Characters.Rigging
{
    [CreateAssetMenu(
        fileName = "HairSet",
        menuName = "Big Retail/Characters/Hair Set")]
    public sealed class NpcHairSet : ScriptableObject
    {
        [SerializeField]
        private string displayName = "Hair";

        [SerializeField]
        private Color hairColor = Color.black;

        [SerializeField]
        private NpcOutfitPartStyle hairRear;

        [SerializeField]
        private NpcOutfitPartStyle hairFront;

        [SerializeField]
        private NpcAppearancePartShape hairRearShape;

        [SerializeField]
        private NpcAppearancePartShape hairFrontShape;


        public string DisplayName => displayName;

        public Color HairColor => hairColor;


        public void Configure(
            string newDisplayName,
            Color newHairColor,
            NpcOutfitPartStyle newHairRear,
            NpcOutfitPartStyle newHairFront,
            NpcAppearancePartShape newHairRearShape,
            NpcAppearancePartShape newHairFrontShape)
        {
            displayName = string.IsNullOrWhiteSpace(newDisplayName)
                ? name
                : newDisplayName;
            hairColor = newHairColor;
            hairRear = newHairRear;
            hairFront = newHairFront;
            hairRearShape = newHairRearShape;
            hairFrontShape = newHairFrontShape;
        }


        public bool TryGetStyle(
            NpcRigPartId partId,
            out NpcOutfitPartStyle spriteStyle,
            out NpcAppearancePartShape shape)
        {
            switch (partId)
            {
                case NpcRigPartId.HairRear:
                    spriteStyle = hairRear;
                    shape = hairRearShape;
                    return spriteStyle != null || shape != null;

                case NpcRigPartId.HairFront:
                    spriteStyle = hairFront;
                    shape = hairFrontShape;
                    return spriteStyle != null || shape != null;

                default:
                    spriteStyle = null;
                    shape = null;
                    return false;
            }
        }


        public bool TryValidate(
            out string failureReason)
        {
            if (hairRear == null || hairRearShape == null)
            {
                failureReason = "Rear hair style is incomplete.";
                return false;
            }

            if (hairFront == null || hairFrontShape == null)
            {
                failureReason = "Front hair style is incomplete.";
                return false;
            }

            if (hairRear.Id != NpcRigPartId.HairRear
                || hairRearShape.Id != NpcRigPartId.HairRear)
            {
                failureReason = "Rear hair uses the wrong part id.";
                return false;
            }

            if (hairFront.Id != NpcRigPartId.HairFront
                || hairFrontShape.Id != NpcRigPartId.HairFront)
            {
                failureReason = "Front hair uses the wrong part id.";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }
    }
}
