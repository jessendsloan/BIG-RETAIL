using System;
using BigRetail.Purchasing.Domain;
using UnityEngine;

namespace BigRetail.Purchasing.Unity
{
    [CreateAssetMenu(
        fileName = "SupplierDefinition",
        menuName = "Big Retail/Purchasing/Supplier Definition")]
    public sealed class SupplierDefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string supplierId;

        [SerializeField]
        private string displayName;

        [SerializeField]
        private string specialty;

        [TextArea(2, 4)]
        [SerializeField]
        private string description;

        [Min(0)]
        [SerializeField]
        private long minimumOrderCents;

        [SerializeField]
        private SupplierDeliveryKind deliveryKind =
            SupplierDeliveryKind.NextDay;

        [Min(1)]
        [SerializeField]
        private int sameDayLeadHours = 3;

        [SerializeField]
        private SupplierWeekday routeDays =
            SupplierWeekday.Tuesday | SupplierWeekday.Friday;

        [Tooltip("Optional supplier mark. A text stub is shown when absent.")]
        [SerializeField]
        private Sprite logo;

        [SerializeField]
        private Color accentColor = new Color(0.18f, 0.39f, 0.48f, 1f);

        [Tooltip(
            "Optional isometric shipping carton used for this supplier's "
            + "physical inbound loads. A supplier-colored graybox carton is "
            + "generated when absent.")]
        [SerializeField]
        private Sprite deliveryBoxSprite;


        public string DisplayName =>
            displayName;

        public string Specialty =>
            specialty;

        public string Description =>
            description;

        public Sprite Logo =>
            logo;

        public Color AccentColor =>
            accentColor;

        public Sprite DeliveryBoxSprite =>
            deliveryBoxSprite;

        public string SupplierIdValue =>
            supplierId ?? string.Empty;


        public bool TryCreateDefinition(
            out SupplierDefinition definition,
            out string error)
        {
            try
            {
                SupplierDeliveryRule deliveryRule;

                switch (deliveryKind)
                {
                    case SupplierDeliveryKind.SameDay:
                        deliveryRule =
                            SupplierDeliveryRule.SameDay(sameDayLeadHours);
                        break;

                    case SupplierDeliveryKind.NextDay:
                        deliveryRule = SupplierDeliveryRule.NextDay();
                        break;

                    case SupplierDeliveryKind.WeeklyRoute:
                        deliveryRule =
                            SupplierDeliveryRule.WeeklyRoute(routeDays);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(
                            nameof(deliveryKind),
                            deliveryKind,
                            "The supplier delivery kind is not supported.");
                }

                definition =
                    new SupplierDefinition(
                        new SupplierId(supplierId),
                        displayName,
                        specialty,
                        minimumOrderCents,
                        deliveryRule);
                error = string.Empty;
                return true;
            }
            catch (ArgumentException exception)
            {
                definition = null;
                error = $"{name}: {exception.Message}";
                return false;
            }
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            supplierId = NormalizeIdentifier(supplierId);
            displayName = NormalizeText(displayName);
            specialty = NormalizeText(specialty);
            description = NormalizeText(description);
            minimumOrderCents = Math.Max(0, minimumOrderCents);
            sameDayLeadHours = Mathf.Max(1, sameDayLeadHours);
            accentColor.a = 1f;
        }

        private static string NormalizeIdentifier(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToUpperInvariant();
        }

        private static string NormalizeText(string value)
        {
            return value == null
                ? string.Empty
                : value.Trim();
        }
#endif
    }
}
