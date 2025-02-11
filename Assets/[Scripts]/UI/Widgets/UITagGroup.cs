using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Planetarium.Stats;
using System.Collections.Generic;

namespace Planetarium.UI
{
    public class UITagGroup : UIWidget
    {
        [Header("References")]
        [SerializeField]
        protected RectTransform _mainRect;
        
        [SerializeField]
        protected Image _background;

        [SerializeField]
        protected TextMeshProUGUI _statNameText;
        
        [SerializeField]
        protected RectTransform _tagsContainer;
        
        [SerializeField]
        protected TextMeshProUGUI _tagTextPrefab;

        [Header("Appearance")]
        [SerializeField]
        protected Color _inRangeColor = Color.green;
        
        [SerializeField]
        protected Color _midRangeColor = Color.yellow;
        
        [SerializeField]
        protected Color _outOfRangeColor = Color.red;

        [SerializeField]
        protected float _baseWidth = 180f;

        [SerializeField]
        protected float _tagSpacing = 2f;

        protected TaggedComponent _target;
        protected List<TextMeshProUGUI> _tagTexts = new List<TextMeshProUGUI>();

        public virtual void Initialize(TaggedComponent target)
        {
            _target = target;

            if (_target != null)
            {
                _statNameText.text = _target.gameObject.name;
                _target.OnTagAdded += HandleTagChanged;
                _target.OnTagRemoved += HandleTagChanged;
            }

            UpdateContent();
        }

        protected virtual void OnDestroy()
        {
            if (_target != null)
            {
                _target.OnTagAdded -= HandleTagChanged;
                _target.OnTagRemoved -= HandleTagChanged;
            }

            ClearTagTexts();
        }

        public virtual void UpdatePosition(UnityEngine.Camera camera)
        {
            if (_target == null || camera == null)
                return;

            Vector3 targetPosition = _target.transform.position;
            Vector3 screenPosition = camera.WorldToScreenPoint(targetPosition);

            // Handle visibility when behind camera
            gameObject.SetActive(screenPosition.z >= 0);
            if (screenPosition.z < 0) return;

            transform.position = screenPosition;
        }

        public virtual void UpdateAppearance(float distance, float minDistance, float maxDistance)
        {
            if (_mainRect != null)
            {
                // Scale width based on distance
                float normalizedDistance = Mathf.Clamp(distance, minDistance, maxDistance);
                float width = _baseWidth * (1 - (normalizedDistance - minDistance) / (maxDistance - minDistance));
                _mainRect.sizeDelta = new Vector2(width, _mainRect.sizeDelta.y);
            }

            if (_background != null)
            {
                // Update color based on distance
                if (distance <= minDistance)
                {
                    _background.color = _inRangeColor;
                }
                else if (distance <= maxDistance - 30)
                {
                    _background.color = _midRangeColor;
                }
                else
                {
                    _background.color = _outOfRangeColor;
                }
            }
        }

        protected virtual void UpdateContent()
        {
            if (_target == null || _tagsContainer == null || _tagTextPrefab == null)
                return;

            ClearTagTexts();

            foreach (var tag in _target.Tags)
            {
                var tagText = Instantiate(_tagTextPrefab, _tagsContainer);
                tagText.text = FormatTagText(tag);
                _tagTexts.Add(tagText);
            }

            // Update container height based on content
            if (_tagsContainer != null)
            {
                float totalHeight = _tagTexts.Count * (_tagTextPrefab.rectTransform.sizeDelta.y + _tagSpacing);
                _tagsContainer.sizeDelta = new Vector2(_tagsContainer.sizeDelta.x, totalHeight);
            }
        }

        protected virtual string FormatTagText(GameplayTag tag)
        {
            return string.IsNullOrEmpty(tag.DevComment) ? 
                tag.TagName : 
                $"{tag.TagName} ({tag.DevComment})";
        }

        protected virtual void HandleTagChanged(GameplayTag tag)
        {
            UpdateContent();
        }

        protected virtual void ClearTagTexts()
        {
            foreach (var text in _tagTexts)
            {
                if (text != null)
                {
                    Destroy(text.gameObject);
                }
            }
            _tagTexts.Clear();
        }

        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
    }
}
