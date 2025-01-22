using System.Collections.Generic;
using UnityEngine;

namespace Planetarium.UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Default Views")]
        [SerializeField] private UIView[] defaultViews;
        
        private List<UIView> activeViews = new List<UIView>();
        private Dictionary<string, UIView> viewCache = new Dictionary<string, UIView>();
        
        private void Awake()
        {
            // Find and cache all UI views in the scene
            UIView[] allViews = GetComponentsInChildren<UIView>(true);
            foreach (var view in allViews)
            {
                view.Initialize(this);
                viewCache[view.GetType().Name] = view;
            }
        }
        
        private void Start()
        {
            // Open default views
            foreach (var view in defaultViews)
            {
                if (view != null)
                    view.Open();
            }
        }
        
        public T GetView<T>() where T : UIView
        {
            string viewName = typeof(T).Name;
            return viewCache.TryGetValue(viewName, out UIView view) ? view as T : null;
        }
        
        public T OpenView<T>() where T : UIView
        {
            T view = GetView<T>();
            if (view != null)
            {
                OpenView(view);
            }
            return view;
        }
        
        public void OpenView(UIView view)
        {
            if (view == null) return;
            
            // Close lower priority views if they block interaction
            foreach (var activeView in activeViews.ToArray())
            {
                if (activeView.Priority < view.Priority)
                {
                    activeView.Close();
                }
            }
            
            view.Open();
            if (!activeViews.Contains(view))
            {
                activeViews.Add(view);
                activeViews.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            }
        }
        
        public T CloseView<T>() where T : UIView
        {
            T view = GetView<T>();
            if (view != null)
            {
                CloseView(view);
            }
            return view;
        }
        
        public void CloseView(UIView view)
        {
            if (view == null) return;
            
            view.Close();
            activeViews.Remove(view);
        }
        
        public void CloseAllViews()
        {
            foreach (var view in activeViews.ToArray())
            {
                view.Close();
            }
            activeViews.Clear();
        }
        
        public bool IsViewOpen<T>() where T : UIView
        {
            T view = GetView<T>();
            return view != null && view.IsOpen;
        }
        
        public bool IsTopView(UIView view)
        {
            if (!view.IsOpen || activeViews.Count == 0) return false;
            return activeViews[0] == view;
        }
    }
}
