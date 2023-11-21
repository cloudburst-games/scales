// SettingsGraphics: a component of the SettingsManager

// TODO:
// Go through each of the display / rendering settings and make them customisable if they affect performance or play.
// When all settings are in, categorise them into subgroups: general, 2D, 3D (so irrelevant settings can easily be hidden)
// Then add tooltip hints for each setting

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
// using System.IO;
// using System.Drawing.Imaging;

public partial class SettingsGraphics : Control
{

    public delegate void SettingsChangedDelegate();
    public event SettingsChangedDelegate SettingsChanged;
    private OptionButton _screenOptions;
    private OptionButton _windowModeOptions;
    private OptionButton _windowSizeOptions;
    private CheckButton _confineMouseBtn;
    private HSlider _screenShakeSlider;
    private OptionButton _VSyncOptions;
    private Callable _screenOptionsCallable;
    private Callable _windowModeOptionsCallable;
    private Callable _windowSizeOptionsCallable;
    private Callable _confineMouseBtnCallable;
    private Callable _screenShakeSliderCallable;
    private Callable _VSyncOptionsCallable;
    private List<Vector2I> _windowSizes = new() {
        new Vector2I(1024,768),
        new Vector2I(1280,800),
        new Vector2I(1280,720),
        new Vector2I(1280,1024),
        new Vector2I(1360,768),
        new Vector2I(1366,768),
        new Vector2I(1440,900),
        new Vector2I(1600,900),
        new Vector2I(1680,1050),
        new Vector2I(1920,1080),
        new Vector2I(1920,1200),
        new Vector2I(2560,1080),
        new Vector2I(2560,1600),
        new Vector2I(2560,1440),
        new Vector2I(3440,1440),
        new Vector2I(3840,2160),
    };
    private Vector2I _previousWindowSize;
    private int _previousIndex = 0;

    private bool _refreshing = false;

    public override void _Ready()
    {
        _screenOptions = GetNode<OptionButton>("Panel/ScrollContainer/VBoxContainer/HScreen/OptionButton");
        _windowModeOptions = GetNode<OptionButton>("Panel/ScrollContainer/VBoxContainer/HDisplayMode/OptionButton");
        _windowSizeOptions = GetNode<OptionButton>("Panel/ScrollContainer/VBoxContainer/HWindowSize/OptionButton");
        _confineMouseBtn = GetNode<CheckButton>("Panel/ScrollContainer/VBoxContainer/HLockMouse/Panel/CheckButton");
        _screenShakeSlider = GetNode<HSlider>("Panel/ScrollContainer/VBoxContainer/HScreenShake/VBoxContainer/HSlider");
        _VSyncOptions = GetNode<OptionButton>("Panel/ScrollContainer/VBoxContainer/HVSync/OptionButton");
        _windowModeOptionsCallable = new Callable(this, nameof(OnWindowModeOptionSelected));
        _windowSizeOptionsCallable = new Callable(this, nameof(OnWindowSizeOptionSelected));
        _screenOptionsCallable = new Callable(this, nameof(OnScreenOptionSelected));
        _confineMouseBtnCallable = new Callable(this, nameof(OnConfineMouseBtnToggled));
        _screenShakeSliderCallable = new Callable(this, nameof(OnScreenShakeSliderValueChanged));
        _VSyncOptionsCallable = new Callable(this, nameof(OnVSyncOptionSelected));

        InitScreenOptions();
        InitWindowSizeOptions();

        Refresh();
    }

    private void InitScreenOptions()
    {
        _screenOptions.Clear();
        for (int i = 0; i < DisplayServer.GetScreenCount(); i++)
        {
            _screenOptions.AddItem(String.Format("Screen {0}", i));
        }
    }

    private void InitWindowSizeOptions()
    {
        _windowSizeOptions.Clear();
        for (int i = 0; i < _windowSizes.Count; i++)
        {
            _windowSizeOptions.AddItem(_windowSizes[i].ToString());
        }
    }
    private void OnWindowSizeOptionSelected(int index)
    {
        _previousWindowSize = GetViewport().GetWindow().Size;
        if (_windowSizes.Contains(GetViewport().GetWindow().Size))
        {
            _previousIndex = _windowSizes.IndexOf(GetViewport().GetWindow().Size);
        }
        GetViewport().GetWindow().Size = _windowSizes[index];
        GetNode<BasePanel>("PnlWindowSizeConfirm").Open();
        GetNode<Timer>("PnlWindowSizeConfirm/Timer").Start();
    }

    private async void OnScreenOptionSelected(int index)
    {
        // change to windowed first, otherwise it bugs out
        var currentMode = DisplayServer.WindowGetMode();
        await ToSignal(GetTree(), "process_frame");
        DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        await ToSignal(GetTree(), "process_frame");
        // set to specified screen
        DisplayServer.WindowSetCurrentScreen(DisplayServer.GetScreenCount() > index ? index : 0);
        await ToSignal(GetTree(), "process_frame");
        AnnounceSettingsChanged();
        // change back to original windowmode
        DisplayServer.WindowSetMode(currentMode);
    }
    private async void OnWindowModeOptionSelected(int index)
    {
        // change back to original screen otherwise it sometimes bugs out (as of godot beta 17)
        var currentScreen = DisplayServer.WindowGetCurrentScreen();
        await ToSignal(GetTree(), "process_frame");

        DisplayServer.WindowSetMode(index == 0 ? DisplayServer.WindowMode.Windowed : index == 1
            ? DisplayServer.WindowMode.Maximized : index == 2 ? DisplayServer.WindowMode.Fullscreen
            : DisplayServer.WindowMode.ExclusiveFullscreen);


        await ToSignal(GetTree(), "process_frame");
        DisplayServer.WindowSetCurrentScreen(currentScreen);
        _windowSizeOptions.Disabled = DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed;
        GetViewport().GetWindow().Size = _windowSizes[_windowSizeOptions.Selected];
        AnnounceSettingsChanged();
    }

    private void OnConfineMouseBtnToggled(bool toggled)
    {
        Input.MouseMode = toggled ? Input.MouseModeEnum.Confined : Input.MouseModeEnum.Visible;
        AnnounceSettingsChanged();
    }

    private void OnScreenShakeSliderValueChanged(float value)
    {
        GetTree().Root.GetNode<GlobalSettings>("GlobalSettings").ScreenShakePercentModifier = value / 100f;
        AnnounceSettingsChanged();
    }

    private void OnVSyncOptionSelected(int index)
    {
        DisplayServer.WindowSetVsyncMode((DisplayServer.VSyncMode)index);
        AnnounceSettingsChanged();
    }

    private void AnnounceSettingsChanged()
    {
        if (!_refreshing)
        {
            SettingsChanged?.Invoke();
        }
    }

    private void ConnectControlCallable(Control control, string signal, Callable callable)
    {
        if (!control.IsConnected(signal, callable))
        {
            control.Connect(signal, callable);
        }
    }

    public void Refresh()
    {
        _refreshing = true;

        // Display mode:
        _windowModeOptions.Selected = DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed ? 0 :
            DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Maximized ? 1 :
            DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Fullscreen ? 2 : 3;
        ConnectControlCallable(_windowModeOptions, "item_selected", _windowModeOptionsCallable);
        // Window size:
        if (_windowSizes.Contains(GetViewport().GetWindow().Size))
        {
            _windowSizeOptions.Selected = _windowSizes.IndexOf(GetViewport().GetWindow().Size);
        }
        ConnectControlCallable(_windowSizeOptions, "item_selected", _windowSizeOptionsCallable);
        _windowSizeOptions.Disabled = DisplayServer.WindowGetMode() != DisplayServer.WindowMode.Windowed;
        // Screen options:
        _screenOptions.Selected = DisplayServer.WindowGetCurrentScreen();
        ConnectControlCallable(_screenOptions, "item_selected", _screenOptionsCallable);
        // Confine Mouse Checkbutton
        _confineMouseBtn.SetPressedNoSignal(Input.MouseMode == Input.MouseModeEnum.Confined);
        ConnectControlCallable(_confineMouseBtn, "toggled", _confineMouseBtnCallable);
        // Screen shake slider
        _screenShakeSlider.SetValueNoSignal(GetTree().Root.GetNode<GlobalSettings>("GlobalSettings").ScreenShakePercentModifier * 100);
        ConnectControlCallable(_screenShakeSlider, "value_changed", _screenShakeSliderCallable);
        // VSync
        _VSyncOptions.Selected = (int)DisplayServer.WindowGetVsyncMode();
        ConnectControlCallable(_VSyncOptions, "item_selected", _VSyncOptionsCallable);

        _refreshing = false;
    }

    private void OnWSizeBtnCancelPressed()
    {
        GetViewport().GetWindow().Size = _previousWindowSize;
        _windowSizeOptions.Selected = _previousIndex;
        GetNode<BasePanel>("PnlWindowSizeConfirm").Close();
    }

    private void OnWSizeBtnConfirmPressed()
    {
        AnnounceSettingsChanged();
        GetNode<BasePanel>("PnlWindowSizeConfirm").Close();
        GetNode<Timer>("PnlWindowSizeConfirm/Timer").Stop();
    }

    public void Exit()
    {
        SettingsChanged = null;
    }
}

/*

// THIS MAY NOT BE NEEDED, but proof of concept for variable texture settings in Godot 4.0:

HOW:
1. Create a script that allows to select a folder, and in all subdirs renames all png if not already done with _HI.png suffix, and generates _MED at half res (/2), and _LO and quart res (/4) 

2. Create a setting stored in global settings for low med or high texture quality 

3. Be able to run e.g. at end of ready in worldscn to set all the textures. If the first sprite or texturerect or texturebutton is already set to the specified suffix, can abort. Perhaps save the scene at the end of this process (first time) as long as not saving scenedata, to save time in future. At high find the HI png if exists and set it at x1 scale, med do med png at .5 scale, low do low png at .25 scale 

TESTS:
string projectDir = ProjectSettings.GlobalizePath("res://");
List<string> allFiles = FindAllFiles(new List<string>(), projectDir);
// test png creation
// CreateScaledPng("res://addons/AudioContainerPlugin/AudioContainerIcon.png", 0.5);
// CreateScaledPng("res://addons/AudioContainerPlugin/AudioContainerIcon.png", 0.25);

// test setting sprite texture
SetSpriteTextureQuality(GetNode<Sprite2D>("AudioContainerIcon"), TextureQuality.High);
SetSpriteTextureQuality(GetNode<Sprite2D>("AudioContainerIcon2"), TextureQuality.Med);
SetSpriteTextureQuality(GetNode<Sprite2D>("AudioContainerIcon3"), TextureQuality.Low);

// FindAllFiles(projectDir);


CONSTITUENT METHODS:
// FIRST loop through all files (BEFORE EXPORT) and create medium and low quality versions

private List<string> FindAllFiles(List<string> res, string targetDir)
{
    foreach (string dir in Directory.GetDirectories(targetDir))
    {
        foreach (string filename in Directory.GetFiles(dir))
        {
            if (filename.Substring(Math.Max(0,filename.Length-4)).ToLower() == ".png")
            {
                // GD.Print(filename);
                res.Add(filename);
            }
        }
        FindAllFiles(res, dir);
    }
    return res;
}

private void CreateAllScaledPngs()
{
    List<string> allFiles = FindAllFiles(new List<string>(), ProjectSettings.GlobalizePath("res://"));
    foreach (string filename in allFiles)
    {
        CreateScaledPng(filename, 0.5);
        CreateScaledPng(filename, 0.25);
    }
}


private void CreateScaledPng(string filename, double scale)
{
    var image = new Image();
    image.Load(filename);
    int newWidth = Convert.ToInt32(image.GetWidth() * scale);
    int newHeight = Convert.ToInt32(image.GetHeight() * scale);
    image.Resize(newWidth, newHeight);
    int dotLocation = filename.LastIndexOf('.');
    string newFilenamePrefix = filename.Substring(0, dotLocation);
    string newFilenameSuffix = scale == 0.25 ? "_LO.png" : "_MED.png";
    if (!ResourceLoader.Exists(newFilenamePrefix + newFilenameSuffix))
    {
        image.SavePng(newFilenamePrefix + newFilenameSuffix);
    }
}

// THEN IN THE GAME, WHENEVER ANYTHING IS ADDED TO THE SCENE TREE, NEED TO RUN THIS

private void SetAllSpriteTextureQuality(TextureQuality quality, Node n)
{
    foreach (Sprite2D sprite in BaseProject.Utils.Node.GetNodesRecursive(new List<Sprite2D>(), n))
    {
        if (SpriteIsOfTargetQuality(sprite, quality))
        {
            return;
        }
        SetSpriteTextureQuality(sprite, quality);
    }
}

private enum TextureQuality { High, Med, Low}

private bool SpriteIsOfTargetQuality(Sprite2D sprite, TextureQuality quality)
{
    //TODO
    // ALSO TODO CLEANUP OF MED AND LO
    return false;
}

private void SetSpriteTextureQuality(Sprite2D s, TextureQuality quality)
{
    string texturePath = s.Texture.ResourcePath;
    int dotLocation = texturePath.LastIndexOf('.');
    string currentSuffix = texturePath.Substring(Math.Max(0, texturePath.Length-5));
    TextureQuality oldQuality = currentSuffix == "D.png" ? TextureQuality.Med : 
        currentSuffix == "O.png" ? TextureQuality.Low : TextureQuality.High;
    string basePath = currentSuffix == "D.png" ? texturePath.Substring(0, dotLocation-4) : 
        currentSuffix == "O.png" ? texturePath.Substring(0, dotLocation-3) : texturePath.Substring(0, dotLocation);
    string suffix = quality == TextureQuality.High ? ".png" : quality == TextureQuality.Med ? "_MED.png" : "_LO.png";
    float scaleMultiplier = textureScaleConversion[new Tuple<TextureQuality, TextureQuality>(oldQuality, quality)];
    // GD.Print(s.Texture.ResourcePath);
    // GD.Print(basePath);
    // GD.Print(basePath + suffix);
    string targetTexturePath = basePath + suffix;
    if (ResourceLoader.Exists(targetTexturePath))
    {
        s.Texture = GD.Load<Texture2D>(targetTexturePath);
        s.Scale *= scaleMultiplier;
    }
}

private System.Collections.Generic.Dictionary<Tuple<TextureQuality, TextureQuality>, float> textureScaleConversion = new() {
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.High, TextureQuality.High), 1},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.High, TextureQuality.Med), 2},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.High, TextureQuality.Low), 4},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.Med, TextureQuality.High), 0.5f},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.Med, TextureQuality.Med), 1},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.Med, TextureQuality.Low), 2},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.Low, TextureQuality.High), 0.25f},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.Low, TextureQuality.Med), 0.5f},
    {new Tuple<TextureQuality, TextureQuality>(TextureQuality.Low, TextureQuality.Low), 1},
};

*/
