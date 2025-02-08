using UnityEngine;
using System;
using UnityEditor;

namespace Planetarium.AI
{
    [CreateAssetMenu(fileName = "EnemyStateConfig", menuName = "PlanetariumTD/Enemy State Config")]
    public class EnemyStateConfig : ScriptableObject
    {
        [System.Serializable]
        public class StateEntry
        {
            [SerializeField] private string stateName;
            [SerializeField] private MonoScript stateScript;
            [SerializeField] private bool isDefaultState;

            public string StateName => stateName;
            public MonoScript StateScript => stateScript;
            public bool IsDefaultState => isDefaultState;

            public Type GetStateType()
            {
                return stateScript?.GetClass();
            }
        }

        [SerializeField] private StateEntry[] states;
        public StateEntry[] States => states;
    }
}
