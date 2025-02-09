using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

public class CodebaseScanner : EditorWindow
{
    private string outputPath = "CodebaseContext";
    private int chunkSize = 50000; // Characters per file
    private bool splitByNamespace = true;

    [MenuItem("Tools/Codebase Scanner")]
    public static void ShowWindow()
    {
        GetWindow<CodebaseScanner>("Codebase Scanner");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Codebase Scanner", EditorStyles.boldLabel);
        outputPath = EditorGUILayout.TextField("Output Base Filename", outputPath);
        chunkSize = EditorGUILayout.IntField("Characters per chunk", chunkSize);
        splitByNamespace = EditorGUILayout.Toggle("Split by Namespace", splitByNamespace);

        if (GUILayout.Button("Scan Codebase"))
        {
            ScanCodebase();
        }
    }

    private string ExtractNamespace(string content)
    {
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            if (line.TrimStart().StartsWith("namespace "))
            {
                return line.TrimStart()
                    .Replace("namespace ", "")
                    .TrimEnd('{', ' ', '\r', '\n');
            }
        }
        return "NoNamespace";
    }

    private void ScanCodebase()
    {
        string[] csFiles = Directory.GetFiles(Application.dataPath, "*.cs", SearchOption.AllDirectories);
        Dictionary<string, StringBuilder> namespaceBuilders = new Dictionary<string, StringBuilder>();
        StringBuilder currentChunk = new StringBuilder();
        int currentChunkNumber = 1;

        // First pass: Collect files by namespace
        foreach (string filePath in csFiles)
        {
            string content = File.ReadAllText(filePath);
            string relativePath = "Assets" + filePath.Substring(Application.dataPath.Length);
            string fileContent = $"FILE: {relativePath}\n```csharp\n{content}\n```\n\n";

            if (splitByNamespace)
            {
                string namespaceName = ExtractNamespace(content);
                if (!namespaceBuilders.ContainsKey(namespaceName))
                {
                    namespaceBuilders[namespaceName] = new StringBuilder();
                }
                namespaceBuilders[namespaceName].Append(fileContent);
            }
            else
            {
                currentChunk.Append(fileContent);
                
                // If chunk is full, save it
                if (currentChunk.Length >= chunkSize)
                {
                    SaveChunk(currentChunk, currentChunkNumber);
                    currentChunk.Clear();
                    currentChunkNumber++;
                }
            }
        }

        // Save remaining content or namespace chunks
        if (splitByNamespace)
        {
            foreach (var kvp in namespaceBuilders)
            {
                string namespaceName = kvp.Key;
                StringBuilder namespaceContent = kvp.Value;
                
                // Split large namespaces into multiple files if needed
                if (namespaceContent.Length > chunkSize)
                {
                    int nsChunkNumber = 1;
                    for (int i = 0; i < namespaceContent.Length; i += chunkSize)
                    {
                        int length = Mathf.Min(chunkSize, namespaceContent.Length - i);
                        string chunkContent = namespaceContent.ToString(i, length);
                        SaveNamespaceChunk(namespaceName, chunkContent, nsChunkNumber);
                        nsChunkNumber++;
                    }
                }
                else
                {
                    SaveNamespaceChunk(namespaceName, namespaceContent.ToString(), 1);
                }
            }
        }
        else if (currentChunk.Length > 0)
        {
            SaveChunk(currentChunk, currentChunkNumber);
        }

        Debug.Log($"Codebase split into chunks in folder: {Path.Combine(Application.dataPath, "..", outputPath)}");
        EditorUtility.RevealInFinder(Path.Combine(Application.dataPath, "..", outputPath));
    }

    private void SaveChunk(StringBuilder content, int chunkNumber)
    {
        string directoryPath = Path.Combine(Application.dataPath, "..", outputPath);
        Directory.CreateDirectory(directoryPath);
        
        string fileName = Path.Combine(directoryPath, $"chunk_{chunkNumber}.txt");
        
        StringBuilder header = new StringBuilder();
        header.AppendLine($"UNITY CODEBASE CONTEXT - CHUNK {chunkNumber}");
        header.AppendLine("=====================================");
        header.AppendLine($"Characters in chunk: {content.Length}");
        header.AppendLine();
        
        File.WriteAllText(fileName, header.ToString() + content.ToString());
    }

    private void SaveNamespaceChunk(string namespaceName, string content, int chunkNumber)
    {
        string directoryPath = Path.Combine(Application.dataPath, "..", outputPath);
        Directory.CreateDirectory(directoryPath);
        
        string fileName = chunkNumber == 1 
            ? Path.Combine(directoryPath, $"{namespaceName}.txt")
            : Path.Combine(directoryPath, $"{namespaceName}_{chunkNumber}.txt");
        
        StringBuilder header = new StringBuilder();
        header.AppendLine($"UNITY CODEBASE CONTEXT - NAMESPACE: {namespaceName}");
        if (chunkNumber > 1) header.AppendLine($"CHUNK: {chunkNumber}");
        header.AppendLine("=====================================");
        header.AppendLine($"Characters in chunk: {content.Length}");
        header.AppendLine();
        
        File.WriteAllText(fileName, header.ToString() + content);
    }
}