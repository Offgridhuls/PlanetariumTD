using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Planetarium.Stats
{
    public class GameplayTagWindow : EditorWindow
    {
        private GameplayTagConfig config;
        private Vector2 scrollPosition;
        private string newTagName = "";
        private string newTagComment = "";
        private string searchFilter = "";
        private bool showRedirects = false;
        private string oldTagName = "";
        private string newRedirectTagName = "";
        private Dictionary<string, bool> tagFoldouts = new Dictionary<string, bool>();
        private bool showSettings = false;
        private GUIStyle headerStyle;
        private GUIStyle categoryStyle;
        private bool stylesInitialized;

        [MenuItem("PlanetariumTD/Gameplay Tags")]
        public static void ShowWindow()
        {
            GetWindow<GameplayTagWindow>("Gameplay Tags");
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 14;
            headerStyle.margin = new RectOffset(0, 0, 10, 10);

            categoryStyle = new GUIStyle(EditorStyles.foldout);
            categoryStyle.fontSize = 12;
            categoryStyle.fontStyle = FontStyle.Bold;

            stylesInitialized = true;
        }

        private void LoadConfig()
        {
            config = GameplayTagConfig.Instance;
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameplayTagConfig>();
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                {
                    AssetDatabase.CreateFolder("Assets", "Resources");
                }
                AssetDatabase.CreateAsset(config, "Assets/Resources/GameplayTagConfig.asset");
                AssetDatabase.SaveAssets();
            }
        }

        private void OnGUI()
        {
            if (!stylesInitialized)
            {
                InitializeStyles();
            }

            if (config == null)
            {
                LoadConfig();
                if (config == null)
                {
                    EditorGUILayout.HelpBox("Failed to load GameplayTagConfig!", MessageType.Error);
                    return;
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Gameplay Tag Configuration", headerStyle);
            EditorGUILayout.Space(5);

            DrawToolbar();
            EditorGUILayout.Space(10);

            if (showSettings)
            {
                DrawSettings();
            }
            else
            {
                DrawTagManagement();
            }

            if (GUI.changed)
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            
            showSettings = GUILayout.Toggle(showSettings, "Settings", EditorStyles.toolbarButton);
            showRedirects = GUILayout.Toggle(showRedirects, "Redirects", EditorStyles.toolbarButton);
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Validate Tags", EditorStyles.toolbarButton))
            {
                ValidateTags();
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Configuration Settings", headerStyle);
            
            EditorGUI.BeginChangeCheck();
            config.ImportTagsFromConfig = EditorGUILayout.Toggle("Import Tags From Config", config.ImportTagsFromConfig);
            config.WarnOnInvalidTags = EditorGUILayout.Toggle("Warn On Invalid Tags", config.WarnOnInvalidTags);
            config.FastReplication = EditorGUILayout.Toggle("Fast Replication", config.FastReplication);
            config.InvalidTagCharacters = EditorGUILayout.TextField("Invalid Characters", config.InvalidTagCharacters);
            
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
            }
        }

        private void DrawTagManagement()
        {
            DrawTagCreation();
            DrawSearch();
            DrawTagList();
            
            if (showRedirects)
            {
                DrawRedirects();
            }
        }

        private void DrawTagCreation()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Create New Tag", headerStyle);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            newTagName = EditorGUILayout.TextField("Tag Name", newTagName);
            newTagComment = EditorGUILayout.TextField("Comment", newTagComment);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.enabled = !string.IsNullOrEmpty(newTagName);
            if (GUILayout.Button("Add Tag", GUILayout.Width(100)))
            {
                if (!string.IsNullOrEmpty(newTagName))
                {
                    config.AddTag(newTagName, newTagComment);
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    newTagName = "";
                    newTagComment = "";
                }
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawSearch()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Search", GUILayout.Width(50));
            searchFilter = EditorGUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawTagList()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Existing Tags", headerStyle);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            var tagList = config.GameplayTagList.ToList();
            var filteredTags = string.IsNullOrEmpty(searchFilter) 
                ? tagList 
                : tagList.Where(t => t.Tag.ToLower().Contains(searchFilter.ToLower()) || 
                                   t.DevComment.ToLower().Contains(searchFilter.ToLower())).ToList();

            // Group tags by category
            var groupedTags = filteredTags.GroupBy(t => GetTagCategory(t.Tag))
                                        .OrderBy(g => g.Key);

            foreach (var group in groupedTags)
            {
                if (!tagFoldouts.ContainsKey(group.Key))
                {
                    tagFoldouts[group.Key] = true;
                }

                EditorGUILayout.Space(5);
                tagFoldouts[group.Key] = EditorGUILayout.Foldout(tagFoldouts[group.Key], group.Key, true, categoryStyle);

                if (tagFoldouts[group.Key])
                {
                    EditorGUI.indentLevel++;
                    foreach (var tagDef in group.OrderBy(t => t.Tag))
                    {
                        DrawTagItem(tagDef);
                    }
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawTagItem(GameplayTagDefinition tagDef)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            
            EditorGUILayout.LabelField(GetTagWithoutCategory(tagDef.Tag), GUILayout.Width(200));
            EditorGUILayout.LabelField(tagDef.DevComment);

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                if (EditorUtility.DisplayDialog("Delete Tag", 
                    $"Are you sure you want to delete the tag '{tagDef.Tag}'?", 
                    "Yes", "No"))
                {
                    var tagList = config.GameplayTagList.ToList();
                    tagList.Remove(tagDef);
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }
            }
            
            EditorGUILayout.EndHorizontal();
        }

        private void DrawRedirects()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Tag Redirects", headerStyle);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            oldTagName = EditorGUILayout.TextField("Old Tag", oldTagName);
            newRedirectTagName = EditorGUILayout.TextField("New Tag", newRedirectTagName);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            GUI.enabled = !string.IsNullOrEmpty(oldTagName) && !string.IsNullOrEmpty(newRedirectTagName);
            if (GUILayout.Button("Add Redirect", GUILayout.Width(100)))
            {
                if (!string.IsNullOrEmpty(oldTagName) && !string.IsNullOrEmpty(newRedirectTagName))
                {
                    config.AddRedirect(oldTagName, newRedirectTagName);
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                    oldTagName = "";
                    newRedirectTagName = "";
                }
            }
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Existing Redirects", EditorStyles.boldLabel);
            
            foreach (var redirect in config.GameplayTagRedirects)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"{redirect.OldTagName} → {redirect.NewTagName}");
                
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                {
                    var redirects = config.GameplayTagRedirects.ToList();
                    redirects.Remove(redirect);
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }
                
                EditorGUILayout.EndHorizontal();
            }
        }

        private string GetTagCategory(string tag)
        {
            var parts = tag.Split('.');
            return parts.Length > 0 ? parts[0] : "Uncategorized";
        }

        private string GetTagWithoutCategory(string tag)
        {
            var parts = tag.Split('.');
            return parts.Length > 1 ? string.Join(".", parts.Skip(1)) : tag;
        }

        private void ValidateTags()
        {
            var invalidTags = new List<GameplayTagDefinition>();
            foreach (var tagDef in config.GameplayTagList)
            {
                if (tagDef.Tag.IndexOfAny(config.InvalidTagCharacters.ToCharArray()) != -1)
                {
                    invalidTags.Add(tagDef);
                }
            }

            if (invalidTags.Count > 0)
            {
                var message = "The following tags contain invalid characters:\n\n";
                foreach (var tag in invalidTags)
                {
                    message += $"- {tag.Tag}\n";
                }
                EditorUtility.DisplayDialog("Invalid Tags", message, "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Tag Validation", "All tags are valid!", "OK");
            }
        }
    }
}
