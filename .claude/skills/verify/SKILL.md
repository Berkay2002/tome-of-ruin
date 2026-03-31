---
name: verify
description: Check all C# scripts for common issues — syntax errors, missing references, Unity API mistakes. Use after making code changes to catch problems before opening in Unity.
---

# Verify

Run a comprehensive check on all C# scripts in the project.

## Steps

1. **Syntax scan** — Check all `.cs` files under `Assets/Scripts/` for:
   - Unmatched braces `{` `}`
   - Missing semicolons on statements
   - Unclosed string literals

2. **Unity 2022.3 API compliance** — Grep for banned APIs:
   - `linearVelocity` → must be `velocity`
   - `FindFirstObjectByType` → must be `FindObjectOfType`
   - `FindObjectsByType` → must be `FindObjectsOfType`

3. **Cross-reference check** — For each class referenced in scripts, verify the defining `.cs` file exists:
   - Check that types used in `GetComponent<T>()`, `AddComponent<T>()`, inheritance, and field declarations have corresponding files
   - Flag any forward references to types that don't exist yet

4. **Test coverage check** — List any scripts in `Assets/Scripts/` that have testable public methods but no corresponding test file in `Assets/Tests/`

5. **Assembly definition check** — Verify:
   - `GameScripts.asmdef` exists and is autoReferenced
   - `EditMode.asmdef` and `PlayMode.asmdef` reference `GameScripts`
   - `Editor.asmdef` references `GameScripts` and targets Editor platform only

Report findings grouped by severity: Errors (will fail to compile), Warnings (potential issues), Info (suggestions).
