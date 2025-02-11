using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using Planetarium.Stats;

namespace Planetarium.UI
{
    public class UITagDebugWindow : EditorWindow
    {
        private Vector2 scrollPosition;
        private string searchFilter = "";
        private Dictionary<GameplayTag, bool> tagFoldouts = new Dictionary<GameplayTag, bool>();
        private Dictionary<GameplayTag, List<TaggedComponent>> taggedComponents = new Dictionary<GameplayTag, List<TaggedComponent>>();
        private bool groupByCategory = false;

        [MenuItem("PlanetariumTD/Debug/Tag Debug Window")]
        private static void ShowWindow()
        {
            var window = GetWindow<UITagDebugWindow>();
            window.titleContent = new GUIContent("Tag Debug");
            window.Show();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to debug Tags", MessageType.Info);
                return;
            }

            DrawToolbar();
            DrawSearchBar();
            DrawTagList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshTagData();
            }

            groupByCategory = EditorGUILayout.ToggleLeft("Group by Category", groupByCategory, GUILayout.Width(120));
            
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

        private void RefreshTagData()
        {
            taggedComponents.Clear();
            
            // Find all TaggedComponents in the scene
            var components = FindObjectsOfType<TaggedComponent>();
            
            // Group components by their tags
            foreach (var component in components)
            {
                foreach (var tag in component.Tags)
                {
                    if (!taggedComponents.ContainsKey(tag))
                    {
                        taggedComponents[tag] = new List<TaggedComponent>();
                    }
                    taggedComponents[tag].Add(component);
                }
            }
        }

        private void DrawTagList()
        {
            if (taggedComponents.Count == 0)
            {
                RefreshTagData();
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var filteredTags = taggedComponents
                .Where(kvp => string.IsNullOrEmpty(searchFilter) || 
                            kvp.Key.ToString().ToLower().Contains(searchFilter.ToLower()));

            if (groupByCategory)
            {
                DrawGroupedTags(filteredTags);
            }
            else
            {
                DrawFlatTagList(filteredTags);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroupedTags(IEnumerable<KeyValuePair<GameplayTag, List<TaggedComponent>>> filteredTags)
        {
            var groupedTags = filteredTags
                .GroupBy(kvp => GetTagCategory(kvp.Key))
                .OrderBy(g => g.Key);

            foreach (var group in groupedTags)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField(group.Key, EditorStyles.boldLabel);
                
                foreach (var kvp in group.OrderBy(kvp => kvp.Key.ToString()))
                {
                    DrawTagEntry(kvp.Key, kvp.Value);
                }
            }
        }

        private void DrawFlatTagList(IEnumerable<KeyValuePair<GameplayTag, List<TaggedComponent>>> filteredTags)
        {
            foreach (var kvp in filteredTags.OrderBy(kvp => kvp.Key.ToString()))
            {
                DrawTagEntry(kvp.Key, kvp.Value);
            }
        }

        private void DrawTagEntry(GameplayTag tag, List<TaggedComponent> components)
        {
            // Ensure we have a foldout state
            if (!tagFoldouts.ContainsKey(tag))
                tagFoldouts[tag] = false;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            
            // Header
            EditorGUILayout.BeginHorizontal();
            
            tagFoldouts[tag] = EditorGUILayout.Foldout(tagFoldouts[tag], "", true);
            
            // Tag name with category color
            GUI.color = GetTagColor(tag);
            EditorGUILayout.LabelField(tag.ToString(), EditorStyles.boldLabel);
            GUI.color = Color.white;
            
            // Count badge
            GUI.backgroundColor = GetTagColor(tag);
            GUILayout.Label($"{components.Count}", EditorStyles.helpBox, GUILayout.Width(30));
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();

            // Expanded details
            if (tagFoldouts[tag])
            {
                EditorGUI.indentLevel++;
                
                foreach (var component in components)
                {
                    if (component == null) continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(component.gameObject, typeof(GameObject), true);
                    
                    // Show component type
                    var typeName = component.GetComponents<Component>()
                        .FirstOrDefault(c => c is ITaggable)?.GetType().Name ?? "Unknown";
                    EditorGUILayout.LabelField(typeName, GUILayout.Width(150));
                    
                    // Show active state
                    var isActive = component.gameObject.activeInHierarchy;
                    GUI.color = isActive ? Color.green : Color.gray;
                    EditorGUILayout.LabelField(isActive ? "Active" : "Inactive", GUILayout.Width(60));
                    GUI.color = Color.white;
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        private string GetTagCategory(GameplayTag tag)
        {
            var tagStr = tag.ToString();
            var firstDot = tagStr.IndexOf('.');
            return firstDot >= 0 ? tagStr.Substring(0, firstDot) : "Other";
        }

        private Color GetTagColor(GameplayTag tag)
        {
            var category = GetTagCategory(tag);
            
            switch (category.ToLower())
            {
                case "state":
                    return new Color(0.3f, 0.8f, 0.3f); // Green for states
                case "enemy":
                    return new Color(0.8f, 0.3f, 0.3f); // Red for enemies
                case "weapon":
                    return new Color(0.8f, 0.8f, 0.3f); // Yellow for weapons
                case "buff":
                    return new Color(0.3f, 0.3f, 0.8f); // Blue for buffs
                default:
                    return Color.gray;
            }
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                Repaint();
            }
        }
    }
}
