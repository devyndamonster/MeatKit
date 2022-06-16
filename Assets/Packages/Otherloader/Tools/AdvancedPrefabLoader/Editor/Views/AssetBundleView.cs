using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class AssetBundleView {

    private static List<string> visibleFolders = new List<string>();

    public static void Draw()
    {
        if (GUILayout.Button("Select Asset Bundle", new GUILayoutOption[] { GUILayout.Height(35) }))
        {
            SelectNewAssetBundle();
        }

        if (PrefabLoaderState.Instance.BundleAssetNames.Count() > 0)
        {
            GUILayout.Label("Selected Bundle : " + Path.GetFileName(PrefabLoaderState.Instance.BundleSelectedPath));

            HorizontalLine.Draw();
            DrawSpawmModeSelector();
            DrawSpawnSettings();
            HorizontalLine.Draw();
            DrawFavorites();
            HorizontalLine.Draw();

            if (PrefabLoaderState.Instance.BundleCurrentAssetPath.Contains("/") && GUILayout.Button("<- Go Back", new GUILayoutOption[] { GUILayout.Height(35) }))
            {
                PrefabLoaderState.Instance.BundleCurrentAssetPath = PrefabLoaderState.Instance.BundleCurrentAssetPath.Substring(0, PrefabLoaderState.Instance.BundleCurrentAssetPath.LastIndexOf("/"));
            }

            DrawPrefabSpawnButtons();
        }
    }

    private static void SelectNewAssetBundle()
    {
        string previousAssetBundleFolder = "";
        try
        {
            previousAssetBundleFolder = Directory.GetParent(PrefabLoaderState.Instance.BundleSelectedPath).FullName;
        }
        catch (Exception e) { }
        
        string assetBundlePath = EditorUtility.OpenFilePanel("Select Asset Bundle", previousAssetBundleFolder, string.Empty);

        if (!string.IsNullOrEmpty(assetBundlePath))
        {
            AssetBundle _bundle = AssetBundle.LoadFromFile(assetBundlePath);

            if (_bundle != null)
            {
                PrefabLoaderState.Instance.SetSelectedAssetBundle(assetBundlePath, _bundle.GetAllAssetNames());
                _bundle.Unload(true);
            }
        }
    }

    private static void DrawSpawmModeSelector()
    {
        PrefabLoaderState.Instance.BundleCurrentSpawnMode = (PrefabLoaderSpawnMode)GUILayout.Toolbar((int)PrefabLoaderState.Instance.BundleCurrentSpawnMode, Enum.GetNames(typeof(PrefabLoaderSpawnMode)));
    }

    private static void DrawSpawnSettings()
    {
        if (PrefabLoaderState.Instance.BundleCurrentSpawnMode == PrefabLoaderSpawnMode.DeepCopy)
        {
            DrawDeepCopySettings();
        }
    }

    private static void DrawDeepCopySettings()
    {
        PrefabLoaderState.Instance.BundleRipMeshes = EditorGUILayout.Toggle("Rip Meshes On Spawn", PrefabLoaderState.Instance.BundleRipMeshes);
    }

    private static void DrawFavorites()
    {
        GUILayout.Label("Favorites");

        GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
        leftButtonStyle.alignment = TextAnchor.MiddleLeft;

        for (int i = 0; i < PrefabLoaderState.Instance.BundleFavorites.Count(); i++)
        {
            FavoritedAsset favorite = PrefabLoaderState.Instance.BundleFavorites[i];

            GUILayout.BeginHorizontal();

            if (GUILayout.Button(favorite.AssetName.Substring(favorite.AssetName.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
            {
                SpawnAssetFromBundle(favorite.AssetName, favorite.AssetBundlePath);
            }
            if (GUILayout.Button("Unfavorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
            {
                PrefabLoaderState.Instance.UnfavoriteBundlePrefab(favorite.AssetName, favorite.AssetBundlePath);
                i -= 1;
            }

            GUILayout.EndHorizontal();
        }
    }

    private static void SpawnAssetFromBundle(string assetName, string bundlePath)
    {
        AssetBundle _bundle = AssetBundle.LoadFromFile(bundlePath);
        UnityEngine.Object spawned = GameObject.Instantiate(_bundle.LoadAsset(assetName));
        _bundle.Unload(false);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        if (spawned is GameObject && PrefabLoaderState.Instance.BundleCurrentSpawnMode == PrefabLoaderSpawnMode.DeepCopy)
        {
            PrefabPostProcess.ProcessSpawnedObject((GameObject)spawned);
        }

        EditorGUIUtility.ExitGUI();
    }

    private static void DrawPrefabSpawnButtons()
    {
        visibleFolders.Clear();

        EditorGUILayout.BeginVertical();

        GUIStyle leftButtonStyle = new GUIStyle(GUI.skin.button);
        leftButtonStyle.alignment = TextAnchor.MiddleLeft;

        Color folderColor = new Color(220f / 255, 220f / 255, 220f / 255);

        for (int i = 0; i < PrefabLoaderState.Instance.BundleAssetNames.Length; i++)
        {
            string prefabName = PrefabLoaderState.Instance.BundleAssetNames[i];

            if (string.IsNullOrEmpty(PrefabLoaderState.Instance.BundleCurrentAssetPath))
            {
                PrefabLoaderState.Instance.BundleCurrentAssetPath = prefabName.Split('/')[0];
            }

            if (IsAssetVisible(prefabName))
            {
                if (IsAssetSpawnable(prefabName))
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(prefabName.Substring(prefabName.LastIndexOf('/') + 1), leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                    {
                        SpawnAssetFromBundle(prefabName, PrefabLoaderState.Instance.BundleSelectedPath);
                    }

                    if (PrefabLoaderState.Instance.IsFavoritedInBundle(prefabName, PrefabLoaderState.Instance.BundleSelectedPath))
                    {
                        if (GUILayout.Button("Unfavorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                        {
                            PrefabLoaderState.Instance.UnfavoriteBundlePrefab(prefabName, PrefabLoaderState.Instance.BundleSelectedPath);
                        }
                    }
                    else
                    {
                        if (GUILayout.Button("Favorite", new GUILayoutOption[] { GUILayout.Height(25), GUILayout.Width(100) }))
                        {
                            PrefabLoaderState.Instance.FavoriteBundlePrefab(prefabName, PrefabLoaderState.Instance.BundleSelectedPath);
                        }
                    }

                    GUILayout.EndHorizontal();
                }

                else
                {
                    string folderPath = PrefabLoaderState.Instance.BundleCurrentAssetPath + "/" + prefabName.Replace(PrefabLoaderState.Instance.BundleCurrentAssetPath, "").Trim('/').Split('/')[0];
                    if (!visibleFolders.Contains(folderPath))
                    {
                        visibleFolders.Add(folderPath);

                        string folderName = folderPath.Substring(folderPath.LastIndexOf('/') + 1);

                        Color prevColor = GUI.backgroundColor;
                        GUI.backgroundColor = folderColor;
                        if (GUILayout.Button(folderName + " ->", leftButtonStyle, new GUILayoutOption[] { GUILayout.Height(25) }))
                        {
                            PrefabLoaderState.Instance.BundleCurrentAssetPath = folderPath;
                        }
                        GUI.backgroundColor = prevColor;
                    }
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static bool IsAssetSpawnable(string prefabName)
    {
        string remainingPath = prefabName.Replace(PrefabLoaderState.Instance.BundleCurrentAssetPath, "").Trim('/');
        return !remainingPath.Contains("/");
    }

    private static bool IsAssetVisible(string prefabName)
    {
        return prefabName.Contains(PrefabLoaderState.Instance.BundleCurrentAssetPath);
    }
}
