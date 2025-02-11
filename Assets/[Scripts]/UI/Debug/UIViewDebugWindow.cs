using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Planetarium.UI
{
    public class UIViewDebugWindow : EditorWindow
    {
        private UIManager uiManager;
        private Vector2 scrollPosition;
        private bool showInactiveViews = true;
        private bool showActiveViews = true;
        private string searchFilter = "";
        private Dictionary<string, bool> viewFoldouts = new Dictionary<string, bool>();

        [MenuItem("PlanetariumTD/UI/View Debug Window")]
        private static void ShowWindow()
        {
            var window = GetWindow<UIViewDebugWindow>();
            window.titleContent = new GUIContent("UI View Debug");
            window.Show();
        }

        private void OnGUI()
        {
            if (Application.isPlaying)
            {
                if (uiManager == null)
                {
                    uiManager = FindObjectOfType<UIManager>();
                    if (uiManager == null)
                    {
                        EditorGUILayout.HelpBox("No UIManager found in scene!", MessageType.Warning);
                        return;
                    }
                }

                DrawToolbar();
                DrawSearchBar();
                DrawViewList();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to debug UI Views", MessageType.Info);
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                Repaint();
            }

            if (GUILayout.Button("Close All", EditorStyles.toolbarButton))
            {
                uiManager.CloseAllViews();
            }

            showActiveViews = EditorGUILayout.ToggleLeft("Show Active", showActiveViews, GUILayout.Width(100));
            showInactiveViews = EditorGUILayout.ToggleLeft("Show Inactive", showInactiveViews, GUILayout.Width(100));
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSearchBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchFilter = EditorGUILayout.TextField("Search", searchFilter, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(50)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawViewList()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var views = uiManager.GetComponentsInChildren<UIView>(true)
                .Where(v => string.IsNullOrEmpty(searchFilter) || 
                           v.name.ToLower().Contains(searchFilter.ToLower()));

            foreach (var view in views)
            {
                bool isActive = view.IsOpen;
                
                // Skip based on filters
                if ((!showActiveViews && isActive) || (!showInactiveViews && !isActive))
                    continue;

                // Ensure we have a foldout state
                if (!viewFoldouts.ContainsKey(view.name))
                    viewFoldouts[view.name] = false;

                EditorGUILayout.BeginVertical(GUI.skin.box);
                
                // Header with foldout and close button
                EditorGUILayout.BeginHorizontal();
                
                // Close button
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    view.Close();
                    continue;
                }
                
                viewFoldouts[view.name] = EditorGUILayout.Foldout(viewFoldouts[view.name], "", true);
                
                // View name and type
                EditorGUILayout.LabelField(view.name, EditorStyles.boldLabel);
                
                // Status indicator
                GUI.color = isActive ? Color.green : Color.gray;
                EditorGUILayout.LabelField(isActive ? "Open" : "Closed", GUILayout.Width(50));
                GUI.color = Color.white;
                
                // Toggle buttons
                if (GUILayout.Button(isActive ? "Close" : "Open", GUILayout.Width(60)))
                {
                    if (isActive)
                        view.Close();
                    else
                        view.Open();
                }
                
                EditorGUILayout.EndHorizontal();

                // Expanded details
                if (viewFoldouts[view.name])
                {
                    EditorGUI.indentLevel++;
                    
                    EditorGUILayout.LabelField("Type", view.GetType().Name);
                    EditorGUILayout.ObjectField("GameObject", view.gameObject, typeof(GameObject), true);
                    EditorGUILayout.Toggle("Start Open", view.startOpen);
                    
                    // Show parent view if any
                    var parentTransform = view.transform.parent;
                    if (parentTransform != null)
                    {
                        var parentView = parentTransform.GetComponent<UIView>();
                        if (parentView != null)
                        {
                            EditorGUILayout.ObjectField("Parent View", parentView, typeof(UIView), true);
                        }
                    }
                    
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();
        }

        private void Update()
        {
            // Repaint the window to update the view states
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
