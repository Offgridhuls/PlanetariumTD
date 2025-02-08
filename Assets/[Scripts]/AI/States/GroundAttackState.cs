using UnityEngine;

namespace Planetarium.AI
{
    public class GroundAttackState : AttackGeneratorState
    {
        private const float GROUND_OFFSET = 0.25f;
        private Vector3 currentVelocity;
        private Vector3 velocityChange;

        public override void Update()
        {
            if (Owner.CurrentTarget == null || Owner.CurrentTarget.IsDestroyed)
            {
                TransitionTo<GroundMoveState>();
                return;
            }

            Vector3 currentPosition = Owner.transform.position;
            Vector3 targetPosition = Owner.CurrentTarget.transform.position;
            
            // Get surface point for ground alignment
            Vector3 surfacePoint, surfaceNormal;
            if (Owner.GetPlanetSurfacePoint(out surfacePoint, out surfaceNormal))
            {
                // Calculate ideal attack position that's both in range and on the ground
                Vector3 toTarget = targetPosition - currentPosition;
                float distanceToTarget = toTarget.magnitude;
                
                // Project attack range onto surface plane
                Vector3 idealPosition;
                if (distanceToTarget > Owner.GetStats().attackRange)
                {
                    // Move closer while staying on ground
                    Vector3 targetDir = Vector3.ProjectOnPlane(toTarget, surfaceNormal).normalized;
                    idealPosition = targetPosition - targetDir * Owner.GetStats().attackRange;
                    
                    // Project ideal position onto surface
                    idealPosition = surfacePoint + Vector3.ProjectOnPlane(idealPosition - surfacePoint, surfaceNormal);
                    idealPosition += surfaceNormal * GROUND_OFFSET;

                    // Move towards ideal position
                    Vector3 moveDirection = (idealPosition - currentPosition).normalized;
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
                }
                else
                {
                    // In range, stop moving
                    if (Owner.rb != null)
                    {
                        Owner.rb.linearVelocity = Vector3.zero;
                    }
                }

                // Update rotation to face target while staying aligned with surface
                Vector3 targetDirection = (targetPosition - currentPosition).normalized;
                Vector3 projectedDirection = Vector3.ProjectOnPlane(targetDirection, surfaceNormal).normalized;
                
                Quaternion targetRotation = Quaternion.LookRotation(projectedDirection, surfaceNormal);
                Owner.transform.rotation = Quaternion.RotateTowards(
                    Owner.transform.rotation,
                    targetRotation,
                    Owner.GetStats().RotSpeed * Time.deltaTime
                );

                // Attack if in range and facing target
                if (distanceToTarget <= Owner.GetStats().attackRange)
                {
                    float angleToTarget = Vector3.Angle(Owner.transform.forward, targetDirection);
                    if (angleToTarget < Owner.GetStats().attackAngle * 0.5f)
                    {
                        Owner.Attack(Owner.CurrentTarget.gameObject.transform.position);
                    }
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            
            if (Owner.rb != null)
            {
                Owner.rb.linearVelocity = Vector3.zero;
            }
        }
    }
}
