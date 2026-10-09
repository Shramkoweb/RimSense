# RimSense

SteelSeries GG lighting for RimWorld 1.6. Your SteelSeries RGB gear lights up only when the colony needs you:

- **Red** — hostiles on a map with your colonists (raids, manhunters, berserk colonists).
- **Amber** — a colonist in trouble (serious mental break, bleeding out, giving birth) or a large fire.
- **Flashing** means it just started; **steady** means it's still going.
- Otherwise your own lighting is untouched.

Works with any SteelSeries RGB device on Windows and macOS. Colors can be changed in GG: Engine → Apps → RimWorld → Configure.

## Build

Requires the .NET SDK. Works on macOS and Windows.

```sh
dotnet build Source/RimSense -c Release
```

The DLL is written to `Mod/1.6/Assemblies/`. `Mod/` is the folder RimWorld loads and the one uploaded to the Steam Workshop — link or copy it into RimWorld's `Mods` folder.
