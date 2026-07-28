using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace BigRetail.Construction.Unity.UI.PC
{
    /// <summary>
    /// Owns the runtime lifecycle of the PC construction toolbar document.
    /// Gameplay presenters can observe the created view without coupling the
    /// UIDocument to construction rules or services.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(100)]
    public sealed class ConstructionToolbarDocumentHost : MonoBehaviour
    {
        [SerializeField]
        private UIDocument document;

        public ConstructionToolbarView View
        {
            get;
            private set;
        }

        public bool HasView => View != null;

        public event Action<ConstructionToolbarView> ViewReady;

        private void Reset()
        {
            document = GetComponent<UIDocument>();
        }

        private void Awake()
        {
            if (document == null)
            {
                document = GetComponent<UIDocument>();
            }
        }

        private void OnEnable()
        {
            CreateView();
        }

        private void OnDisable()
        {
            DisposeView();
        }

        public bool CreateView()
        {
            DisposeView();

            if (document == null)
            {
                Debug.LogError(
                    "ConstructionToolbarDocumentHost has no UIDocument assigned.",
                    this);
                return false;
            }

            VisualElement root = document.rootVisualElement;

            if (root == null)
            {
                Debug.LogError(
                    "ConstructionToolbarDocumentHost could not access the UIDocument root.",
                    this);
                return false;
            }

            try
            {
                View = new ConstructionToolbarView(root);
                ViewReady?.Invoke(View);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"ConstructionToolbarDocumentHost could not create its view: {exception.Message}",
                    this);
                return false;
            }
        }

        private void DisposeView()
        {
            if (View == null)
            {
                return;
            }

            View.Dispose();
            View = null;
        }
    }
}
