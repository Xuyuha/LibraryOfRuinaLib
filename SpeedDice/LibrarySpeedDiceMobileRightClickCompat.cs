using System.Reflection;
using Godot;
using HarmonyLib;
using LibraryLib.SpeedDice;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Cards.Holders;
using MegaCrit.Sts2.Core.Nodes.Combat;

namespace Library.SpeedDice;

internal static class LibrarySpeedDiceMobileRightClickCompat
{
    private const string LogTag =
        "[LibrarySpeedDiceMobileRightClick]";
    private const string AndroidInputTypeName =
        "STS2Mobile.Patches.AndroidInputCompatPatches";
    private const string MobilePreviewTypeName =
        "STS2Mobile.Patches.MobileTapPreviewPatches";
    private const string DispatchMethodName =
        "TryDispatchDirectMobileRightClick";
    private const string ClearPinnedMethodName =
        "ClearAllPinned";
    private const int MaxSettleFrames = 2;

    private static readonly FieldInfo? CurrentCardPlayField =
        AccessTools.Field(typeof(NPlayerHand), "_currentCardPlay");

    private static MethodInfo? _clearAllPinnedMethod;
    private static CardModel? _pendingCard;
    private static bool _bridgeInstalled;

    public static void TryInstall(Harmony harmony)
    {
        if (OS.GetName() != "Android")
            return;
        if (_bridgeInstalled)
            return;

        try
        {
            Type? inputCompatType =
                AccessTools.TypeByName(AndroidInputTypeName);
            if (inputCompatType == null)
            {
                Log.Warn(
                    $"{LogTag} event=bridge-unavailable "
                    + $"reason=type-not-found type={AndroidInputTypeName}");
                return;
            }

            MethodInfo? dispatchMethod = inputCompatType.GetMethod(
                DispatchMethodName,
                BindingFlags.NonPublic | BindingFlags.Static,
                binder: null,
                types: [typeof(Vector2)],
                modifiers: null);
            if (dispatchMethod == null
                || dispatchMethod.ReturnType != typeof(bool))
            {
                Log.Warn(
                    $"{LogTag} event=bridge-unavailable "
                    + $"reason=signature-mismatch "
                    + $"method={AndroidInputTypeName}.{DispatchMethodName}");
                return;
            }

            Type? previewCompatType =
                AccessTools.TypeByName(MobilePreviewTypeName);
            _clearAllPinnedMethod = previewCompatType?.GetMethod(
                ClearPinnedMethodName,
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: Type.EmptyTypes,
                modifiers: null);
            if (_clearAllPinnedMethod?.ReturnType != typeof(void))
            {
                _clearAllPinnedMethod = null;
                Log.Warn(
                    $"{LogTag} event=bridge-warning "
                    + $"reason=clear-pinned-signature-mismatch "
                    + $"method={MobilePreviewTypeName}.{ClearPinnedMethodName}");
            }

            if (CurrentCardPlayField == null)
            {
                Log.Warn(
                    $"{LogTag} event=bridge-warning "
                    + "reason=current-card-play-field-missing");
            }

            MethodInfo prefix = AccessTools.Method(
                typeof(LibrarySpeedDiceMobileRightClickCompat),
                nameof(DispatchPrefix));
            harmony.Patch(
                dispatchMethod,
                prefix: new HarmonyMethod(prefix));
            _bridgeInstalled = true;
            Log.Info(
                $"{LogTag} event=bridge-installed "
                + $"target={AndroidInputTypeName}.{DispatchMethodName}");
        }
        catch (Exception exception)
        {
            Log.Warn(
                $"{LogTag} event=bridge-unavailable "
                + $"reason=patch-failed error={FormatException(exception)}");
        }
    }

    private static bool DispatchPrefix(
        Vector2 position,
        ref bool __result)
    {
        try
        {
            NPlayerHand? hand = NPlayerHand.Instance;
            if (hand == null)
                return true;

            CardModel? card = ResolveCandidate(hand, position);
            if (card == null || !IsEligibleLocalCard(card))
                return true;

            __result = true;
            if (_pendingCard != null)
            {
                Log.Info(
                    $"{LogTag} event=aborted card={card.Id} "
                    + $"reason=selection-already-pending "
                    + $"pendingCard={_pendingCard.Id}");
                return false;
            }

            _pendingCard = card;
            Log.Info(
                $"{LogTag} event=intercepted "
                + $"card={card.Id} position={position}");
            TaskHelper.RunSafely(HandleInterceptedAsync(card));
            return false;
        }
        catch (Exception exception)
        {
            Log.Warn(
                $"{LogTag} event=aborted "
                + $"reason=prefix-failed error={FormatException(exception)}");
            return true;
        }
    }

    private static CardModel? ResolveCandidate(
        NPlayerHand hand,
        Vector2 position)
    {
        if (hand.InCardPlay
            && CurrentCardPlayField?.GetValue(hand) is NCardPlay cardPlay)
        {
            return cardPlay.Holder?.CardModel;
        }

        // A second gesture during speed-die or enemy selection must retain the
        // mobile compatibility layer's native cancel behavior.
        if (NTargetManager.Instance?.IsInSelection == true)
            return null;

        return FindTopmostCardHolderAtPosition(
            NGame.Instance,
            position) is NHandCardHolder holder
                ? holder.CardModel
                : null;
    }

    private static bool IsEligibleLocalCard(CardModel card)
    {
        return LocalContext.IsMe(card.Owner)
            && LibrarySpeedDiceRightClickService.CanRequestSelection(card);
    }

    private static NCardHolder? FindTopmostCardHolderAtPosition(
        Node? node,
        Vector2 position)
    {
        if (node == null || !GodotObject.IsInstanceValid(node))
            return null;

        for (int index = node.GetChildCount() - 1; index >= 0; index--)
        {
            NCardHolder? found = FindTopmostCardHolderAtPosition(
                node.GetChild(index),
                position);
            if (found != null)
                return found;
        }

        return node is NCardHolder holder
            && IsCardHolderHit(holder, position)
                ? holder
                : null;
    }

    private static bool IsCardHolderHit(
        NCardHolder holder,
        Vector2 position)
    {
        Control? hitbox = holder.Hitbox;
        return GodotObject.IsInstanceValid(hitbox)
            && holder.IsVisibleInTree()
            && hitbox.IsVisibleInTree()
            && hitbox.IsInsideTree()
            && hitbox.GetGlobalRect().HasPoint(position);
    }

    private static async Task HandleInterceptedAsync(CardModel card)
    {
        try
        {
            NPlayerHand? hand = NPlayerHand.Instance;
            if (hand == null)
            {
                LogAborted(card, "hand-unavailable");
                return;
            }

            hand.CancelAllCardPlay();
            if (NTargetManager.Instance?.IsInSelection == true)
                NTargetManager.Instance.CancelTargeting();
            ClearAllPinned();

            for (int frame = 0; frame < MaxSettleFrames; frame++)
            {
                if (!GodotObject.IsInstanceValid(hand)
                    || !hand.IsInsideTree())
                {
                    LogAborted(card, "hand-invalidated");
                    return;
                }

                await hand.ToSignal(
                    hand.GetTree(),
                    SceneTree.SignalName.ProcessFrame);
                if (!hand.InCardPlay
                    && NTargetManager.Instance?.IsInSelection != true)
                {
                    break;
                }
            }

            ClearAllPinned();
            if (hand.InCardPlay)
            {
                LogAborted(card, "card-play-did-not-settle");
                return;
            }

            if (NTargetManager.Instance?.IsInSelection == true)
            {
                LogAborted(card, "target-selection-did-not-settle");
                return;
            }

            if (!ReferenceEquals(_pendingCard, card)
                || !IsEligibleLocalCard(card))
            {
                LogAborted(card, "card-no-longer-eligible");
                return;
            }

            Log.Info(
                $"{LogTag} event=selection-started card={card.Id}");
            await LibrarySpeedDiceRightClickService.BeginSelectionAsync(
                card,
                usingController: false);

            if (LibrarySpeedDiceService.TryGetEquippedSlot(
                    card,
                    out LibrarySpeedDiceSlot? slot)
                && slot != null)
            {
                Log.Info(
                    $"{LogTag} event=equipped "
                    + $"card={card.Id} slot={slot.Index}");
            }
            else
            {
                LogAborted(card, "selection-ended-without-equip");
            }
        }
        catch (Exception exception)
        {
            LogAborted(
                card,
                "exception",
                FormatException(exception));
        }
        finally
        {
            if (ReferenceEquals(_pendingCard, card))
                _pendingCard = null;
        }
    }

    private static void ClearAllPinned()
    {
        try
        {
            _clearAllPinnedMethod?.Invoke(null, null);
        }
        catch (Exception exception)
        {
            Log.Warn(
                $"{LogTag} event=bridge-warning "
                + $"reason=clear-pinned-failed "
                + $"error={FormatException(exception)}");
        }
    }

    private static void LogAborted(
        CardModel card,
        string reason,
        string? error = null)
    {
        Log.Info(
            $"{LogTag} event=aborted card={card.Id} reason={reason}"
            + (error == null ? string.Empty : $" error={error}"));
    }

    private static string FormatException(Exception exception)
    {
        Exception actual = exception is TargetInvocationException
            {
                InnerException: not null,
            }
                ? exception.InnerException
                : exception;
        return $"{actual.GetType().Name}:{actual.Message}";
    }
}
