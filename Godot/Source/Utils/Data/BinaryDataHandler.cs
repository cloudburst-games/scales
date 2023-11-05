// BINARY SERIALIZATION IS DEPRECATED AND NO LONGER WORKS IN GODOT 4

// File binary: helper class containing static methods for saving and loading data in a binary format. Used by DataBinary to save 
// serializable data.

using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

public static class BinaryDataHandler
{
	public static void SaveToFile(string filename, BinaryDataContainer dataBinary)
	{
		// Create folder
		string dir = new System.IO.FileInfo(filename).Directory.FullName;
		System.IO.Directory.CreateDirectory(dir);

		// Open a new stream in write/create mode
		Stream stream = System.IO.File.Open (filename, FileMode.Create);

		// Create new binaryformatter
		BinaryFormatter bf = new BinaryFormatter ();


		// Serialize the data
		bf.Serialize (stream, dataBinary);

		// Close the stream
		stream.Close ();
	}

	public static BinaryDataContainer LoadFromFile(string filename)
	{
		if (! System.IO.File.Exists(filename))
			return null;

		// assign the file to our stream
		Stream stream = System.IO.File.Open (filename, FileMode.Open);

		// new bf
		BinaryFormatter bf = new BinaryFormatter ();

		// Deserialise the stream
		BinaryDataContainer dataBinary = (BinaryDataContainer)bf.Deserialize (stream);
		// And close it
		stream.Close ();

		return dataBinary;
	}

	/* USAGE (from outside this class)

	Ideally save user-editable data in JSON e.g. game settings.
	Save data which should not be user-editable in binary e.g. high scores, game save files
	
	*** SaveBinary ***

		// SAVING //
		// We create the dictionary and place our data inside
		System.Collections.Generic.Dictionary<string, object> testDict = new System.Collections.Generic.Dictionary<string, object>()
		{
			{"number", 1},
			{"float", 3.1f}
		};
		// We create the object in which our data will be stored
		DataBinary dataBinary = new DataBinary();
		// We run the SaveBinary method on this object
		dataBinary.SaveBinary(testDict, "testpath");

		// LOADING //
		DataBinary dataBinary = FileBinary.LoadFromFile("testpath");
		GD.Print(dataBinary.dataDict["float"].ToString());
	*/
}


