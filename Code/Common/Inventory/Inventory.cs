using System.Collections.Generic;
using Lavender.Common.Inventory.Items;

namespace Lavender.Common.Inventory;

public class Inventory
{
    public Inventory(uint maxSlots = 1)
    {
        MaxSlotsCount = maxSlots;
    }

    public uint MaxSlotsCount = 1;
    
    public List<Item> items = new();
}