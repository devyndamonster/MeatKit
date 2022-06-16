using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;


public class PrefabLoaderState
{
    [SerializeField]
    private PrefabLoaderFileType _prefabLoaderFileMode;

    [SerializeField]
    private string _bundleSelectedPath;

    [SerializeField]
    private string[] _bundleAssetNames = new string[0];

    [SerializeField]
    private string _bundleCurrentAssetPath;

    [SerializeField]
    private PrefabLoaderSpawnMode _bundleCurrentSpawnMode;

    [SerializeField]
    public List<FavoritedAsset> BundleFavorites = new List<FavoritedAsset>();

    [SerializeField]
    private bool _bundleRipMeshes;

    [SerializeField]
    private string _gameSelectedPath;

    public string BundleSelectedPath
    {
        get { return _bundleSelectedPath; }
    }

    public string GameSelectedPath
    {
        get { return _gameSelectedPath; }
        set
        {
            if (_gameSelectedPath != value)
            {
                _gameSelectedPath = value;
                WriteStateToCache(this);
            }
        }
    }

    public string[] BundleAssetNames
    {
        get { return _bundleAssetNames; }
    }

    public string BundleCurrentAssetPath
    {
        get { return _bundleCurrentAssetPath; }
        set
        {
            if (_bundleCurrentAssetPath != value)
            {
                _bundleCurrentAssetPath = value;
                WriteStateToCache(this);
            }
        }
    }

    public PrefabLoaderSpawnMode BundleCurrentSpawnMode
    {
        get { return _bundleCurrentSpawnMode; }
        set
        {
            if (_bundleCurrentSpawnMode != value)
            {
                _bundleCurrentSpawnMode = value;
                WriteStateToCache(this);
            }
        }
    }

    public PrefabLoaderFileType PrefabLoaderFileMode
    {
        get { return _prefabLoaderFileMode; }
        set
        {
            if (_prefabLoaderFileMode != value)
            {
                _prefabLoaderFileMode = value;
                WriteStateToCache(this);
            }
        }
    }

    public bool BundleRipMeshes
    {
        get { return _bundleRipMeshes; }
        set
        {
            if (_bundleRipMeshes != value)
            {
                _bundleRipMeshes = value;
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
        _bundleSelectedPath = bundlePath;
        _bundleAssetNames = assetNames.OrderByDescending(o => o.Count(sub => sub == '/')).ToArray();
        _bundleCurrentAssetPath = "";
        WriteStateToCache(this);
    }

    public void FavoriteBundlePrefab(string prefabName, string bundlePath)
    {
        BundleFavorites.Add(new FavoritedAsset() { AssetBundlePath = bundlePath, AssetName = prefabName });
        WriteStateToCache(this);
    }

    public void UnfavoriteBundlePrefab(string prefabName, string bundlePath)
    {
        BundleFavorites.RemoveAll(o => o.AssetBundlePath == bundlePath && o.AssetName == prefabName);
        WriteStateToCache(this);
    }

    public bool IsFavoritedInBundle(string prefabName, string bundlePath)
    {
        return PrefabLoaderState.Instance.BundleFavorites.Any(o => o.AssetName == prefabName && o.AssetBundlePath == bundlePath);
    }
}

public enum PrefabLoaderSpawnMode
{
    Temporary,
    DeepCopy
}

public enum PrefabLoaderFileType
{
    AssetBundle,
    GameRip
}
