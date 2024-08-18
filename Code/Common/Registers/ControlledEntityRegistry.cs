using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Types;

namespace Lavender.Common.Registers;

public class ControlledEntityRegistry
{
    public void LoadDefaults()
    {
        Register(EntityType.Player, ControllerType.Player);
        Register(EntityType.PlayerSoul, ControllerType.PlayerSoul);
    }
    
    public void Register( EntityType entityType, ControllerType controllerType )
    {
        if (entityType == EntityType.Unknown || controllerType == ControllerType.Unknown)
            throw new Exception( "Unable to register entity-controller bundle of Unknown type!" );

        _entries.Add(entityType, controllerType);
    }
    public ControllerType GetControllerFor( EntityType entityType )
    {
        if ( _entries.TryGetValue( entityType, out ControllerType result ) )
        {
            return result;
        }

        throw new Exception( $"Unknown entry EntityType.{entityType.ToString()} given." );
    }

    private readonly Dictionary<EntityType, ControllerType> _entries = new( );
}