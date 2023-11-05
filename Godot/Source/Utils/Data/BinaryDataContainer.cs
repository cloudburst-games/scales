// BINARY SERIALIZATION IS DEPRECATED AND NO LONGER WORKS IN GODOT 4

// DataBinary: serializable data container class for data to be saved in a binary format. This is required for the Serializable attribute.
using System;
using System.Collections.Generic;

[Serializable()]
public class BinaryDataContainer
{

	public Dictionary<string,object> DataDict {get; private set;}

	public void SaveBinary(Dictionary<string,object> dataDict, string filename)
	{
		this.DataDict = dataDict;
		BinaryDataHandler.SaveToFile(filename, this);
	}

}

