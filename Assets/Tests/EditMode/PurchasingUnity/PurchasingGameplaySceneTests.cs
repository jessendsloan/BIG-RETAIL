using BigRetail.Map.Unity.Fixtures;
using BigRetail.Purchasing.Unity.UI;
using BigRetail.Receiving.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
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
                ReceivingAreaRuntimeHost receivingAreaRuntimeHost =
                    Object.FindAnyObjectByType<ReceivingAreaRuntimeHost>(
                        FindObjectsInactive.Include);
                ReceivingAreaViewSystem receivingAreaViewSystem =
                    Object.FindAnyObjectByType<ReceivingAreaViewSystem>(
                        FindObjectsInactive.Include);
                FixturePlanogramRuntimeHost planogramHost =
                    Object.FindAnyObjectByType<FixturePlanogramRuntimeHost>(
                        FindObjectsInactive.Include);
                PurchasingWorkspacePresenter presenter =
                    FindSceneComponent<PurchasingWorkspacePresenter>(scene);
                PanelRenderer panel =
                    FindSceneComponent<PanelRenderer>(scene, "PurchasingWorkspaceUI");
                InputSystemUIInputModule uiInputModule =
                    FindSceneComponent<InputSystemUIInputModule>(scene);

                Assert.That(runtimeHost, Is.Not.Null);
                Assert.That(runtimeHost.CatalogAsset, Is.Not.Null);
                Assert.That(deliveryViewSystem, Is.Not.Null);
                Assert.That(receivingAreaRuntimeHost, Is.Not.Null);
                Assert.That(receivingAreaViewSystem, Is.Not.Null);
                Assert.That(planogramHost, Is.Not.Null);
                Assert.That(presenter, Is.Not.Null);
                Assert.That(presenter.gameObject.activeSelf, Is.False);
                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.visualTreeAsset, Is.Not.Null);
                Assert.That(panel.sortingOrder, Is.EqualTo(100));
                Assert.That(uiInputModule, Is.Not.Null);
                Assert.That(uiInputModule.actionsAsset, Is.Not.Null);
                Assert.That(
                    uiInputModule.actionsAsset.name,
                    Is.EqualTo("InputSystem_Actions"));
                Assert.That(uiInputModule.move, Is.Not.Null);
                Assert.That(uiInputModule.move.action, Is.Not.Null);
                InputAction navigationAction = uiInputModule.move.action;
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/w",
                    false);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/a",
                    false);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/s",
                    false);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/d",
                    false);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/upArrow",
                    true);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/downArrow",
                    true);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/leftArrow",
                    true);
                AssertNavigationBinding(
                    navigationAction,
                    "<Keyboard>/rightArrow",
                    true);

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
                    serializedDeliveryView
                        .FindProperty("receivingAreaRuntimeHost")
                        .objectReferenceValue,
                    Is.SameAs(receivingAreaRuntimeHost));
                Assert.That(
                    serializedDeliveryView.FindProperty("viewHost")
                        .objectReferenceValue,
                    Is.Not.Null);

                SerializedObject serializedRuntimeHost =
                    new SerializedObject(runtimeHost);
                Assert.That(
                    serializedRuntimeHost
                        .FindProperty("receivingAreaRuntimeHost")
                        .objectReferenceValue,
                    Is.SameAs(receivingAreaRuntimeHost));

                SerializedObject serializedReceivingView =
                    new SerializedObject(receivingAreaViewSystem);
                Assert.That(
                    serializedReceivingView.FindProperty("runtimeHost")
                        .objectReferenceValue,
                    Is.SameAs(receivingAreaRuntimeHost));
                Assert.That(
                    serializedReceivingView.FindProperty("overlayTilemap")
                        .objectReferenceValue,
                    Is.Not.Null);
                Assert.That(
                    serializedReceivingView.FindProperty("markerTile")
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


        private static void AssertNavigationBinding(
            InputAction action,
            string path,
            bool expected)
        {
            bool found = false;

            for (int index = 0; index < action.bindings.Count; index++)
            {
                if (action.bindings[index].path == path)
                {
                    found = true;
                    break;
                }
            }

            Assert.That(
                found,
                Is.EqualTo(expected),
                $"Navigation binding '{path}' did not match the gameplay UI contract.");
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
