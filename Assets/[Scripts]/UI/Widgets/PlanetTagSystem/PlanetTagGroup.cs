using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Planetarium.Stats;
using DG.Tweening;
using UnityEngine.EventSystems;

namespace Planetarium.UI
{
    public class PlanetTagGroup : UITagGroup, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Planet Tag UI")]
        [SerializeField] private Image planetIcon;
        [SerializeField] private Image resourceIcon;
        [SerializeField] private TextMeshProUGUI planetName;
        [SerializeField] private TextMeshProUGUI resourceCount;
        [SerializeField] private Button interactButton;
        
        [Header("Animation Settings")]
        [SerializeField] private float expandedScale = 1.2f;
        [SerializeField] private float expandDuration = 0.3f;
        [SerializeField] private Ease expandEase = Ease.OutBack;

        private Vector3 _originalScale;
        private TaggedComponent _target;
        private Tween _scaleTween;

        private void Start()
        {
            if (interactButton != null)
            {
                interactButton.onClick.AddListener(OnInteractButtonClicked);
            }

            _originalScale = transform.localScale;
        }

        public void Initialize(TaggedComponent target)
        {
            base.Initialize(target);
            _target = target;
            
            if (_target != null)
            {
                UpdatePlanetInfo();
                _target.OnTagAdded += OnTagChanged;
                _target.OnTagRemoved += OnTagChanged;
            }
        }

        private void OnTagChanged(GameplayTag tag)
        {
            UpdatePlanetInfo();
        }

        private void UpdatePlanetInfo()
        {
            if (_target == null) return;

            if (planetName != null)
            {
                planetName.text = _target.gameObject.name;
            }

            // Update status based on tags
            if (planetName != null)
            {
                string status = "";
                foreach (var tag in _target.Tags)
                {
                    status += tag.ToString() + " ";
                }
                planetName.text = status.Trim();
            }
        }

        public void SetResourceInfo(int count, Sprite resourceSprite = null)
        {
            if (resourceCount != null) resourceCount.text = count.ToString();
            if (resourceIcon != null && resourceSprite != null) resourceIcon.sprite = resourceSprite;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale * expandedScale, expandDuration)
                .SetEase(expandEase);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_originalScale, expandDuration)
                .SetEase(expandEase);
        }

        private void OnInteractButtonClicked()
        {
            SendMessageUpwards("OnPlanetInteraction", _target?.gameObject, SendMessageOptions.DontRequireReceiver);
        }

        private void OnDestroy()
        {
            _scaleTween?.Kill();

            if (_target != null)
            {
                _target.OnTagAdded -= OnTagChanged;
                _target.OnTagRemoved -= OnTagChanged;
            }

            if (interactButton != null)
            {
                interactButton.onClick.RemoveListener(OnInteractButtonClicked);
            }
        }
    }
}
