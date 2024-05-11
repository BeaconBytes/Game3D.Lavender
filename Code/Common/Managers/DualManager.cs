using System;
using Lavender.Client.Managers;
using Lavender.Server.Managers;

namespace Lavender.Common.Managers;

public partial class DualManager : LoadableNode
{
    protected override void Load()
    {
        base.Load();

        ClientManager = GetNode<ClientManager>("../ClientEnv/Manager");
        ServerManager = GetNode<ServerManager>("../ServerEnv/Manager");

        if (ClientManager == null || ServerManager == null)
            throw new Exception("ClientManager or ServerManager not found!");
    }
    
    public ClientManager ClientManager { get; protected set; }
    public ServerManager ServerManager { get; protected set; }
    
}