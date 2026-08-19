using BigRetail.Map.Unity.Fixtures;
using BigRetail.Purchasing.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class PurchasingGameplaySceneTests
    {
        private const string ScenePath =
            "Assets/Scenes/Gameplay.unity";


        [Test]
        public void Gameplay_WiresLivePurchasingOverlayAndOpeningProducts()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                Scene scene = EditorSceneManager.OpenScene(
                    ScenePath,
                    OpenSceneMode.Single);
                PurchasingRuntimeHost runtimeHost =
                    Object.FindAnyObjectByType<PurchasingRuntimeHost>(
                        FindObjectsInactive.Include);
                InboundDeliveryViewSystem deliveryViewSystem =
                    Object.FindAnyObjectByType<InboundDeliveryViewSystem>(
                        FindObjectsInactive.Include);
                FixturePlanogramRuntimeHost planogramHost =
                    Object.FindAnyObjectByType<FixturePlanogramRuntimeHost>(
                        FindObjectsInactive.Include);
                PurchasingWorkspacePresenter presenter =
                    FindSceneComponent<PurchasingWorkspacePresenter>(scene);
                PanelRenderer panel =
                    FindSceneComponent<PanelRenderer>(scene, "PurchasingWorkspaceUI");

                Assert.That(runtimeHost, Is.Not.Null);
                Assert.That(runtimeHost.CatalogAsset, Is.Not.Null);
                Assert.That(deliveryViewSystem, Is.Not.Null);
                Assert.That(planogramHost, Is.Not.Null);
                Assert.That(presenter, Is.Not.Null);
                Assert.That(presenter.gameObject.activeSelf, Is.False);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.visualTreeAsset, Is.Not.Null);
                Assert.That(panel.sortingOrder, Is.EqualTo(100));

                SerializedObject serializedPlanogram =
                    new SerializedObject(planogramHost);
                Object productCatalog = serializedPlanogram
                    .FindProperty("productCatalogAsset")
                    .objectReferenceValue;
                Assert.That(productCatalog, Is.Not.Null);
                Assert.That(productCatalog.name, Is.EqualTo("OpeningProductCatalog"));

                SerializedObject serializedPresenter =
                    new SerializedObject(presenter);
                Assert.That(
                    serializedPresenter.FindProperty("runtimeHost")
                        .objectReferenceValue,
                    Is.SameAs(runtimeHost));

                SerializedObject serializedDeliveryView =
                    new SerializedObject(deliveryViewSystem);
                Assert.That(
                    serializedDeliveryView
                        .FindProperty("purchasingRuntimeHost")
                        .objectReferenceValue,
                    Is.SameAs(runtimeHost));
                Assert.That(
                    serializedDeliveryView.FindProperty("mapHost")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedDeliveryView.FindProperty("viewHost")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedDeliveryView
                        .FindProperty("coordinateTilemap")
                        .objectReferenceValue,
                    Is.Not.Null);
            }
            finally
            {
                if (previousSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
                }
                else
                {
                    EditorSceneManager.NewScene(
                        NewSceneSetup.EmptyScene,
                        NewSceneMode.Single);
                }
            }
        }


        private static T FindSceneComponent<T>(
            Scene scene,
            string objectName = null)
            where T : Component
        {
            T[] candidates =
                Resources.FindObjectsOfTypeAll<T>();

            for (int index = 0; index < candidates.Length; index++)
            {
                T candidate = candidates[index];

                if (candidate.gameObject.scene == scene
                    && (string.IsNullOrEmpty(objectName)
                        || candidate.gameObject.name == objectName))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
