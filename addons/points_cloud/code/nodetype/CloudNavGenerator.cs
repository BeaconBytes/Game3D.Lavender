using System;
using Godot;
using Lavender.Common.Data.Saving.Mapping;

[Tool]
public partial class CloudNavGenerator : Node3D
{
    public override void _EnterTree()
    {
        _runningGenerate = false;
        base._EnterTree();
    }

    public override void _ExitTree()
    {
        _runningGenerate = false;
        _startGenerating = false;
        base._ExitTree();
    }

    private void StartGenerate()
    {
        RemoveDisplays();
		
        _boxShape = PhysicsServer3D.BoxShapeCreate();
		
        PhysicsServer3D.ShapeSetData(_boxShape, new Vector3(1f,1f,1f));
		
        _boxPosition = new Vector3I(_searchPos.X - Mathf.FloorToInt(_searchSize.X / 2f),_searchPos.Y - Mathf.FloorToInt(_searchSize.Y / 2f),_searchPos.Z - Mathf.FloorToInt(_searchSize.Z / 2f));
        _boxParams = new PhysicsShapeQueryParameters3D();
        _boxParams.ShapeRid = _boxShape;
        _boxTransform = _boxParams.Transform;
        _boxTransform.Origin = _boxPosition;
        _boxParams.Transform = _boxTransform;

        _hitsDisplayRootNode = new Node3D();
        _hitsDisplayRootNode.Name = "HitsDisplayNode";
        AddChild(_hitsDisplayRootNode);

		
        _cachedHitDisplayerNode = new StaticBody3D();
        MeshInstance3D tmpMI = new MeshInstance3D();
        BoxMesh tmpMIMesh = new BoxMesh();
        tmpMIMesh.Size = new Vector3(1f, 1f, 1f);
        StandardMaterial3D tmpMIMat = new StandardMaterial3D();
        tmpMIMat.AlbedoColor = Colors.OrangeRed;
        tmpMIMesh.Material = tmpMIMat;
        tmpMI.Mesh = tmpMIMesh;
        _cachedHitDisplayerNode.AddChild(tmpMI);
        _hitsDisplayRootNode.AddChild(_cachedHitDisplayerNode);

        _navSave = new VolumetricNavSave(_searchPos, _searchSize );
		
        _worldSpaceState = GetWorld3D().DirectSpaceState;
        _currentFrame = 0;
        _runningGenerate = true;

        GD.Print("[PointsCloud] Started Generation!");
    }
    
    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!_runningGenerate && !_startGenerating && _removeDisplays)
        {
            RemoveDisplays();
            _removeDisplays = false;
        }
        
        if (!_runningGenerate && _startGenerating)
        {
            StartGenerate();
        }
        
        if (!_runningGenerate)
            return;

        _startGenerating = true;

        _currentFrame++;
        if (_currentFrame % 1 != 0)
            return;

        for (int i = 0; i < 75; i++)
        {
            DoLogic();
            if (!_runningGenerate)
                break;
        }
    }
    

    private void StopProcessing()
    {
        _startGenerating = false;
        if (!_runningGenerate)
            return;
        
        _runningGenerate = false;

        if (_hitsDisplayRootNode != null && _cachedHitDisplayerNode != null)
        {
            _hitsDisplayRootNode.RemoveChild(_cachedHitDisplayerNode);
            _cachedHitDisplayerNode.Free();
        }
		
        PhysicsServer3D.FreeRid(_boxShape);

        if (!_removeDisplayedOnStop)
            return;

        RemoveDisplays();

    }

    private void DoLogic()
    {
        Godot.Collections.Array<Godot.Collections.Dictionary> hits = _worldSpaceState.IntersectShape(_boxParams);
		
        if (hits.Count > 0)
        {
            StaticBody3D displayer = (StaticBody3D)_cachedHitDisplayerNode.Duplicate();
            displayer.GlobalPosition = _boxTransform.Origin;
            _hitsDisplayRootNode.AddChild(displayer);
            
            _navSave.AddUnwalkablePoint(_boxPosition);
        }

        if (_boxPosition.Y > _searchPos.Y + Mathf.CeilToInt(_searchSize.Y / 2f))
        {
            _navSave.SaveToFile("./save/mapping/new_volumetric.nav");
			
            StopProcessing();
            GD.Print($"[PointsCloud] Completed. Took {_currentFrame} frames.");
            return;
        }

        if (_boxPosition.X >= _searchPos.X + Mathf.CeilToInt(_searchSize.X / 2f))
        {
            _boxPosition.X = _searchPos.X - Mathf.FloorToInt(_searchSize.X / 2f);
            if (_boxPosition.Z >=  _searchPos.Z + Mathf.CeilToInt(_searchSize.Z / 2f))
            {
                _boxPosition.Z = _searchPos.Z - Mathf.FloorToInt(_searchSize.Z / 2f);
                _boxPosition.Y++;
            }
            else
            {
                _boxPosition.Z++;
            }
        }
        else
        {
            _boxPosition.X++;
        }
        
        _boxTransform.Origin = _boxPosition;
        _boxParams.Transform = _boxTransform;
    }

    void RemoveDisplays()
    {
        if (_hitsDisplayRootNode != null)
        {
            RemoveChild(_hitsDisplayRootNode);
            _hitsDisplayRootNode.Free();
            _hitsDisplayRootNode = null;
        }

        if (HasNode("HitsDisplayNode"))
        {
            Node rootNodeFound = GetNode("HitsDisplayNode");
            while (rootNodeFound != null)
            {
                RemoveChild(rootNodeFound);
                rootNodeFound.Free();
                if (HasNode("HitsDisplayNode"))
                {
                    rootNodeFound = GetNode("HitsDisplayNode");
                }
            }
        }
    }
    
    [Export]
    private bool _startGenerating = false;

    [Export]
    private bool _removeDisplays = false;
    
    private bool _runningGenerate = false;
    
    [Export]
    private bool _removeDisplayedOnStop = false;
    
    private int _currentFrame = 0;
    
    [Export]
    private Vector3I _searchSize = new Vector3I(75, 45, 75);
    
    [Export]
    private Vector3I _searchPos = new Vector3I(0, 0, 0);

    private PhysicsDirectSpaceState3D _worldSpaceState;
    private PhysicsShapeQueryParameters3D _boxParams;
    private Rid _boxShape;
    private Transform3D _boxTransform;
    private Vector3I _boxPosition;

    private Node3D _hitsDisplayRootNode;
    private StaticBody3D _cachedHitDisplayerNode;

    private VolumetricNavSave _navSave;
}