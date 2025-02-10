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
            
            // Clear existing cache
            viewCache.Clear();
            activeViews.Clear();
            
            foreach (var view in allViews)
            {
                if (view == null)
                {
                    Debug.LogError("UIManager: Found null view in hierarchy!");
                    continue;
                }

                Debug.Log($"UIManager: Initializing view {view.GetType().Name} on GameObject {view.gameObject.name}");
                
                // Initialize the view
                view.Initialize(this, null);
                string viewName = view.GetType().Name;
                if (!viewCache.ContainsKey(viewName))
                {
                    viewCache[viewName] = view;
                    Debug.Log($"UIManager: Cached view {viewName}");
                }
                else
                {
                    Debug.LogWarning($"UIManager: Duplicate view type found: {viewName}. Skipping cache.");
                }
            }

            // Log all cached views
            Debug.Log("UIManager: Cached views:");
            foreach (var kvp in viewCache)
            {
                Debug.Log($"- {kvp.Key}: {kvp.Value.gameObject.name}");
            }

            // Open views based on their startOpen flag
            foreach (var view in allViews)
            {
                if (view != null && view.startOpen)
                {
                    Debug.Log($"UIManager: Auto-opening view {view.GetType().Name}");
                    view.Open(false);
                    if (!activeViews.Contains(view))
                    {
                        activeViews.Add(view);
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
            if (this == null) return;  // Check if this object is destroyed
            
            Debug.Log("UIManager: OnDeactivate called");
            base.OnDeactivate();

            // Close all views if we're still valid
            if (this != null && gameObject != null)
            {
                CloseAllViews();
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

        public void CloseAllViews()
        {
            if (this == null || gameObject == null) return;  // Early out if destroyed
            
            var views = GetComponentsInChildren<UIView>(true);
            if (views != null)
            {
                foreach (var view in views)
                {
                    if (view != null && view.gameObject != null)
                    {
                        view.Close(true);
                    }
                }
            }
            
            // Also clear active views list
            activeViews.Clear();
        }

        public void ResetAllViews()
        {
            try
            {
                Debug.Log("UIManager: Resetting all views");
                
                // Store views that were open before reset
                var openViews = new List<UIView>(activeViews);
                
                // Close all views first
                CloseAllViews();

                // Re-initialize all views and cache
                allViews = GetComponentsInChildren<UIView>(true);
                viewCache.Clear();
                
                foreach (var view in allViews)
                {
                    if (view != null && view.gameObject != null)
                    {
                        Debug.Log($"UIManager: Re-initializing view {view.GetType().Name}");
                        view.Initialize(this, null);
                        string viewName = view.GetType().Name;
                        if (!viewCache.ContainsKey(viewName))
                        {
                            viewCache[viewName] = view;
                        }
                    }
                }

                // Reopen views that should stay open
                foreach (var view in allViews)
                {
                    if (view != null && (view.startOpen || openViews.Contains(view)))
                    {
                        Debug.Log($"UIManager: Reopening view {view.GetType().Name}");
                        view.Open(true);
                        if (!activeViews.Contains(view))
                        {
                            activeViews.Add(view);
                        }
                    }
                }
                
                Debug.Log("UIManager: All views reset complete");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting views: {e.Message}\n{e.StackTrace}");
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
