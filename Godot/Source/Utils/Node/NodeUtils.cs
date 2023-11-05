// NodeUtils: utility methods that expand functionality of Node
using Godot;
using System.Collections.Generic;

namespace BaseProject.Utils {
    public partial class Node
    {
        // Hide children - nodes that can hide are canvasitem or spatial nodes
        public static void SetVisibleRecursive(Godot.Node parent, bool show)
        {
            // There is a bug with LineEdit - the context menu appears. This bypasses the bug.
            // However, if we need to hide or show LineEdits we should make their parents Spatials or CanvasItems
            if (parent is LineEdit) 
                return;
            if (parent is CanvasItem p)
                p.Visible = show;

            foreach (Godot.Node n in parent.GetChildren()){
                if (n.GetChildCount() > 0)
                    SetVisibleRecursive(n, show);

                if (n is CanvasItem c)
                {
                    c.Visible = show;
                }
                else if (n is Node3D s)
                {
                    s.Visible = show;;
                }
            }

        }

        public static List<T> GetNodesRecursive<T>(List<T> res, Godot.Node parent)
        {
            foreach (Godot.Node n in parent.GetChildren())
            {
                if (n is T targetNode)
                {
                    res.Add(targetNode);
                }
                GetNodesRecursive<T>(res, n);
            }
            return res;
        }

        // Modulate children (canvasitems only)
        public static void SetModulateRecursive(Godot.Node parent, Color color)
        {
            // There is a bug with LineEdit - the context menu appears. This bypasses the bug.
            // However, if we need to hide or show LineEdits we should make their parents Spatials or CanvasItems
            if (parent is LineEdit) 
                return;
            if (parent is CanvasItem p)
                p.Modulate = color;

            foreach (Godot.Node n in parent.GetChildren()){
                if (n.GetChildCount() > 0)
                    SetModulateRecursive(n, color);

                if (n is CanvasItem c)
                {
                    c.Modulate = color;
                }
            }

        }

        // Set MouseFilter (controls only)
        public static void SetMouseFilterRecursive(Godot.Node parent, Control.MouseFilterEnum filter)
        {
            foreach (Godot.Node n in parent.GetChildren())
            {
                if (n.GetChildCount() > 0)
                {
                    SetMouseFilterRecursive(n, filter);
                }
                if (n is Control control)
                {
                    control.MouseFilter = filter;
                }
            }
        }
    }
}
