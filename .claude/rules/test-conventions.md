---
paths:
  - "Assets/Tests/**"
---

# Test Conventions

- Use NUnit `[TestFixture]`, `[Test]`, `[SetUp]`, `[TearDown]` attributes
- Create GameObjects in `SetUp`, destroy with `Object.DestroyImmediate()` in `TearDown`
- Create ScriptableObjects via `ScriptableObject.CreateInstance<T>()` (no asset files needed)
- EditMode tests: pure logic only (no MonoBehaviour instantiation) — path: `Assets/Tests/EditMode/`
- PlayMode tests: can create GameObjects and test MonoBehaviour interactions — path: `Assets/Tests/PlayMode/`
- Test assemblies reference `GameScripts` assembly — add new script assemblies to test `.asmdef` references if needed
- Use `ComboBookData.ForceInitSlots()` in tests (not `InitSlots()` which no-ops on pre-configured books)
