using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameRipFileIndex {

	[SerializeField]
	public Dictionary<string, string> GUIDToFilePath = new Dictionary<string, string> ();

}
