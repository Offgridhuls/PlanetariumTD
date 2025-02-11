using UnityEngine;
using Planetarium.Stats;

namespace Planetarium.AI
{
    public abstract class EnemyStateBase
    {
        protected EnemyBase Owner { get; private set; }
        private string StateName => GetType().Name;
        
        protected GameplayTag StateTag { get; private set; }

        public virtual void Initialize(EnemyBase owner)
        {
            Owner = owner;
            
            // Create state tag from the class name, e.g. "EnemyIdleState" becomes "State.Idle"
            string tagName = StateName.Replace("Enemy", "").Replace("State", "");
            StateTag = new GameplayTag($"State.{tagName}");
            
            LogState("Initialized");
        }

        public virtual void Enter()
        {
            if (Owner?.taggedComponent != null)
            {
                Owner.taggedComponent.AddTag(StateTag);
                LogState($"Added tag {StateTag}");
            }
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
            if (Owner?.taggedComponent != null)
            {
                Owner.taggedComponent.RemoveTag(StateTag);
                LogState($"Removed tag {StateTag}");
            }
        }

        protected void TransitionTo<T>() where T : EnemyStateBase
        {
            LogState($"Requesting transition to {typeof(T).Name}");
            Owner.TransitionToState<T>();
        }

        protected void LogState(string action)
        {
            Debug.Log($"[{Owner?.gameObject.name ?? "Unknown"}] {StateName}: {action}", Owner);
        }

        protected void LogStateVerbose(string action)
        {
            if (Debug.isDebugBuild)
            {
                LogState(action);
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
