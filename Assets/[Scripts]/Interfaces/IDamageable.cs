using UnityEngine;
using UnityEngine.Events;

public enum DamageableType
{
    Planet,
    Enemy,
    Turret,
    Structure
}

public interface IDamageable
{
    DamageableType GetDamageableType();
    void TakeDamage(float damage);
    void ProcessDamage(DamageData data);
    bool IsAlive { get; }
    float CurrentHealth { get; }
    float MaxHealth { get; }
}
