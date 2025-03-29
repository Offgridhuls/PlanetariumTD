using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Planetarium.UI
{
    public class MainMenuView : UIView
    {
        [Header("Buttons")]
        [SerializeField] private Button singlePlayerButton;
        [SerializeField] private Button multiplayerButton;
        [SerializeField] private Button profileButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Animation Settings")]
        [SerializeField] private float buttonAnimationDelay = 0.1f;
        [SerializeField] private float buttonAnimationDuration = 0.3f;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            // Setup button listeners
            if (singlePlayerButton != null)
            {
                singlePlayerButton.onClick.AddListener(() => {
                    PlayClickSound();
                    UIManager.TransitionToView<SinglePlayerView>(this);
                });
            }
            
            

            if (multiplayerButton != null)
            {
                multiplayerButton.onClick.AddListener(() => {
                    PlayClickSound();
                    // TODO: Implement multiplayer view transition
                });
            }
            
            if (profileButton != null)
            {
                profileButton.onClick.AddListener(() => {
                    PlayClickSound();
                    // TODO: Implement profile view transition
                    
                });
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(() => {
                    PlayClickSound();
                    // TODO: Implement settings view transition
                });
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(() => {
                    PlayClickSound();
                    #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                    #else
                    Application.Quit();
                    #endif
                });
            }
        }

        public override void Open(bool instant = false)
        {
            base.Open(instant);

            if (useAnimation)
            {
                // Animate buttons sequentially
                //AnimateButtons(true);
            }
        }

        public override void Close(bool instant = false)
        {
            base.Close(instant);

            if (useAnimation)
            {
                // Animate buttons out
                //AnimateButtons(false);
            }
        }

        private void AnimateButtons(bool opening)
        {
            Button[] buttons = { singlePlayerButton, multiplayerButton, settingsButton, quitButton };
            float delay = 0f;

            foreach (var button in buttons)
            {
                if (button != null)
                {
                    var buttonTransform = button.transform;
                    if (opening)
                    {
                        // Setup initial state
                        buttonTransform.localScale = Vector3.zero;
                        // Animate in
                        buttonTransform.DOScale(Vector3.one, buttonAnimationDuration)
                            .SetDelay(delay)
                            .SetEase(Ease.OutBack);
                    }
                    else
                    {
                        // Animate out
                        buttonTransform.DOScale(Vector3.zero, buttonAnimationDuration)
                            .SetDelay(delay)
                            .SetEase(Ease.InBack);
                    }
                    delay += buttonAnimationDelay;
                }
            }
        }

     
    }
}
