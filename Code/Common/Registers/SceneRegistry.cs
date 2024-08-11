using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Lavender.Common.Registers;

public class SceneRegistry
{
    public void LoadDefaults()
    {
        
    }
    
    public void Register( string resPath )
    {
        if ( _entries.ContainsKey( resPath ) )
            return;
        PackedScene scene = ResourceLoader.Load<PackedScene>( resPath );
        if ( scene == null )
        {
            throw new Exception( $"Failed to find a Scene @ '{resPath}'" );
        }

        _entries.Add( resPath, scene );
    }

    public T GetInstance<T>( string resPath ) where T : Node
    {
        if ( String.IsNullOrEmpty( resPath ) )
            throw new Exception( $"Tried to GetInstance of null resource path" );
        if ( !_entries.TryGetValue( resPath, out PackedScene scene ) )
        {
            return default(T);
        }
        return scene.Instantiate<T>( );
    }

    public bool HasEntry(string resPath)
    {
        return _entries.Any(x => x.Key.Equals(resPath));
    }

    readonly private Dictionary<string, PackedScene> _entries = new Dictionary<string, PackedScene>( );
}