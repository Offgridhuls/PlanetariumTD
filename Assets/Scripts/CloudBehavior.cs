// Add this to a new script called "CloudTestSetup"
using UnityEngine;

public class CloudTestSetup : MonoBehaviour
{
    void Start()
    {
        // Create cube
        GameObject cloudObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cloudObject.transform.position = new Vector3(0, 0, 5); // 5 units in front of camera
        cloudObject.transform.localScale = new Vector3(2, 2, 2);
        
        // Create and assign material
        Material cloudMaterial = new Material(Shader.Find("Custom/BasicCloud"));
        cloudMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.5f));
        cloudMaterial.SetFloat("_Density", 0.5f);
        
        cloudObject.GetComponent<Renderer>().material = cloudMaterial;
    }
}