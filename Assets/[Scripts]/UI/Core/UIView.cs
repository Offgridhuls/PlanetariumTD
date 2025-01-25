using UnityEngine;
using UnityEngine.Events;
using DG.Tweening;

namespace Planetarium.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : UIWidget
    {
        public bool IsOpen { get; private set; }
        public int Priority => priority;
        
        [Header("View Settings")]
        [SerializeField] public bool startOpen;
        [SerializeField] protected int priority;
        [SerializeField] protected bool useAnimation;
        [SerializeField] protected float animationDuration = 0.3f;
        [SerializeField] protected Transform layoutContainer; // Reference to the container holding the layout elements
        
        public UnityEvent onOpen;
        public UnityEvent onClose;

        protected Tweener currentTween;
        
        protected override void OnInitialize()
        {
            Debug.Log($"UIView: OnInitialize called for {GetType().Name} on {gameObject.name}");
            base.OnInitialize();

            // Ensure we have a CanvasGroup
            if (CanvasGroup == null)
            {
                Debug.LogWarning($"UIView: Adding missing CanvasGroup to {gameObject.name}");
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            // Find layout container if not set
            if (layoutContainer == null)
            {
                // Try to find the first child that might be the layout container
                if (transform.childCount > 0)
                {
                    layoutContainer = transform.GetChild(0);
                }
            }

            // Ensure layout container exists
            if (layoutContainer == null)
            {
                Debug.LogWarning($"UIView: No layout container found for {gameObject.name}");
                return;
            }
            
            // Initialize state based on startOpen
            IsOpen = startOpen;

            // Force everything to start in the correct state
            if (!startOpen)
            {
                // Ensure layout is inactive before setting canvas state
                layoutContainer.gameObject.SetActive(false);
                
                // Set canvas state without animation
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
            }
            else
            {
                // For open state, let SetCanvasState handle it
                SetCanvasState(true, true);
            }
        }

        internal void Initialize(UIManager uiManager, UIWidget owner)
        {
            base.Initialize(uiManager, owner);
        }

        protected void SetCanvasState(bool isOpen, bool instant = false)
        {
            // Kill any existing tween
            currentTween?.Kill();
            currentTween = null;

            // Always set layout container active state immediately
            if (layoutContainer != null)
            {
                layoutContainer.gameObject.SetActive(isOpen);
            }
            
            if (instant)
            {
                CanvasGroup.alpha = isOpen ? 1f : 0f;
                CanvasGroup.interactable = isOpen;
                CanvasGroup.blocksRaycasts = isOpen;
            }
            else
            {
                // Set immediate properties
                CanvasGroup.interactable = isOpen;
                CanvasGroup.blocksRaycasts = isOpen;
                
                // Animate alpha
                currentTween = CanvasGroup.DOFade(isOpen ? 1f : 0f, animationDuration)
                    .SetEase(isOpen ? Ease.OutQuad : Ease.InQuad)
                    .SetUpdate(true);
            }
        }

        public virtual void Open(bool instant = false)
        {
            if (IsOpen) return;
            IsOpen = true;

            SetCanvasState(true, instant || !useAnimation);
            onOpen?.Invoke();
            Debug.Log($"UIView: {GetType().Name} opened");
        }

        public virtual void Close(bool instant = false)
        {
            if (!IsOpen) return;
            IsOpen = false;

            SetCanvasState(false, instant || !useAnimation);
            onClose?.Invoke();
            Debug.Log($"UIView: {GetType().Name} closed");
        }

        public void Toggle(bool instant = false)
        {
            if (IsOpen)
                Close(instant);
            else
                Open(instant);
        }

        protected override void OnDeinitialize()
        {
            base.OnDeinitialize();
            
            // Kill any active tween
            currentTween?.Kill();
            currentTween = null;
        }
    }
}
