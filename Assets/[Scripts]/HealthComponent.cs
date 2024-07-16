using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthComponent : MonoBehaviour, IDamageable
{
    float CurrentHealth;
    float MaxHealth;

    float StartingIntegrity;
    float Integrity;

    bool isDead = false;
    public float GetCurrentHealth()
    {
        return CurrentHealth;
    }
    public float GetMaxHealth()
    {
        return MaxHealth;
    }
    public void ProcessDamage(DamageData data)
    {
        CurrentHealth -= ((data.Damage * data.DamageMultiplier) / Integrity);
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            isDead = true;
        }
    }
}
