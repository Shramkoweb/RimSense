using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimSense
{
    // Created automatically by RimWorld for every game. Runs every frame, including while paused.
    public class AlertMonitor : GameComponent
    {
        private const float CheckIntervalSeconds = 1f;

        private float nextCheck;
        private bool loggedError;
        private static ClientStatus lastLoggedStatus = ClientStatus.NotFound;

        public AlertMonitor(Game game)
        {
        }

        public override void GameComponentUpdate()
        {
            float now = Time.realtimeSinceStartup;
            if (now < nextCheck)
                return;
            nextCheck = now + CheckIntervalSeconds;

            LogStatusChange();

            try
            {
                GameSenseClient.Report(Evaluate());
            }
            catch (Exception e)
            {
                if (!loggedError)
                {
                    loggedError = true;
                    Log.Error("[RimSense] Failed to evaluate colony state: " + e);
                }
                GameSenseClient.Report(AlertLevel.Calm);
            }
        }

        private static AlertLevel Evaluate()
        {
            RimSenseSettings settings = RimSenseMod.Settings;
            bool attention = false;

            foreach (Map map in Find.Maps)
            {
                var colonists = map.mapPawns.FreeColonistsSpawned;
                if (colonists.Count == 0)
                    continue;

                if (settings.danger && GenHostility.AnyHostileActiveThreatToPlayer(map))
                    return settings.flash ? AlertLevel.Danger : AlertLevel.DangerSteady;

                if (settings.attention && !attention)
                    attention = map.fireWatcher.LargeFireDangerPresent || AnyColonistNeedsAttention(colonists);
            }

            if (!attention)
                return AlertLevel.Calm;
            return settings.flash ? AlertLevel.Attention : AlertLevel.AttentionSteady;
        }

        // Uses the game's own checks where they exist, so we agree with RimWorld's alerts.
        private static bool AnyColonistNeedsAttention(List<Pawn> colonists)
        {
            foreach (Pawn pawn in colonists)
            {
                // Bleeding out within ~18 in-game hours (the game's "urgent tend" rule).
                if (HealthAIUtility.ShouldBeTendedNowByPlayerUrgent(pawn))
                    return true;

                // Giving birth. Returns false by itself when Biotech isn't active.
                if (pawn.health.hediffSet.InLabor(includePostpartumExhaustion: false))
                    return true;

                // A major/extreme break that is about to happen or is happening now.
                // Hostile breaks (berserk, murderous rage) are already Danger via GenHostility.
                MentalStateDef state = pawn.MentalStateDef;
                if (state != null)
                {
                    if (SeriousBreakStates.Contains(state))
                        return true;
                    continue;
                }

                var breaker = pawn.mindState?.mentalBreaker;
                if (breaker != null && (breaker.BreakExtremeIsImminent || breaker.BreakMajorIsImminent))
                    return true;
            }
            return false;
        }

        private static HashSet<MentalStateDef> seriousBreakStates;

        // Mental states used only by major/extreme breaks. Built from defs, so modded breaks are covered.
        // A state shared with any minor break is excluded (e.g. Royalty's Wild Decree reuses minor Wander_Sad).
        private static HashSet<MentalStateDef> SeriousBreakStates
        {
            get
            {
                if (seriousBreakStates != null)
                    return seriousBreakStates;

                var serious = new HashSet<MentalStateDef>();
                var minor = new HashSet<MentalStateDef>();
                foreach (MentalBreakDef breakDef in DefDatabase<MentalBreakDef>.AllDefs)
                {
                    if (breakDef.mentalState == null)
                        continue;
                    if (breakDef.intensity >= MentalBreakIntensity.Major)
                        serious.Add(breakDef.mentalState);
                    else
                        minor.Add(breakDef.mentalState);
                }
                serious.ExceptWith(minor);
                return seriousBreakStates = serious;
            }
        }

        private static void LogStatusChange()
        {
            ClientStatus status = GameSenseClient.Status;
            if (status == lastLoggedStatus)
                return;
            lastLoggedStatus = status;
            if (status == ClientStatus.Connected)
                Log.Message("[RimSense] Connected to SteelSeries GG at " + GameSenseClient.Address);
        }
    }
}
