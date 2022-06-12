using System;
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
        private int _selectedAssetIndex;

        public string SelectedBundlePath 
        { 
            get { return _selectedBundlePath; }
        }

        public string[] AssetNames
        {
            get { return _assetNames; }
        }

        public int SelectedAssetIndex 
        {
            get { return _selectedAssetIndex; }
            set 
            { 
                if(_selectedAssetIndex != value)
                {
                    _selectedAssetIndex = value;
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
            _assetNames = assetNames;
            WriteStateToCache(this);
        }

        public string GetSelectedAssetName()
        {
            return _assetNames[_selectedAssetIndex];
        }

    }
}
