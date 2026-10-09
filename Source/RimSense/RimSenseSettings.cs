using Verse;

namespace RimSense
{
    public class RimSenseSettings : ModSettings
    {
        public bool danger = true;
        public bool flash = true;
        public bool attention = true;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref danger, "danger", true);
            Scribe_Values.Look(ref flash, "flash", true);
            Scribe_Values.Look(ref attention, "attention", true);
        }
    }
}
