using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage);
    bool IsAlive { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
}
