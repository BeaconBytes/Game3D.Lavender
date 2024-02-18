using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Entity;
using Lavender.Common.Entity.Variants;
using Lavender.Common.Enums.Types;

namespace Lavender.Common.Registers.Variants;

public class EntityRegistry
{
    public void LoadDefaults()
    {
        Register<DevEntity>(EntityType.Dev, "res://Scenes/Core/Entities/dev_entity.tscn");
        Register<PlayerEntity>(EntityType.Player, "res://Scenes/Core/Entities/Player/player_entity.tscn");
        Register<LighthouseEntity>(EntityType.Lighthouse, "res://Scenes/Core/Entities/lighthouse_entity.tscn");
    }
    
    public void Register<TEntity>( EntityType entityType, string resPath ) where TEntity : IGameEntity
    {
        if ( entityType == EntityType.Unknown )
            throw new Exception( "Unable to register entity with type Unknown!" );
        if ( _entEntries.ContainsKey( entityType ) )
            throw new Exception( $"Tried to EntityRegister twice with EntityType.{entityType}" );

        if (!Registers.Register.Scenes.HasEntry(resPath))
        {
            Registers.Register.Scenes.Register(resPath);
        }
        
        _resEntries.Add( entityType, resPath );
        _entEntries.Add( entityType, typeof(TEntity) );
    }
    public string GetEntityResPath( EntityType entityType )
    {
        if ( _resEntries.TryGetValue( entityType, out string result ) )
        {
            return result;
        }

        throw new Exception( $"Invalid EntityType given." );
    }
    public EntityType GetEntityTypeFromEntity<TEntity>( ) where TEntity : Node3D
    {
        EntityType entType = EntityType.Unknown;
        foreach (KeyValuePair<EntityType,Type> pair in _entEntries)
        {
            if ( pair.Value == typeof(TEntity) )
            {
                entType = pair.Key;
                break;
            }
        }
        return entType;
    }
    public EntityType GetEntityType( IGameEntity gameEntity )
    {
        EntityType entityType = EntityType.Unknown;
        foreach (KeyValuePair<EntityType,Type> pair in _entEntries)
        {
            if ( pair.Value == gameEntity.GetType( ) )
            {
                entityType = pair.Key;
                break;
            }
        }

        return entityType;
    }

    private readonly Dictionary<EntityType, Type> _entEntries = new( );
    private readonly Dictionary<EntityType, string> _resEntries = new( );
}