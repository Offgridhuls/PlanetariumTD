using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Planetarium.Stats
{
    public class GameplayTagGenerator : AssetPostprocessor
    {
        private const string GENERATED_CLASS_PATH = "Assets/[Scripts]/Stats/GameplayTagSystem/Generated";
        private const string GENERATED_CLASS_NAME = "GeneratedTags.cs";

        [MenuItem("PlanetariumTD/Generate Tag Classes")]
        public static void GenerateTagClasses()
        {
            var config = GameplayTagConfig.Instance;
            if (config == null)
            {
                UnityEngine.Debug.LogError("GameplayTagConfig not found!");
                return;
            }

            // Ensure directory exists
            if (!Directory.Exists(GENERATED_CLASS_PATH))
            {
                Directory.CreateDirectory(GENERATED_CLASS_PATH);
            }

            var filePath = Path.Combine(GENERATED_CLASS_PATH, GENERATED_CLASS_NAME);
            var code = GenerateCode(config);
            File.WriteAllText(filePath, code);
            AssetDatabase.Refresh();

            UnityEngine.Debug.Log($"Generated tag classes at {filePath}");
        }

        private static string GenerateCode(GameplayTagConfig config)
        {
            var sb = new StringBuilder();

            // File header
            sb.AppendLine("// This file is auto-generated. Do not modify it manually!");
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine();
            sb.AppendLine("namespace Planetarium.Stats");
            sb.AppendLine("{");

            // Main static class
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// Auto-generated gameplay tags for compile-time safety");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    public static class Tags");
            sb.AppendLine("    {");

            // Generate nested classes for each tag category
            var tagsByCategory = config.GameplayTagList
                .GroupBy(t => GetTagCategory(t.Tag))
                .OrderBy(g => g.Key);

            foreach (var category in tagsByCategory)
            {
                GenerateCategoryClass(sb, category.Key, category.ToList(), 2);
            }

            // Add static constructor to initialize all tags
            sb.AppendLine("        static Tags()");
            sb.AppendLine("        {");
            sb.AppendLine("            AllTags = new List<GameplayTag>();");
            foreach (var tag in config.GameplayTagList)
            {
                var fieldName = GetTagFieldPath(tag.Tag);
                sb.AppendLine($"            AllTags.Add({fieldName});");
            }
            sb.AppendLine("        }");
            sb.AppendLine();

            // Add helper methods
            sb.AppendLine("        public static readonly List<GameplayTag> AllTags;");
            sb.AppendLine();
            sb.AppendLine("        public static GameplayTag GetTag(string tagName)");
            sb.AppendLine("        {");
            sb.AppendLine("            return AllTags.Find(t => t.TagName == tagName);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public static bool HasTag(string tagName)");
            sb.AppendLine("        {");
            sb.AppendLine("            return AllTags.Exists(t => t.TagName == tagName);");
            sb.AppendLine("        }");

            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static void GenerateCategoryClass(StringBuilder sb, string category, List<GameplayTagDefinition> tags, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            // Class declaration
            sb.AppendLine($"{indent}public static class {SanitizeIdentifier(category)}");
            sb.AppendLine($"{indent}{{");

            // Generate fields for direct tags in this category
            var directTags = tags.Where(t => GetTagPartsCount(t.Tag) == 1).ToList();
            foreach (var tag in directTags)
            {
                GenerateTagField(sb, tag, indentLevel + 1);
            }

            // Generate nested classes for subcategories
            var subcategories = tags
                .Where(t => GetTagPartsCount(t.Tag) > 1)
                .GroupBy(t => GetFirstSubcategory(t.Tag))
                .OrderBy(g => g.Key);

            foreach (var subcategory in subcategories)
            {
                var subcategoryTags = subcategory.Select(t => new GameplayTagDefinition
                {
                    Tag = RemoveFirstPart(t.Tag),
                    DevComment = t.DevComment
                }).ToList();

                GenerateCategoryClass(sb, subcategory.Key, subcategoryTags, indentLevel + 1);
            }

            sb.AppendLine($"{indent}}}");
            sb.AppendLine();
        }

        private static void GenerateTagField(StringBuilder sb, GameplayTagDefinition tag, int indentLevel)
        {
            var indent = new string(' ', indentLevel * 4);
            
            // Add XML documentation if there's a comment
            if (!string.IsNullOrEmpty(tag.DevComment))
            {
                sb.AppendLine($"{indent}/// <summary>");
                sb.AppendLine($"{indent}/// {tag.DevComment}");
                sb.AppendLine($"{indent}/// </summary>");
            }

            var fieldName = SanitizeIdentifier(GetLastPart(tag.Tag));
            sb.AppendLine($"{indent}public static readonly GameplayTag {fieldName} = new GameplayTag(\"{tag.Tag}\", \"{tag.DevComment}\");");
            sb.AppendLine();
        }

        private static string GetTagCategory(string tag)
        {
            return tag.Split('.')[0];
        }

        private static string GetFirstSubcategory(string tag)
        {
            var parts = tag.Split('.');
            return parts.Length > 1 ? parts[1] : "";
        }

        private static string RemoveFirstPart(string tag)
        {
            var parts = tag.Split('.');
            return string.Join(".", parts.Skip(1));
        }

        private static string GetLastPart(string tag)
        {
            var parts = tag.Split('.');
            return parts[parts.Length - 1];
        }

        private static int GetTagPartsCount(string tag)
        {
            return tag.Split('.').Length;
        }

        private static string GetTagFieldPath(string tag)
        {
            var parts = tag.Split('.');
            var path = new List<string>();
            
            for (int i = 0; i < parts.Length; i++)
            {
                path.Add(SanitizeIdentifier(parts[i]));
            }

            return string.Join(".", path);
        }

        private static string SanitizeIdentifier(string identifier)
        {
            // Handle C# keywords
            switch (identifier.ToLower())
            {
                case "class": return "@class";
                case "static": return "@static";
                case "public": return "@public";
                case "private": return "@private";
                case "protected": return "@protected";
                case "internal": return "@internal";
                case "virtual": return "@virtual";
                case "abstract": return "@abstract";
                case "override": return "@override";
                case "new": return "@new";
                case "sealed": return "@sealed";
                default: return identifier;
            }
        }
    }
}
