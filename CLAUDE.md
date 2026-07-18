# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

"ANTE / MORTEM" — a 48-hour game jam project (Unity 6, `6000.5.2f1`, URP). A blackjack table game where, once the player runs out of money, they start selling their own organs to keep playing. The full design doc is `House Cut.md` (Turkish) — read it before implementing any gameplay/economy feature; section references (e.g. `§03.1`) in commit messages and code comments point back to it. Key sections: §02 core loop, §03 table rules & economy, §04 money=health & organ economy, §05 organ matrix (not yet implemented, deliberately out of GameConfig's scope), §08 technical design / state machine table.

Work happens on branch `meu-milstone1` (never commit directly to `main`/`master`). Issues are tracked in GitHub (`M3UHRZN/House-Cut`) and are the primary unit of work — commit messages are prefixed `issueN: <description>` and closing an issue means the acceptance criteria in `gh issue view N` are met and pushed.

**This project is driven almost entirely through a live Unity Editor via the UnityMCP MCP server**, not by editing files blind. The running Editor instance is bound to this exact working directory — a git worktree copy would NOT be reflected in it, so avoid worktree-based isolation for tasks that need Editor verification (compiling, running tests, scene edits).

## Commands

There is no CLI build/test entrypoint (Unity project) — everything goes through UnityMCP tools against the live Editor:

- **Compile after any script change:** `mcp__UnityMCP__refresh_unity` with `compile: "request"`, then `mcp__UnityMCP__read_console` with `types: ["error"]` to confirm zero errors. `refresh_unity` occasionally returns a spurious "Connection closed" error even though the refresh succeeded — verify via `read_console` or `manage_asset get_info` instead of blindly retrying.
- **Run EditMode tests:** `mcp__UnityMCP__run_tests` with `mode: "EditMode"` (returns a `job_id` immediately, async), then poll with `mcp__UnityMCP__get_test_job` (`wait_timeout: 30` avoids busy-polling). Pass `include_failed_tests: true` to get failure details inline.
- **Scene/GameObject edits:** `mcp__UnityMCP__manage_scene` (hierarchy, save, play/stop via `manage_editor`), `mcp__UnityMCP__manage_gameobject` (create/modify GameObjects), `mcp__UnityMCP__manage_components` (add components, set serialized fields — object references are set by instance ID or `{"guid": "..."}`).
- **ScriptableObject assets:** created via `mcp__UnityMCP__manage_scriptable_object` (NOT `manage_asset create` — that tool only supports Folder/Material/PhysicsMaterial, not arbitrary `[CreateAssetMenu]` types).
- **Play mode smoke check:** `mcp__UnityMCP__manage_editor` (`action: "play"` / `"stop"`) + `read_console` to confirm no runtime errors after scene changes.

No separate lint step exists; compile-clean (0 errors in `read_console`) is the bar.

## Architecture

**Everything shipped so far is backend/logic — there is no playable integration yet.** Systems are built and unit-tested in isolation; nothing wires them together into an actual playable loop (no UI, no Bank/money persistence tied to hand outcomes, `GameFlow` doesn't yet drive `Deck`/`PlayerTurnController`/`HandResolver`). Expect this to change as later issues do the integration work — don't assume a class being tested means it's reachable from gameplay.

### Assembly layout
- `Assets/_Game/Scripts/Game.asmdef` (namespace root `Game`) — all runtime code, recursively covers every subfolder (`Core/`, `Cards/`, `Data/`, and currently-empty scaffold folders `Audio/`, `Economy/`, `Effects/`, `Organs/`, `Presentation/` reserved for future systems).
- `Assets/_Game/Scripts/Editor/Game.Editor.asmdef` — editor-only tooling.
- `Assets/_Game/Tests/EditMode/Game.EditMode.Tests.asmdef` — NUnit EditMode tests (`Game.Tests.EditMode` namespace), references `Game`.

### Core systems (as of issues #1–#9)
- **`Game.Data.GameConfig`** (`Scripts/Data/GameConfig.cs`) — `[CreateAssetMenu]` ScriptableObject holding tuning values transcribed verbatim from the GDD's §03 economy table (`startMoney`, `minBet`, `betStep`, `betRamp`, `decisionTime`, `blackjackPayout`). The single instance lives at `Assets/_Game/Data/GameConfig.asset` and is exposed scene-side via `Game.Core.GameConfigProvider` (a thin `[SerializeField]` MonoBehaviour). Other systems read tuning through this provider rather than hardcoding numbers. Organ-matrix data (§05) is intentionally out of scope here — it belongs to a future per-organ ScriptableObject.
- **`Game.Core.GameFlow`** (`Scripts/Core/GameFlow.cs`) — plain C# state machine (not a MonoBehaviour) over `GameState` (`Intro, BetCheck, Deal, PlayerTurn, DealerTurn, Resolve, NextHand, OrganSale, Extraction, GameOver`), with a static transition table copied verbatim from the GDD §08 state table. Transitions require an explicit `SignalTransitionReady()` before `TryTransition(target)` succeeds (models "no transition without an animation-finished signal", the GDD's anti-softlock rule) — the signal is consumed only on a successful transition, not on a rejected one. `Game.Core.GameFlowRunner` hosts the single scene instance and logs `StateChanged`. **Known gap:** the `PlayerTurn → Deal` transition is in the table verbatim even though it looks wrong for a bust (should arguably route to `Resolve`) — this is deliberate because #4 only built the state-machine skeleton, not bust-routing logic; flagged for whoever wires `HandResolver` into `GameFlow`.
- **`Game.Cards`** (`Scripts/Cards/`) — `Suit`, `Rank`, `Card` (struct with `BaseValue`: 2–10 own value, J/Q/K=10, Ace=11 base), `Deck` (52 unique cards, Fisher-Yates `Shuffle()`, `Draw()` auto-`Reset()`s — full refill + reshuffle — once count drops below `Deck.ReshuffleThreshold` = 15, per GDD §03.1 rule 7), `Hand` (`Total` resolves Ace soft/hard by downgrading 11→1 one Ace at a time while bust, per GDD §03.2; `IsBust`; `IsBlackjack` = exactly 2 cards totalling 21), `DealerRules.ShouldHit(Hand)` (fixed rule: hit while `Total < 17`, stands on soft or hard 17 — GDD §03.5), `HandResolver` (`Resolve` — player bust → dealer wins, dealer bust → player wins, tie → dealer wins with **no push**, per GDD §03.6; `CalculatePayout` — `-bet` on loss, `+bet` on normal win, `+bet * blackjackPayout` on blackjack, using `GameConfig.blackjackPayout`).
- **`Game.Core.PlayerTurnController`** (`Scripts/Core/PlayerTurnController.cs`) — time-agnostic Hit/Stand decision loop driven by `Tick(deltaTime)` rather than Unity's `Update`, so it's directly unit-testable. Auto-`Stand`s when the decision timer expires (GDD §02.3 rule 3: 15s default from `GameConfig.decisionTime`); `Hit()` resets the timer and ends the turn immediately as `Bust` if the hand busts. `Game.Core.PlayerTurnRunner` is the MonoBehaviour host wiring `GameConfigProvider` and ticking via `Update`; its public `Hit()`/`Stand()` are meant for UI button `OnClick` bindings (no actual Canvas/UI exists yet).

### Testing conventions
- Every gameplay rule task ships EditMode tests asserting the exact GDD-specified numbers (not just "works") — e.g. `GameConfigTests.Defaults_MatchGddSpec`, `GameFlowTests.AllowedTransitions_MatchGddTableExactly`. Follow this pattern: pull exact values from `House Cut.md`, don't approximate.
- Time-dependent logic (decision timer, anything that would need Unity's frame loop) is designed as plain C# driven by an explicit `Tick(deltaTime)`/similar seam specifically so it can be unit tested in EditMode without PlayMode tests.
- `Deck` tests seed the `Random` via the constructor (`new Deck(seed: N)`) for determinism.

### Scratch/tooling directories (gitignored)
- `.superpowers/sdd/` — subagent-driven-development progress ledger and per-task briefs/reports/review packages, used when a plan is executed via fresh-subagent-per-task. Not part of the shipped project.
