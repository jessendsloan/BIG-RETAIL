using System;
using UnityEngine;

namespace BigRetail.StoreLayouts.Unity
{
    /// <summary>
    /// Versioned, reusable physical-store template. Runtime consumers always
    /// receive a detached canonical copy so loading cannot mutate the asset.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StoreLayout",
        menuName = "Big Retail/Store Layouts/Store Layout")]
    public sealed class StoreLayoutAsset : ScriptableObject
    {
        [SerializeField]
        private StoreLayoutData layout =
            new StoreLayoutData();


        public string LayoutId =>
            layout != null
                ? layout.LayoutId
                : string.Empty;

        public string DisplayName =>
            layout != null
                ? layout.DisplayName
                : string.Empty;


        public StoreLayoutData CreateRuntimeCopy()
        {
            if (layout == null)
            {
                throw new InvalidOperationException(
                    "The StoreLayoutAsset has no serialized layout data.");
            }

            return new StoreDataCanonicalizer()
                .CreateCanonicalCopy(layout);
        }


        public void ReplaceData(
            StoreLayoutData replacement)
        {
            if (replacement == null)
            {
                throw new ArgumentNullException(
                    nameof(replacement));
            }

            layout =
                new StoreDataCanonicalizer()
                    .CreateCanonicalCopy(replacement);
        }
    }
}
