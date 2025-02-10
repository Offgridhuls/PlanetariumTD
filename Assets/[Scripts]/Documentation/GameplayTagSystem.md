# Gameplay Tag System Documentation

## Overview
The Gameplay Tag System is a flexible and efficient way to add metadata and behavior to game objects in PlanetariumTD. Inspired by Unreal Engine's Gameplay Tags, this system allows for hierarchical organization of tags and provides tools for querying and filtering objects based on their tags.

## Core Components

### GameplayTag
The fundamental building block of the system. A GameplayTag represents a single tag with optional metadata.

```csharp
var tag = new GameplayTag("Category.Subcategory.Name", "Optional comment");
```

Key features:
- Hierarchical structure using dot notation
- Optional DevComment for metadata
- Immutable after creation
- Efficient comparison and matching

### GameplayTagContainer
Manages collections of tags for a single entity.

```csharp
var container = new GameplayTagContainer();
container.AddTag(new GameplayTag("Enemy.Flying"));
container.HasTag(new GameplayTag("Enemy")); // True (hierarchical matching)
```

Features:
- Add/remove tags
- Query for exact or hierarchical matches
- Batch operations for multiple tags
- Event system for tag changes

### TaggedComponent
MonoBehaviour wrapper for GameplayTagContainer.

```csharp
// In your MonoBehaviour
[RequireComponent(typeof(TaggedComponent))]
public class Enemy : MonoBehaviour, ITaggable
{
    private TaggedComponent taggedComponent;
    
    void Awake()
    {
        taggedComponent = GetComponent<TaggedComponent>();
        taggedComponent.AddTag(new GameplayTag("Enemy.Base"));
    }
}
```

Features:
- Unity component interface for tag system
- Automatic registration with TaggedObjectFilter
- Event callbacks for tag changes
- Editor visualization support

### TaggedObjectFilter
Global system for querying GameObjects by their tags.

```csharp
// Find all flying enemies
var flyingEnemies = TaggedObjectFilter.Instance.GetObjectsWithTag(new GameplayTag("Enemy.Flying"));
```

Features:
- Singleton access point
- Efficient object querying
- Automatic registration of TaggedComponents
- Support for complex tag queries

## Configuration

### GameplayTagConfig
ScriptableObject that defines valid tags and their relationships.

Location: `Assets/Resources/GameplayTagConfig.asset`

Features:
- Define valid tags and metadata
- Set up tag redirects
- Configure validation rules
- Editor window for management

### Editor Tools

#### Gameplay Tag Window
Access via: `PlanetariumTD > Gameplay Tags`

Features:
- Create/edit tags
- Add tag metadata
- Set up redirects
- Search and filter tags
- Validate tag configuration

#### Tag Visualizer
Component for debugging tag state in-game.

```csharp
// Add to any GameObject with TaggedComponent
gameObject.AddComponent<TagVisualizer>();
```

Features:
- Real-time tag display
- Editor and runtime support
- Customizable display options

## Best Practices

### Tag Naming
- Use dot notation for hierarchy: `Category.Subcategory.Name`
- Keep categories clear and consistent
- Use PascalCase for each segment
- Example: `Enemy.Type.Flying`

### Performance
- Cache commonly used tags as static readonly fields
- Use TaggedObjectFilter for querying instead of manual searches
- Consider tag hierarchy depth (deeper = slower queries)

### Organization
- Group related tags under common categories
- Use meaningful DevComments
- Keep tag hierarchy shallow when possible
- Document tag purposes and relationships

### Code Integration
- Implement ITaggable interface for tag-aware classes
- Use TaggedComponent's events for reactive behavior
- Cache TaggedComponent references
- Validate tags against config at runtime

## Example Usage

### Basic Tag Management
```csharp
// Add tags to an object
var tagged = GetComponent<TaggedComponent>();
tagged.AddTag(new GameplayTag("Enemy.Flying"));
tagged.AddTag(new GameplayTag("Status.Buffed"));

// Check for tags
if (tagged.HasTag(new GameplayTag("Enemy"))) // Matches any Enemy.*
{
    // Handle enemy logic
}
```

### Tag-based Filtering
```csharp
// Get all active turrets
var activeTurrets = TaggedObjectFilter.Instance
    .GetObjectsWithTag(new GameplayTag("Turret.Active"));

// Complex queries
var query = GameplayTagQuery.CreateAllQuery(new[]
{
    new GameplayTag("Enemy.Flying"),
    new GameplayTag("Status.Buffed")
});
var buffedFlyers = TaggedObjectFilter.Instance.GetObjectsMatchingQuery(query);
```

### Reactive Tag Behavior
```csharp
public class BuffableEntity : MonoBehaviour, ITaggable
{
    private TaggedComponent tags;

    void Awake()
    {
        tags = GetComponent<TaggedComponent>();
        tags.OnTagAdded += HandleTagAdded;
    }

    private void HandleTagAdded(GameplayTag tag)
    {
        if (tag.Matches(new GameplayTag("Status.Buffed")))
        {
            // Apply buff effects
        }
    }
}
```

## Troubleshooting

### Common Issues
1. **Tags not recognized**
   - Ensure tags are defined in GameplayTagConfig
   - Check for typos in tag strings
   - Verify correct namespace usage

2. **Performance Issues**
   - Review tag hierarchy depth
   - Cache commonly used tags
   - Use appropriate query methods

3. **Missing Tags**
   - Verify TaggedComponent is added
   - Check Awake/Start initialization order
   - Ensure proper registration with TaggedObjectFilter

### Debug Tools
- Use TagVisualizer for runtime inspection
- Enable debug logging in GameplayTagConfig
- Use the Gameplay Tags window to verify configuration
