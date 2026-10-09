namespace RimSense
{
    // Values are sent to GG as the ALERT event value; the bound handlers key off them.
    // One rule for players: color = how serious, flashing = it just started, steady = still going.
    // Ordered by severity so GG's Configure color bar reads amber → red.
    public enum AlertLevel
    {
        Calm = 0,            // GG gets stop_game: the player's own lighting comes back
        AttentionSteady = 1, // amber
        Attention = 2,       // amber, 3 slow flashes then steady
        DangerSteady = 3,    // red
        Danger = 4,          // red, 10 fast flashes then steady
    }
}
