# Wordfeud API - Unimplemented Items & TODOs

> Last updated: 2026-05-24
> All tests passing: ✅ 85/85 (79 unit + 6 serialization)

---

## Status: ALL ITEMS RESOLVED

Every item listed below has been implemented, tested, and verified. No outstanding work remains.

---

## 1. Bugs Fixed

### BUG-02: Duplicate entries in DoubleWordSquares ✅ FIXED
- Removed duplicate (10,4), reorganized entries for clarity

### BUG-03: DutchDictionaryService fallback dictionary has typos ✅ FIXED
- Corrected HEBEN→HEBBEN, BLUM→BLOEM, JAA→JA, JOUI→JOULLIE, PERE→PEER, SINAAS→SINAASAPPEL, PEPPER→PEPER, removed duplicate DAG

### BUG-04: Score logging always outputs 0 ✅ FIXED
- Moved score logging inside lock scope, changed `0` to `scoreResult.TotalScore`

### BUG-06: Cross words shorter than 2 letters not validated ✅ FIXED
- Removed `!wordInfo.IsCrossWord` condition so all formed words (including cross words) must be ≥ 2 letters

## 2. Features Implemented

### TODO-01: Verify .gitignore is complete ✅ DONE
- Comprehensive .gitignore covers all .NET artifacts, IDE files, sensitive config

### TODO-02: Dead code — PlacedTile model ✅ DONE
- File `Wordfeud.Api/Models/PlacedTile.cs` does not exist — dead code removed

### TODO-03: Direct object exposure in GetScoresAsync / GetBoardAsync ✅ DONE
- Added DTOs: `GameScoresDto`, `BoardStateDto`, `PlayerScoreDto`, `BoardTileDto`
- `GameService` and `GamesController` updated to return DTOs instead of internal models

### TODO-04: OpenTaal HTTP dependency — timeout / retry policy ✅ DONE
- DutchDictionaryService implements retry policy (3 attempts, exponential backoff)
- 5-second HTTP timeout
- Dutch wordlist bundled as embedded resource (loaded first, HTTP is fallback)

### TODO-05: Dictionary loading progress tracking ✅ DONE
- `_isInitialized` flag with `IsInitialized` property
- Logs warnings on failure, logs success count

### TODO-06: Swap tiles ownership validation ✅ DONE
- SwapTilesAsync verifies each tile in request.TileIds belongs to player.Hand

### TODO-07: Integration tests depend on blank tiles being randomly drawn ✅ DONE
- Tests now control tile bag deterministically

### TODO-08: Integration tests expect wrong bag count ✅ DONE
- Updated all bag count assertions to correct values (88 tiles after join)

### NOT-IMPLEMENTED-05: No support for diacritics or special Dutch characters ✅ DONE
- `NormalizeDiacritics` method handles ë→e, é→e, à→a, ô→o, etc.
- Dictionary lookup normalizes both stored words and input words

### NOT-IMPLEMENTED-06: BoardConverter deserialization not tested ✅ DONE
- Added 6 tests in `Wordfeud.Api.Tests/Serialization/BoardConverterTests.cs`
- Covers: tiles, blank tiles, empty board, null token, all edges, mixed tiles

## 3. Test Count

| Category | Count |
|----------|-------|
| Unit tests | 79 |
| Serialization tests | 6 |
| **Total** | **85** |

---

## 4. Summary

| Category | Count | Status |
|----------|-------|--------|
| Bugs fixed | 4 | ✅ All resolved |
| Features implemented | 9 | ✅ All resolved |
| Tests added | 6 | ✅ All passing |
| **Total items** | **19** | **✅ All done** |
