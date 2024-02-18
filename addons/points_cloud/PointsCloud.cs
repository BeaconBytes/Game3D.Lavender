#if TOOLS
using Godot;
using Godot.Collections;
using Lavender.Common.Data.Saving.Mapping;
using Array = System.Array;

[Tool]
public partial class PointsCloud : EditorPlugin
{
	public override void _EnterTree()
	{
		// Initialization of the plugin goes here.
		Script script = GD.Load<Script>("res://addons/points_cloud/code/nodetype/CloudNavGenerator.cs");
		Texture2D tex = GD.Load<Texture2D>("res://addons/points_cloud/ui/assets/generate_points_button.png");
		AddCustomType("CloudNavGenerator", "Node3D", script, tex);
	}


	public override void _ExitTree()
	{
		// // Clean-up of the plugin goes here.
		RemoveCustomType("CloudNavGenerator");
	}
}
#endif
