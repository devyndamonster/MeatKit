using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.Newtonsoft.Json;

public class PrefabLoaderAssetBundleState : SaveState
{
    [JsonProperty]
    private string _selectedPath;

    [JsonProperty]
    private string[] _assetNames = new string[0];

    [JsonProperty]
    private string _currentAssetPath;

    [JsonProperty]
    private PrefabLoaderSpawnMode _currentSpawnMode;

    [JsonProperty]
    public List<FavoritedAsset> Favorites = new List<FavoritedAsset>();

    [JsonProperty]
    private bool _ripMeshes;

    [JsonIgnore]
    public string SelectedPath
    {
        get { return _selectedPath; }
    }

    [JsonIgnore]
    public string[] AssetNames
    {
        get { return _assetNames; }
    }

    [JsonIgnore]
    public string CurrentAssetPath
    {
        get { return _currentAssetPath; }
        set
        {
            if (_currentAssetPath != value)
            {
                _currentAssetPath = value;
                Save();
            }
        }
    }

    [JsonIgnore]
    public PrefabLoaderSpawnMode CurrentSpawnMode
    {
        get { return _currentSpawnMode; }
        set
        {
            if (_currentSpawnMode != value)
            {
                _currentSpawnMode = value;
                Save();
            }
        }
    }

    [JsonIgnore]
    public bool RipMeshes
    {
        get { return _ripMeshes; }
        set
        {
            if (_ripMeshes != value)
            {
                _ripMeshes = value;
                Save();
            }
        }
    }

    public PrefabLoaderAssetBundleState() { }
    public PrefabLoaderAssetBundleState(string fileName) : base(fileName) { }


    public void SetSelectedAssetBundle(string bundlePath, string[] assetNames)
    {
        _selectedPath = bundlePath;
        _assetNames = assetNames.OrderByDescending(o => o.Count(sub => sub == '/')).ToArray();
        _currentAssetPath = "";
        Save();
    }

    public void FavoritePrefab(string prefabName, string bundlePath)
    {
        Favorites.Add(new FavoritedAsset() { AssetBundlePath = bundlePath, AssetName = prefabName });
        Save();
    }

    public void UnfavoritePrefab(string prefabName, string bundlePath)
    {
        Favorites.RemoveAll(o => o.AssetBundlePath == bundlePath && o.AssetName == prefabName);
        Save();
    }

    public bool IsFavoritede(string prefabName, string bundlePath)
    {
        return Favorites.Any(o => o.AssetName == prefabName && o.AssetBundlePath == bundlePath);
    }
}

public enum PrefabLoaderSpawnMode
{
    Temporary,
    DeepCopy
}


