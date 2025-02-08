using UnityEngine;

namespace Planetarium.AI
{
    public class AttackGeneratorState : EnemyStateBase
    {
        private float attackTimer;
        private Vector3 currentVelocity;
        private Vector3 velocityChange;
        private const float OPTIMAL_DISTANCE_RATIO = 0.75f; // Stay at 75% of max attack range
        private const float POSITION_TOLERANCE = 2f; // How much variance in position is acceptable

        public override void Enter()
        {
            attackTimer = 0f;
            
            if (Owner.rb != null)
            {
                Owner.rb.linearVelocity = Vector3.zero;
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
            Vector3 targetPosition = Owner.CurrentTarget.transform.position;
            float distanceToTarget = Vector3.Distance(currentPosition, targetPosition);
            float attackRange = Owner.GetStats().attackRange;

            // Check if we're still in range
            if (distanceToTarget > attackRange)
            {
                TransitionTo<MoveToGeneratorState>();
                return;
            }

            // Calculate ideal hover position at optimal distance
            float optimalDistance = attackRange * OPTIMAL_DISTANCE_RATIO;
            Vector3 toTarget = targetPosition - currentPosition;
            Vector3 targetDir = toTarget.normalized;
            Vector3 idealPosition = targetPosition - targetDir * optimalDistance;

            // Adjust ideal position based on planet surface
            if (Owner.CurrentPlanet != null)
            {
                Vector3 surfacePoint, surfaceNormal;
                if (Owner.GetPlanetSurfacePoint(out surfacePoint, out surfaceNormal))
                {
                    float surfaceDistance = Vector3.Distance(idealPosition, surfacePoint);
                    float minSurfaceDistance = optimalDistance * 0.5f;

                    if (surfaceDistance < minSurfaceDistance)
                    {
                        Vector3 awayFromSurface = (idealPosition - surfacePoint).normalized;
                        idealPosition = surfacePoint + awayFromSurface * minSurfaceDistance;

                        // Ensure we maintain optimal attack distance
                        Vector3 toTargetFromNew = targetPosition - idealPosition;
                        if (toTargetFromNew.magnitude > attackRange)
                        {
                            idealPosition = targetPosition - toTargetFromNew.normalized * optimalDistance;
                        }
                    }
                }
            }

            // Move towards ideal position if we're too far from it
            float distanceToIdeal = Vector3.Distance(currentPosition, idealPosition);
            if (distanceToIdeal > POSITION_TOLERANCE)
            {
                Vector3 moveDirection = (idealPosition - currentPosition).normalized;
                Vector3 targetVelocity = moveDirection * Owner.GetStats().MoveSpeed * 0.5f; // Use half speed for precise positioning

                currentVelocity = Vector3.SmoothDamp(
                    currentVelocity,
                    targetVelocity,
                    ref velocityChange,
                    0.1f,
                    Owner.GetStats().MoveSpeed * 0.5f
                );

                if (Owner.rb != null)
                {
                    Owner.rb.linearVelocity = currentVelocity;
                }
            }
            else if (Owner.rb != null)
            {
                Owner.rb.linearVelocity = Vector3.zero;
            }

            // Update attack timer using enemy's attack speed
            attackTimer += Time.deltaTime;
            if (attackTimer >= 1f / Owner.GetStats().attackSpeed)
            {
                Attack();
                attackTimer = 0f;
            }

            // Update rotation to face target
            if (Owner.CurrentPlanet != null)
            {
                Vector3 directionToTarget = (targetPosition - currentPosition).normalized;
                Vector3 gravityDir = (currentPosition - Owner.CurrentPlanet.transform.position).normalized;
                
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget, -gravityDir);
                Owner.transform.rotation = Quaternion.RotateTowards(
                    Owner.transform.rotation,
                    targetRotation,
                    Owner.GetStats().RotSpeed * Time.deltaTime
                );
            }
        }

        private void Attack()
        {
            if (Owner.CurrentTarget == null) return;

            ProjectileBase projectile = Owner.ShootProjectile(Owner.CurrentTarget.transform.position);
            if (projectile != null)
            {
                projectile.Initialize(
                    Owner.GetStats().attackDamage,
                    Owner.CurrentTarget.transform.position,
                    Owner.GetStats().ProjectileSpeed
                );
                projectile.ShootProjectile(Owner.CurrentTarget.transform.position, Owner.CurrentTarget.gameObject);
            }
        }
    }
}
