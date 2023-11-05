// References
// https://www.redblobgames.com/grids/hexagons/
// https://www.codeproject.com/Articles/14948/Hexagonal-grid-for-games-and-other-projects-Part-1
//
// A hexagon is made up of 6 equilateral triangles
//
// The principle of calculating lengths / angles / positions is similar with flat and pointy top hexes
// We will calculate for pointy top first, then use a similar process for flat top
//
// We know the origin vector, and the length of each side (s)
//
//     origin
//     /\
//    |  | - s
//     \/
//
// We need to calculate the other 5 position vectors to draw the hexagon
// To do this, lets make a right angled triangle made up of the side of a hexagon for the hypotenuse
//
//          adj
//   origin --
//         /\| opp
//
// The known angles for the right angled triangle are 90 (the top right angle), and 30 (between hypotenuse and adjacent)
// The angle is 30 because we know the equilateral triangle forming the hexagon (opposite) is 60, and this is part of an 
// imaginary right angled triangle with the adj line, so 90 - 60 = 30.
//
//        a
//      ______
//     /\30 90|
//    /  \    |
//   /120 \s  | o
//  /      \  |
// /        \ |
//
// Let the adjacent side = "a" and the opposite side = "o", and we know the length of the hypotenuse "s"
// For right angled triangles we can use s and the angle 30 to calculate a and o, so:
// o = s * sin 30
// a = s * cos 30
//
// With the values of "s", "o", and "a" now known, and the origin vector, we can calculate the relative positions of the
// other 5 points of the hexagon (below obviously not to scale)
//
//                a
//             0 ____
//             /\    |
//            /  \   | o
//           /    \  |
//       5  /      \1
//         |        | 
//         |        |
//         |        | s
//         |        |
//       4  \      / 2
//           \    /
//            \  /
//             \/
//             3
//
// 0 = origin vector
// s = side
// 1 = origin + a.X + o.Y
// 2 = origin + a.X + o.Y + s.Y
// 3 = origin + s.Y + 2 * o.Y
// 4 = origin - a.X + o.Y + s.Y
// 5 = origin - a.X + o.Y
//
// For flat top hexagons, the process is the same, except "a" and "o" are essentially reversed, because the 30 degree
// angle is the other non right angle side. This is reflected when we calculate the relative positions of other 5 points
// of the hexagon
//
//                 o
//      ________ _____
//     /        \   90|
//    /          \    |
//   /          s \   | a
//  /              \30|
// /                \ |
//
// Putting it all together for flat-top hexagons (again, not to scale):
//
//                      o
//          0________1____
//          /        \    |
//         /         s\   | a
//        /            \  |
//     5 /              \ 2
//       \              /
//        \            /
//         \          /
//          \________/
//         4          3
//
// 0 = origin vector
// s = side
// 1 = origin + s.X
// 2 = origin + s.X + o.X + a.Y
// 3 = origin + s.X + 2 * a.Y
// 4 = origin + 2 * a.Y
// 5 = origin - o.X + a.Y

using Godot;
using System;
using System.Linq;
using System.Collections.Generic;

public class Hexagon
{

    public float O { get; private set; }
    public float A { get; private set; }
    public float S { get; private set; }

    public bool Obstacle { get; set; } = false;
    public Sprite2D HexSprite { get; set; }

    private Dictionary<int, Vector2> _pointPositions;

    public Hexagon()
    {
        throw new NotImplementedException();
    }


    // consider splitting static methods into a utility class
    public static float CalculateA(float sideLength)
    {
        return (float)Math.Cos(30 * (Math.PI / 180)) * sideLength;
    }
    public static float CalculateO(float sideLength)
    {
        return (float)Math.Sin(30 * (Math.PI / 180)) * sideLength;
    }

    public static float CalculateArea(float sideLength)
    {
        return 3 * (float)Math.Sqrt(3) / 2 * (float)Math.Pow(sideLength, 2);
    }

    // calculates the size of an individual hex
    public static Vector2 CalculateRect(HexGrid.OrientationMode orientation, float sideLength)
    {
        float x = orientation == HexGrid.OrientationMode.FlatTop ? 2 * CalculateO(sideLength) + sideLength :
            2 * CalculateA(sideLength);
        float y = orientation == HexGrid.OrientationMode.FlatTop ? 2 * CalculateA(sideLength) :
            2 * CalculateO(sideLength) + sideLength;
        return new Vector2(x, y);
    }

    public Hexagon(HexGrid.OrientationMode orientation, Vector2 startPosition, float s)
    {
        S = s;
        O = CalculateO(s);
        A = CalculateA(s);
        CalculatePointPositions(orientation, startPosition);

    }

    private void CalculatePointPositions(HexGrid.OrientationMode orientation, Vector2 startPosition)
    {
        if (orientation == HexGrid.OrientationMode.PointyTop)
        {
            _pointPositions = new Dictionary<int, Vector2>()
            {
                {0, startPosition},
                {1, new Vector2(startPosition[0] + A, startPosition[1] + O)},
                {2, new Vector2(startPosition[0] + A, startPosition[1] + O + S)},
                {3, new Vector2(startPosition[0], startPosition[1] + S + (2 * O))},
                {4, new Vector2(startPosition[0] - A, startPosition[1] + O + S)},
                {5, new Vector2(startPosition[0] - A, startPosition[1] + O)},
            };
        }
        else
        {
            _pointPositions = new Dictionary<int, Vector2>()
            {
                {0, startPosition},
                {1, new Vector2(startPosition[0] + S, startPosition[1])},
                {2, new Vector2(startPosition[0] + S + O, startPosition[1] + A)},
                {3, new Vector2(startPosition[0] + S, startPosition[1] + A + A)},
                {4, new Vector2(startPosition[0], startPosition[1] + A + A)},
                {5, new Vector2(startPosition[0] - O, startPosition[1] + A)},
            };
        }
    }

    public List<Vector2> GetPointPositions()
    {
        return _pointPositions.Values.ToList();
    }

}