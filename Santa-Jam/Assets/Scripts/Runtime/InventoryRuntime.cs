using System.Collections.Generic;
using UnityEngine;

public class InventoryRuntime : MonoBehaviour
{
    private Dictionary<string, int> items = new Dictionary<string, int>();

    public void AddItem(InGameItem item, int amount)
    {
        if (item == null)
            return;

        string key = item.ItemName;

        if (items.ContainsKey(key))
            items[key] += amount;
        else
            items.Add(key, amount);

        Debug.Log($"Inventory: {key} = {items[key]}");
    }
}
