#nullable enable
using System;
using System.Collections.Generic;
using MegaCrit.Sts2.Core.Models;

namespace Library.Resistance;

/// <summary>角色 mod 在 Initialize 中为攻击牌登记斩 / 打 / 刺归属。</summary>
public static class LibraryAttackDamageKindRegistry
{
    private static readonly Dictionary<Type, LibraryDamageKind> Map = new();

    public static void RegisterSlash<TCard>() where TCard : CardModel => Register(typeof(TCard), LibraryDamageKind.Slash);

    public static void RegisterBlunt<TCard>() where TCard : CardModel => Register(typeof(TCard), LibraryDamageKind.Blunt);

    public static void RegisterPierce<TCard>() where TCard : CardModel => Register(typeof(TCard), LibraryDamageKind.Pierce);

    public static void Register(Type cardType, LibraryDamageKind kind) => Map[cardType] = kind;

    public static bool TryGetKind(CardModel? card, out LibraryDamageKind kind)
    {
        kind = default;
        if (card == null)
        {
            return false;
        }

        return Map.TryGetValue(card.GetType(), out kind);
    }
}
