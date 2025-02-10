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
        }

        [Header("View Controls")]
        [SerializeField] private ViewButtonControl[] viewControls;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            
            if (viewControls == null) return;

            // Initialize each control
            foreach (var control in viewControls)
            {
                if (control == null || control.button == null || control.view == null) continue;

                // Clear any existing listeners
                control.button.onClick.RemoveAllListeners();
                
                // Set initial button text
                UpdateButtonText(control);
                
                // Setup click handler
                var localControl = control; // Avoid closure issues
                control.button.onClick.AddListener(() => OnButtonClicked(localControl));
                
                // Setup view state change handlers
                control.view.onOpen.AddListener(() => UpdateButtonText(localControl));
                control.view.onClose.AddListener(() => UpdateButtonText(localControl));
                
                Debug.Log($"[ViewControlsView] Initialized {control.view.name} control");
            }
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            if (viewControls == null) return;

            // Cleanup all controls
            foreach (var control in viewControls)
            {
                if (control == null) continue;

                if (control.button != null)
                {
                    control.button.onClick.RemoveAllListeners();
                }

                if (control.view != null)
                {
                    control.view.onOpen.RemoveAllListeners();
                    control.view.onClose.RemoveAllListeners();
                }
            }
        }

        private void OnButtonClicked(ViewButtonControl control)
        {
            if (control == null || control.view == null) return;

            Debug.Log($"[ViewControlsView] Button clicked for {control.view.name}, IsOpen: {control.view.IsOpen}");

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

        private void UpdateButtonText(ViewButtonControl control)
        {
            if (control == null || control.button == null || control.view == null) return;

            string text = control.view.IsOpen ? control.openStateText : control.closedStateText;
            control.button.SetText(text);
            control.button.UpdateUI();
            
            Debug.Log($"[ViewControlsView] Updated {control.view.name} button text to: {text}");
        }
    }
}
