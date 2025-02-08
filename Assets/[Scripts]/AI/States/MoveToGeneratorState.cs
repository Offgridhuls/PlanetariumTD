using UnityEngine;

namespace Planetarium.AI
{
    public class MoveToGeneratorState : EnemyStateBase
    {
        protected Vector3 currentVelocity;
        protected Vector3 velocityChange;
        
        public override void Enter()
        {
            if (Owner.CurrentTarget == null)
            {
                Owner.FindNearestGenerator();
            }
        }

        public override void Update()
        {
            if (Owner.CurrentTarget == null || Owner.CurrentTarget.IsDestroyed)
            {
                Owner.FindNearestGenerator();
                if (Owner.CurrentTarget == null) return;
            }

            Vector3 currentPosition = Owner.transform.position;
            Vector3 targetPoint = Owner.CurrentTarget.transform.position;
            float distanceToTarget = Vector3.Distance(currentPosition, targetPoint);

            // Check if we're in attack range
            if (distanceToTarget <= Owner.GetStats().attackRange)
            {
                TransitionTo<AttackGeneratorState>();
                return;
            }

            // Move towards target
            Vector3 moveDirection = (targetPoint - currentPosition).normalized;
            Vector3 targetVelocity = moveDirection * Owner.GetStats().MoveSpeed;

            currentVelocity = Vector3.SmoothDamp(
                currentVelocity,
                targetVelocity,
                ref velocityChange,
                0.1f,
                Owner.GetStats().MoveSpeed
            );

            if (Owner.rb != null)
            {
                Owner.rb.linearVelocity = currentVelocity;
            }

            // Update rotation
            if (Owner.CurrentPlanet != null)
            {
                Vector3 gravityDir = (currentPosition - Owner.CurrentPlanet.transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, -gravityDir);
                
                Owner.transform.rotation = Quaternion.RotateTowards(
                    Owner.transform.rotation,
                    targetRotation,
                    Owner.GetStats().RotSpeed * Time.deltaTime
                );
            }
        }

        public override void Exit()
        {
            if (Owner.rb != null)
            {
                Owner.rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
