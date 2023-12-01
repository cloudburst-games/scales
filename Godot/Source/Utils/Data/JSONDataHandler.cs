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
        string finalPath = user ? OS.GetUserDataDir() + path : "res://" + path;
        FileAccess file = FileAccess.Open(finalPath, FileAccess.ModeFlags.Read);
        GD.Print("\n\n" + finalPath);
        GD.Print(file);
        GD.Print(file == null ? "Invalid file access" : "valid file access\n\n");
        // res://DirectionTest.png
        // string absolutePath = user ? OS.GetUserDataDir() + path : path;
        if (file == null)
        {
            // {
            GD.Print("JSONDataHandler.cs: ERROR, file at " + finalPath + " may not exist.");
            //     throw new Exception();
        }

        // string loaded = System.IO.File.ReadAllText(absolutePath);
        string loaded = "";
        if (file != null)
        {
            loaded = file.GetAsText();
        }

        // GD.Print(loaded);

        T deserialized = JsonConvert.DeserializeObject<T>(loaded);
        if (!(deserialized is IJSONSaveable))
        {
            GD.Print("JSONDataHandler.cs: WARNING, accessing object without interface + ", nameof(IJSONSaveable));
        }
        return deserialized;
    }

}