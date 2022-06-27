using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

public class SaveState {

    protected string _fileName;

    public SaveState() { }
    public SaveState(string fileName) 
    { 
        _fileName = fileName;
        WriteStateToFile(this, fileName);
    }

    public void SetFileName(string fileName)
    {
        _fileName = fileName;
    }

    protected void Save()
    {
        WriteStateToFile(this, _fileName);
    }

    public static void WriteStateToFile<T>(T state, string fileName) where T : SaveState
    {
        File.WriteAllText(GetStateFilePath(fileName), JsonConvert.SerializeObject(state));
    }

    public static T LoadStateFromFile<T>(string fileName) where T : SaveState
    {
        string json = File.ReadAllText(GetStateFilePath(fileName));
        T state = JsonConvert.DeserializeObject<T>(json);
        state.SetFileName(fileName);
        return state;
    }

    public static string GetStateFilePath(string fileName)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), fileName);
    }


}
