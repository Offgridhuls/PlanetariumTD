using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System.Collections.Generic;

namespace Planetarium.Stats.Debug
{
    [ExecuteInEditMode]
    public class TagVisualizer : MonoBehaviour
    {
        [Header("Visualization")]
        [SerializeField] private bool showInGame = true;
        [SerializeField] private bool showInEditor = true;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.8f);
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Vector3 offset = new Vector3(0, 1, 0);

        [Header("Layout")]
        [SerializeField] private float width = 200f;
        [SerializeField] private float padding = 5f;
        [SerializeField] private float tagSpacing = 2f;

        private TaggedComponent taggedComponent;
        private Canvas canvas;
        private RectTransform containerRect;
        private Image backgroundImage;
        private List<TextMeshProUGUI> tagTexts = new List<TextMeshProUGUI>();
        private Camera mainCamera;

        private void OnEnable()
        {
            taggedComponent = GetComponent<TaggedComponent>();
            if (taggedComponent == null)
            {
                UnityEngine.Debug.LogError("TagVisualizer requires a TaggedComponent!");
                enabled = false;
                return;
            }

            mainCamera = Camera.main;
            SetupUI();

            // Subscribe to tag events
            taggedComponent.OnTagAdded += OnTagChanged;
            taggedComponent.OnTagRemoved += OnTagChanged;
        }

        private void OnDisable()
        {
            if (taggedComponent != null)
            {
                taggedComponent.OnTagAdded -= OnTagChanged;
                taggedComponent.OnTagRemoved -= OnTagChanged;
            }

            if (canvas != null)
            {
                DestroyImmediate(canvas.gameObject);
            }
        }

        private void SetupUI()
        {
            // Create canvas
            var go = new GameObject("TagVisualizerCanvas");
            go.transform.SetParent(transform);
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 100;

            // Create container
            var containerGo = new GameObject("Container");
            containerGo.transform.SetParent(canvas.transform);
            containerRect = containerGo.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(width, 0);

            // Create background
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(containerRect);
            backgroundImage = bgGo.AddComponent<Image>();
            backgroundImage.color = backgroundColor;
            var bgRect = backgroundImage.rectTransform;
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;

            // Position canvas
            canvas.transform.localPosition = offset;
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * 0.01f; // Scale down to reasonable world size

            UpdateTags();
        }

        private void OnTagChanged(GameplayTag tag)
        {
            UpdateTags();
        }

        private void UpdateTags()
        {
            // Clear existing tag texts
            foreach (var text in tagTexts)
            {
                if (text != null)
                    DestroyImmediate(text.gameObject);
            }
            tagTexts.Clear();

            // Create new tag texts
            float currentY = -padding;
            var tags = taggedComponent.Tags.OrderBy(t => t.TagName).ToList();

            foreach (var tag in tags)
            {
                var tagGo = new GameObject("TagText");
                tagGo.transform.SetParent(containerRect);

                var text = tagGo.AddComponent<TextMeshProUGUI>();
                text.text = tag.TagName;
                text.color = textColor;
                text.fontSize = 12;
                text.alignment = TextAlignmentOptions.Left;

                var rect = text.rectTransform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(1, 1);
                rect.sizeDelta = new Vector2(-padding * 2, 20);
                rect.anchoredPosition = new Vector2(padding, currentY);

                tagTexts.Add(text);
                currentY -= 20 + tagSpacing;
            }

            // Update container height
            containerRect.sizeDelta = new Vector2(width, -currentY + padding);
        }

        private void LateUpdate()
        {
            if ((!showInGame && Application.isPlaying) || (!showInEditor && !Application.isPlaying))
            {
                canvas.enabled = false;
                return;
            }

            canvas.enabled = true;

            // Face camera
            if (mainCamera != null)
            {
                canvas.transform.forward = mainCamera.transform.forward;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (canvas != null)
            {
                canvas.transform.localPosition = offset;
                if (backgroundImage != null)
                    backgroundImage.color = backgroundColor;
                
                foreach (var text in tagTexts)
                {
                    if (text != null)
                        text.color = textColor;
                }
            }
        }
#endif
    }
}
