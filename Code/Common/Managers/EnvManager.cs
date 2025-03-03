using System;
using System.IO;
using Godot;
using Lavender.Common.Data.Config;
using Lavender.Common.Registers;

namespace Lavender.Common.Managers;

public partial class EnvManager : Node
{
    private const string ConfigFileName = "UserConfig.json";
    

    public bool IsServer { get; protected set; } = false;
    public string ClientTargetIp { get; protected set; } = string.Empty;
    public int ClientTargetPort { get; protected set; }  = 0;
    public int ServerPort { get; protected set; } = 8778;

    public bool IsDualManagers { get; protected set; } = false;
    public bool IsSinglePlayer { get; protected set; } = false;

    // private readonly JsonSerializer _jsonSerializer = new JsonSerializer();
    

    public GameManager CurrentGameManager { get; protected set; }

    public UserConfig UserConfig { get; protected set; } = new UserConfig();

    
    private Node _curSceneRoot;
    
    private Node _globalSceneSocketNode;
    
    
    public override void _Ready()
    {
        bool isProperlyExported = OS.HasFeature("server") || OS.HasFeature("client");

        if (isProperlyExported)
        {
            if (OS.HasFeature("server"))
                IsServer = true;

            if (OS.HasFeature("client"))
                IsServer = false;
        }

        // Load the defaults for registries
        Register.LoadDefaults();

        //LoadConfigData();
        //SaveConfigData(UserConfig);

        if (_globalSceneSocketNode == null)
        {
            _globalSceneSocketNode = new Node3D();
            _globalSceneSocketNode.Name = "GlobalSceneSocket";
            AddChild(_globalSceneSocketNode);
        }
        
        // If _globalRootNode isn't set, do nothing further.
        if (_globalSceneSocketNode == null)
            throw new Exception("Invalid _globalSceneSocketNode");

        // Load the right environment/tree/scene:
        if (IsServer)
        {
            GotoScene(Register.SceneTable["env_server"], true);
        }
        else
        {
            GotoScene(Register.SceneTable["loadup_menu_main"]);
        }
    }
    
    /// <summary>
    /// Calls GotoScene as deferred via the DeferredGotoScene() method
    /// </summary>
    public void GotoScene( string fullPath, bool keeperExists = false, bool isDualManager = false )
    {
        CallDeferred( nameof(DeferredGotoScene), fullPath, keeperExists, isDualManager );
    }

    /// <summary>
    /// Changes the scene to the given scene path. If keeperExists then grab our Keeper's reference. If not, clear it.
    /// </summary>
    private void DeferredGotoScene( string fullPath, bool keeperExists = false, bool isDualManager = false )
    {
        // If there is a current scene, remove and free it.
        if (_curSceneRoot != null)
        {
            _globalSceneSocketNode.RemoveChild(_curSceneRoot);
            _curSceneRoot.Free();
        }

        // Load the next scene.
        PackedScene nextScene = (PackedScene)GD.Load(fullPath);

        if (nextScene == null)
        {
            throw new Exception($"Invalid scene at path '{fullPath}'");
        }
        
        // Instance the next scene.
        _curSceneRoot = nextScene.Instantiate();

        // Add it to the _globalRootNode as child.
        _globalSceneSocketNode.AddChild(_curSceneRoot);

        if ( keeperExists )
        {
            CurrentGameManager = _curSceneRoot.GetNode<GameManager>( "Manager" );

            if (CurrentGameManager == null)
            {
                GD.PrintErr($"Couldn't found a GameManager for resPath '{fullPath}'.");
                return;
            }
        }
        else
        {
            CurrentGameManager = null;
        }
    }
    
    // /// <summary>
    // /// Saves the given UserConfigData to the preset ConfigFileName and sets the cached UserConfig to it.
    // /// </summary>
    // public void SaveConfigData( UserConfig config )
    // {
    //     using StreamWriter sw = new StreamWriter( $"./{ConfigFileName}" );
    //     using JsonTextWriter tw = new JsonTextWriter( sw );
    //     tw.Formatting = Formatting.Indented;
    //     _jsonSerializer.Serialize( tw, config );
    //     tw.Flush(  );
    //         
    //     UserConfig = config;
    // }
    // /// <summary>
    // /// Loads config data if it can (exists and is valid data) into UserConfig value
    // /// </summary>
    // private void LoadConfigData( )
    // {
    //     if ( File.Exists( $"./{ConfigFileName}" ) )
    //     {
    //         using StreamReader sr = new StreamReader( $"./{ConfigFileName}" );
    //         using JsonTextReader tr = new JsonTextReader( sr );
    //         UserConfig = _jsonSerializer.Deserialize<UserConfig>( tr ) ?? new UserConfig( );
    //     }
    // }

    public bool JoinServer(string ipAddress, int port)
    {
        if (ClientTargetIp != string.Empty || ipAddress == string.Empty || IsServer)
            return false;

        ClientTargetIp = ipAddress;
        ClientTargetPort = port;

        GotoScene(Register.SceneTable["env_client"], true, false);

        return true;
    }

    public void JoinSinglePlayer()
    {
        ClientTargetIp = "localhost";
        ClientTargetPort = ServerPort;
        ServerPort = ClientTargetPort;
        IsDualManagers = true;
        IsSinglePlayer = true;
        
        GotoScene(Register.SceneTable["env_dual"], false, true);
    }
}