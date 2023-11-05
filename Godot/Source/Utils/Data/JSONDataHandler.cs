// Coordinates JSON file operations

using Godot;
using System;
using Newtonsoft.Json;


public class JSONDataHandler
{
    public void SaveToDisk(IJSONSaveable dataContainer, string path, bool user = true) // e.g. path might be /Config/Settings.json
    {
        string absolutePath = user ? OS.GetUserDataDir() + path : path;
		string jsonStr = JsonConvert.SerializeObject(dataContainer);
		string dir = new System.IO.FileInfo(absolutePath).Directory.FullName;
		System.IO.Directory.CreateDirectory(dir);
		System.IO.File.WriteAllText(absolutePath, jsonStr);

    }

	public T LoadFromJSON<T>(string path, bool user = true) // e.g. path might be /Config/Settings.json
	{
        string absolutePath = user ? OS.GetUserDataDir() + path : path;
		if (! System.IO.File.Exists(absolutePath)){
			GD.Print("JSONDataHandler.cs: ERROR, file at " + absolutePath + " does not exist.");
			throw new Exception();
		}

		string loaded = System.IO.File.ReadAllText(absolutePath);
		T deserialized = JsonConvert.DeserializeObject<T>(loaded);
		if (! (deserialized is IJSONSaveable))
        {
			GD.Print("JSONDataHandler.cs: WARNING, accessing object without interface + ", nameof(IJSONSaveable));
        }
		return deserialized;
	}

}