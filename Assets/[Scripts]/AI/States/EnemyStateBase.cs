using UnityEngine;

namespace Planetarium.AI
{
    public abstract class EnemyStateBase
    {
        protected EnemyBase Owner { get; private set; }

        public virtual void Initialize(EnemyBase owner)
        {
            Owner = owner;
        }

        public virtual void Enter() { }
        public virtual void Update() { }
        public virtual void FixedUpdate() { }
        public virtual void Exit() { }

        protected void TransitionTo<T>() where T : EnemyStateBase
        {
            Owner.TransitionToState<T>();
        }
    }
}
