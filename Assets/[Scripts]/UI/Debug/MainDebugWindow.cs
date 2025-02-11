using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Planetarium.UI
{
    public class MainDebugWindow : EditorWindow
    {
        private Dictionary<System.Type, EditorWindow> activeWindows = new Dictionary<System.Type, EditorWindow>();
        private Vector2 scrollPosition;

        [MenuItem("PlanetariumTD/Debug/Main Debug Window %#d")] // Ctrl/Cmd + Shift + D
        private static void ShowWindow()
        {
            var window = GetWindow<MainDebugWindow>();
            window.titleContent = new GUIContent("Debug Tools");
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("PlanetariumTD Debug Tools", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawDebugWindowButton<UIViewDebugWindow>("UI View Debug", "Monitor and control UI views");
            DrawDebugWindowButton<UITagDebugWindow>("Tag Debug", "View all gameplay tags in the scene");

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);
            DrawGlobalControls();
        }

        private void DrawDebugWindowButton<T>(string name, string description) where T : EditorWindow
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // Window status indicator
            bool isOpen = IsWindowOpen<T>();
            GUI.color = isOpen ? Color.green : Color.gray;
            EditorGUILayout.LabelField("●", GUILayout.Width(20));
            GUI.color = Color.white;

            // Title and description
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            // Toggle button
            if (GUILayout.Button(isOpen ? "Close" : "Open", GUILayout.Width(60)))
            {
                ToggleWindow<T>();
            }
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private void DrawGlobalControls()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Open All", EditorStyles.toolbarButton))
            {
                OpenWindow<UIViewDebugWindow>();
                OpenWindow<UITagDebugWindow>();
            }
            
            if (GUILayout.Button("Close All", EditorStyles.toolbarButton))
            {
                CloseAllWindows();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private bool IsWindowOpen<T>() where T : EditorWindow
        {
            return activeWindows.ContainsKey(typeof(T)) && activeWindows[typeof(T)] != null;
        }

        private void ToggleWindow<T>() where T : EditorWindow
        {
            if (IsWindowOpen<T>())
                CloseWindow<T>();
            else
                OpenWindow<T>();
        }

        private void OpenWindow<T>() where T : EditorWindow
        {
            if (!IsWindowOpen<T>())
            {
                var window = GetWindow<T>();
                activeWindows[typeof(T)] = window;
            }
        }

        private void CloseWindow<T>() where T : EditorWindow
        {
            if (IsWindowOpen<T>())
            {
                activeWindows[typeof(T)].Close();
                activeWindows.Remove(typeof(T));
            }
        }

        private void CloseAllWindows()
        {
            foreach (var window in activeWindows.Values)
            {
                if (window != null)
                    window.Close();
            }
            activeWindows.Clear();
        }

        private void OnDestroy()
        {
            CloseAllWindows();
        }

        private void Update()
        {
            // Clean up any null references
            var nullWindows = new List<System.Type>();
            foreach (var kvp in activeWindows)
            {
                if (kvp.Value == null)
                    nullWindows.Add(kvp.Key);
            }
            foreach (var type in nullWindows)
            {
                activeWindows.Remove(type);
            }

            Repaint();
        }
    }
}
