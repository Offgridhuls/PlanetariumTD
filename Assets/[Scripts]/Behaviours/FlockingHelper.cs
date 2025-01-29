using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Planetarium
{
    /// <summary>
    /// Helper class that provides flocking calculations for enemy behaviors
    /// </summary>
    public class FlockingHelper
    {
        private static readonly Collider[] neighborColliders = new Collider[20];
        private readonly List<FlyingEnemyBase> neighbors = new List<FlyingEnemyBase>();

        public float cohesionWeight = 1.0f;
        public float separationWeight = 2.0f;
        public float alignmentWeight = 1.0f;
        public float targetWeight = 1.5f;
        public float neighborRadius = 5f;
        public float separationRadius = 3f;
        public float maxSpeed = 10f;
        public float maxSteerForce = 3f;

        public Vector3 CalculateFlockingForce(FlyingEnemyBase owner, Vector3 targetPosition)
        {
            Vector3 currentPosition = owner.transform.position;
            
            // Find neighbors
            int numFound = Physics.OverlapSphereNonAlloc(
                currentPosition,
                neighborRadius,
                neighborColliders,
                LayerMask.GetMask("Enemy")
            );

            neighbors.Clear();
            for (int i = 0; i < numFound; i++)
            {
                var enemy = neighborColliders[i].GetComponent<FlyingEnemyBase>();
                if (enemy && enemy != owner && enemy.GetType() == owner.GetType())
                {
                    neighbors.Add(enemy);
                }
            }

            // Calculate flocking forces
            Vector3 cohesion = CalculateCohesion(owner) * cohesionWeight;
            Vector3 separation = CalculateSeparation(owner) * separationWeight;
            Vector3 alignment = CalculateAlignment() * alignmentWeight;
            Vector3 targetForce = CalculateTargetForce(owner, targetPosition) * targetWeight;

            // Return combined force
            Vector3 totalForce = cohesion + separation + alignment + targetForce;
            return Vector3.ClampMagnitude(totalForce, maxSteerForce);
        }

        private Vector3 CalculateCohesion(FlyingEnemyBase owner)
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 centerOfMass = neighbors.Aggregate(
                Vector3.zero,
                (sum, enemy) => sum + enemy.transform.position
            ) / neighbors.Count;

            return SteerTowards(owner, centerOfMass - owner.transform.position);
        }

        private Vector3 CalculateSeparation(FlyingEnemyBase owner)
        {
            if (neighbors.Count == 0) return Vector3.zero;

            Vector3 separationForce = neighbors.Aggregate(
                Vector3.zero,
                (sum, enemy) =>
                {
                    float distance = Vector3.Distance(
                        owner.transform.position,
                        enemy.transform.position
                    );
                    if (distance < separationRadius)
                    {
                        Vector3 awayFromNeighbor = (owner.transform.position - enemy.transform.position).normalized;
                        awayFromNeighbor *= (separationRadius - distance) / separationRadius;
                        return sum + awayFromNeighbor;
                    }
                    return sum;
                }
            );

            return SteerTowards(owner, separationForce);
        }

        private Vector3 CalculateAlignment()
        {
            if (neighbors.Count == 0) return Vector3.zero;

            return neighbors.Aggregate(
                Vector3.zero,
                (sum, enemy) => sum + enemy.rb.linearVelocity
            ) / neighbors.Count;
        }

        private Vector3 CalculateTargetForce(FlyingEnemyBase owner, Vector3 targetPosition)
        {
            if (targetPosition == Vector3.zero) return Vector3.zero;
            return (targetPosition - owner.transform.position).normalized * maxSpeed;
        }

        private Vector3 SteerTowards(FlyingEnemyBase owner, Vector3 vector)
        {
            Vector3 desired = vector.normalized * maxSpeed;
            return Vector3.ClampMagnitude(desired - owner.rb.linearVelocity, maxSteerForce);
        }
    }
}
