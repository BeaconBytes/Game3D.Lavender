using Godot;

namespace Lavender.Common;

public abstract partial class LoadableNode : Node
{
	protected virtual void Initialize() { }
	protected virtual void Preload() { }
	protected virtual void Load() { }
	protected virtual void Unload() { }

	public override void _EnterTree()
	{
		base._EnterTree();
		Initialize();
	}

	public override void _Ready()
	{
		base._Ready();
		Preload();
		Load();
	}

	public override void _ExitTree()
	{
		Unload();
		base._ExitTree();
	}

	public override void _Notification(int what)
	{
		if (what == NotificationWMCloseRequest)
		{
			Unload();
		}

		base._Notification(what);
	}
}
