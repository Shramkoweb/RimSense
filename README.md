# RimSense

> Your SteelSeries RGB gear lights up only when the colony needs you.

A RimWorld 1.6 mod for SteelSeries GG. Alt-tabbed while the game runs at 3× speed? Your mouse, keyboard or headset tells you when to come back — and stays out of the way the rest of the time.

[Project page](https://shramko.dev/rimsense) · Steam Workshop *(coming soon)* · [Report an Issue](https://github.com/Shramkoweb/RimSense/issues)

## How it works

One rule: **color = how serious, flashing = it just started, steady = still going.**

| Light | When |
|---|---|
| 🔴 **Red** — 10 fast flashes, then steady | Hostiles on a map with your colonists: raids, manhunters, infestations, berserk colonists |
| 🟠 **Amber** — 3 slow flashes, then steady | A colonist in trouble (serious mental break, bleeding out, giving birth) or a large fire |
| Your own lighting | Everything else — RimSense hands control back to GG |

## Features

- **Calm by default** — no mood bars or weather colors; the light only means something when it's on
- **Every SteelSeries RGB device** — mice, keyboards, headsets, mousepads; whole-device lighting
- **Customizable in GG** — change colors or zones in GG → Engine → Apps → RimWorld → Configure
- **In-game settings** — toggle danger, attention and flashing in Options → Mod settings → RimSense
- **Safe** — no Harmony patches, no DLC required, does nothing if GG isn't running
- **Lightweight** — one check per second; all networking runs off the game thread
- **Languages** — English, Ukrainian, Russian

## Installation

### Steam Workshop

Coming soon.

### Manual

1. Download or clone this repository.
2. Build the DLL (see [Development](#development)).
3. Copy or link the `Mod` folder into RimWorld's `Mods` folder as `RimSense`.
4. Enable **RimSense** in the mod list and make sure [SteelSeries GG](https://steelseries.com/gg) is running.

## Requirements

- RimWorld 1.6 (Royalty, Ideology and Biotech are optional)
- SteelSeries GG on Windows or macOS

## Development

Requires the [.NET SDK](https://dotnet.microsoft.com/download). Builds on macOS and Windows.

```sh
dotnet build Source/RimSense -c Release
```

The DLL is written to `Mod/1.6/Assemblies/`. `Mod/` is exactly what RimWorld loads and what is uploaded to the Steam Workshop.

Built on the [SteelSeries GameSense SDK](https://github.com/SteelSeries/gamesense-sdk).

## License

[MIT](LICENSE)

## Author

[Serhii Shramko](https://shramko.dev/) — also maintains the [Ukrainian translation of RimWorld](https://github.com/Shramkoweb/RimWorld-Ukrainian).

## Contributing

Contributions, issues, and feature requests are welcome! Check the [issues page](https://github.com/Shramkoweb/RimSense/issues).

## Support

If you found this project helpful, please consider giving it a star!
