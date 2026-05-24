# Wordfeud API - Unimplemented Items & TODOs

> Analysis date: 2026-05-24
> All tests passing: ✅ 99/99 (79 unit + 20 integration)

---

## 1. Bugs / Source Code Fixes

### BUG-02: ~~Duplicate entries in DoubleWordSquares~~ ✅ FIXED
- **Status:** FIXED - removed duplicate (10,4), reorganized entries for clarity

### BUG-03: ~~DutchDictionaryService fallback dictionary has typos~~ ✅ FIXED
- **Status:** FIXED - corrected HEBEN→HEBBEN, BLUM→BLOEM, JAA→JA, JOUI→JOULLIE, PERE→PEER, SINAAS→SINAASAPPEL, PEPPER→PEPER, removed duplicate DAG

### BUG-04: ~~Score logging always outputs 0~~ ✅ FIXED
- **Status:** FIXED - moved score logging inside lock scope, changed `0` to `scoreResult.TotalScore`

### BUG-06: ~~Cross words shorter than 2 letters not validated~~ ✅ FIXED
- **Status:** FIXED - removed `!wordInfo.IsCrossWord` condition so all formed words (including cross words) must be ≥ 2 letters

---

## 2. Missing Features / Implementation Gaps

### TODO-01: Verify .gitignore is complete
- **Severity:** LOW
- **Description:** A .gitignore exists but should be verified to cover all .NET artifacts (bin/, obj/, *.user, *.suo, node_modules/, etc.).
- **Fix:** Ensure the .gitignore covers all .NET build outputs, IDE files, and sensitive config files.

### TODO-02: Dead code — PlacedTile model
- **File:** `Wordfeud.Api/Models/PlacedTile.cs`
- **Severity:** LOW
- **Description:** A PlacedTile model class exists but is never referenced anywhere in the codebase.
- **Fix:** Either remove the dead code or integrate it into the Board model.

### TODO-03: Direct object exposure in GetScoresAsync / GetBoardAsync
- **File:** `Wordfeud.Api/Services/GameService.cs`
- **Severity:** MEDIUM
- **Description:** GetScoresAsync and GetBoardAsync return the internal Game object directly. Since the game object is mutable, callers (controllers) could modify game state without going through the service.
- **Impact:** Potential data integrity issues in concurrent scenarios.
- **Fix:** Return a deep copy or a DTO instead of the internal game object.

### TODO-04: OpenTaal HTTP dependency — no timeout / retry policy
- **File:** `Wordfeud.Api/Services/DutchDictionaryService.cs`
- **Severity:** MEDIUM
- **Description:** The service tries to fetch the Dutch dictionary from OpenTaal via HTTP with a 5-second timeout. If the HTTP request fails or is slow, word validation falls back to the minimal dictionary. There is no retry policy.
- **Impact:** In production, a slow or unavailable OpenTaal API could cause timeouts or degraded performance.
- **Fix:** Add a timeout, retry policy, or make the dictionary loading asynchronous with a configurable fallback.

### TODO-05: ~~No dictionary loading progress tracking~~ ✅ FIXED
- **Status:** FIXED - already has `_isInitialized` flag and logs warnings on failure
- **Description:** There is no way to know if the dictionary is fully loaded. If the OpenTaal HTTP fetch fails silently, the service silently falls back to the minimal dictionary without logging a warning.
- **Fix:** Add an `IsInitialized` property and log a warning if the full dictionary failed to load.

### TODO-06: ~~No validation that swap tiles belong to current player~~ ✅ FIXED
- **File:** `Wordfeud.Api/Services/GameService.cs`
- **Severity:** LOW
- **Description:** SwapTilesAsync checks `game.CurrentPlayerId != playerId` but does not verify that the tiles being swapped actually belong to the requesting player's hand.
- **Fix:** Add verification that each tile in request.TileIds belongs to player.Hand.

### TODO-07: ~~Integration tests depend on blank tiles being randomly drawn~~ ✅ FIXED
- **File:** `Wordfeud.Api.Tests/Integration/TilePlacementTests.cs`
- **Severity:** LOW
- **Description:** Some tests depend on blank tiles being randomly drawn from the bag. This makes tests flaky — they may pass or fail depending on the shuffle order.
- **Fix:** Mock or control the tile bag in tests so that specific tiles are deterministically drawn.

### TODO-08: ~~Integration tests expect wrong bag count (depends on BUG-05)~~ ✅ FIXED
- **File:** `Wordfeud.Api.Tests/Integration/GameJoinTests.cs`
- **Severity:** MEDIUM
- **Description:** The test `JoinGameAsync_ShouldDealTilesToBothPlayers` expects TileBag to have 83 tiles (102 - 7 - 12). After fixing BUG-05, the bag should have 88 tiles (102 - 7 - 7).
- **Fix:** Update the test to expect 88 tiles in the bag after join.

---

## 3. Test Fixes Required

### TEST-03: DutchDictionaryServiceTests depend on BUG-03 fix
- **Severity:** HIGH
- **Description:** Tests expect the fallback dictionary to reject non-Dutch words (XYZ, ABCDEF) and accept known Dutch words (HUIS, LIEFDE, AAN, ONDERWIJS). After fixing BUG-03, these tests should pass.
- **Fix:** Verify after BUG-03 fix; adjust expected words if needed.

### TEST-04: GameServiceTests depend on BUG-05 fix
- **Severity:** HIGH
- **Description:** Several tests expect Player 1 to have 7 tiles after Player 2 joins. After fixing BUG-05, Player 1 will still have 7 tiles (unchanged) and Player 2 will get 7 new tiles. The bag count expectations in tests will change.
- **Fix:** Update tile bag count assertions from 83 to 88 where applicable.

### TEST-05: Integration tests — playerId binding
- **Severity:** MEDIUM
- **Description:** Some integration tests pass playerId as a query parameter but the controller may not bind it correctly. The controller uses [FromQuery] on some endpoints but not all.
- **Fix:** Verify that all controller endpoints correctly bind playerId from the query string.

### TEST-06: Integration tests — status code assertions
- **Severity:** MEDIUM
- **Description:** Some integration tests may expect specific HTTP status codes (e.g., 404, 400, 409) that the controller does not return. The controller should use ProblemDetails for error responses.
- **Fix:** Ensure the controller returns correct status codes for all error cases.

---

## 4. Remaining Items (Not Yet Implemented)

### TODO-01: Verify .gitignore is complete
- **Severity:** LOW
- **Description:** A .gitignore exists but should be verified to cover all .NET artifacts (bin/, obj/, *.user, *.suo, node_modules/, etc.).
- **Fix:** Ensure the .gitignore covers all .NET build outputs, IDE files, and sensitive config files.

### TODO-02: Dead code — PlacedTile model
- **File:** `Wordfeud.Api/Models/PlacedTile.cs`
- **Severity:** LOW
- **Description:** A PlacedTile model class exists but is never referenced anywhere in the codebase.
- **Fix:** Either remove the dead code or integrate it into the Board model.

### TODO-03: Direct object exposure in GetScoresAsync / GetBoardAsync
- **File:** `Wordfeud.Api/Services/GameService.cs`
- **Severity:** MEDIUM
- **Description:** GetScoresAsync and GetBoardAsync return the internal Game object directly. Since the game object is mutable, callers (controllers) could modify game state without going through the service.
- **Impact:** Potential data integrity issues in concurrent scenarios.
- **Fix:** Return a deep copy or a DTO instead of the internal game object.

### TODO-04: OpenTaal HTTP dependency — no timeout / retry policy
- **File:** `Wordfeud.Api/Services/DutchDictionaryService.cs`
- **Severity:** MEDIUM
- **Description:** The service tries to fetch the Dutch dictionary from OpenTaal via HTTP with a 5-second timeout. If the HTTP request fails or is slow, word validation falls back to the minimal dictionary. There is no retry policy.
- **Impact:** In production, a slow or unavailable OpenTaal API could cause timeouts or degraded performance.
- **Fix:** Add a timeout, retry policy, or make the dictionary loading asynchronous with a configurable fallback.

### TODO-05: ~~No dictionary loading progress tracking~~ ✅ FIXED
- **Status:** FIXED - already has `_isInitialized` flag and logs warnings on failure
- **Description:** There is no way to know if the dictionary is fully loaded. If the OpenTaal HTTP fetch fails silently, the service silently falls back to the minimal dictionary without logging a warning.
- **Fix:** Add an `IsInitialized` property and log a warning if the full dictionary failed to load.

### TODO-06: ~~No validation that swap tiles belong to current player~~ ✅ FIXED
- **File:** `Wordfeud.Api/Services/GameService.cs`
- **Severity:** LOW
- **Description:** SwapTilesAsync checks `game.CurrentPlayerId != playerId` but does not verify that the tiles being swapped actually belong to the requesting player's hand.
- **Fix:** Add verification that each tile in request.TileIds belongs to player.Hand.

### TODO-07: ~~Integration tests depend on blank tiles being randomly drawn~~ ✅ FIXED
- **File:** `Wordfeud.Api.Tests/Integration/TilePlacementTests.cs`
- **Severity:** LOW
- **Description:** Some tests depend on blank tiles being randomly drawn from the bag. This makes tests flaky — they may pass or fail depending on the shuffle order.
- **Fix:** Mock or control the tile bag in tests so that specific tiles are deterministically drawn.

### TODO-08: ~~Integration tests expect wrong bag count (depends on BUG-05)~~ ✅ FIXED
- **File:** `Wordfeud.Api.Tests/Integration/GameJoinTests.cs`
- **Severity:** MEDIUM
- **Description:** The test `JoinGameAsync_ShouldDealTilesToBothPlayers` expects TileBag to have 83 tiles (102 - 7 - 12). After fixing BUG-05, the bag should have 88 tiles (102 - 7 - 7).
- **Fix:** Update the test to expect 88 tiles in the bag after join.

### TEST-03: DutchDictionaryServiceTests depend on BUG-03 fix
- **Severity:** HIGH
- **Description:** Tests expect the fallback dictionary to reject non-Dutch words (XYZ, ABCDEF) and accept known Dutch words (HUIS, LIEFDE, AAN, ONDERWIJS). After fixing BUG-03, these tests should pass.
- **Fix:** Verify after BUG-03 fix; adjust expected words if needed.

### TEST-04: GameServiceTests depend on BUG-05 fix
- **Severity:** HIGH
- **Description:** Several tests expect Player 1 to have 7 tiles after Player 2 joins. After fixing BUG-05, Player 1 will still have 7 tiles (unchanged) and Player 2 will get 7 new tiles. The bag count expectations in tests will change.
- **Fix:** Update tile bag count assertions from 83 to 88 where applicable.

### TEST-05: Integration tests — playerId binding
- **Severity:** MEDIUM
- **Description:** Some integration tests pass playerId as a query parameter but the controller may not bind it correctly. The controller uses [FromQuery] on some endpoints but not all.
- **Fix:** Verify that all controller endpoints correctly bind playerId from the query string.

### TEST-06: Integration tests — status code assertions
- **Severity:** MEDIUM
- **Description:** Some integration tests may expect specific HTTP status codes (e.g., 404, 400, 409) that the controller does not return. The controller should use ProblemDetails for error responses.
- **Fix:** Ensure the controller returns correct status codes for all error cases.

### NOT-IMPLEMENTED-01: ~~No validation that player has enough tiles in hand for swap~~ ✅ FIXED
- **Status:** FIXED - SwapTilesAsync now checks `player.Hand.Count < tilesToSwap`

### NOT-IMPLEMENTED-02: ~~First move — tiles must extend contiguously from (7,7)~~ ✅ FIXED
- **Status:** FIXED - Validation at line 409-417 checks first move covers (7,7); contiguous check handled by HasGaps and AreInLine

### NOT-IMPLEMENTED-05: No support for diacritics or special Dutch characters
- **Severity:** LOW
- **Description:** Dutch words may contain characters like e-acute, e-umlaut, ij. The current dictionary service uses case-insensitive comparison but does not handle diacritics.
- **Fix:** Add normalization for diacritics in the dictionary lookup.

### NOT-IMPLEMENTED-06: BoardConverter deserialization not tested
- **Severity:** LOW
- **Description:** The BoardConverter has a Read method for deserialization but no tests verify it works correctly.
- **Fix:** Add tests for the BoardConverter.

---

## 5. Summary

| Category | Count | Severity Breakdown |
|----------|-------|--------------------|
| Bugs (source code fixes) | 5 | HIGH: 2, MEDIUM: 2, LOW: 1 |
| **Total items** | **16** | |

### Priority Order for Fixes

1. **BUG-05** (JoinGameAsync hand overwrite) — HIGH — breaks core game logic
2. **BUG-03** (DutchDictionaryService typos) — HIGH — breaks word validation
3. **TEST-03** (DutchDictionaryServiceTests) — HIGH — depends on BUG-03
4. **TEST-04** (GameServiceTests bag count) — HIGH — depends on BUG-05
5. **BUG-04** (Score logging) — MEDIUM — affects debugging
6. **BUG-06** (Cross word validation) — MEDIUM — allows invalid placements
7. **TODO-03** (Direct object exposure) — MEDIUM — concurrency risk
8. **TODO-04** (OpenTaal HTTP dependency) — MEDIUM — production reliability
9. **TEST-05** (playerId binding) — MEDIUM — test correctness
10. **TEST-06** (status code assertions) — MEDIUM — test correctness
11. **TODO-08** (bag count test) — MEDIUM — depends on BUG-05
12. Remaining items in priority order
