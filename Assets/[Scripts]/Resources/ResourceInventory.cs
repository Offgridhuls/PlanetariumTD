using System.Collections.Generic;
using Planetarium;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class ResourceAmount
{
    public ResourceType resourceType;
    public int amount;
}

public class ResourceInventory : MonoBehaviour
{
    [SerializeField] private List<ResourceAmount> resources = new List<ResourceAmount>();
    
    // Events
    public UnityEvent<ResourceType, int> onResourceChanged = new UnityEvent<ResourceType, int>();
    
    public void AddResource(ResourceType type, int amount)
    {
        ResourceAmount existingResource = resources.Find(r => r.resourceType == type);
        
        if (existingResource != null)
        {
            existingResource.amount += amount;
        }
        else
        {
            resources.Add(new ResourceAmount { resourceType = type, amount = amount });
        }
        
        onResourceChanged.Invoke(type, GetResourceAmount(type));
    }
    
    public bool SpendResource(ResourceType type, int amount)
    {
        ResourceAmount existingResource = resources.Find(r => r.resourceType == type);
        
        if (existingResource != null && existingResource.amount >= amount)
        {
            existingResource.amount -= amount;
            onResourceChanged.Invoke(type, existingResource.amount);
            return true;
        }
        
        return false;
    }
    
    public int GetResourceAmount(ResourceType type)
    {
        ResourceAmount resource = resources.Find(r => r.resourceType == type);
        return resource != null ? resource.amount : 0;
    }
}
