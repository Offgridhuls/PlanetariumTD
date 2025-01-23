using UnityEngine;

namespace Planetarium
{
    [System.Serializable]
    public class InventoryItem
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public Sprite Icon { get; private set; }
        public int Count { get; private set; }

        public InventoryItem(string id, string name, Sprite icon, int count = 1)
        {
            Id = id;
            Name = name;
            Icon = icon;
            Count = count;
        }

        public void UpdateCount(int newCount)
        {
            Count = Mathf.Max(0, newCount);
        }
    }
}
