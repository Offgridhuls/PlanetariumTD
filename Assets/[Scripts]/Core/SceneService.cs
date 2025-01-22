using UnityEngine;

namespace Planetarium
{
    public abstract class SceneService : CoreBehaviour
    {
        // PUBLIC MEMBERS
        public SceneContext Context => _context;
        public Scene Scene => _scene;
        public bool IsActive => _isActive;
        public bool IsInitialized => _isInitialized;

        // PRIVATE MEMBERS
        private Scene _scene;
        private SceneContext _context;
        private bool _isInitialized;
        private bool _isActive;

        // INTERNAL METHODS
        internal void Initialize(Scene scene, SceneContext context)
        {
            if (_isInitialized)
                return;

            _scene = scene;
            _context = context;

            OnInitialize();

            _isInitialized = true;
        }

        internal void Deinitialize()
        {
            if (!_isInitialized)
                return;

            Deactivate();

            OnDeinitialize();

            _scene = null;
            _context = null;

            _isInitialized = false;
        }

        internal void Activate()
        {
            if (!_isInitialized)
                return;

            if (_isActive)
                return;

            OnActivate();

            _isActive = true;
        }

        internal void Deactivate()
        {
            if (!_isActive)
                return;

            OnDeactivate();

            _isActive = false;
        }

        internal void Tick()
        {
            if (!_isActive)
                return;

            OnTick();
        }

        internal void LateTick()
        {
            if (!_isActive)
                return;

            OnLateTick();
        }

        // PROTECTED VIRTUAL METHODS
        protected virtual void OnInitialize() { }
        protected virtual void OnDeinitialize() { }
        protected virtual void OnActivate() { }
        protected virtual void OnDeactivate() { }
        protected virtual void OnTick() { }
        protected virtual void OnLateTick() { }
    }
}
