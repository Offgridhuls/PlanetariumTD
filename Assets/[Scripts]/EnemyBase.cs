using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSMC.Runtime;

public class EnemyBase : FSMC_Executer
{
   
    HealthComponent HealthComponent;

    [Header("Stats")]
    [SerializeField]
    private EnemyStats EnemyStats;

    PlanetBase CurrentPlanet;
    protected override void Start()
    {
        HealthComponent = GetComponent<HealthComponent>();
        CurrentPlanet = FindObjectOfType<PlanetBase>();
        base.Start();
    }

    protected override void Update()
    {
        base.Update();
    }

    public PlanetBase GetCurrentPlanet()
    {
        return CurrentPlanet;
    }
    public EnemyStats GetStats()
    {
        return EnemyStats;
    }
}
