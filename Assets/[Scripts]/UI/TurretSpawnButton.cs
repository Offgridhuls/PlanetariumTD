using System.Collections;
using System.Collections.Generic;
using Planetarium.Deployables;
using UnityEngine.UI;
using TMPro;
using UnityEngine;

public class TurretSpawnButton : MonoBehaviour
{
    [SerializeField]
    DeployableBase Turret;

    private void Start()
    {
        Button b = GetComponent<Button>();
        b.onClick.AddListener(delegate () { OnTurretButtonClicked(); });
    }

    void OnTurretButtonClicked()
    {
        //GameManager.Instance.EnableSelectedTurret(Turret);
    }
}
