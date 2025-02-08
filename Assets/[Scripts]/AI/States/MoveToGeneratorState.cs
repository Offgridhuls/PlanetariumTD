using UnityEngine;

namespace Planetarium.AI
{
    public class MoveToGeneratorState : EnemyStateBase
    {
        protected Vector3 currentVelocity;
        protected Vector3 velocityChange;
        private const float MIN_PLANET_DISTANCE_RATIO = 0.5f; // Minimum safe distance as ratio of attack range

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
            float minPlanetDistance = attackRange * MIN_PLANET_DISTANCE_RATIO;

            // Check if we're in attack range
            if (distanceToTarget <= attackRange)
            {
                TransitionTo<AttackGeneratorState>();
                return;
            }

            // Calculate direction to target while considering planet distance
            Vector3 moveDirection = (targetPoint - currentPosition).normalized;
            
            // Adjust position based on planet distance
            if (Owner.CurrentPlanet != null)
            {
                Vector3 surfacePoint, surfaceNormal;
                if (Owner.GetPlanetSurfacePoint(out surfacePoint, out surfaceNormal))
                {
                    float distanceFromSurface = Vector3.Distance(currentPosition, surfacePoint);
                    
                    // If too close to surface, blend movement direction with away from surface
                    if (distanceFromSurface < minPlanetDistance)
                    {
                        Vector3 awayFromSurface = (currentPosition - surfacePoint).normalized;
                        float blend = 1f - (distanceFromSurface / minPlanetDistance);
                        moveDirection = Vector3.Lerp(moveDirection, awayFromSurface, blend);
                    }
                }
            }

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

            // Update rotation to face movement direction while considering planet gravity
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
