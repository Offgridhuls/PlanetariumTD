using UnityEngine;
using UnityEngine.Events;

namespace Planetarium.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : UIWidget
    {
        public bool IsOpen { get; private set; }
        public int Priority => priority;
        
        [Header("View Settings")]
        [SerializeField] protected bool startOpen;
        [SerializeField] protected int priority;
        [SerializeField] protected bool useAnimation;
        [SerializeField] protected float animationDuration = 0.3f;
        
        public UnityEvent onOpen;
        public UnityEvent onClose;
        
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
            
            // Initialize in closed state
            IsOpen = false;
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
        }

        internal void Initialize(UIManager uiManager, UIWidget owner)
        {
            base.Initialize(uiManager, owner);
            
            // After initialization, open if needed
            if (startOpen)
            {
                Debug.Log($"UIView: {GetType().Name} is marked as startOpen, opening...");
                Open(true);
            }
            else
            {
                Debug.Log($"UIView: {GetType().Name} is not marked as startOpen, closing...");
                Close(true);
            }
        }
        
        public virtual void Open(bool instant = false)
        {
            Debug.Log($"UIView: Opening {GetType().Name} on {gameObject.name} (instant: {instant})");
            if (IsOpen)
            {
                Debug.Log($"UIView: {GetType().Name} is already open, returning");
                return;
            }
            
            // Enable the GameObject first
            if (!gameObject.activeSelf)
            {
                Debug.Log($"UIView: Activating GameObject for {GetType().Name}");
                gameObject.SetActive(true);
            }
            
            IsOpen = true;
            
            if (useAnimation && !instant)
            {
                Debug.Log($"UIView: Starting open animation for {GetType().Name}");
                StartCoroutine(AnimateOpen());
            }
            else
            {
                Debug.Log($"UIView: Instantly opening {GetType().Name}");
                CanvasGroup.alpha = 1f;
                CanvasGroup.interactable = true;
                CanvasGroup.blocksRaycasts = true;
            }
            
            onOpen?.Invoke();
        }
        
        public virtual void Close(bool instant = false)
        {
            Debug.Log($"UIView: Closing {GetType().Name} on {gameObject.name} (instant: {instant})");
            if (!IsOpen)
            {
                Debug.Log($"UIView: {GetType().Name} is already closed, returning");
                return;
            }
            
            IsOpen = false;
            
            if (useAnimation && !instant)
            {
                Debug.Log($"UIView: Starting close animation for {GetType().Name}");
                StartCoroutine(AnimateClose());
            }
            else
            {
                Debug.Log($"UIView: Instantly closing {GetType().Name}");
                CanvasGroup.alpha = 0f;
                CanvasGroup.interactable = false;
                CanvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
            
            onClose?.Invoke();
        }
        
        public virtual void Toggle()
        {
            if (IsOpen)
                Close();
            else
                Open();
        }
        
        protected virtual System.Collections.IEnumerator AnimateOpen()
        {
            CanvasGroup.alpha = 0f;
            CanvasGroup.interactable = true;
            CanvasGroup.blocksRaycasts = true;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                CanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / animationDuration);
                yield return null;
            }
            
            CanvasGroup.alpha = 1f;
        }
        
        protected virtual System.Collections.IEnumerator AnimateClose()
        {
            CanvasGroup.interactable = false;
            CanvasGroup.blocksRaycasts = false;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                CanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / animationDuration);
                yield return null;
            }
            
            CanvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
