using System.Collections;
using System.Collections.Generic;
using Planetarium;
using UnityEngine;

namespace Planetarium
{
    public class Scene : CoreBehaviour
    {
        // PUBLIC MEMBERS
        public bool ContextReady { get; private set; }
        public bool IsActive { get; private set; }
        public SceneContext Context => _context;

        // PRIVATE MEMBERS
        [SerializeField]
        private bool _selfInitialize;
        [SerializeField]
        private SceneContext _context;

        private bool _isInitialized;
        private List<SceneService> _services = new List<SceneService>();

        // PUBLIC METHODS
        public void PrepareContext()
        {
            OnPrepareContext(_context);
        }

        public void Initialize()
        {
            if (_isInitialized)
                return;

            if (!ContextReady)
            {
                OnPrepareContext(_context);
            }

            OnInitialize();
            _isInitialized = true;
        }

        public IEnumerator Activate()
        {
            if (!_isInitialized)
                yield break;

            yield return OnActivate();
            IsActive = true;
        }

        public void Deactivate()
        {
            if (!IsActive)
                return;

            OnDeactivate();
            IsActive = false;
        }

        public void Deinitialize()
        {
            if (!_isInitialized)
                return;

            Deactivate();
            OnDeinitialize();

            ContextReady = false;
            _isInitialized = false;
        }

        public T GetService<T>() where T : SceneService
        {
            for (int i = 0; i < _services.Count; i++)
            {
                if (_services[i] is T service)
                    return service;
            }
            return null;
        }

        // MONOBEHAVIOUR
        protected void Awake()
        {
            if (_selfInitialize)
            {
                Initialize();
            }
        }

        protected IEnumerator Start()
        {
            if (!_isInitialized)
                yield break;

            if (_selfInitialize && !IsActive)
            {
                yield return Activate();
            }
        }

        protected virtual void Update()
        {
            if (!IsActive)
                return;

            OnTick();
        }

        protected virtual void LateUpdate()
        {
            if (!IsActive)
                return;

            OnLateTick();
        }

        protected void OnDestroy()
        {
            Deinitialize();
        }

        // PROTECTED METHODS
        protected virtual void OnPrepareContext(SceneContext context)
        {
           
            context.MainCamera = Camera.main;
            context.HasInput = true;
            context.IsVisible = true;

            ContextReady = true;
        }

        protected virtual void OnInitialize()
        {
            CollectServices();
        }

        protected virtual IEnumerator OnActivate()
        {
            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].Activate();
            }
            yield break;
        }

        protected virtual void OnTick()
        {
            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].Tick();
            }
        }

        protected virtual void OnLateTick()
        {
            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].LateTick();
            }
        }

        protected virtual void OnDeactivate()
        {
            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].Deactivate();
            }
        }

        protected virtual void OnDeinitialize()
        {
            for (int i = 0; i < _services.Count; i++)
            {
                _services[i].Deinitialize();
            }
            _services.Clear();
        }

        protected virtual void CollectServices()
        {
            var services = FindObjectsOfType<SceneService>();
            foreach (var service in services)
            {
                AddService(service);
            }
        }

        protected void AddService(SceneService service)
        {
            if (service == null)
            {
                Debug.LogError($"Missing service");
                return;
            }

            if (_services.Contains(service))
            {
                Debug.LogError($"Service {service.gameObject.name} already added.");
                return;
            }

            service.Initialize(this, Context);
            _services.Add(service);
        }
    }
}
