using System.Collections.Generic;

namespace Lavender.Common.Registers;

public static class Register
{
    public static readonly Dictionary<string, string> SceneTable = new( )
    {
        { "env_server", "res://Scenes/Core/Environments/server_environment.tscn" },
        { "env_client", "res://Scenes/Core/Environments/client_environment.tscn" },
        { "env_dual", "res://Scenes/Core/Environments/dual_environment.tscn" },
        
        { "loadup_menu_main", "res://Scenes/Core/MainMenus/loadup_menu_main.tscn" },
        { "loadup_menu_selection", "res://Scenes/Core/MainMenus/loadup_menu_selection.tscn" },
        { "loadup_menu_join", "res://Scenes/Core/MainMenus/loadup_menu_join.tscn" },
        
        { "map_default", "res://Scenes/Core/Maps/Kraken/kraken_map.tscn" },
        { "map_debug", "res://Scenes/Core/Maps/DEBUG/debug_map.tscn" },
    };

    public static ControllerRegistry Controllers { get; } = new();
    public static PacketRegistry Packets { get; } = new();
    public static EntityRegistry Entities { get; } = new();
    public static SceneRegistry Scenes { get; } = new();
    public static ControlledEntityRegistry ControlledEntities { get; } = new();

    public static void LoadDefaults()
    {
        Packets.LoadDefaults();
        Scenes.LoadDefaults();
        Controllers.LoadDefaults();
        Entities.LoadDefaults();
        ControlledEntities.LoadDefaults();
    }
}