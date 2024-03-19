namespace Lavender.Common.Enums.States;

public enum WaveState : byte
{
    Stopped = 0, // Stage is completely clean and no enemies are out. In between rounds. AKA the "buying" phase
    Starting = 1, // Countdown to the beginning of the new wave
    
    Active = 2, // Wave currently active and enemies are currently out
    Finished = 4, // Wave completed
    
    Paused = 5, // Currently unused, but later may be used for a pause mid-wave
}