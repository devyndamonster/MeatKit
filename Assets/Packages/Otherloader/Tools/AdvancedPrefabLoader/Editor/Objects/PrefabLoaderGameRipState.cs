using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

public class PrefabLoaderGameRipState : SaveState {

    [JsonProperty]
    public Dictionary<string, string> GUIDToFilePath = new Dictionary<string, string> ();

    [JsonProperty]
    private string _selectedPath;

    [JsonIgnore]
    public string SelectedPath
    {
        get { return _selectedPath; }
        set
        {
            if (_selectedPath != value)
            {
                _selectedPath = value;
                Save();
            }
        }
    }

    public PrefabLoaderGameRipState() { }
    public PrefabLoaderGameRipState(string fileName) : base(fileName) { }
}
