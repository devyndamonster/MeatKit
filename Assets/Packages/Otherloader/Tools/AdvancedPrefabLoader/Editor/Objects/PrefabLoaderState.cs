using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Valve.Newtonsoft.Json;

public class PrefabLoaderState : SaveState {

    [JsonProperty]
    private PrefabLoaderFileType _prefabLoaderFileMode;

    [JsonIgnore]
    public PrefabLoaderFileType PrefabLoaderFileMode
    {
        get { return _prefabLoaderFileMode; }
        set
        {
            if (_prefabLoaderFileMode != value)
            {
                _prefabLoaderFileMode = value;
                Save();
            }
        }
    }

    public PrefabLoaderState() { }
    public PrefabLoaderState(string fileName) : base(fileName) { }
}

public enum PrefabLoaderFileType
{
    AssetBundle,
    GameRip
}
