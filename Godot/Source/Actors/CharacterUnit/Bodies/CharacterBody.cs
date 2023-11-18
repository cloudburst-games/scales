// CharacterBody. Straightforward script that allows to make multiple character sprites with collisions and anims,
// and swap them easily for characters.
using Godot;
using System;

public partial class CharacterBody : Node
{
    public void SetBody(CharacterUnit characterUnit)
    {
        Sprite2D oldSprite = characterUnit.GetNode<Sprite2D>("Sprite");
        CollisionShape2D oldShape = characterUnit.GetNode<CollisionShape2D>("Shape");
        AnimationPlayer oldAnim = characterUnit.GetNode<AnimationPlayer>("Anim");
        AnimationTree oldAnimTree = characterUnit.GetNode<AnimationTree>("AnimTree");
        Control oldClickArea = characterUnit.GetNode<Control>("ClickArea");
        // NavigationObstacle2D oldNavObstacle = characterUnit.GetNode<NavigationObstacle2D>("NavObstacle");

        oldSprite.Name = "SpriteOld";
        oldShape.Name = "ShapeOld";
        oldAnim.Name = "AnimOld";
        oldAnimTree.Name = "AnimTreeOld";
        oldClickArea.Name = "ClickAreaOld";
        // oldNavObstacle.Name = "NavObstacleOld";

        Sprite2D newSprite = GetNode<Sprite2D>("Sprite");
        CollisionShape2D newShape = GetNode<CollisionShape2D>("Shape");
        AnimationPlayer newAnim = GetNode<AnimationPlayer>("Anim");
        AnimationTree newAnimTree = GetNode<AnimationTree>("AnimTree");
        Control newClickArea = GetNode<Control>("ClickArea");
        // NavigationObstacle2D newNavObstacle = GetNode<NavigationObstacle2D>("NavObstacle");

        RemoveChild(newSprite);
        RemoveChild(newShape);
        RemoveChild(newAnim);
        RemoveChild(newAnimTree);
        RemoveChild(newClickArea);
        // RemoveChild(newNavObstacle);

        characterUnit.AddChild(newSprite);
        newSprite.Owner = characterUnit;
        characterUnit.AddChild(newShape);
        newShape.Owner = characterUnit;
        characterUnit.AddChild(newAnim);
        newAnim.Owner = characterUnit;
        characterUnit.AddChild(newAnimTree);
        newAnimTree.Owner = characterUnit;
        characterUnit.AddChild(newClickArea);
        newClickArea.Owner = characterUnit;
        // characterUnit.AddChild(newNavObstacle);

        newClickArea.GuiInput += characterUnit.OnClickAreaGUIInput;

        oldSprite.QueueFree();
        oldShape.QueueFree();
        oldAnim.QueueFree();
        oldAnimTree.QueueFree();
        oldClickArea.QueueFree();
        // oldNavObstacle.QueueFree();

        // GD.Print(Name);
        // GD.Print(GetParent());
        // // await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        // Free();
    }
}
