namespace Library.Light;

internal readonly record struct LibraryLightRecoveryPlan(
    int LastEmotionLevel,
    bool ShouldRecover,
    bool ShouldRefill,
    int RecoveryAmount);

internal static class LibraryLightRules
{
    public static int CalculateBaseMaximum(
        LibraryLightOptions options,
        int emotionLevel,
        int permanentModifier,
        int temporaryModifier)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Math.Max(
            0,
            checked(
                options.BaseMaximum
                + Math.Max(0, emotionLevel)
                * options.MaximumPerEmotionLevel
                + permanentModifier
                + temporaryModifier));
    }

    public static LibraryLightRecoveryPlan CreateRecoveryPlan(
        LibraryLightOptions options,
        int previousEmotionLevel,
        int currentEmotionLevel,
        int lastEmotionLevel,
        int modifiedRecovery,
        bool shouldRecover)
    {
        ArgumentNullException.ThrowIfNull(options);
        int normalizedCurrent = Math.Max(0, currentEmotionLevel);
        int normalizedLast = Math.Max(0, lastEmotionLevel);
        bool increased =
            normalizedCurrent > Math.Max(
                Math.Max(0, previousEmotionLevel),
                normalizedLast);
        return new LibraryLightRecoveryPlan(
            Math.Max(normalizedLast, normalizedCurrent),
            shouldRecover,
            shouldRecover
            && increased
            && options.RefillOnLevelIncrease,
            shouldRecover ? Math.Max(0, modifiedRecovery) : 0);
    }
}
