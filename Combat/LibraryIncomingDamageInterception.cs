using System.Threading;
using Library.Resistance;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace Library.Combat;

public sealed record LibraryIncomingDamageContext(
    PlayerChoiceContext ChoiceContext,
    Creature Target,
    Creature? Dealer,
    decimal OriginalDamage,
    decimal IncomingDamage,
    ValueProp Props,
    CardModel? CardSource,
    CardPlay? CardPlay,
    LibraryDamageType DamageType);

public readonly record struct LibraryIncomingDamageResolution(
    decimal RemainingDamage,
    decimal InterceptedDamage,
    bool WasFullyIntercepted)
{
    public static LibraryIncomingDamageResolution PassThrough(
        decimal incomingDamage)
    {
        return new(
            Math.Max(0m, incomingDamage),
            0m,
            false);
    }

    public static LibraryIncomingDamageResolution FromRemaining(
        decimal incomingDamage,
        decimal remainingDamage)
    {
        decimal normalizedIncoming = Math.Max(0m, incomingDamage);
        decimal normalizedRemaining = Math.Clamp(
            remainingDamage,
            0m,
            normalizedIncoming);
        decimal intercepted =
            normalizedIncoming - normalizedRemaining;
        return new(
            normalizedRemaining,
            intercepted,
            intercepted > 0m && normalizedRemaining <= 0m);
    }
}

public interface ILibraryIncomingDamageInterceptor
{
    Task<LibraryIncomingDamageResolution> InterceptIncomingDamageAsync(
        LibraryIncomingDamageContext context);
}

public static class LibraryIncomingDamageInterception
{
    private static readonly AsyncLocal<int> SuppressionDepth = new();

    internal static bool IsSuppressed => SuppressionDepth.Value > 0;

    public static IDisposable Suppress()
    {
        SuppressionDepth.Value++;
        return new SuppressionScope();
    }

    private sealed class SuppressionScope : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            SuppressionDepth.Value =
                Math.Max(0, SuppressionDepth.Value - 1);
        }
    }
}
