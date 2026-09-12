namespace FarmerQuest.Progress
{
    /// <summary>
    /// Progress-regler for Climate Wheel.
    /// <para>
    /// <see cref="DefaultLockOuterUntilInnerComplete"/> = én linje at flippe:
    /// true  → ydre felter er grå/låst indtil indre badge i samme kategori er optjent.
    /// false → alle felter kan opnås fra start (som før).
    /// </para>
    /// DEBUG-toggle i Climate Wheel kan overstyre runtime uden at genkompilere.
    /// </summary>
    public static class PlayerProgressRules
    {
        /// <summary>Skift denne til false for at åbne alle ydre felter fra start.</summary>
        public const bool DefaultLockOuterUntilInnerComplete = true;

        /// <summary>Indre badge er optjent ved max slot-point (fuld fyldning af feltet).</summary>
        public const int InnerCompletePoints = PlayerProgressData.MaxSlotPoints;

        private static bool? _runtimeOverride;

        public static bool LockOuterUntilInnerComplete
        {
            get => _runtimeOverride ?? DefaultLockOuterUntilInnerComplete;
            set => _runtimeOverride = value;
        }

        public static void ResetRuntimeOverride() => _runtimeOverride = null;
    }
}
