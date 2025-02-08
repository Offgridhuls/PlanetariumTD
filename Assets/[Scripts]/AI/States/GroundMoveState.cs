using UnityEngine;

namespace Planetarium.AI
{
    public class GroundMoveState : EnemyStateBase
    {
        protected Vector3 currentVelocity;
        protected Vector3 velocityChange;
        private const float GROUND_OFFSET = 0.5f; // Distance to maintain from ground

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
            float attackRange = Owner.GetStats().attackRange;

            // Check if we're in attack range
            if (distanceToTarget <= attackRange)
            {
                TransitionTo<GroundAttackState>();
                return;
            }

            // Get surface point for movement
            Vector3 surfacePoint, surfaceNormal;
            if (Owner.GetPlanetSurfacePoint(out surfacePoint, out surfaceNormal))
            {
                // Project target direction onto the surface plane
                Vector3 toTarget = targetPoint - currentPosition;
                Vector3 projectedDirection = Vector3.ProjectOnPlane(toTarget, surfaceNormal).normalized;
                
                // Calculate ideal position slightly above surface
                Vector3 idealPosition = surfacePoint + surfaceNormal * GROUND_OFFSET;
                
                // Blend between moving towards target and maintaining ground distance
                Vector3 moveDirection = Vector3.Lerp(
                    projectedDirection,
                    (idealPosition - currentPosition).normalized,
                    0.5f
                ).normalized;

                // Calculate target velocity
                Vector3 targetVelocity = moveDirection * Owner.GetStats().MoveSpeed;

                // Smooth velocity change
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

                // Update rotation to face movement direction while aligned with surface
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection, surfaceNormal);
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
