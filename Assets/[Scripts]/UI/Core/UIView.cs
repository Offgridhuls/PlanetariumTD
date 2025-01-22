using UnityEngine;
using UnityEngine.Events;

namespace Planetarium.UI
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class UIView : MonoBehaviour
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
        
        protected CanvasGroup canvasGroup;
        protected UIManager uiManager;
        
        protected virtual void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }
        
        protected virtual void Start()
        {
            if (startOpen)
            {
                Open();
            }
            else
            {
                Close(true);
            }
        }
        
        public virtual void Initialize(UIManager manager)
        {
            uiManager = manager;
        }
        
        public virtual void Open(bool instant = false)
        {
            if (IsOpen) return;
            
            gameObject.SetActive(true);
            IsOpen = true;
            
            if (useAnimation && !instant)
            {
                StartCoroutine(AnimateOpen());
            }
            else
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            onOpen?.Invoke();
        }
        
        public virtual void Close(bool instant = false)
        {
            if (!IsOpen) return;
            
            IsOpen = false;
            
            if (useAnimation && !instant)
            {
                StartCoroutine(AnimateClose());
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
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
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / animationDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
        }
        
        protected virtual System.Collections.IEnumerator AnimateClose()
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            
            float elapsed = 0f;
            while (elapsed < animationDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / animationDuration);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }
}
