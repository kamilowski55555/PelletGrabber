# UNITY_CSHARP_STYLE.md
Minimal Unity C# Coding Guidelines (For ML-Agents & Environment Logic)

This is a lightweight, strict set of rules designed to keep Unity-side code clean, predictable, and maintainable when integrating with ML-Agents and Python RL pipelines.

---

## 1. Guiding Principles
- Keep Unity scripts **simple, explicit, and deterministic**.
- Avoid heavy logic inside MonoBehaviour lifecycle methods.
- Prefer composition over god-classes.
- All parameters that influence environment behaviour must be **visible, serialized, and documented**.

---

## 2. Script Structure
Every script should follow this order:
```
using statements

public class ClassName : MonoBehaviour
{
    // Serialized fields
    // Public read-only properties
    // Private fields

    // Unity lifecycle (Awake, Start, OnEnable, Update...) in correct order

    // Public methods
    // Private methods
}
```

---

## 3. MonoBehaviour Lifecycle Rules
### Allowed:
- `Awake()` only for internal initialization (no scene references).
- `Start()` only for referencing other components.
- `FixedUpdate()` for physics-based movement.
- `Update()` for minimal, lightweight logic.

### Forbidden:
- Heavy logic in `Update()`.
- Random initialization across multiple lifecycle methods.
- Hidden side effects inside lifecycle methods.

Environment behaviour must be predictable for ML training.

---

## 4. Serialized Fields & Parameters
All tunable values must be exposed through `[SerializeField]`.
```
[SerializeField] private float moveSpeed = 3.0f;
```

Rules:
- **No magic numbers** inside code.
- Use descriptive names.
- Avoid public fields.

This ensures environment settings remain visible and configurable.

---

## 5. Naming Conventions
- Classes: `PascalCase`
- Methods: `PascalCase`
- Variables/fields: `camelCase`
- Private fields: `_camelCase`
- Serialized fields: `camelCase`
- Constants: `UPPER_CASE`

Example:
```
[SerializeField] private float agentSpeed;
private Rigidbody _rb;
```

---

## 6. Component Access
Rules:
- Use `GetComponent<T>()` only in `Awake()` or `Start()`.
- Cache references; never call GetComponent repeatedly.
```
private Rigidbody _rb;
private void Awake() => _rb = GetComponent<Rigidbody>();
```

---

## 7. Environment Reset & Episode Flow
Any ML-Agent environment must:
- Have a **single clear Reset method**.
- Reset all agent-relevant objects explicitly.
- Avoid random object states unless required by design.
- Logically separate reward logic from movement logic.

```
public void ResetEnvironment()
{
    // Reset agent position
    // Reset target
    // Reset obstacles
}
```

Do **not** scatter reset logic across multiple scripts.

---

## 8. Reward Logic
Reward logic must be kept in:
- A dedicated reward method, or
- A separate `Agent` subclass.

Never mix:
- Reward logic
- Movement logic
- Environment mechanics

Each reward component must be documented.

---

## 9. Movement & Input Logic
Rules:
- Movement must be deterministic.
- Use Rigidbody for physics.
- Apply movement in `FixedUpdate()`.
- Keep movement/math helpers pure functions.

Avoid physics logic inside `Update()`.

---

## 10. Inter-Script Communication
Preferred:
- Direct references (assigned in inspector or via Awake/Start)
- C# events

Avoid:
- `FindObjectOfType` (slow, unpredictable)
- Singleton patterns unless unavoidable

---

## 11. Folder Structure
Minimum required folders:
```
Scripts/
    Agents/
    Environment/
    Utils/
```

- `Agents/`: ML-Agent subclasses, reward logic.
- `Environment/`: environment mechanics, reset logic, obstacles.
- `Utils/`: math helpers, randomizers, extension methods.

---

## 12. No Anti-Patterns
Forbidden:
- God classes controlling entire scene.
- Unserialized magic constants.
- Implicit behaviour depending on Unity execution order.
- Creating/destroying objects every frame.
- Doing ML computations in C#.
- Using MonoBehaviour as a data container.

---

## Summary for AI Tools
Generated Unity C# code must:
1. Use serialized fields for all tunable values.
2. Avoid heavy logic in Update(); prefer FixedUpdate for movement.
3. Keep environment reset deterministic and centralized.
4. Separate movement, reward, and environment logic.
5. Cache components; avoid repeated GetComponent calls.
6. Produce clean, readable, minimal scripts suited for ML training.

