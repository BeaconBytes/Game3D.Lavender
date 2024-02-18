using Lavender.Common.Globals;

namespace Lavender.Common.Menus;

public partial class LoadableMenu : LoadableNode
{
    protected override void Load()
    {
        Overseer = GetNode<Overseer>("/root/Overseer");
    }

    protected Overseer Overseer { get; private set; }
}