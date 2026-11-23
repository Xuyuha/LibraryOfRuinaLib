using LibraryLib.Combat;
using LibraryLib.Commands;
using LibraryLib.Compat;
using LibraryLib.Patches;
using LibraryLib.Powers;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine($"PASS {++checks}: {message}");
}

var owner = new Creature();
var player = new Creature { IsPlayer = true };
var other = new Creature();
var power = new LibraryUntargetablePower { Owner = owner };
owner.Powers.Add(power);
Check(!power.ShouldAllowHitting(owner) && !power.ShouldAllowTargeting(owner), "owner hit and selection vetoes");
Check(power.ShouldAllowHitting(other) && power.ShouldAllowTargeting(other), "other creature unaffected by owner hook");
Check(!power.ShouldAllowTargeting(owner) && power.ShouldAllowTargeting(other), "ownership is local to the protected creature");
Check(power.Type == PowerType.None && power.StackType == PowerStackType.Single, "original neutral/single-stack semantics");
Check(power.LocalizationPrefix == "LIBRARY_UNTARGETABLE_POWER", "new localization prefix does not collide with the content mod");
Check(power.PackedIconPath == power.ResolvedBigIconPath && power.PackedIconPath.StartsWith("res://LibraryOfRuinaLib/"), "icon is owned by the library");
Check(LibraryUntargetable.BlocksHit(owner) && LibraryUntargetable.BlocksSelection(owner), "command guards follow the power");
Check(!LibraryUntargetable.BlocksHit(null) && !LibraryUntargetable.BlocksSelection(null), "null remains for existing command validation");
Check(!LibraryUntargetable.BlocksHit(other) && !LibraryUntargetable.BlocksSelection(other), "unprotected targets are unchanged");
player.Powers.Add(new LibraryUntargetablePower { Owner = player });
Check(LibraryUntargetable.BlocksHit(player), "players can use the shared state as well as monsters");

IReadOnlyList<Creature> original = [owner, other, player, other];
var filtered = LibraryUntargetable.FilterHittable(original);
Check(filtered.SequenceEqual([other, other]), "area hits retain order and duplicate unprotected targets");
Check(original.Count == 4, "input target collections are not mutated");
Check(LibraryUntargetable.FilterHittable([owner, player]).Count == 0, "fully protected area hit has no targets");
IReadOnlyList<Creature> unchanged = [other];
Check(ReferenceEquals(unchanged, LibraryUntargetable.FilterHittable(unchanged)), "unaffected list preserves identity");
Check(LibraryUntargetable.FilterHittable(Array.Empty<Creature>()).Count == 0, "empty target collection stays empty");
IReadOnlyList<Creature> native = original;
LibraryUntargetableAttackTargetsPatch.Postfix(ref native);
Check(native.SequenceEqual(filtered), "native attack handler uses the shared filter");

bool selectable = true;
LibraryUntargetableCardTargetPatch.Postfix(owner, ref selectable);
Check(!selectable, "protected single-card target is rejected");
selectable = false;
LibraryUntargetableCardTargetPatch.Postfix(other, ref selectable);
Check(!selectable, "card handler never revives a previously invalid target");
selectable = true;
LibraryUntargetableCardTargetPatch.Postfix(other, ref selectable);
Check(selectable, "other valid card targets stay valid");

Task result = Task.CompletedTask;
Check(!LibraryUntargetableStunPatch.Prefix(owner, ref result) && result.IsCompletedSuccessfully, "native stun skips protected owner with a completed result");
Check(LibraryUntargetableStunPatch.Prefix(other, ref result), "native stun remains enabled on other creatures");
owner.Powers.Clear();
Check(!LibraryUntargetable.BlocksHit(owner) && !LibraryUntargetable.BlocksSelection(owner), "removal immediately restores interactions");
Check(LibraryUntargetableStunPatch.Prefix(owner, ref result), "removal restores stun eligibility");

owner.Powers.Add(new CompatibilityPower { Owner = owner });
Check(LibraryUntargetable.BlocksHit(owner), "derived compatibility power receives all guards");
owner.Powers.Clear();
owner.Powers.Add(new SelectionOnlyPower { Owner = owner });
Check(!LibraryUntargetable.BlocksHit(owner) && LibraryUntargetable.BlocksSelection(owner), "derived selection-only rule does not become damage immunity");
Check(LibraryUntargetableStunPatch.Prefix(owner, ref result), "selection-only protection still permits stun");
owner.Powers.Clear();
owner.Powers.Add(new ForeignVetoPower { Owner = owner });
Check(!owner.IsHittable && !LibraryUntargetable.BlocksHit(owner), "unrelated vetoes are not globally promoted to stun/damage immunity");
Check(LibraryUntargetableStunPatch.Prefix(owner, ref result), "foreign side/source-sensitive rules remain outside the port");

owner.Powers.Clear();
owner.Powers.Add(new LegacyAdapter { Owner = owner });
Check(LibraryUntargetable.BlocksHit(owner) && LibraryUntargetable.BlocksSelection(owner), "legacy model opts in without changing its base class");
Check(!LibraryUntargetableStunPatch.Prefix(owner, ref result), "legacy adapter receives the shared stun guard");

owner.Powers.Clear();
owner.Powers.Add(power);
LibraryAttackTargets.RegisterFilter(new ReintroduceTarget(owner));
Check(LibraryAttackTargets.Filter(new LibraryAttackCommand(), [other]).SequenceEqual([other]), "shared Library attack guard runs after downstream target rewrites");

var first = new DamageTargetData(other) { Vfx = "first", Marker = 11 };
var second = new DamageTargetData(owner) { Vfx = "hidden", Marker = 12 };
var third = new DamageTargetData(other) { Vfx = "third", Marker = 13 };
var creatures = DamageTargetDataCompat.ToCreatures([first, second, third], out var snapshot)!;
var rebuilt = DamageTargetDataCompat.Rebuild(snapshot, LibraryUntargetable.FilterHittable(creatures))!.ToArray();
Check(rebuilt.Length == 2 && ReferenceEquals(rebuilt[0], first) && ReferenceEquals(rebuilt[1], third), "0.113.1 filtered packets preserve per-hit metadata and duplicate identity");
Check(snapshot!.Count == 3, "packet snapshot is not consumed by rebuild");
Check(DamageTargetDataCompat.Rebuild(snapshot, Array.Empty<Creature>())!.Count() == 0, "empty packet rebuild stays empty");

Console.WriteLine($"{checks} offline contract checks passed. No real combat or Harmony execution was run.");

sealed class CompatibilityPower : LibraryUntargetablePower { }
sealed class SelectionOnlyPower : LibraryUntargetablePower
{
    public override bool ShouldAllowHitting(Creature creature) => true;
}
sealed class ForeignVetoPower : PowerModel
{
    public override bool ShouldAllowHitting(Creature creature) => creature != Owner;
}
sealed class LegacyAdapter : PowerModel, ILibraryUntargetablePower
{
    public override bool ShouldAllowHitting(Creature creature) => creature != Owner;
    public override bool ShouldAllowTargeting(Creature creature) => creature != Owner;
}
sealed class ReintroduceTarget(Creature hidden) : ILibraryAttackTargetFilter
{
    public IReadOnlyList<Creature> FilterTargets(LibraryAttackCommand command, IReadOnlyList<Creature> targets) =>
        targets.Append(hidden).ToArray();
}
