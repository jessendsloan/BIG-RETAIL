using System;
using UnityEngine;

namespace BigRetail.StoreLayouts.Unity
{
    /// <summary>
    /// Versioned starting state layered over one reusable store layout.
    /// Runtime consumers always receive a detached canonical copy.
    /// </summary>
    [CreateAssetMenu(
        fileName = "StoreScenario",
        menuName = "Big Retail/Store Layouts/Store Scenario")]
    public sealed class StoreScenarioAsset : ScriptableObject
    {
        [SerializeField]
        private StoreScenarioData scenario =
            new StoreScenarioData();


        public string ScenarioId =>
            scenario != null
                ? scenario.ScenarioId
                : string.Empty;

        public string DisplayName =>
            scenario != null
                ? scenario.DisplayName
                : string.Empty;


        public StoreScenarioData CreateRuntimeCopy()
        {
            if (scenario == null)
            {
                throw new InvalidOperationException(
                    "The StoreScenarioAsset has no serialized scenario data.");
            }

            return new StoreDataCanonicalizer()
                .CreateCanonicalCopy(scenario);
        }

        public void ReplaceData(
            StoreScenarioData replacement)
        {
            if (replacement == null)
            {
                throw new ArgumentNullException(
                    nameof(replacement));
            }

            scenario =
                new StoreDataCanonicalizer()
                    .CreateCanonicalCopy(replacement);
        }
    }
}
