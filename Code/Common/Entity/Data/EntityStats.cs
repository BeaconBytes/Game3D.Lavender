using Lavender.Common.Data;
using Lavender.Common.Enums.Entity;

namespace Lavender.Common.Entity.Data;

public class EntityStats
{
    private void FireStatChanged(StatType statType, float newValue)
    {
        StatChangedEvent?.Invoke(statType, newValue);
    }
    
    public float Health
    {
        get => _health;
        set
        {
            FireStatChanged(StatType.Health, value);
            _health = value;
        }
    }
    private float _health = 100f;
    
    public float MaxHealth
    {
        get => _maxHealth;
        set
        {
            FireStatChanged(StatType.MaxHealth, value);
            _maxHealth = value;
        }
    }
    private float _maxHealth = 100f;
    
    public float Stamina
    {
        get => _stamina;
        set
        {
            FireStatChanged(StatType.Stamina, value);
            _stamina = value;
        }
    }
    private float _stamina = 100f;
    
    public float MaxStamina
    {
        get => _maxStamina;
        set
        {
            FireStatChanged(StatType.MaxStamina, value);
            _maxStamina = value;
        }
    }
    private float _maxStamina = 100f;
    
    public float Weight
    {
        get => _weight;
        set
        {
            FireStatChanged(StatType.Weight, value);
            _weight = value;
        }
    }
    private float _weight = 100f;
    
    public float MaxWeight
    {
        get => _maxWeight;
        set
        {
            FireStatChanged(StatType.MaxWeight, value);
            _maxWeight = value;
        }
    }
    private float _maxWeight = 100f;


    public float Armor
    {
        get => _armor;
        set
        {
            FireStatChanged(StatType.Armor, value);
            _armor = value;
        }
    }
    private float _armor = 0f;

    public float Resistance
    {
        get => _resistance;
        set
        {
            FireStatChanged(StatType.Resistance, value);
            _resistance = value;
        }
    }
    private float _resistance = 0f;
    
    
    public float MovementJumpImpulse
    {
        get => _moveJumpImpulse;
        set
        {
            FireStatChanged(StatType.JumpImpulseBase, value);
            _moveJumpImpulse = value;
        }
    }
    private float _moveJumpImpulse = 7f;
    
    public float MovementAcceleration
    {
        get => _moveAcceleration;
        set
        {
            FireStatChanged(StatType.MovementAcceleration, value);
            _moveAcceleration = value;
        }
    }
    private float _moveAcceleration = 35f;
    
    public float MovementSpeedBase
    {
        get => _moveSpeedBase;
        set
        {
            FireStatChanged(StatType.MoveSpeedBase, value);
            _moveSpeedBase = value;
        }
    }
    private float _moveSpeedBase = 7f;
    
    public float MovementSpeedMultiplier
    {
        get => _moveSpeedMultiplier;
        set
        {
            FireStatChanged(StatType.MoveSpeedMultiplier, value);
            _moveSpeedMultiplier = value;
        }
    }
    private float _moveSpeedMultiplier = 1.0f;
    
    public float MovementSprintMultiplier
    {
        get => _moveSprintMultiplier;
        set
        {
            FireStatChanged(StatType.MoveSpeedSprintMultiplier, value);
            _moveSprintMultiplier = value;
        }
    }
    private float _moveSprintMultiplier = 1.4f;

    
    // HANDLERS //
    public delegate void EntityStatChangedHandler(StatType statType, float newValue);
    
    
    // EVENTS //
    public event EntityStatChangedHandler StatChangedEvent;
}