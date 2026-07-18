# GameFlow State Machine Skeleton Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the deterministic `GameFlow` state machine skeleton (GitHub issue #4) that will drive the whole run: 10 named states, a hard-coded transition table, and a "signal gate" that blocks moving to the next state until the current state explicitly signals it's done — this is what prevents the double-click soft-locks the GDD calls out.

**Architecture:** A plain (non-MonoBehaviour) C# class `GameFlow` in `Game.Core` owns `CurrentState`, a static `Dictionary<GameState, GameState[]>` transition table, and a `signalReady` gate flag. `TryTransition(target)` only succeeds if `signalReady` is true AND `target` is in the allowed-transitions list for `CurrentState`; on success it flips `signalReady` back to `false` and fires a `StateChanged` event. Plain C# (no `MonoBehaviour`) so it's testable in EditMode without touching a scene, matching how `GameConfig` was tested in issue #3. A thin `GameFlowRunner` MonoBehaviour wires it into the `Table` scene for visibility/logging, mirroring the `GameConfigProvider` pattern already in the project.

**Tech Stack:** Unity 6 / C#, `com.unity.test-framework` (NUnit) EditMode tests, existing `Game.asmdef` / `Game.EditMode.Tests` assemblies from issues #2–#3.

## Global Constraints

- States are exactly these 10, spelled as C# PascalCase enum members mapping 1:1 to the GDD's SCREAMING_SNAKE_CASE names: `Intro`, `BetCheck`, `Deal`, `PlayerTurn`, `DealerTurn`, `Resolve`, `NextHand`, `OrganSale`, `Extraction`, `GameOver`. (GDD `House Cut.md` §08, state table.)
- Transition table is taken verbatim from the GDD §08 state table's "ÇIKIŞ" (exit) column — do not invent or "fix" transitions that look wrong (e.g. `PlayerTurn → Deal` is listed in the GDD even though it looks like it should be `Resolve`; implement it as written, this skeleton doesn't yet encode bust/stand logic):
  - `Intro → BetCheck`
  - `BetCheck → Deal | OrganSale | GameOver`
  - `OrganSale → Extraction`
  - `Extraction → BetCheck | GameOver`
  - `Deal → PlayerTurn`
  - `PlayerTurn → Deal | DealerTurn`
  - `DealerTurn → Resolve`
  - `Resolve → NextHand`
  - `NextHand → BetCheck`
  - `GameOver → Intro`
- No transition may occur without an explicit "ready" signal first (`SignalTransitionReady()`); entering any state resets the gate to not-ready. This is the "animasyon bitti sinyali olmadan geçiş yok" rule from the GDD.
- New code lives under the existing `Game` assembly (`Assets/_Game/Scripts/Core/`) and `Game.EditMode.Tests` assembly (`Assets/_Game/Tests/EditMode/`) created in issues #2–#3. Do not create new asmdefs.
- Run tests via the Unity MCP test runner (`mcp__UnityMCP__run_tests`, `mode: EditMode`, `assembly_names: ["Game.EditMode.Tests"]`), not a CLI test runner — there is no `pytest`/`dotnet test` in this repo, Unity owns compilation.

---

### Task 1: `GameState` enum

**Files:**
- Create: `Assets/_Game/Scripts/Core/GameState.cs`

**Interfaces:**
- Produces: `namespace Game.Core { public enum GameState { Intro, BetCheck, Deal, PlayerTurn, DealerTurn, Resolve, NextHand, OrganSale, Extraction, GameOver } }` — consumed by every later task.

- [ ] **Step 1: Write the enum**

```csharp
namespace Game.Core
{
    public enum GameState
    {
        Intro,
        BetCheck,
        Deal,
        PlayerTurn,
        DealerTurn,
        Resolve,
        NextHand,
        OrganSale,
        Extraction,
        GameOver
    }
}
```

- [ ] **Step 2: Refresh Unity and confirm no compile errors**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console` with `types: ["error"]`.
Expected: 0 error entries.

- [ ] **Step 3: Commit**

```bash
git add "Assets/_Game/Scripts/Core/GameState.cs" "Assets/_Game/Scripts/Core/GameState.cs.meta"
git commit -m "issue4: add GameState enum"
```

---

### Task 2: `GameFlow` transition table + signal gate (TDD)

**Files:**
- Create: `Assets/_Game/Scripts/Core/GameFlow.cs`
- Test: `Assets/_Game/Tests/EditMode/GameFlowTests.cs`

**Interfaces:**
- Consumes: `Game.Core.GameState` (Task 1).
- Produces:
  - `namespace Game.Core { public class GameFlow { public GameState CurrentState { get; } public bool SignalReady { get; } public event Action<GameState, GameState> StateChanged; public void SignalTransitionReady(); public bool TryTransition(GameState target); public static IReadOnlyList<GameState> GetAllowedTransitions(GameState from); } }`
  - `GetAllowedTransitions` and the transition table are consumed by Task 3's exhaustive table test and by Task 4's `GameFlowRunner`.

- [ ] **Step 1: Write the failing tests**

```csharp
using System;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameFlowTests
    {
        [Test]
        public void StartsInIntro()
        {
            var flow = new GameFlow();
            Assert.AreEqual(GameState.Intro, flow.CurrentState);
        }

        [Test]
        public void TryTransition_WithoutSignal_Fails()
        {
            var flow = new GameFlow();
            bool result = flow.TryTransition(GameState.BetCheck);

            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Intro, flow.CurrentState);
        }

        [Test]
        public void TryTransition_AfterSignal_ToAllowedState_Succeeds()
        {
            var flow = new GameFlow();
            flow.SignalTransitionReady();

            bool result = flow.TryTransition(GameState.BetCheck);

            Assert.IsTrue(result);
            Assert.AreEqual(GameState.BetCheck, flow.CurrentState);
        }

        [Test]
        public void TryTransition_ToDisallowedState_Fails()
        {
            var flow = new GameFlow();
            flow.SignalTransitionReady();

            bool result = flow.TryTransition(GameState.DealerTurn);

            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Intro, flow.CurrentState);
        }

        [Test]
        public void TryTransition_ConsumesSignal_SoDoubleClickIsBlocked()
        {
            var flow = new GameFlow();
            flow.SignalTransitionReady();
            flow.TryTransition(GameState.BetCheck);

            bool secondAttempt = flow.TryTransition(GameState.Deal);

            Assert.IsFalse(secondAttempt, "second transition must be blocked until a new signal arrives");
            Assert.AreEqual(GameState.BetCheck, flow.CurrentState);
        }

        [Test]
        public void TryTransition_RaisesStateChanged_WithOldAndNewState()
        {
            var flow = new GameFlow();
            GameState observedOld = default;
            GameState observedNew = default;
            flow.StateChanged += (oldState, newState) =>
            {
                observedOld = oldState;
                observedNew = newState;
            };

            flow.SignalTransitionReady();
            flow.TryTransition(GameState.BetCheck);

            Assert.AreEqual(GameState.Intro, observedOld);
            Assert.AreEqual(GameState.BetCheck, observedNew);
        }
    }
}
```

- [ ] **Step 2: Run tests to verify they fail to compile (GameFlow doesn't exist yet)**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console` with `types: ["error"]`.
Expected: compile errors mentioning `GameFlow` not found.

- [ ] **Step 3: Write the implementation**

```csharp
using System;
using System.Collections.Generic;

namespace Game.Core
{
    public class GameFlow
    {
        private static readonly Dictionary<GameState, GameState[]> Transitions = new()
        {
            { GameState.Intro, new[] { GameState.BetCheck } },
            { GameState.BetCheck, new[] { GameState.Deal, GameState.OrganSale, GameState.GameOver } },
            { GameState.OrganSale, new[] { GameState.Extraction } },
            { GameState.Extraction, new[] { GameState.BetCheck, GameState.GameOver } },
            { GameState.Deal, new[] { GameState.PlayerTurn } },
            { GameState.PlayerTurn, new[] { GameState.Deal, GameState.DealerTurn } },
            { GameState.DealerTurn, new[] { GameState.Resolve } },
            { GameState.Resolve, new[] { GameState.NextHand } },
            { GameState.NextHand, new[] { GameState.BetCheck } },
            { GameState.GameOver, new[] { GameState.Intro } },
        };

        public GameState CurrentState { get; private set; } = GameState.Intro;
        public bool SignalReady { get; private set; }

        public event Action<GameState, GameState> StateChanged;

        public void SignalTransitionReady()
        {
            SignalReady = true;
        }

        public bool TryTransition(GameState target)
        {
            if (!SignalReady)
            {
                return false;
            }

            if (!IsAllowed(CurrentState, target))
            {
                return false;
            }

            var previous = CurrentState;
            CurrentState = target;
            SignalReady = false;
            StateChanged?.Invoke(previous, target);
            return true;
        }

        public static IReadOnlyList<GameState> GetAllowedTransitions(GameState from)
        {
            return Transitions[from];
        }

        private static bool IsAllowed(GameState from, GameState to)
        {
            return Array.IndexOf(Transitions[from], to) >= 0;
        }
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Call `mcp__UnityMCP__run_tests` with `mode: "EditMode"`, `assembly_names: ["Game.EditMode.Tests"]`, `include_failed_tests: true`, then poll `mcp__UnityMCP__get_test_job` until `status: "succeeded"`.
Expected: all 6 new tests pass (plus the 2 existing `GameConfigTests`), `failed: 0`.

- [ ] **Step 5: Commit**

```bash
git add "Assets/_Game/Scripts/Core/GameFlow.cs" "Assets/_Game/Scripts/Core/GameFlow.cs.meta" \
        "Assets/_Game/Tests/EditMode/GameFlowTests.cs" "Assets/_Game/Tests/EditMode/GameFlowTests.cs.meta"
git commit -m "issue4: add GameFlow transition table with signal-gated transitions"
```

---

### Task 3: Exhaustive transition-table test (full GDD coverage)

**Files:**
- Modify: `Assets/_Game/Tests/EditMode/GameFlowTests.cs` (append to the `GameFlowTests` class from Task 2)

**Interfaces:**
- Consumes: `Game.Core.GameFlow.GetAllowedTransitions(GameState)` (Task 2).
- Produces: nothing new — this task only adds test coverage so every row of the GDD table in "Global Constraints" is checked by name, not just the four states Task 2 happened to exercise.

- [ ] **Step 1: Write the failing/pending test**

Append this test method inside the existing `GameFlowTests` class (same file as Task 2):

```csharp
        [Test]
        public void AllowedTransitions_MatchGddTableExactly()
        {
            var expected = new Dictionary<GameState, GameState[]>
            {
                { GameState.Intro, new[] { GameState.BetCheck } },
                { GameState.BetCheck, new[] { GameState.Deal, GameState.OrganSale, GameState.GameOver } },
                { GameState.OrganSale, new[] { GameState.Extraction } },
                { GameState.Extraction, new[] { GameState.BetCheck, GameState.GameOver } },
                { GameState.Deal, new[] { GameState.PlayerTurn } },
                { GameState.PlayerTurn, new[] { GameState.Deal, GameState.DealerTurn } },
                { GameState.DealerTurn, new[] { GameState.Resolve } },
                { GameState.Resolve, new[] { GameState.NextHand } },
                { GameState.NextHand, new[] { GameState.BetCheck } },
                { GameState.GameOver, new[] { GameState.Intro } },
            };

            foreach (var pair in expected)
            {
                CollectionAssert.AreEquivalent(
                    pair.Value,
                    GameFlow.GetAllowedTransitions(pair.Key),
                    $"transitions for {pair.Key} did not match the GDD table");
            }
        }

        [Test]
        public void EveryState_CanReachGameOverOrLoopsBackToIntro()
        {
            // Sanity check: no state is a dead end that can never progress.
            foreach (GameState state in Enum.GetValues(typeof(GameState)))
            {
                var allowed = GameFlow.GetAllowedTransitions(state);
                Assert.IsNotEmpty(allowed, $"{state} has no outgoing transitions — dead end");
            }
        }
```

Add `using System.Collections.Generic;` to the top of the file if not already present from Task 2.

- [ ] **Step 2: Run tests to verify they fail**

This step is expected to already pass if Task 2's table matches — run it anyway to confirm the test itself is wired correctly before trusting it:
Call `mcp__UnityMCP__run_tests` with `mode: "EditMode"`, `assembly_names: ["Game.EditMode.Tests"]`, `test_names: ["Game.Tests.EditMode.GameFlowTests.AllowedTransitions_MatchGddTableExactly", "Game.Tests.EditMode.GameFlowTests.EveryState_CanReachGameOverOrLoopsBackToIntro"]`.
Expected: PASS (table was already correct from Task 2 — this test exists to catch future accidental edits to the table, not to drive new implementation).

- [ ] **Step 3: Run the full EditMode suite**

Call `mcp__UnityMCP__run_tests` with `mode: "EditMode"`, `assembly_names: ["Game.EditMode.Tests"]`, `include_failed_tests: true`, poll via `mcp__UnityMCP__get_test_job`.
Expected: `failed: 0`, total test count = 10 (2 `GameConfigTests` + 8 `GameFlowTests`).

- [ ] **Step 4: Commit**

```bash
git add "Assets/_Game/Tests/EditMode/GameFlowTests.cs"
git commit -m "issue4: exhaustive transition-table test covering all 10 GDD states"
```

---

### Task 4: `GameFlowRunner` — scene wiring for visibility

**Files:**
- Create: `Assets/_Game/Scripts/Core/GameFlowRunner.cs`
- Modify (via Unity MCP, not text edit): `Assets/_Game/Scenes/Table.unity` — add a `GameFlowRunner` component to a new `GameFlow` GameObject.

**Interfaces:**
- Consumes: `Game.Core.GameFlow` (Task 2).
- Produces: nothing consumed by later plan tasks — this is a leaf/integration task, matching the `GameConfigProvider` pattern already in the scene from issue #3.

- [ ] **Step 1: Write the MonoBehaviour**

```csharp
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Scene-visible host for the single GameFlow instance. Later issues (Bank,
    /// Deck, Dealer, Presentation) will hook into GameFlow.StateChanged instead
    /// of talking to each other directly.
    /// </summary>
    public class GameFlowRunner : MonoBehaviour
    {
        public GameFlow Flow { get; private set; }

        private void Awake()
        {
            Flow = new GameFlow();
            Flow.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            Debug.Log($"GameFlow: {previous} -> {next}");
        }

        private void OnDestroy()
        {
            if (Flow != null)
            {
                Flow.StateChanged -= OnStateChanged;
            }
        }
    }
}
```

- [ ] **Step 2: Refresh Unity and confirm no compile errors**

Call `mcp__UnityMCP__refresh_unity`, then `mcp__UnityMCP__read_console` with `types: ["error"]`.
Expected: 0 error entries.

- [ ] **Step 3: Add the GameObject + component to the Table scene**

Use Unity MCP tools (Table scene must be the active/loaded scene first — check with `mcp__UnityMCP__manage_scene` `action: "get_loaded_scenes"`):
- `mcp__UnityMCP__manage_gameobject` `action: "create"`, `name: "GameFlow"`
- `mcp__UnityMCP__manage_components` `action: "add"`, `target: "GameFlow"`, `search_method: "by_name"`, `component_type: "Game.Core.GameFlowRunner"`

- [ ] **Step 4: Verify in Play mode**

- `mcp__UnityMCP__manage_editor` `action: "play"`
- `mcp__UnityMCP__read_console` `action: "get"`, `filter_text: "GameFlow"` — expected: no log yet (no transition has happened, `Awake` doesn't log by itself)
- `mcp__UnityMCP__manage_editor` `action: "stop"`

- [ ] **Step 5: Save the scene**

`mcp__UnityMCP__manage_scene` `action: "save"`

- [ ] **Step 6: Commit**

```bash
git add "Assets/_Game/Scripts/Core/GameFlowRunner.cs" "Assets/_Game/Scripts/Core/GameFlowRunner.cs.meta" \
        "Assets/_Game/Scenes/Table.unity"
git commit -m "issue4: wire GameFlowRunner into Table scene"
```

---

### Task 5: Final verification + close issue #4

**Files:** none (verification only)

- [ ] **Step 1: Run the full EditMode suite one more time**

Call `mcp__UnityMCP__run_tests` with `mode: "EditMode"`, `assembly_names: ["Game.EditMode.Tests"]`, `include_failed_tests: true`.
Expected: `failed: 0`, total 10 tests.

- [ ] **Step 2: Confirm zero console errors/warnings**

`mcp__UnityMCP__read_console` `action: "get"`, `types: ["error", "warning"]`.
Expected: 0 entries (or only pre-existing unrelated ones — compare against the baseline from before Task 1).

- [ ] **Step 3: Close GitHub issue #4**

```bash
gh issue close 4 --comment "GameFlow eklendi: 10 state (Intro/BetCheck/Deal/PlayerTurn/DealerTurn/Resolve/NextHand/OrganSale/Extraction/GameOver), GDD §08 geçiş tablosuyla birebir, sinyal kapısı (SignalTransitionReady + TryTransition) çift-tık soft-lock'u engelliyor. GameFlowRunner Table sahnesine bağlandı. 10 EditMode testi geçiyor. Commit: <fill in latest commit hash>"
```
