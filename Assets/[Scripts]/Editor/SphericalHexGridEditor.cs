using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(SphericalHexGrid))]
public class SphericalHexGridEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SphericalHexGrid grid = (SphericalHexGrid)target;
        
        EditorGUILayout.Space();
        if (GUILayout.Button("Generate Hex Sphere"))
        {
            Undo.RecordObject(grid.gameObject, "Generate Hex Sphere");
            
            // Also record existing children for undo
            Transform transform = grid.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                Undo.RecordObject(transform.GetChild(i).gameObject, "Generate Hex Sphere");
            }
            
            grid.GenerateSphere();
        }
    }
}
#endif