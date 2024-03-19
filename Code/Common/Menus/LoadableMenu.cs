using System;
using Lavender.Common.Exceptions;
using Lavender.Common.Managers;

namespace Lavender.Common.Menus;

public partial class LoadableMenu : LoadableNode
{
    protected override void Load()
    {
        EnvManager = GetTree().CurrentScene.GetNode<EnvManager>("EnvManager");
        if (EnvManager == null)
            throw new BadNodeSetupException("EnvManager not found!");
    }

    protected EnvManager EnvManager { get; private set; }
}