using Godot;
using Newtonsoft.Json;
using System;

using System.Collections.Generic;
public partial class AnimationJSONMaker : Node
{
    [Export]
    private string _jsonpath;

    // [Export]
    // private string _prefix;

    [Export]
    private Godot.Collections.Array<string> _prefixes = new();

    [Export]
    private string _animName;

    [Export]
    private string _destinationFolder;

    [Export]
    private float _lengthLimit = -0.1f;
    [Export]
    private Texture _texture = null;
    [Export]
    private float _relativeSpeed = 1;

    [Export]
    private Animation.LoopModeEnum _loop = Animation.LoopModeEnum.Linear;
    public class Frame
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }
    }

    public class SpriteInfo
    {
        public Frame frame { get; set; }
        public bool rotated { get; set; }
        public bool trimmed { get; set; }
        public Frame spriteSourceSize { get; set; }
        public Frame sourceSize { get; set; }
    }

    public class RootObject
    {
        public Dictionary<string, SpriteInfo> frames { get; set; }
    }
    public override void _Ready()
    {
        foreach (string prefix in _prefixes)
        {
            JSONDataHandler dataHandler = new();
            string path = ProjectSettings.GlobalizePath(_jsonpath);
            string rawData = System.IO.File.ReadAllText(path);

            RootObject rootObject = JsonConvert.DeserializeObject<RootObject>(rawData);

            Animation anim = new();
            float animLength = 0;
            anim.AddTrack(Animation.TrackType.Value);
            anim.TrackSetPath(0, "Sprite:region_rect");

            if (_texture != null)
            {
                anim.AddTrack(Animation.TrackType.Value);
                anim.TrackSetPath(1, "Sprite:texture");
                anim.TrackInsertKey(1, 0, _texture);
                anim.ValueTrackSetUpdateMode(1, Animation.UpdateMode.Discrete);
            }
            anim.LoopMode = _loop;

            // Loop through each frame (tile data container) in the JSON data
            foreach (KeyValuePair<string, SpriteInfo> frameInfo in rootObject.frames)
            {
                string name = frameInfo.Key.Split('.')[0];

                int prefixLength = prefix.Length;
                if (name.Substring(0, prefixLength) != prefix)
                {
                    continue;
                }

                Vector2 pos = new Vector2(frameInfo.Value.frame.x, frameInfo.Value.frame.y);
                Vector2 size = new Vector2(frameInfo.Value.frame.w, frameInfo.Value.frame.h);

                anim.TrackInsertKey(0, animLength, new Rect2(pos, size));

                animLength += 0.1f * 1 / _relativeSpeed;
                if (_lengthLimit > 0)
                {
                    if (animLength >= _lengthLimit)
                    {
                        break;
                    }
                }

            }

            anim.ValueTrackSetUpdateMode(0, Animation.UpdateMode.Discrete);

            anim.Length = animLength;

            GD.Print(ResourceSaver.Save(anim, _destinationFolder + "/" + prefix + _animName + ".tres"));


            Finish();
        }
    }

    private async void Finish()
    {
        await ToSignal(GetTree().CreateTimer(1), SceneTreeTimer.SignalName.Timeout);

        GetTree().Quit();
    }

}

// public class StoryCharacterJSONInterface
// {

//     public static StoryCharacterData GetStoryCharacterJSONData(StoryCharacter.StoryCharacterMode storyCharacter)
//     {
//         string filename = storyCharacter switch
//         {
//             StoryCharacter.StoryCharacterMode.Enkidu => "Data/Characters/Story/enkidu.json",
//             StoryCharacter.StoryCharacterMode.Eresh => "Data/Characters/Story/eresj.json",
//             StoryCharacter.StoryCharacterMode.Dumuzi => "Data/Characters/Story/dumuzi.json",
//             StoryCharacter.StoryCharacterMode.Gesht => "Data/Characters/Story/gesht.json",
//             StoryCharacter.StoryCharacterMode.Gilgam => "Data/Characters/Story/gilgam.json",
//             StoryCharacter.StoryCharacterMode.Lugal => "Data/Characters/Story/lugal.json",
//             StoryCharacter.StoryCharacterMode.Ningal => "Data/Characters/Story/ningal.json",
//             StoryCharacter.StoryCharacterMode.Utug => "Data/Characters/Story/utug.json",
//             _ => "Data/Characters/Story/enkidu.json",
//         };
//         JSONDataHandler dataHandler = new();
//         StoryCharacterData characterData = dataHandler.LoadFromJSON<StoryCharacterData>(filename, false);
//         return characterData;

//         // _perksGrid.ShowPerks(characterData.Perks); // TODO
//     }

// }