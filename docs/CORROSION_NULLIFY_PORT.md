# Corrosion and Power Nullification

Ported from `s1f102500012/LibraryOfRuina` revision
`9cab269d83cd0ab58ca85efe2ef983e514cdc27d` onto
`Xuyuha/LibraryOfRuinaLib` revision `0fd5080`.
Source attribution and license are in `THIRD_PARTY_NOTICES`.

## Public powers

`LibraryLib.Powers.LibraryDecayPower` keeps the original library corrosion behavior.
The content mod's current `RnfmabjCorrosionPower` behavior is exposed separately as
`LibraryLib.Powers.LibraryCorrosionPower`:

- An owner's turn end deals unpowered, blockable damage equal to its stacks,
  then decrements once. Teammates' extra turns do not tick it; pets follow their owner.
- A powered attack with positive total damage (including blocked damage)
  triggers one unpowered follow-up. Zero damage, dead owners and unpowered
  damage do not trigger it. It no longer explicitly deals chaos damage.
- Damage attribution follows the original applier, then attacker/owner fallback.
- Its health-bar forecast uses the existing shared forecast interface.

`LibraryLib.Powers.LibraryNullifyPower` ports `NullifyPower` through the existing
`ILibraryCombatValueResolutionPolicy`. It uses `LIBRARY_NULLIFY_POWER` rather
than `NULLIFY_POWER` so both mods can load without model/localization collisions.

- Owner physical/chaos attack values and owner card block use
  `BaseValueAndResistanceOnly` when powered. Independent unpowered effects
  and other creatures' actions remain unchanged.
- Matches the author's removal timing: player-turn application expires at the
  following enemy turn end; enemy-turn application skips that immediate turn
  end and expires at the next enemy turn end.
- The private grace flag follows the author's implementation. It is not claimed
  to persist through a combat-state snapshot or reconnection. Merely adding
  `SavedProperty` would not serialize power state in the native game.

Both icons and zhs/eng/jpn/kor descriptions come from the content mod.
No content-mod assembly reference or dependency is introduced. Existing callers
 can use `PowerCmd.Apply<LibraryCorrosionPower>` and `PowerCmd.Apply<LibraryNullifyPower>`;
 existing library content may continue using `LibraryDecayPower`.
This does not migrate independently defined downstream corrosion classes.

## Build prerequisite repair

The upstream `0fd5080` changed hook subscriptions to require `CombatState`,
but left four `LibraryMultipleModePowerModel` callers using the removed signature.
Those calls now pass the owner's (or removed owner's) combat state. This is
required for a clean checkout to build and does not replace that upstream design.

## Checks

```text
dotnet build LibraryLib.csproj -c Release -p:Sts2Dir="<installed game directory>"
dotnet run --project tools/BuffContractCheck -c Release
```

The second command links the production hook implementations and value-policy
resolver into an offline test executable. Command/model boundaries are test
doubles; it covers 46 checks for turn ownership, pets, extra turns, hit filters,
recursion prevention, damage attribution, forecasts, policy ownership, preview
parity and removal timing. It does not execute Godot, Harmony, real damage
absorption, saves or networking.

Validated against game 0.111.0: Release build (0 errors, 5 existing warnings),
offline checks, four-language keys, original icon hashes, and Godot-generated
callback metadata. Game 0.107.1 was not built; no in-game or two-client test was
performed. Re-export the shared PCK with these resources before deployment;
a DLL-only update leaves stale descriptions and missing icons.
