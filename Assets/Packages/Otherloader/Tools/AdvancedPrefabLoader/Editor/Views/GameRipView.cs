using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Valve.Newtonsoft;
using Valve.Newtonsoft.Json;

public static class GameRipView{

    public static void Draw()
    {
        if (GUILayout.Button("Select Select Game Rip", new GUILayoutOption[] { GUILayout.Height(35) }))
        {
            SelectNewGameRipFolder();
        }

        if (!string.IsNullOrEmpty(PrefabLoaderState.Instance.GameSelectedPath))
        {
            GUILayout.Label("Selected Game Rip Folder : " + PrefabLoaderState.Instance.GameSelectedPath);
        }
    }

    private static void SelectNewGameRipFolder()
    {
        string previousGameRipFolder = PrefabLoaderState.Instance.GameSelectedPath;
        string gameRipPath = EditorUtility.OpenFolderPanel("Select Game Rip Folder", previousGameRipFolder, string.Empty);

        if (!string.IsNullOrEmpty(gameRipPath))
        {
            if(PrefabLoaderState.Instance.GameSelectedPath != gameRipPath)
            {
                GameRipFileIndex gameRipFileIndex = CreateFileIndexForGameRip(gameRipPath);
                WriteGameRipIndexToFile(gameRipFileIndex);
            }

            PrefabLoaderState.Instance.GameSelectedPath = gameRipPath;
        }
    }

    private static GameRipFileIndex CreateFileIndexForGameRip(string gameRipPath)
    {
        GameRipFileIndex gameRipFileIndex = new GameRipFileIndex();
        PopulateFileIndexesForFolder(gameRipPath, gameRipFileIndex);
        return gameRipFileIndex;
    }

    private static void PopulateFileIndexesForFolder(string folderPath, GameRipFileIndex gameRipFileIndex)
    {
        Debug.Log("Populating indexes for folder: " + folderPath);
        foreach(string dir in Directory.GetDirectories(folderPath))
        {
            PopulateFileIndexesForFolder(dir, gameRipFileIndex);
        }

        foreach(string metaFile in Directory.GetFiles(folderPath, "*.meta"))
        {
            string guid = GetGUIDFromMetaFile(metaFile);
            string realFile = metaFile.Replace(".meta", "");
            gameRipFileIndex.GUIDToFilePath[guid] = realFile;
        }
    }

    private static string GetGUIDFromMetaFile(string filePath)
    {
        string[] lines = File.ReadAllLines(filePath);
        return lines.FirstOrDefault(o => o.Contains("guid: ")).Replace("guid: ", "").Trim();
    }

    private static void WriteGameRipIndexToFile(GameRipFileIndex fileIndex)
    {
        string filePath = GetGameRipFileIndexPath();
        string fileJson = JsonConvert.SerializeObject(fileIndex);
        File.WriteAllText(filePath, fileJson);
    }

    private static string GetGameRipFileIndexPath()
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), "GameRipFileIndex.json");
    }

}
