using UnityEngine;

namespace Planetarium.AI
{
    public class AttackGeneratorState : EnemyStateBase
    {
        private float attackTimer;

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
                TransitionTo<MoveToGeneratorState>();
                return;
            }

            Vector3 targetPosition = Owner.CurrentTarget.transform.position;
            float distanceToTarget = Vector3.Distance(Owner.transform.position, targetPosition);

            // Check if we're still in range
            if (distanceToTarget > Owner.GetStats().attackRange)
            {
                TransitionTo<MoveToGeneratorState>();
                return;
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
                Vector3 directionToTarget = (targetPosition - Owner.transform.position).normalized;
                Vector3 gravityDir = (Owner.transform.position - Owner.CurrentPlanet.transform.position).normalized;
                
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
