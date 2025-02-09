using UnityEngine;

namespace Planetarium.AI
{
    public abstract class EnemyStateBase
    {
        protected EnemyBase Owner { get; private set; }
        private string StateName => GetType().Name;

        public virtual void Initialize(EnemyBase owner)
        {
            Owner = owner;
            LogState("Initialized");
        }

        public virtual void Enter()
        {
            //LogState("Enter");
        }

        public virtual void Update()
        {
            //LogStateVerbose("Update");
        }

        public virtual void FixedUpdate()
        {
            //LogStateVerbose("FixedUpdate");
        }

        public virtual void Exit()
        {
            //LogState("Exit");
        }

        protected void TransitionTo<T>() where T : EnemyStateBase
        {
            //LogState($"Requesting transition to {typeof(T).Name}");
            Owner.TransitionToState<T>();
        }

        protected void LogState(string action)
        {
           // Debug.Log($"[{Owner?.gameObject.name ?? "Unknown"}] {StateName}: {action}", Owner);
        }

        protected void LogStateVerbose(string action)
        {
            if (Debug.isDebugBuild)
            {
                Debug.Log($"[{Owner?.gameObject.name ?? "Unknown"}] {StateName}: {action}", Owner);
            }
        }

        protected void LogWarning(string message)
        {
            Debug.LogWarning($"[{Owner?.gameObject.name ?? "Unknown"}] {StateName}: {message}", Owner);
        }

        protected void LogError(string message)
        {
            Debug.LogError($"[{Owner?.gameObject.name ?? "Unknown"}] {StateName}: {message}", Owner);
        }
    }
}
