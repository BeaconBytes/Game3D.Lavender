using Godot;
using Lavender.Common.Managers;

namespace Lavender.Common.Interfaces.Management.Waves;

public interface IWaveEnemy
{
    public void WaveSetup(Marker3D[] botPathPoints, WaveManager waveManager);
}