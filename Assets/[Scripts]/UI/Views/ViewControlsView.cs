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
        private bool showDebug;

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
            ResetControls();
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            CleanupViewControls();
        }

        public override void Close(bool instant = false)
        {
            base.Close(instant);
            CleanupViewControls();
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);
        }

        /// <summary>
        /// Resets and reinitializes all view controls
        /// </summary>
        public void ResetControls()
        {
            try
            {
                // First cleanup existing controls
                CleanupViewControls();
                
                // Reset initialization flags
                hasInitializedControls = false;
                if (viewControls != null)
                {
                    foreach (var control in viewControls)
                    {
                        if (control != null)
                        {
                            control.isInitialized = false;
                        }
                    }
                }

                // Restart initialization process
                StartCoroutine(InitializeViewControlsWhenReady());
                
                if (showDebug)
                {
                    Debug.Log($"ViewControlsView: Controls reset on {gameObject.name}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error resetting ViewControlsView controls: {e.Message}");
            }
        }

        private void CleanupViewControls()
        {
            if (viewControls == null) return;

            foreach (var control in viewControls)
            {
                if (control == null || !control.isInitialized) continue;

                if (control.button != null)
                {
                    // Remove click handler
                    control.button.onClick.RemoveAllListeners();
                }

                if (control.view != null)
                {
                    // Remove view event handlers
                    control.view.onOpen.RemoveAllListeners();
                    control.view.onClose.RemoveAllListeners();
                }

                control.isInitialized = false;
            }

            hasInitializedControls = false;
        }

        private IEnumerator InitializeViewControlsWhenReady()
        {
            // Wait a frame to ensure all views are registered
            yield return null;

            if (viewControls == null) yield break;

            foreach (var control in viewControls)
            {
                if (control.button == null || control.view == null) 
                {
                    Debug.LogWarning($"ViewControlsView: Null button or view reference found in {gameObject.name}");
                    continue;
                }

                // Wait for ButtonManager to initialize
                float timeout = 5f; // 5 second timeout
                float elapsed = 0f;
                while (!control.button.gameObject.activeInHierarchy)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed > timeout)
                    {
                        Debug.LogWarning($"ViewControlsView: Timeout waiting for button {control.button.name} to initialize");
                        break;
                    }
                    yield return null;
                }

                // Setup initial button state if not already initialized
                if (!control.isInitialized)
                {
                    Debug.Log($"ViewControlsView: Initializing button for {control.view.name}");
                    
                    try
                    {
                        // Setup initial text
                        UpdateButtonText(control, control.view.IsOpen);
                        
                        // Setup button click handler
                        control.button.onClick.RemoveAllListeners(); // Clear any existing listeners
                        control.button.onClick.AddListener(() => OnViewButtonClicked(control));
                        
                        // Subscribe to view events
                        control.view.onOpen.RemoveListener(() => UpdateButtonText(control, true));
                        control.view.onClose.RemoveListener(() => UpdateButtonText(control, false));
                        control.view.onOpen.AddListener(() => UpdateButtonText(control, true));
                        control.view.onClose.AddListener(() => UpdateButtonText(control, false));
                        
                        control.isInitialized = true;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error initializing control for {control.view.name}: {e.Message}");
                    }
                }
            }

            hasInitializedControls = true;
            Debug.Log("ViewControlsView: All controls initialized");
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
