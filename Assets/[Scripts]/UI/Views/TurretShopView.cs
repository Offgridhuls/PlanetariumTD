using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP;

namespace Planetarium.UI
{
    public class TurretShopView : UIView
    {
        [Header("Shop References")]
        [SerializeField] private Transform turretButtonContainer;
        [SerializeField] private List<TurretSelectionButton> turretButtons = new List<TurretSelectionButton>();
        
        [Header("Animation Settings")]
        [SerializeField] private float buttonDelay = 0.1f; // Delay between each button animation
        [SerializeField] private bool useStaggeredAnimation = true;

        private bool isAnimating;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            FindTurretButtons();
            InitializeButtons();
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            CleanupButtons();
            StopAllCoroutines();
            isAnimating = false;
            turretButtons.Clear();
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);
            
            if (instant)
            {
                // Instantly show all buttons
                foreach (var button in turretButtons)
                {
                    if (button != null)
                    {
                        button.gameObject.SetActive(true);
                        var buttonManager = button.GetComponent<ButtonManager>();
                        if (buttonManager != null)
                        {
                            buttonManager.enabled = true;
                            buttonManager.UpdateUI();
                        }
                    }
                }
            }
            else
            {
                // Start staggered animation
                StartCoroutine(AnimateButtonsIn());
            }
            
            RefreshShop();
        }

        public override void Close(bool instant = false)
        {
            if (!instant && isAnimating)
            {
                // If we're still animating in, stop that first
                StopAllCoroutines();
                isAnimating = false;
            }

            if (instant)
            {
                // Instantly hide all buttons
                foreach (var button in turretButtons)
                {
                    if (button != null)
                    {
                        button.gameObject.SetActive(false);
                        var buttonManager = button.GetComponent<ButtonManager>();
                        if (buttonManager != null)
                        {
                            buttonManager.enabled = false;
                        }
                    }
                }
            }
            else
            {
                // Start fade out animation
                StartCoroutine(AnimateButtonsOut());
            }

            base.Close(instant);
        }

        private void FindTurretButtons()
        {
            // Find all TurretSelectionButtons in the container if not set in inspector
            if (turretButtons.Count == 0 && turretButtonContainer != null)
            {
                turretButtons.AddRange(turretButtonContainer.GetComponentsInChildren<TurretSelectionButton>(true));
            }
        }

        private void InitializeButtons()
        {
            foreach (var button in turretButtons)
            {
                if (button != null)
                {
                    button.Initialize();
                    // Set initial visibility based on view state
                    button.gameObject.SetActive(IsOpen);
                }
            }
        }

        private void CleanupButtons()
        {
            foreach (var button in turretButtons)
            {
                if (button != null)
                {
                    button.Cleanup();
                }
            }
        }

        private IEnumerator AnimateButtonsIn()
        {
            isAnimating = true;

            if (!useStaggeredAnimation)
            {
                // Activate all buttons at once
                foreach (var button in turretButtons)
                {
                    if (button != null)
                    {
                        button.gameObject.SetActive(true);
                        var buttonManager = button.GetComponent<ButtonManager>();
                        if (buttonManager != null)
                        {
                            buttonManager.enabled = true;
                            buttonManager.UpdateUI();
                        }
                    }
                }
            }
            else
            {
                // Staggered animation
                foreach (var button in turretButtons)
                {
                    if (button != null)
                    {
                        button.gameObject.SetActive(true);
                        var buttonManager = button.GetComponent<ButtonManager>();
                        if (buttonManager != null)
                        {
                            buttonManager.enabled = true;
                            buttonManager.UpdateUI();
                        }

                        yield return new WaitForSecondsRealtime(buttonDelay);
                    }
                }
            }

            isAnimating = false;
        }

        private IEnumerator AnimateButtonsOut()
        {
            isAnimating = true;

            if (!useStaggeredAnimation)
            {
                // Deactivate all buttons at once
                foreach (var button in turretButtons)
                {
                    if (button != null)
                    {
                        var buttonManager = button.GetComponent<ButtonManager>();
                        if (buttonManager != null)
                        {
                            buttonManager.enabled = false;
                        }
                        button.gameObject.SetActive(false);
                    }
                }
            }
            else
            {
                // Staggered animation in reverse order
                for (int i = turretButtons.Count - 1; i >= 0; i--)
                {
                    var button = turretButtons[i];
                    if (button != null)
                    {
                        var buttonManager = button.GetComponent<ButtonManager>();
                        if (buttonManager != null)
                        {
                            buttonManager.enabled = false;
                        }
                        button.gameObject.SetActive(false);
                        
                        yield return new WaitForSecondsRealtime(buttonDelay);
                    }
                }
            }

            isAnimating = false;
        }

        public void RefreshShop()
        {
            if (!IsOpen) return;
            
            foreach (var button in turretButtons)
            {
                if (button != null)
                {
                    button.RefreshDisplay();
                }
            }
        }
    }
}
