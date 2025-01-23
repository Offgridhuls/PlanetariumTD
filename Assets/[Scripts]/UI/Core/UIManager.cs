using System.Collections.Generic;
using UnityEngine;

namespace Planetarium.UI
{
    public class UIManager : SceneService
    {
        // PUBLIC MEMBERS
        public Canvas Canvas { get; private set; }
        public Camera UICamera { get; private set; }

        // PRIVATE MEMBERS
        [SerializeField] private UIView[] defaultViews;
        
        private List<UIView> activeViews = new List<UIView>();
        private Dictionary<string, UIView> viewCache = new Dictionary<string, UIView>();
        private UIView[] allViews;

        // PROTECTED METHODS
        protected override void OnInitialize()
        {
            Debug.Log("UIManager: OnInitialize called");
            Canvas = GetComponent<Canvas>();
            UICamera = Canvas.worldCamera;

            // Find and cache all UI views
            allViews = GetComponentsInChildren<UIView>(true);
            Debug.Log($"UIManager: Found {allViews.Length} UI views");
            
            foreach (var view in allViews)
            {
                if (view == null)
                {
                    Debug.LogError("UIManager: Found null view in hierarchy!");
                    continue;
                }

                Debug.Log($"UIManager: Initializing view {view.GetType().Name} on GameObject {view.gameObject.name}");
                
                // Ensure the view has a CanvasGroup
                var canvasGroup = view.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    Debug.LogWarning($"UIManager: Adding missing CanvasGroup to {view.gameObject.name}");
                    canvasGroup = view.gameObject.AddComponent<CanvasGroup>();
                }

                // Initialize the view
                view.Initialize(this, null);
                viewCache[view.GetType().Name] = view;
            }

            // Log all cached views
            Debug.Log("UIManager: Cached views:");
            foreach (var kvp in viewCache)
            {
                Debug.Log($"- {kvp.Key}: {kvp.Value.gameObject.name}");
            }

            // Open default views
            if (defaultViews != null && defaultViews.Length > 0)
            {
                Debug.Log("UIManager: Opening default views:");
                foreach (var view in defaultViews)
                {
                    if (view != null)
                    {
                        Debug.Log($"- Opening default view: {view.GetType().Name}");
                        view.Open(true);
                    }
                }
            }
        }

        protected override void OnDeinitialize()
        {
            Debug.Log("UIManager: OnDeinitialize called");
            if (allViews != null)
            {
                for (int i = 0; i < allViews.Length; i++)
                {
                    if (allViews[i] != null)
                    {
                        Debug.Log($"UIManager: Deinitializing view {allViews[i].GetType().Name}");
                        allViews[i].Deinitialize();
                    }
                }
            }

            activeViews.Clear();
            viewCache.Clear();
            allViews = null;
        }

        protected override void OnActivate()
        {
            Debug.Log("UIManager: OnActivate called");
            base.OnActivate();

            // Ensure Canvas is enabled
            if (Canvas != null)
            {
                Canvas.enabled = true;
            }
        }

        protected override void OnDeactivate()
        {
            Debug.Log("UIManager: OnDeactivate called");
            base.OnDeactivate();

            // Close all views
            if (allViews != null)
            {
                foreach (var view in allViews)
                {
                    if (view != null)
                    {
                        Debug.Log($"UIManager: Closing view {view.GetType().Name}");
                        view.Close(true);
                    }
                }
            }

            // Disable Canvas
            if (Canvas != null)
            {
                Canvas.enabled = false;
            }
        }

        public T GetView<T>() where T : UIView
        {
            string viewName = typeof(T).Name;
            if (viewCache.TryGetValue(viewName, out UIView view))
            {
                return view as T;
            }

            Debug.LogWarning($"UIManager: View {viewName} not found in cache");
            return null;
        }

        public UIView GetView(string viewName)
        {
            if (viewCache.TryGetValue(viewName, out UIView view))
            {
                return view;
            }

            Debug.LogWarning($"UIManager: View {viewName} not found in cache");
            return null;
        }

        public void OpenView<T>(bool instant = false) where T : UIView
        {
            var view = GetView<T>();
            if (view != null)
            {
                Debug.Log($"UIManager: Opening view {typeof(T).Name}");
                view.Open(instant);
                if (!activeViews.Contains(view))
                {
                    activeViews.Add(view);
                }
            }
        }

        public void CloseView<T>(bool instant = false) where T : UIView
        {
            var view = GetView<T>();
            if (view != null)
            {
                Debug.Log($"UIManager: Closing view {typeof(T).Name}");
                view.Close(instant);
                activeViews.Remove(view);
            }
        }

        protected override void OnTick()
        {
            base.OnTick();

            // Tick all active views
            for (int i = activeViews.Count - 1; i >= 0; i--)
            {
                if (activeViews[i] != null)
                {
                    activeViews[i].Tick();
                }
                else
                {
                    activeViews.RemoveAt(i);
                }
            }
        }
    }
}
