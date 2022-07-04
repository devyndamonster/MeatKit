using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

public class PrefabLoaderGameRipState : SaveState {

    [JsonProperty]
    public Dictionary<string, string> GUIDToFilePath = new Dictionary<string, string> ();

    [JsonProperty]
    public Dictionary<string, string> FilePathToGUID = new Dictionary<string, string>();

    [JsonProperty]
    public Dictionary<string, List<string>> FolderToSubfiles = new Dictionary<string, List<string>>();

    [JsonProperty]
    public Dictionary<string, List<string>> FolderToSubfolders = new Dictionary<string, List<string>>();

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

    [JsonProperty]
    private string _currentPath;

    [JsonIgnore]
    public string CurrentPath
    {
        get { return _currentPath; }
        set
        {
            if (_currentPath != value)
            {
                _currentPath = value;
                Save();
            }
        }
    }

    public PrefabLoaderGameRipState() { }
    public PrefabLoaderGameRipState(string fileName) : base(fileName) { }
}
