using BigRetail.Purchasing.Unity.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Purchasing.Unity.Tests
{
    public sealed class PurchasingWorkspaceLabSceneTests
    {
        private const string ScenePath =
            "Assets/Scenes/Labs/PurchasingWorkspaceLab.unity";


        [Test]
        public void LabScene_WiresDocumentPresenterAndPanel()
        {
            SceneSetup[] previousSetup =
                EditorSceneManager.GetSceneManagerSetup();

            try
            {
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

                PanelRenderer panel =
                    Object.FindAnyObjectByType<PanelRenderer>();
                PurchasingWorkspaceDocumentHost host =
                    Object.FindAnyObjectByType<PurchasingWorkspaceDocumentHost>();
                PurchasingWorkspacePresenter presenter =
                    Object.FindAnyObjectByType<PurchasingWorkspacePresenter>();

                Assert.That(panel, Is.Not.Null);
                Assert.That(panel.panelSettings, Is.Not.Null);
                Assert.That(panel.visualTreeAsset, Is.Not.Null);
                Assert.That(host, Is.Not.Null);
                Assert.That(presenter, Is.Not.Null);

                SerializedObject serializedPresenter =
                    new SerializedObject(presenter);
                Assert.That(
                    serializedPresenter.FindProperty("commercialCatalog")
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
    }
}
