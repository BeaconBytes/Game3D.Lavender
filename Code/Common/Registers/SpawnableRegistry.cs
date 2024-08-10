using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Types;

namespace Lavender.Common.Registers;

public class SpawnableRegistry
{
    public void LoadDefaults()
    {
        Register(EntityType.Player, "res://Scenes/Core/Spawnables/player_spawnable.tscn");
    }
    
    public void Register( EntityType entityType, string resPath )
    {
        if ( entityType == EntityType.Unknown )
            throw new Exception( "Unable to register entity with type Unknown!" );

        if (!Registers.Register.Scenes.HasEntry(resPath))
        {
            Registers.Register.Scenes.Register(resPath);
        }
        
        _resEntries.Add( entityType, resPath );
    }
    public string GetResPath( EntityType entityType )
    {
        if ( _resEntries.TryGetValue( entityType, out string result ) )
        {
            return result;
        }

        throw new Exception( $"Invalid EntityType.{entityType.ToString()} given." );
    }
    

    private readonly Dictionary<EntityType, string> _resEntries = new( );
}