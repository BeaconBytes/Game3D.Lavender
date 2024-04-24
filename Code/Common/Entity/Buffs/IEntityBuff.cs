namespace Lavender.Common.Entity.Buffs;

public interface IEntityBuff
{
    public bool Enabled { get; }
    public void NetworkTick(double delta);
    public void GameTick(double delta);
    public void Setup();
}