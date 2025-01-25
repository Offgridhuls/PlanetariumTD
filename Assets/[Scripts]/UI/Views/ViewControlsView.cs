using UnityEngine;
using Michsky.MUIP;
using System.Collections;

namespace Planetarium.UI
{
    public class ViewControlsView : UIView
    {
        [System.Serializable]
        private class ViewButtonControl
        {
            public ButtonManager button;
            public UIView view;
            [Tooltip("If true, clicking will toggle the view on/off. If false, clicking will only open the view")]
            public bool toggleOnClick = true;
            [Tooltip("Button text to show when the view is closed")]
            public string closedStateText = "Show";
            [Tooltip("Button text to show when the view is open")]
            public string openStateText = "Hide";
            [HideInInspector] public bool isInitialized;
        }

        [Header("View Controls")]
        [SerializeField] private ViewButtonControl[] viewControls;
        
        private bool hasInitializedControls;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            // Get UIManager reference from SceneService
            
            if (UIManager == null)
            {
                Debug.LogError("ViewControlsView: Could not find UIManager in parent hierarchy");
                return;
            }

            // Start initialization coroutine
            StartCoroutine(InitializeViewControlsWhenReady());
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            CleanupViewControls();
        }

        private IEnumerator InitializeViewControlsWhenReady()
        {
            // Wait a frame to ensure all views are registered
            yield return null;

            if (viewControls == null) yield break;

            foreach (var control in viewControls)
            {
                if (control.button == null || control.view == null) continue;

                // Wait for ButtonManager to initialize
                while (!control.button.gameObject.activeInHierarchy)
                    yield return null;

                // Setup initial button state if not already initialized
                if (!control.isInitialized)
                {
                    Debug.Log($"ViewControlsView: Initializing button for {control.view.name}");
                    
                    // Setup initial text
                    UpdateButtonText(control, control.view.IsOpen);
                    
                    // Setup button click handler
                    control.button.onClick.AddListener(() => OnViewButtonClicked(control));
                    
                    // Subscribe to view events
                    control.view.onOpen.AddListener(() => UpdateButtonText(control, true));
                    control.view.onClose.AddListener(() => UpdateButtonText(control, false));
                    
                    control.isInitialized = true;
                }
            }

            hasInitializedControls = true;
            Debug.Log("ViewControlsView: All controls initialized");
        }

        private void CleanupViewControls()
        {
            if (viewControls == null) return;

            foreach (var control in viewControls)
            {
                if (!control.isInitialized || control.button == null || control.view == null) continue;

                // Remove button click listener
                control.button.onClick.RemoveAllListeners();

                // Remove view event listeners
                control.view.onOpen.RemoveAllListeners();
                control.view.onClose.RemoveAllListeners();

                control.isInitialized = false;
            }

            hasInitializedControls = false;
        }

        private void OnViewButtonClicked(ViewButtonControl control)
        {
            if (!hasInitializedControls || control == null || control.view == null) return;

            if (control.toggleOnClick)
            {
                if (control.view.IsOpen)
                    control.view.Close();
                else
                    control.view.Open();
            }
            else
            {
                control.view.Open();
            }
        }

        private void UpdateButtonText(ViewButtonControl control, bool isViewOpen)
        {
            if (control.button != null)
            {
                control.button.SetText(isViewOpen ? control.openStateText : control.closedStateText);
                control.button.UpdateUI(); // Force MUIP button to update its visuals
            }
        }
    }
}
