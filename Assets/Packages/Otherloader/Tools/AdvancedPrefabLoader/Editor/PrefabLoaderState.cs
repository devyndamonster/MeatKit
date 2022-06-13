﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Tools.AdvancedPrefabLoader
{
    public class PrefabLoaderState
    {
        [SerializeField]
        private string _selectedBundlePath;

        [SerializeField]
        private string[] _assetNames = new string[0];

        [SerializeField]
        private string _currentAssetPath;

        [SerializeField]
        private PrefabLoaderSpawnMode _currentSpawnMode;

        [SerializeField]
        public List<FavoritedAsset> Favorites = new List<FavoritedAsset>();

        public string SelectedBundlePath
        {
            get { return _selectedBundlePath; }
        }

        public string[] AssetNames
        {
            get { return _assetNames; }
        }


        public string CurrentAssetPath
        {
            get { return _currentAssetPath; }
            set
            {
                if (_currentAssetPath != value)
                {
                    _currentAssetPath = value;
                    WriteStateToCache(this);
                }
            }
        }

        public PrefabLoaderSpawnMode CurrentSpawnMode
        {
            get { return _currentSpawnMode; }
            set
            {
                if (_currentSpawnMode != value)
                {
                    _currentSpawnMode = value;
                    WriteStateToCache(this);
                }
            }
        }

        private static string PrefabStateFileName
        {
            get { return "PrefabLoaderState.json"; }
        }

        private static string PrefabStateFilePath
        {
            get { return Path.Combine(Path.GetDirectoryName(Application.dataPath), PrefabStateFileName); }
        }

        private static PrefabLoaderState _instance;

        public static PrefabLoaderState Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }
                else if (!File.Exists(PrefabStateFilePath))
                {
                    _instance = CreateNewCachedState();
                }
                else
                {
                    _instance = ReadStateFromCache();
                }

                return _instance;
            }
        }

        private static PrefabLoaderState CreateNewCachedState()
        {
            PrefabLoaderState newState = new PrefabLoaderState();
            WriteStateToCache(newState);
            return newState;
        }

        private static PrefabLoaderState ReadStateFromCache()
        {
            Debug.Log("Reading state from cache");
            return JsonUtility.FromJson<PrefabLoaderState>(File.ReadAllText(PrefabStateFileName));
        }

        private static void WriteStateToCache(PrefabLoaderState state)
        {
            Debug.Log("Writing state to cache");
            File.WriteAllText(PrefabStateFilePath, JsonUtility.ToJson(state));
        }

        public void SetSelectedAssetBundle(string bundlePath, string[] assetNames)
        {
            _selectedBundlePath = bundlePath;
            _assetNames = assetNames.OrderByDescending(o => o.Count(sub => sub == '/')).ToArray();
            _currentAssetPath = "";
            WriteStateToCache(this);
        }

        public void FavoritePrefab(string prefabName, string bundlePath)
        {
            Favorites.Add(new FavoritedAsset() { AssetBundlePath = bundlePath, AssetName = prefabName });
            WriteStateToCache(this);
        }

        public void UnfavoritePrefab(string prefabName, string bundlePath)
        {
            Favorites.RemoveAll(o => o.AssetBundlePath == bundlePath && o.AssetName == prefabName);
            WriteStateToCache(this);
        }

        public bool IsFavorited(string prefabName, string bundlePath)
        {
            return PrefabLoaderState.Instance.Favorites.Any(o => o.AssetName == prefabName && o.AssetBundlePath == bundlePath);
        }
    }

    public enum PrefabLoaderSpawnMode
    {
        Temporary,
        DeepCopy
    }

}
