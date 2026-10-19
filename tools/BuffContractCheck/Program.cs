using LibraryLib.Combat;
using LibraryLib.Combat.HealthBars;
using LibraryLib.Compat;
using LibraryLib.Powers;
using LibraryLib.Utils.Resistance;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;

int checks = 0;
void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    checks++;
    Console.WriteLine($"PASS {message}");
}
var combat = new CombatState();
var owner = new Creature { IsPlayer = true, Side = CombatSide.Player, CombatState = combat };
var teammate = new Creature { IsPlayer = true, Side = CombatSide.Player, CombatState = combat };
var enemy = new Creature { Side = CombatSide.Enemy, CombatState = combat };
var context = new PlayerChoiceContext();
var powered = ValueProp.Move | ValueProp.Powered;
var decay = new LibraryDecayPower { Owner = owner, Applier = enemy, Amount = 3 };

await decay.AfterSideTurnEnd(context, CombatSide.Enemy, [enemy]);
Check(GameApi.Hits.Count == 0 && decay.Amount == 3, "corrosion does not tick on opposite side");
CombatManager.Instance.PlayersTakingExtraTurn.Add(new Player { Creature = teammate });
await decay.AfterSideTurnEnd(context, CombatSide.Player, [teammate]);
Check(GameApi.Hits.Count == 0 && decay.Amount == 3, "teammate extra turn does not tick owner");
await decay.AfterSideTurnEnd(context, CombatSide.Player, [owner]);
Check(GameApi.Hits.Count == 1 && GameApi.Hits[0].Amount == 3 && decay.Amount == 2, "own turn damages once then loses one stack");
Check(GameApi.Hits[0].Props == ValueProp.Unpowered && GameApi.Hits[0].Dealer == enemy, "turn-end damage is blockable and keeps applier attribution");
GameApi.Hits.Clear();
var pet = new Creature { Side = CombatSide.Player, PetOwner = new Player { Creature = owner }, CombatState = combat };
decay.Owner = pet;
await decay.AfterSideTurnEnd(context, CombatSide.Player, [teammate]);
Check(GameApi.Hits.Count == 0, "pet ignores teammate extra turn");
await decay.AfterSideTurnEnd(context, CombatSide.Player, [owner]);
Check(GameApi.Hits.Count == 1 && decay.Amount == 1, "pet ticks with its owner even absent from participants");
decay.Owner = new Creature { Side = CombatSide.Player, CombatState = combat };
GameApi.Hits.Clear();
await decay.AfterSideTurnEnd(context, CombatSide.Player, [teammate]);
Check(GameApi.Hits.Count == 0, "ownerless ally ignores extra player turns");
CombatManager.Instance.PlayersTakingExtraTurn.Clear();
await decay.AfterSideTurnEnd(context, CombatSide.Player, [owner, teammate]);
Check(GameApi.Hits.Count == 1 && decay.Removed, "ownerless ally ticks on normal round; last stack expires");
decay.Owner = enemy;
decay.Amount = 4;
GameApi.Hits.Clear();
await decay.AfterSideTurnEnd(context, CombatSide.Enemy, []);
Check(GameApi.Hits.Count == 1 && decay.Amount == 3, "enemy corrosion ticks on enemy side");

decay.Owner = owner;
GameApi.Hits.Clear();
await decay.AfterDamageReceived(context, teammate, new(8), powered, enemy, null);
await decay.AfterDamageReceived(context, owner, new(0), powered, enemy, null);
await decay.AfterDamageReceived(context, owner, new(8), ValueProp.Unpowered, enemy, null);
Check(GameApi.Hits.Count == 0, "wrong target, zero damage and unpowered effects do not trigger");
owner.IsDead = true;
await decay.AfterDamageReceived(context, owner, new(8), powered, enemy, null);
await decay.AfterSideTurnEnd(context, CombatSide.Player, [owner]);
Check(GameApi.Hits.Count == 0, "dead owner receives no follow-up or turn damage");
owner.IsDead = false;
GameApi.FollowUp = hit => decay.AfterDamageReceived(context, hit.Target, new((int)hit.Amount), hit.Props, hit.Dealer, hit.Card);
await decay.AfterDamageReceived(context, owner, new(8), powered, enemy, null);
Check(GameApi.Hits.Count == 1 && GameApi.Hits[0].Amount == 3, "positive total damage (including blocked hit) triggers exactly once, without recursion");
GameApi.FollowUp = null;
decay.Applier = null;
GameApi.Hits.Clear();
await decay.AfterDamageReceived(context, owner, new(1), powered, teammate, null);
Check(GameApi.Hits.Single().Dealer == teammate, "missing applier falls back to attacker");
var forecast = decay.GetLibraryHealthBarDamageForecasts(new(owner)).Single();
Check(forecast.Damage == 3 && forecast.Props == ValueProp.Unpowered && forecast.Dealer == owner, "health-bar forecast matches turn-end damage");
decay.Amount = 0;
GameApi.Hits.Clear();
await decay.AfterDamageReceived(context, owner, new(1), powered, enemy, null);
Check(GameApi.Hits.Count == 0 && !decay.GetLibraryHealthBarDamageForecasts(new(owner)).Any(), "zero stacks have neither damage nor forecast");

var nullify = new LibraryNullifyPower { Owner = owner };
combat.Listeners.Add(nullify);
foreach (var kind in Enum.GetValues<LibraryCombatValueKind>())
foreach (var preview in Enum.GetValues<CardPreviewMode>())
{
    var resolved = LibraryCombatValueResolver.Resolve(combat, kind, 8, owner, owner, powered, previewMode: preview, damageType: LibraryDamageType.Slash);
    Check(resolved == (kind == LibraryCombatValueKind.HpLoss ? LibraryCombatValueResolution.Default : LibraryCombatValueResolution.BaseValueAndResistanceOnly), $"nullify {kind} in {preview}: matches author policy");
    Check(LibraryCombatValueResolver.Resolve(combat, kind, 8, enemy, enemy, powered, previewMode: preview) == LibraryCombatValueResolution.Default, $"nullify {kind} in {preview}: other creature unaffected");
    Check(LibraryCombatValueResolver.Resolve(combat, kind, 8, owner, owner, ValueProp.Unpowered, previewMode: preview) == LibraryCombatValueResolution.Default, $"nullify {kind} in {preview}: independent effects unaffected");
}
Check(LibraryCombatValueResolver.ResolveBaseValue(LibraryCombatValueResolution.BaseValueAndResistanceOnly, 8) == 8, "base value remains eight");
Check(LibraryCombatValueResolver.Resolve(null, LibraryCombatValueKind.PhysicalDamage, 8, owner, owner, powered) == LibraryCombatValueResolution.Default, "no combat has default resolution");
await nullify.AfterApplied(null, null);
await nullify.AfterSideTurnEndLate(context, CombatSide.Player, [owner]);
Check(!nullify.Removed, "player application survives player turn end");
await nullify.AfterSideTurnEndLate(context, CombatSide.Enemy, [enemy]);
Check(nullify.Removed, "player application expires after enemy turn");
combat.CurrentSide = CombatSide.Enemy;
nullify.Removed = false;
await nullify.AfterApplied(null, null);
await nullify.AfterSideTurnEndLate(context, CombatSide.Enemy, [enemy]);
Check(!nullify.Removed, "enemy application skips immediate enemy turn end");
await nullify.AfterSideTurnEndLate(context, CombatSide.Player, [owner]);
Check(!nullify.Removed, "enemy application survives next player turn");
await nullify.AfterSideTurnEndLate(context, CombatSide.Enemy, [enemy]);
Check(nullify.Removed, "enemy application expires at next enemy turn end");
Console.WriteLine($"Buff contract checks passed: {checks}. Offline command boundaries; not an in-game or two-client test.");
