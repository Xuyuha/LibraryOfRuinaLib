# Shared untargetable power

Adapted from `s1f102500012/LibraryOfRuina` revision
`9cab269d83cd0ab58ca85efe2ef983e514cdc27d`, specifically
`src/framework/powers/UntargetablePower.cs` and the generic interaction guards in
`src/content/abnormalities/QueenOfHatred/UntargetableInteractionPatch.cs`.
The library base is `Xuyuha/LibraryOfRuinaLib` revision `76feddd`.
Attribution and retained license/asset terms are in `THIRD_PARTY_NOTICES`.

## API and behavior

`LibraryLib.Powers.LibraryUntargetablePower` is a public, neutral, single-stack
power. Applying it vetoes selection and hitting of its owner through the native
`ShouldAllowTargeting` and `ShouldAllowHitting` hooks. It does not expire by itself:
the mechanic applying it must remove it when its protection condition ends.

```csharp
await PowerCmd.Apply<LibraryUntargetablePower>(
    choiceContext, target, 1m, applier, cardSource);

// At the mechanic's chosen end condition:
if (target.GetPower<LibraryUntargetablePower>() is { } protection)
    await PowerCmd.Remove(protection);
```

The model/localization prefix is `LIBRARY_UNTARGETABLE_POWER`. It deliberately
does not take the content mod's `UNTARGETABLE_POWER` ID. The original icon is
copied without alteration to
`res://LibraryOfRuinaLib/images/powers/library_untargetable_power.png`;
zhs/eng/jpn/kor titles and descriptions retain the source translations.

`LibraryLib.Combat.LibraryUntargetable.BlocksHit` / `BlocksSelection` expose the
same opt-in guard for downstream custom commands. Neither modifies state or
depends on the local player, UI, random numbers, or current client.

### Guarded paths

| Path | Guard |
| --- | --- |
| Native card `IsValidTarget` | Reject selection; never turn an already-invalid target valid |
| Native `AttackCommand.GetPossibleTargets` | Remove protected targets, including area/random candidate lists |
| `LibraryAttackTargets.Filter` | Final guard after registered downstream target rewrites |
| Native `CreatureCmd.Damage` | Filter in the existing last-priority library damage Prefix, before its unpowered/move/library-card early returns and before interception |
| 0.113.1 `DamageTargetData` | Use the existing snapshot/rebuild adapter; preserve retained targets' per-hit metadata and order |
| `LibraryCreatureCmd.Damage` | Guard the requested target and `ModifyDamageTarget` result before damage hooks/block loss; guard the final `ModifyUnblockedDamageTarget` result before HP loss |
| `LibraryCreatureCmd.ChaoDamage` | Guard both the requested target and the result of `ModifyChaoDamageTarget` before chaos reduction |
| Native `CreatureCmd.Stun` | Guard the callback overload used by its convenience overload; return a completed task when protected |
| `LibraryCreatureCmd.Stun` | Return before before-stun hooks and move replacement |

Filtering preserves the relative order and duplicate occurrences of other
targets. It does not skip entire multi-target attacks just because one target
is protected. Removing the power restores eligibility immediately.

If an unprotected original target has already spent block before a late
`ModifyUnblockedDamageTarget` redirect, that original block expenditure is not
rolled back; the protected final target's HP is left unchanged.

The direct chaos-command guard closes a gap in the source implementation:
rejecting only its eventual `Stun` would still drain a protected creature's
chaos value. The shared state prevents that reduction as well.

This is not a universal immunity to arbitrary game-state edits. Direct HP-loss,
kill/removal commands, direct power application, or another mod calling internal
state-changing methods are not intercepted. Health-bar forecasts supplied by
separate content mods are not changed here; those mods should consult the public
guard if they independently forecast damage against a protected target.

## Compatibility with existing content

Only `LibraryUntargetablePower`, its subclasses, and powers explicitly implementing
`LibraryLib.Combat.ILibraryUntargetablePower` opt into the additional command
guards. The interface uses the inherited public power hook methods:

```csharp
public class UntargetablePower : LibraryOfRuinaPowerModel,
    ILibraryUntargetablePower
{
    // Keep the existing legacy ID, localization, icon, base type and hook methods.
}
```

This lets the content mod retain its `UntargetablePower` and
`SpiderBudUntargetablePower` classes, public inheritance relationships and saved
model IDs. It need not replace serialized powers with a new model or change a
legacy class's base type. New mechanics can use the library power directly.

The interface is an opt-in contract, not a replacement for the native hooks.
Implementations must keep both hooks pure. A derived/adapter power that permits
hitting while vetoing selection does not gain damage or stun immunity.

The port intentionally does **not** broaden every third-party `IsHittable == false`
condition into damage/stun immunity. For example, Big Bad Wolf's source/side-aware
friendly exception and the content mod's friendly-ally registry remain in the
content mod. No content assembly reference, ally-faction policy, boss condition,
automatic duration, or encounter dependency is introduced into the library.

### Content-mod migration sequence

1. Merge/release this library addition, export the shared PCK, and set the content
   mod's minimum library dependency to the version containing it.
2. Add `ILibraryUntargetablePower` to its generic `UntargetablePower` declaration.
   Preserve the old class, its base class, ID, icon and inherited spider-bud alias.
3. Route new generic uses through the library model as appropriate. Remove only
   redundant *generic* guards after verification. Keep friendly ally filtering,
   Big Bad Wolf exceptions, encounter transitions and other content-specific guards.
4. Test both mods together, saved battles with legacy IDs, disappearance/reappearance,
   area hits, direct damage, direct chaos damage, stun, and multiplayer peers.

This PR does not remove working state classes from the independently released
content mod before the new library dependency exists. It supplies the shared
implementation and a migration path that preserves those compatibility contracts.
The library version is left for its maintainer to choose; no game files or
Workshop installation are replaced by this change.

## Validation

```text
dotnet build LibraryLib.csproj -c Release -p:CompatibilityTarget=0.113.1 -p:Sts2Dir="<game directory>"
dotnet run --project tools/UntargetableContractCheck -c Release
dotnet run --project tools/UntargetableBindingCheck -c Release -p:GameManagedDir="<game managed directory>"
```

The contract executable links the production power, filter, patch handlers and
packet adapter to test doubles. It covers owner isolation, players, selection,
area filtering, duplicates, removal, downstream rewrites, derived/legacy adapters,
unrelated veto isolation, stun early completion and packet metadata preservation.
It does not execute the production damage commands or install Harmony patches.

The binding executable inspects the **real compiled library and game assemblies**:
Harmony target resolution/parameter compatibility, native hook override identity,
and guard calls in the compiled async HP/chaos/stun command bodies. It also checks
that native damage filtering occurs before the damage Prefix's early exits.
It does not install patches, initialize a combat, or verify live patch ordering.

Validated on the available 0.113.1 game references: Release build with zero errors
(12 existing warnings), 34 offline contract checks, 24 real-assembly binding/IL checks,
four-language JSON keys and original icon hash. The source guards are included in
both legacy and official damage branches. **0.107.1/0.111.0 were not built or run.**
No in-game, visual, save/reconnection or two-client multiplayer validation was run.
Re-export the shared PCK before deployment; a DLL-only update leaves the icon and
localization unavailable.

The separate pre-existing `tools/BuffContractCheck` could not run on this upstream
base because its project references missing `Combat/LibraryTurnParticipants.cs`.
That file is also absent from the unchanged base commit; this port does not repair
that unrelated upstream test project or claim it passed.
