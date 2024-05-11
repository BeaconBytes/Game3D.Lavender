using System;
using System.Collections.Generic;
using Godot;
using Lavender.Common.Controllers;
using Lavender.Common.Enums.Types;

namespace Lavender.Common.Registers;

public class ControllerRegistry
{
    public void LoadDefaults()
    {
        Register<PlayerController>(ControllerType.Player, "res://Scenes/Core/Controllers/player_controller.tscn");
    }
    
    public void Register<TController>( ControllerType controllerType, string resPath ) where TController : IController
    {
        if ( controllerType == ControllerType.Unknown )
            throw new Exception( "Unable to register entity with type Unknown!" );
        if ( _ctrlEntries.ContainsKey( controllerType ) )
            throw new Exception( $"Tried to EntityRegister twice with EntityType.{controllerType}" );

        if (!Registers.Register.Scenes.HasEntry(resPath))
        {
            Registers.Register.Scenes.Register(resPath);
        }
        
        _resEntries.Add( controllerType, resPath );
        _ctrlEntries.Add( controllerType, typeof(TController) );
    }
    public string GetResPath( ControllerType controllerType )
    {
        if ( _resEntries.TryGetValue( controllerType, out string result ) )
        {
            return result;
        }

        throw new Exception( $"Invalid EntityType.{controllerType.ToString()} given." );
    }
    public ControllerType GetControllerType<TController>( ) where TController : Node
    {
        ControllerType ctrlType = ControllerType.Unknown;
        foreach (KeyValuePair<ControllerType,Type> pair in _ctrlEntries)
        {
            if ( pair.Value == typeof(TController) )
            {
                ctrlType = pair.Key;
                break;
            }
        }
        return ctrlType;
    }
    public ControllerType GetControllerType( IController controller )
    {
        ControllerType entityType = ControllerType.Unknown;
        foreach (KeyValuePair<ControllerType,Type> pair in _ctrlEntries)
        {
            if ( pair.Value == controller.GetType( ) )
            {
                entityType = pair.Key;
                break;
            }
        }

        return entityType;
    }

    private readonly Dictionary<ControllerType, Type> _ctrlEntries = new( );
    private readonly Dictionary<ControllerType, string> _resEntries = new( );
}