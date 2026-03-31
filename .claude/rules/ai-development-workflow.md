---
paths:
  - "**"
---

# AI-Driven Development Workflow

This project is built with AI-driven development — minimal human interaction. Architecture choices maximize what AI agents can generate reliably.

## Subagent Model Assignments

When dispatching subagents for implementation tasks:

| Complexity | Model | Examples |
|-----------|-------|---------|
| Mechanical | Sonnet | Scaffolding, enums, SOs, simple UI, world scripts |
| Standard | Sonnet | Player systems, input wiring, scene transitions |
| Complex | Opus | Enemy composition, boss fights, scene assembly, polish, integration |
| Reviews (mechanical) | Haiku | Spec + quality reviews for simple tasks |
| Reviews (complex) | Sonnet | Reviews for multi-file or design-heavy tasks |

Escalate model if an agent gets stuck rather than retrying the same model.

## Task Splitting

Large tasks (250+ lines of plan text, 3+ files) should be split before dispatch:
- Split by file grouping — keep tightly coupled files together
- Each subtask should be completable by a single agent in one pass
- Create stubs for forward references (types not yet implemented) so code compiles

## Agent Types

- **Implementation:** `game-development:unity-developer` — specialized for Unity C#, URP, asset patterns
- **Reviews:** `superpowers:code-reviewer` — spec compliance and code quality
- **All implementers** follow `superpowers:test-driven-development` (red-green-refactor)

## Content Creation

- **Art:** AI-generated sprites and assets (3/4 top-down perspective, dark desaturated palette)
- **Data:** All game content defined as ScriptableObjects for easy AI generation
- **One class per file** — keeps agent context focused and diffs clean
