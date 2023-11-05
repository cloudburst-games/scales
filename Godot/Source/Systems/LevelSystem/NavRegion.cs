using Godot;
using System;
using System.Collections.Generic;

public partial class NavRegion : NavigationRegion2D
{
    private NavigationPolygon _newNavPolygon;

	public override void _Ready()
	{
        _newNavPolygon = NavigationPolygon;
	}

    public void UpdateCollisionShapes(int levelID, List<CollisionPolygon2D> shapes)
    {
        // to avoid E 0:00:04.006   make_polygons_from_outlines: NavigationPolygon: Convex partition failed! on reloading scene
        // string navPolyPath = "user://RuntimeData/NavPoly" + levelID.ToString() + ".tres";
        // if (ResourceLoader.Exists(navPolyPath))
        // {
        //     _newNavPolygon = ResourceLoader.Load<NavigationPolygon>(navPolyPath);
        //     return;
        // }
        // else
        // {
            foreach (CollisionPolygon2D shape in shapes)
            {
                Transform2D collisionPolygonTransform = shape.GetGlobalTransform();
                
                Vector2[] collisionPolygon = shape.Polygon;

                Vector2[] newCollisionOutline = collisionPolygonTransform * collisionPolygon;
                _newNavPolygon.AddOutline(newCollisionOutline);
            }
            _newNavPolygon.MakePolygonsFromOutlines();
            NavigationPolygon = _newNavPolygon;

            // System.IO.Directory.CreateDirectory(ProjectSettings.GlobalizePath("user://RuntimeData"));
            // ResourceSaver.Save(_newNavPolygon, navPolyPath);

            // Enabled = false;
            // Enabled = true;
        // }
    }
}
// private Godot.Collections.Array<Vector2> ToGodotArray(Vector2[] input)
//     {
//         Godot.Collections.Array<Vector2> result = new();
//         foreach (Vector2 vec in input)
//         {
//             result.Add(vec);
//         }
//         return result;
//     }

//     private Vector2[] ToArray(Godot.Collections.Array<Vector2> input)
//     {
//         Vector2[] result = new Vector2[input.Count];
//         for (int i = 0; i < input.Count; i++)
//         {
//             result[i] = input[i];
//         }
//         return result;
//     }