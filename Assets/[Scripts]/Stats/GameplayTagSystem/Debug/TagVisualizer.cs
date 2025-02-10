using UnityEngine;
using Planetarium.UI;
using Planetarium.Stats;

namespace Planetarium.Stats.Debug
{
    /// <summary>
    /// Component that registers a TaggedComponent with the UITagView system for visualization.
    /// </summary>
    [RequireComponent(typeof(TaggedComponent))]
    public class TagVisualizer : CoreBehaviour
    {
        private TaggedComponent _taggedComponent;
        private bool _isRegistered = false;

        private void Awake()
        {
            _taggedComponent = GetComponent<TaggedComponent>();
        }

        private void OnEnable()
        {
            TryRegisterWithView();
            // Also start checking for UITagView in case it's not ready yet
            if (!_isRegistered)
            {
                InvokeRepeating(nameof(TryRegisterWithView), 0.1f, 0.1f);
            }
        }

        private void OnDisable()
        {
            UnregisterFromView();
        }

        private void OnDestroy()
        {
            UnregisterFromView();
        }

        private void TryRegisterWithView()
        {
            if (_isRegistered || _taggedComponent == null) return;

            var view = UITagView.Instance;
            if (view != null)
            {
                view.AddTrackedComponent(_taggedComponent);
                _isRegistered = true;
                CancelInvoke(nameof(TryRegisterWithView));
            }
        }

        private void UnregisterFromView()
        {
            if (!_isRegistered || _taggedComponent == null) return;

            var view = UITagView.Instance;
            if (view != null)
            {
                view.RemoveTrackedComponent(_taggedComponent);
            }
            _isRegistered = false;
            CancelInvoke(nameof(TryRegisterWithView));
        }
    }
}