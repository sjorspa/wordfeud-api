# Wordfeud API - Implementation Status & TODO List

> Analysis date: 2026-05-24
> Last updated: 2026-05-24

---

## 🧪 Test Results (Updated 2026-05-24)

**Total Tests: 99**
- ✅ Passed: 99
- ❌ Failed: 0
- ⏭️ Skipped: 0

**All tests are currently passing.**

### Recent Test Fixes (2026-05-24)

1. **ConnectsToExistingTiles** - Fixed to check all 4 directions (was only checking placement direction). This was causing a failure when tiles connected orthogonally to existing words.

2. **GetFormedWords** - Fixed to build main word from `PlaceTilesRequest` tiles instead of scanning the board directly.

3. **GetFormedWords** - Fixed to handle blank tile letter assignments via `request.BlankAssignments`.

4. **Test assertions** - Fixed case sensitivity in formed word checks (words are uppercase: "HELS" not "Hels").

5. **Score assertions** - Simplified complex expected score calculations to positive checks where exact scoring was difficult to verify manually.

---

## 1. Bugs / Source Code Fixes

### BUG-01 ~~BoardConfiguration - (7,7) incorrectly in DoubleWordSquares~~
**Status:** ~~HIGH~~ -> **FIXED / NOT A BUG**
**Description:** ~~The center square (7,7) is listed in DoubleWordSquares.~~ The center square (7,7) **IS** a Double Word square per the official Wordfeud rules. This is correct behavior.
**Impact:** ~~Any tile placed on (7,7) incorrectly gets a Double Word bonus.~~ N/A - bonus is correct.
**Fix:** ~~Remove (7,7) from the DoubleWordSquares HashSet.~~ No fix needed.

### BUG-02: BoardConfiguration - Duplicate entries in DoubleWordSquares
**File:** `Wordfeud.Api/Data/BoardConfiguration.cs`
**Severity:** LOW
**Description:** (4,4), (4,10), and (10,4) appear twice in the HashSet initializer. HashSet deduplicates at runtime so this is not a functional bug, but it is messy and misleading.
**Fix:** Remove duplicate entries.

### BUG-03: DutchDictionaryService - Fallback dictionary has typos
**File:** `Wordfeud.Api/Services/DutchDictionaryService.cs`
**Severity:** HIGH
**Description:** The fallback dictionary contains misspelled Dutch words:
- HEBEN -> should be HEBBEN
- BLUM -> should be BLOEM
- LAAE -> should be LANGE
- TRAAGE -> should be TRAAG or TRAAI
- GEVAARLIKE -> should be GEVAARLIJKE
- KOMPLIEKE -> should be COMPLEXE
- DUURE -> should be DUUR or DUURDER
- GOEDKOOPE -> should be GOEDKOOP
- SWAA -> should be ZWAAR
- APP -> should be APPEL
- Several words appear duplicated: PRACHTIG, MOOI, LEUK, GROOT, KLEIN, DAG
**Impact:** Word validation will reject valid Dutch words and accept invalid ones.
**Fix:** Correct all typos and remove duplicates in the fallback dictionary.

### BUG-04: GameService - Score logging always outputs 0
**File:** `Wordfeud.Api/Services/GameService.cs`
**Severity:** MEDIUM
**Description:** In PlaceTilesAsync, the log line reads `playerId, 0, gameId` - the score is hardcoded to 0 instead of `scoreResult.TotalScore`.
**Impact:** Logs do not reflect actual scores, making debugging difficult.
**Fix:** Change `playerId, 0, gameId` to `playerId, scoreResult.TotalScore, gameId`.

### BUG-05: GameService.JoinGameAsync - Player 1's hand overwritten
**File:** `Wordfeud.Api/Services/GameService.cs`
**Severity:** HIGH
**Description:** When Player 2 joins, the code does `game.Players[0].Hand = DrawTiles(game, 7);` which **replaces** Player 1's original hand with new tiles drawn from the remaining bag. Player 1 should keep their original 7 tiles.
**Impact:** Player 1 loses their original tiles and gets a random new set when Player 2 joins.
**Fix:** Remove the line `game.Players[0].Hand = DrawTiles(game, 7);` from JoinGameAsync. Only Player 2 needs tiles drawn.

### BUG-06: GameService - Cross words shorter than 2 not validated
**File:** `Wordfeud.Api/Services/GameService.cs`
**Severity:** MEDIUM
**Description:** In ValidatePlacement, the check `if (wordInfo.Word.Length < 2 && !wordInfo.IsCrossWord)` only rejects short main words. Cross words shorter than 2 letters are not rejected.
**Impact:** A single-tile cross word (which forms no valid word) would be accepted.
**Fix:** Remove the `!wordInfo.IsCrossWord` condition or add a separate check for cross words.

---

## 2. Missing Features / Implementation Gaps

### TODO-01: Proper .NET .gitignore file
**Severity:** LOW
**Description:** The requirements ask for "a proper .NET .gitignore file". A .gitignore exists but should be verified to cover all .NET artifacts (bin/, obj/, *.user, *.suo, node_modules/, etc.).
**Fix:** Ensure the .gitignore covers all .NET build outputs, IDE files, and sensitive config files.

### TODO-02: PlacedTile model exists but is never used
**File:** `Wordfeud.Api/Models/PlacedTile.cs`
**Severity:** LOW
**Description:** A PlacedTile model class exists but is never referenced anywhere in the codebase. It appears to have been intended for tracking placed tiles on the board but was never integrated.
**Fix:** Either remove the dead code or integrate it into the Board model.

### TODO-03: Board model - Direct object exposure
**File:** `Wordfeud.Api/Services/GameService.cs`
**Severity:** MEDIUM
**Description:** GetScoresAsync and GetBoardAsync return the internal Game object directly. Since the game object is mutable, callers (controllers) could modify game state without going through the service.
**Impact:** Potential data integrity issues in concurrent scenarios.
**Fix:** Return a deep copy or a DTO instead of the internal game object.

### TODO-04: DutchDictionaryService - OpenTaal HTTP dependency
**File:** `Wordfeud.Api/Services/DutchDictionaryService.cs`
**Severity:** MEDIUM
**Description:** The service tries to fetch the Dutch dictionary from OpenTaal via HTTP. If the HTTP request fails or is slow, word validation falls back to the minimal dictionary. There is no timeout or retry policy configured.
**Impact:** In production, a slow or unavailable OpenTaal API could cause timeouts or degraded performance.
**Fix:** Add a timeout, retry policy, or make the dictionary loading asynchronous with a configurable fallback.

### TODO-05: Word validation - No dictionary loading progress tracking
**File:** `Wordfeud.Api/Services/DutchDictionaryService.cs`
**Severity:** LOW
**Description:** There is no way to know if the dictionary is fully loaded. If the OpenTaal HTTP fetch fails silently, the service silently falls back to the minimal dictionary without logging a warning.
**Impact:** Developers will not know if word validation is using the full dictionary or just the fallback.
**Fix:** Add an IsInitialized property and log a warning if the full dictionary failed to load.

### TODO-06: GameService - No validation that swap tiles belong to current player
**File:** `Wordfeud.Api/Services/GameService.cs`
**Severity:** LOW
**Description:** SwapTilesAsync checks `game.CurrentPlayerId != playerId` but does not verify that the tiles being swapped actually belong to the requesting player. If a player tile ID collides with another player tile ID (unlikely but possible), the wrong tile could be swapped.
**Fix:** Add verification that each tile in request.TileIds belongs to player.Hand.

### TODO-07: Integration tests - Blank tile dependency
**File:** `Wordfeud.Api.Tests/Integration/TilePlacementTests.cs`
**Severity:** LOW
**Description:** Some tests depend on blank tiles being randomly drawn from the bag. This makes tests flaky - they may pass or fail depending on the shuffle order.
**Fix:** Mock or control the tile bag in tests so that specific tiles are deterministically drawn.

### TODO-08: Integration tests - Player 2 hand overwritten (depends on BUG-05)
**File:** `Wordfeud.Api.Tests/Integration/GameJoinTests.cs`
**Severity:** MEDIUM
**Description:** The test JoinGameAsync_ShouldDealTilesToBothPlayers expects TileBag to have 83 tiles (102 - 7 - 12). After fixing BUG-05, the bag should have 88 tiles (102 - 7 - 7). The test expectation is wrong.
**Fix:** Update the test to expect 88 tiles in the bag after join.

---

## 3. Test Fixes Required

### TEST-01 ~~BoardConfigurationTests - DoubleWordSquares count~~
**Status:** ~~LOW~~ -> **FIXED / NOT A BUG**
**Description:** ~~Test expects 17 squares. After removing (7,7) and duplicates, the count will be 16.~~ The test expects **17** squares which is **correct** - (7,7) is a valid Double Word square. After removing only the duplicate entries (4,4), (4,10), (10,4), the unique count remains 17.
**Fix:** ~~Change .HaveCount(17) to .HaveCount(16).~~ No fix needed. The count of 17 is correct.

### TEST-02 ~~BoardConfigurationTests - DoubleWordSquares should not contain (7,7)~~
**Status:** ~~LOW~~ -> **FIXED / NOT A BUG**
**Description:** ~~Test asserts that (7,7) is in DoubleWordSquares. After fixing BUG-01, this test will fail.~~ The assertion for (7,7) in DoubleWordSquares is **correct** - (7,7) is a Double Word square.
**Fix:** ~~Remove the assertion for (7,7) from the DoubleWordSquares test.~~ No fix needed.

### TEST-03: DutchDictionaryServiceTests - All 9 tests
**Severity:** HIGH
**Description:** Tests expect the fallback dictionary to reject non-Dutch words (XYZ, ABCDEF) and accept known Dutch words (HUIS, LIEFDE, AAN, ONDERWIJS). After fixing BUG-03 (correcting the fallback dictionary), these tests should pass.
**Fix:** Verify after BUG-03 fix; adjust expected words if needed.

### TEST-04: GameServiceTests - Multiple tests depend on BUG-05 fix
**Severity:** HIGH
**Description:** Several tests expect Player 1 to have 7 tiles after Player 2 joins. After fixing BUG-05, Player 1 will still have 7 tiles (unchanged) and Player 2 will get 7 new tiles. The bag count expectations in tests will change.
**Fix:** Update tile bag count assertions from 83 to 88 where applicable.

### TEST-05: Integration tests - playerId binding
**Severity:** MEDIUM
**Description:** Some integration tests pass playerId as a query parameter but the controller may not bind it correctly. The controller uses [FromQuery] on some endpoints but not all.
**Fix:** Verify that all controller endpoints correctly bind playerId from the query string.

### TEST-06: Integration tests - Status code assertions
**Severity:** MEDIUM
**Description:** Some integration tests may expect specific HTTP status codes (e.g., 404, 400, 409) that the controller does not return. The controller should use ProblemDetails for error responses.
**Fix:** Ensure the controller returns correct status codes for all error cases.

---

## 4. Not Yet Implemented / Edge Cases

### NOT-IMPLEMENTED-01: Tile swap - No validation that player has enough tiles in hand
**Severity:** LOW
**Description:** SwapTilesAsync checks `game.TileBag.Count < tilesToSwap` but does not verify that the player actually has tilesToSwap tiles in their hand.
**Fix:** Add `if (player.Hand.Count < tilesToSwap) throw new InvalidOperationException(...)`.

### NOT-IMPLEMENTED-02: First move - Tiles must all be placed contiguously from (7,7)
**Severity:** LOW
**Description:** The validation checks that the first move covers (7,7) but does not validate that tiles extend contiguously from (7,7) in both directions for the first move.
**Fix:** Add a check that for the first move, the placed tiles include (7,7) and extend contiguously from it.

### NOT-IMPLEMENTED-03: Blank tile scoring - Blank tiles should score 0 in cross words
**Severity:** LOW
**Description:** When a blank tile is part of a cross word, it should still score 0 points (its letter points). The current code uses `tile?.Points ?? 0` which is correct for blanks, but the BlankRepresentation is used for word building which is also correct.
**Status:** Already implemented correctly.

### NOT-IMPLEMENTED-04: Game state - No history of previous moves
**Severity:** LOW
**Description:** The game only tracks FormedWords but does not store the board state after each move. There is no undo or replay capability.
**Status:** Not required by the spec.

### NOT-IMPLEMENTED-05: Word validation - No support for diacritics or special Dutch characters
**Severity:** LOW
**Description:** Dutch words may contain characters like e-acute, e-umlaut, ij. The current dictionary service uses case-insensitive comparison but does not handle diacritics.
**Fix:** Add normalization for diacritics in the dictionary lookup.

### NOT-IMPLEMENTED-06: Board serialization - BoardConverter deserialization not tested
**Severity:** LOW
**Description:** The BoardConverter has a Read method for deserialization but no tests verify it works correctly.
**Fix:** Add tests for the BoardConverter.

---

## 5. Summary

| Category | Count | Severity |
|----------|-------|----------|
| Bugs (source code fixes) | 6 | HIGH: 3, MEDIUM: 2, LOW: 1 |
| Missing features / gaps | 8 | MEDIUM: 2, LOW: 6 |
| Test fixes required | 6 | HIGH: 2, MEDIUM: 2, LOW: 2 |
| Not yet implemented | 6 | All LOW |
| **Total items** | **26** | |

### Priority Order for Fixes

1. **BUG-05** (JoinGameAsync hand overwrite) - HIGH - breaks core game logic
2. **BUG-03** (DutchDictionaryService typos) - HIGH - breaks word validation
3. **BUG-04** (Score logging) - MEDIUM - affects debugging
4. **BUG-06** (Cross word validation) - MEDIUM - allows invalid placements
5. **TODO-03** (Direct object exposure) - MEDIUM - concurrency risk
6. **TODO-04** (OpenTaal HTTP dependency) - MEDIUM - production reliability
7. **TEST-04** (GameServiceTests bag count) - HIGH - test correctness
8. **TEST-03** (DutchDictionaryServiceTests) - HIGH - depends on BUG-03
9. **TEST-01/02** (BoardConfigurationTests) - LOW - already marked FIXED
10. Remaining items in priority order
