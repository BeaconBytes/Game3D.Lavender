using System.Collections.Generic;
using Lavender.Common.Inventory.Items;

namespace Lavender.Common.Inventory;

public class ItemStack
{
    public ItemStack(Item item, uint count, Inventory parentInventory, string name, List<string> description = null)
    {
        Item = item;
        Count = count;
        Name = name;

        if (description == null)
            description = new();
        Description = description;
        
        ParentInventory = parentInventory;
    }
    
    public void SetCount(uint newCount)
    {
        Count = newCount;
        StackUpdateEvent?.Invoke(this);
    }

    public void Decrement(uint amount = 1)
    {
        if (Count <= amount)
        {
            // Destroy the stack, we're empty!
            Count = 0;
            return;
        }
        Count -= amount;
    }
    public void Increment(uint amount = 1)
    {
        if (uint.MaxValue - Count < amount)
        {
            // MAX the stack I guess? We cant fit any more
            Count = uint.MaxValue;
            return;
        }
        Count += amount;
    }

    public uint Count { get; protected set; }
    public Item Item { get; protected set; }
    public string Name { get; protected set; }
    public List<string> Description { get; protected set; }
    public Inventory ParentInventory { get; protected set; }
    

    public delegate void SimpleItemStackEventHandler(ItemStack stack);

    public event SimpleItemStackEventHandler StackUpdateEvent;
}