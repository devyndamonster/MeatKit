using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using FistVR;
using HarmonyLib;
using System.Reflection;
using System.Linq;
using AssetsTools.NET.Extra;

namespace Tools.AdvancedPrefabLoader
{
    public class AdvancedPrefabLoader : EditorWindow
    {
        [MenuItem("Tools/Advanced Prefab Loader")]
        private static void Init()
        {
            GetWindow<AdvancedPrefabLoader>().Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("Select Asset Bundle"))
            {
                SelectNewAssetBundle();
            }

            if (PrefabLoaderState.Instance.AssetNames.Length > 0)
            {
                PrefabLoaderState.Instance.SelectedAssetIndex = EditorGUILayout.Popup(PrefabLoaderState.Instance.SelectedAssetIndex, PrefabLoaderState.Instance.AssetNames);

                if (GUILayout.Button("Spawn"))
                {
                    SpawnSelectedPrefab();
                }
            }
        }

        private void SelectNewAssetBundle()
        {
            string assetBundlePath = EditorUtility.OpenFilePanel("Select Asset Bundle", string.Empty, string.Empty);

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

        private void SpawnSelectedPrefab()
        {
            AssetBundle _bundle = AssetBundle.LoadFromFile(PrefabLoaderState.Instance.SelectedBundlePath);

            UnityEngine.Object spawned = Instantiate(_bundle.LoadAsset(PrefabLoaderState.Instance.GetSelectedAssetName()));

            _bundle.Unload(false);

            if(spawned is GameObject)
            {
                PrefabPostProcess.ProcessSpawnedObject((GameObject)spawned);
            }
        }
    }
}
