using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

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
            PrefabLoaderState.Instance.GameSelectedPath = gameRipPath;

            //Check for indexed game rip folder

            //If not exist, index the game rip folder
        }
    }

}
