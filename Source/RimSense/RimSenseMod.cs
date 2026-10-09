using UnityEngine;
using Verse;

namespace RimSense
{
    public class RimSenseMod : Mod
    {
        public static RimSenseSettings Settings;

        public RimSenseMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<RimSenseSettings>();
            GameSenseClient.Start();
        }

        public override string SettingsCategory() => "RimSense";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            listing.CheckboxLabeled("RimSense_Danger".Translate(), ref Settings.danger, "RimSense_DangerDesc".Translate());
            listing.CheckboxLabeled("RimSense_Flash".Translate(), ref Settings.flash, "RimSense_FlashDesc".Translate());
            listing.CheckboxLabeled("RimSense_Attention".Translate(), ref Settings.attention, "RimSense_AttentionDesc".Translate());

            listing.GapLine();
            listing.Label("RimSense_Status".Translate(StatusText()));
            listing.Label("RimSense_ColorsHint".Translate());

            listing.End();
        }

        public static string StatusText()
        {
            switch (GameSenseClient.Status)
            {
                case ClientStatus.Connected: return "RimSense_StatusConnected".Translate(GameSenseClient.Address);
                case ClientStatus.Unsupported: return "RimSense_StatusUnsupported".Translate();
                default: return "RimSense_StatusNotFound".Translate();
            }
        }
    }
}
