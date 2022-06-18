using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Valve.Newtonsoft.Json;

public static class SaveStateManager {

    public static void WriteStateToFile<T>(T state, string fileName) where T : SaveState
    {
        File.WriteAllText(GetStateFilePath(fileName), JsonConvert.SerializeObject(state));
    }

    public static T LoadStateFromFile<T>(string fileName) where T : SaveState
    {
        T state = JsonConvert.DeserializeObject<T>(GetStateFilePath(fileName));
        state.SetFileName(fileName);
        return state;
    }

    private static string GetStateFilePath(string fileName)
    {
        return Path.Combine(Path.GetDirectoryName(Application.dataPath), fileName);
    }
}
