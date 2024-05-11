using Godot;
using Lavender.Common.Enums.Net;
using Lavender.Common.Exceptions;
using Lavender.Common.Networking.Packets.Variants.Entity.Movement;
using Lavender.Common.Registers;

namespace Lavender.Common.Entity.GameEntities;

public partial class HumanoidEntityBase : LivingEntityBase
{
    [Export] public Node3D HeadNode { get; protected set; }

    public override void _Ready()
    {
        base._Ready();
        
        Register.Packets.Subscribe<EntityRotatePacket>(OnEntityRotatePacket);


        if (HeadNode == null)
            throw new BadNodeSetupException("HumanoidEntity has no HeadNode set!");
    }

    protected override Vector3 ApplyMovementRotation(Vector3 inputRotate)
    {
        return RotateHeadInput(inputRotate);
    }

    protected override void ReconciliationRotateTo(Vector3 rot)
    {
        RotateHead(rot);
    }

    public Vector3 RotateHeadInput(Vector3 input)
    {
        if (HeadNode == null)
            return Vector3.Zero;
        RotateY( input.X );
        HeadNode.RotateX( input.Y );

        Vector3 rot = HeadNode.Rotation;
        rot.X = Mathf.Clamp( HeadNode.Rotation.X, Mathf.DegToRad( -90f ), Mathf.DegToRad( 90f ) );
        HeadNode.Rotation = rot;

        return new Vector3(HeadNode.Rotation.X, GlobalRotation.Y, 0f);
    }

    public void RotateHead(Vector3 rotation)
    {
        if (HeadNode == null)
            return;
        Vector3 bodyRot = GlobalRotation;
        Vector3 headRot = HeadNode.Rotation;

        bodyRot.X = 0f;
        bodyRot.Y = rotation.Y;
        bodyRot.Z = 0f;

        headRot.X = rotation.X;
        headRot.Y = 0f;
        headRot.Z = 0f;

        GlobalRotation = bodyRot;
        HeadNode.Rotation = headRot;
    }

    public Vector2 GetHeadRotation()
    {
        if (HeadNode == null)
            return new Vector2(0, GlobalRotation.Y);
        return new Vector2(HeadNode.Rotation.X, GlobalRotation.Y);
    }

    public Vector3 GetRotationWithHead()
    {
        if (HeadNode == null)
            return new Vector3(0, GlobalRotation.Y, 0f);
        return new Vector3(HeadNode.Rotation.X, GlobalRotation.Y, 0f);
    }
    
    private void OnEntityRotatePacket(EntityRotatePacket packet, uint sourceNetId)
    {
        if (packet.NetId != NetId || sourceNetId != (uint)StaticNetId.Server)
            return;

        if (HeadNode == null)
            return;
        
        RotateHead(packet.Rotation);
    }
}