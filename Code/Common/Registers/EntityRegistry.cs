using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Entity;
using Lavender.Common.Entity.GameEntities;
using Lavender.Common.Enums.Types;
using PlayerEntity = Lavender.Common.Entity.GameEntities.PlayerEntity;

namespace Lavender.Common.Registers;

public class EntityRegistry
{
    public void LoadDefaults()
    {
        // Game Entities
        Register<PlayerEntity>(EntityType.Player,"res://Scenes/Core/Entities/Player/player_entity.tscn");
        //Register<BuddyEnemy>(EntityType.BuddyEnemy, ControllerType.Unknown, "res://Scenes/Core/Entities/Enemies/Buddy/buddy_enemy.tscn");
    }
    
    public void Register<TEntity>( EntityType entityType, string resPath ) where TEntity : IGameEntity
    {
        if ( entityType == EntityType.Unknown )
            throw new Exception( "Unable to register entity with type Unknown!" );
        if ( _entityEntries.ContainsKey( entityType ) )
            throw new Exception( $"Tried to EntityRegister twice with EntityType.{entityType}" );

        if (!Registers.Register.Scenes.HasEntry(resPath))
        {
            Registers.Register.Scenes.Register(resPath);
        }
        
        _resEntries.Add(entityType, resPath);
        _entityEntries.Add(entityType, typeof(TEntity));
    }
    public string GetEntityResPath( EntityType entityType )
    {
        if ( _resEntries.TryGetValue( entityType, out string result ) )
        {
            return result;
        }

        throw new Exception( $"Invalid EntityType.{entityType.ToString()} given." );
    }
    public EntityType GetEntityType<TEntity>( ) where TEntity : Node3D
    {
        EntityType entType = EntityType.Unknown;
        foreach (KeyValuePair<EntityType,Type> pair in _entityEntries)
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
        foreach (KeyValuePair<EntityType,Type> pair in _entityEntries)
        {
            if ( pair.Value == gameEntity.GetType( ) )
            {
                entityType = pair.Key;
                break;
            }
        }

        return entityType;
    }
    
    [Obsolete("No longer works after refactoring controller spawning of their entity")]
    public EntityType GetEntityType(IController controller)
    {
        return EntityType.Unknown;
    }
    
    
    private readonly Dictionary<EntityType, Type> _entityEntries = new( );
    private readonly Dictionary<EntityType, string> _resEntries = new( );
}