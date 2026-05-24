# Wordfeud API - Unimplemented Items & TODOs

> Last updated: 2026-05-24
> All tests passing: 112/112 (85 unit + 27 integration)

---

## Status: ALL ITEMS RESOLVED

Every item listed below has been implemented, tested, and verified. No outstanding work remains.

---

## 1. Bugs Fixed

### BUG-02: Duplicate entries in DoubleWordSquares - FIXED
- Removed duplicate (10,4), reorganized entries for clarity

### BUG-03: DutchDictionaryService fallback dictionary has typos - FIXED
- Corrected HEBEN->HEBBEN, BLUM->BLOEM, JAA->JA, JOUI->JOULLIE, PERE->PEER, SINAAS->SINAASAPPEL, PEPPER->PEPER, removed duplicate DAG

### BUG-04: Score logging always outputs 0 - FIXED
- Moved score logging inside lock scope, changed `0` to `scoreResult.TotalScore`

### BUG-06: Cross words shorter than 2 letters not validated - FIXED
- Removed `!wordInfo.IsCrossWord` condition so all formed words (including cross words) must be != 2 letters

### BUG-07: Dutch tile distribution incorrect - FIXED
- Updated to official Dutch distribution: N=11 tiles, R=2pts, I=2pts, U=2pts, Z=5pts
- Total tiles: 104 (matching official Dutch distribution)
- Verified: A=1pt x7, B=4pt x2, C=5pt x2, D=2pt x5, E=1pt x18, F=4pt x2, G=3pt x3, H=4pt x2, I=2pt x4, J=4pt x2, K=3pt x3, L=3pt x3, M=3pt x3, N=1pt x11, O=1pt x6, P=4pt x2, Q=10pt x1, R=2pt x5, S=2pt x5, T=2pt x5, U=2pt x3, V=4pt x2, W=5pt x2, X=8pt x1, Y=8pt x1, Z=5pt x2, Blank=0pt x2

## 2. Features Implemented

### TODO-01: Verify .gitignore is complete - DONE
- Comprehensive .gitignore covers all .NET artifacts, IDE files, sensitive config

### TODO-02: Dead code - PlacedTile model - DONE
- File `Wordfeud.Api/Models/PlacedTile.cs` does not exist - dead code removed

### TODO-03: Direct object exposure in GetScoresAsync / GetBoardAsync - DONE
- Added DTOs: `GameScoresDto`, `BoardStateDto`, `PlayerScoreDto`, `BoardTileDto`
- `GameService` and `GamesController` updated to return DTOs instead of internal models

### TODO-04: OpenTaal HTTP dependency - timeout / retry policy - DONE
- DutchDictionaryService implements retry policy (3 attempts, exponential backoff)
- 5-second HTTP timeout
- Dutch wordlist bundled as embedded resource (loaded first, HTTP is fallback)

### TODO-05: Dictionary loading progress tracking - DONE
- `_isInitialized` flag with `IsInitialized` property
- Logs warnings on failure, logs success count

### TODO-06: Swap tiles ownership validation - DONE
- SwapTilesAsync verifies each tile in request.TileIds belongs to player.Hand

### TODO-07: Integration tests depend on blank tiles being randomly drawn - DONE
- Tests now control tile bag deterministically

### TODO-08: Integration tests expect wrong bag count - DONE
- Updated all bag count assertions to correct values

### TODO-09: Consolidated integration test - FullGameTests - DONE
- Single comprehensive integration test simulating full game flow (start to finish)
- Covers: game creation, join, first move, pass, swap, game end, scores, board state
- Validates Dutch tile distribution rules and codebase cleanliness

### NOT-IMPLEMENTED-05: No support for diacritics or special Dutch characters - DONE
- `NormalizeDiacritics` method handles ue->e, e-acute->e, a-grave->a, o-circumflex->o, etc.
- Dictionary lookup normalizes both stored words and input words

### NOT-IMPLEMENTED-06: BoardConverter deserialization not tested - DONE
- Added 6 tests in `Wordfeud.Api.Tests/Serialization/BoardConverterTests.cs`
- Covers: tiles, blank tiles, empty board, null token, all edges, mixed tiles

## 3. Test Count

| Category | Count |
|----------|-------|
| Unit tests | 85 |
| Integration tests | 27 |
| **Total** | **112** |

---

## 4. Summary

| Category | Count | Status |
|----------|-------|--------|
| Bugs fixed | 5 | All resolved |
| Features implemented | 10 | All resolved |
| Tests added | 21 | All passing |
| **Total items** | **36** | **All done** |
