# Enemy State Machine Documentation

## Overview
The Enemy State Machine is a flexible, type-safe system for managing enemy behavior in PlanetariumTD. It provides a clean architecture for implementing complex enemy behaviors through composable states.

## Core Components

### EnemyBase
The base class for all enemies in the game. It manages the state machine and provides common functionality.

```csharp
public class EnemyBase : MonoBehaviour
{
    protected Dictionary<Type, EnemyStateBase> states;
    protected EnemyStateBase currentState;
    
    // Register states in derived classes
    protected virtual void InitializeStates()
    {
        RegisterState<MoveToGeneratorState>();
        RegisterState<AttackGeneratorState>();
    }
}
```

### EnemyStateBase
The abstract base class for all enemy states. Provides lifecycle methods and access to the owner.

```csharp
public abstract class EnemyStateBase
{
    protected EnemyBase Owner { get; private set; }
    
    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }
    
    protected void TransitionTo<T>() where T : EnemyStateBase
    {
        Owner.TransitionToState<T>();
    }
}
```

## Built-in States

### MoveToGeneratorState
Handles movement towards target generators.
- Finds nearest generator if no target exists
- Moves towards target using physics-based movement
- Transitions to AttackGeneratorState when in range
- Handles rotation based on planet gravity

### AttackGeneratorState
Manages attack behavior when in range of a generator.
- Maintains position while attacking
- Fires projectiles at configured rate
- Transitions back to MoveToGeneratorState if target is lost or out of range
- Rotates to face target while considering planet gravity

## Usage Examples

### Creating a Basic Flying Enemy
```csharp
public class FlyingEnemyBase : EnemyBase
{
    protected override void InitializeStates()
    {
        base.InitializeStates();
        RegisterState<MoveToGeneratorState>();
        RegisterState<AttackGeneratorState>();
    }

    protected override void TransitionToInitialState()
    {
        TransitionToState<MoveToGeneratorState>();
    }
}
```

### Creating a Custom State
```csharp
public class CircleTargetState : EnemyStateBase
{
    private float radius = 10f;
    private float speed = 5f;
    private float angle;
    
    public override void Enter()
    {
        // Initialize state
        angle = 0f;
    }
    
    public override void Update()
    {
        if (Owner.CurrentTarget == null)
        {
            TransitionTo<MoveToGeneratorState>();
            return;
        }
        
        // Circle around target
        angle += speed * Time.deltaTime;
        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );
        
        Owner.transform.position = Owner.CurrentTarget.transform.position + offset;
    }
}
```

## State Machine Features

### Type Safety
The state machine uses generics to ensure type safety:
```csharp
// Correct usage:
TransitionTo<AttackGeneratorState>();

// Compile error if state isn't registered:
TransitionTo<NonExistentState>();
```

### State Registration
States must be registered before use:
```csharp
protected override void InitializeStates()
{
    RegisterState<CustomState1>();
    RegisterState<CustomState2>();
}
```

### State Lifecycle
1. **Enter**: Called when transitioning into the state
2. **Update**: Called every frame while state is active
3. **FixedUpdate**: Called at fixed time intervals for physics
4. **Exit**: Called when transitioning out of the state

## Best Practices

1. **State Independence**
   - States should be self-contained
   - Avoid dependencies between states
   - Use EnemyBase for shared functionality

2. **State Transitions**
   - Keep transition logic simple and clear
   - Check for null references before transitions
   - Clean up resources in Exit()

3. **Performance**
   - Minimize allocations in Update loops
   - Cache component references
   - Use FixedUpdate for physics operations

4. **Extension**
   - Create new states by inheriting from EnemyStateBase
   - Override InitializeStates() to add custom states
   - Consider creating intermediate base classes for common behavior sets

## Examples

### Ranged Enemy Implementation
```csharp
public class RangedEnemy : EnemyBase
{
    protected override void InitializeStates()
    {
        base.InitializeStates();
        RegisterState<MoveToGeneratorState>();
        RegisterState<AttackGeneratorState>();
        RegisterState<RetreatState>();
    }
    
    // Custom state for low health behavior
    private class RetreatState : EnemyStateBase
    {
        public override void Update()
        {
            if (Owner.CurrentHealth > Owner.MaxHealth * 0.5f)
            {
                TransitionTo<MoveToGeneratorState>();
            }
            // Retreat logic...
        }
    }
}
```

### Adding State Parameters
```csharp
public class PatrolState : EnemyStateBase
{
    private Vector3[] patrolPoints;
    private int currentPoint;
    
    public void SetPatrolPoints(Vector3[] points)
    {
        patrolPoints = points;
        currentPoint = 0;
    }
    
    public override void Update()
    {
        // Patrol logic using points...
    }
}
```

## Debugging

The state machine includes built-in debugging features:
- Current state name is displayed in the Unity Inspector
- State transitions are logged in debug mode
- Easy to add custom debug visualization per state

## Future Considerations

1. **State Hierarchies**
   - Implement nested states
   - Add state inheritance for behavior reuse

2. **Global States**
   - Add support for concurrent states
   - Implement global state monitoring

3. **State Coroutines**
   - Add support for coroutines within states
   - Handle time-based state transitions
