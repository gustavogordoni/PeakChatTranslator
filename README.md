# PeakChatTranslator

Automatic chat translation for **PEAK** (via [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/)).

[🇧🇷 Português](https://github.com/gustavogordoni/PeakChatTranslator/blob/main/README.ptbr.md) | **English**

## Features

### Incoming message translation
- Automatically translates other players' messages to your configured language
- Shows original message + translation below with colored prefix `[TR-XX]`
- Supports **PT**, **EN**, **ES** (dropdown selection in Mod Config)
- Default provider: **MyMemory** (free, no API key, auto-detect language)

### Outgoing translation (command)
- `/tr your message` → sends original + public translation to everyone
- Configurable languages: `OutgoingSourceLanguage` (your language) → `OutgoingTargetLanguage` (target language)

### Whisper translation (TinyTweaks integration)
- `/w player /tr message` → sends original whisper + translated whisper **only to target**
- Purple formatting (`#8973a1`) with `(secret msg for you)` matching TinyTweaks style
- You see translation locally; recipient receives it formatted as private whisper

### Settings (Mod Config / Gale)
| Section | Option | Type | Default |
|---------|--------|------|---------|
| General | Enabled | Toggle | On |
| General | TargetLanguage | Dropdown (pt/en/es) | pt |
| General | TranslateOwnMessages | Toggle | Off |
| General | Provider | Dropdown | MyMemory |
| Display | TranslationPrefix | String | TR |
| Display | TranslationColor | Hex | #7FC8FF |
| Outgoing | OutgoingCommandPrefix | String | /tr |
| Outgoing | OutgoingTargetLanguage | Dropdown (pt/en/es) | en |
| Outgoing | OutgoingSourceLanguage | Dropdown (pt/en/es) | pt |

## Dependencies
- [BepInExPack_PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) 5.4.75301+
- [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/) 1.3.4+
- Optional: [TinyTweaks](https://thunderstore.io/c/peak/p/YonDev/TinyTweaks/) (for `/w` whispers)

## Installation
1. Install dependencies above via Thunderstore/Gale
2. Download `afxgg-PeakChatTranslator-0.2.0.zip` from Thunderstore
3. Install via Gale or extract to `BepInEx/plugins/`

## Local Build
```bash
dotnet build -c Release
# DLL at artifacts/bin/PeakChatTranslator/release/Gordoni.PeakChatTranslator.dll
# Thunderstore package at artifacts/thunderstore/release/
```

---

## Credits / AI Disclosure

> **This project was developed almost entirely with AI.**
>
> - **Tool**: [opencode](https://opencode.ai/) (software engineering CLI)
> - **Model**: Nemotron 3 Ultra Free (NVIDIA)
> - All C# code, Harmony patches, BepInEx/Photon integration, configs, ThunderPipe build/packaging, and documentation were generated/iterated via opencode prompts.
> - AI analyzed base mods (PeakTextChat, TinyTweaks) to properly integrate via public APIs and Photon events.

Human contribution: requirements review, in-game testing, UX decisions (dropdowns, commands, colors), publishing.

## License
MIT — see [LICENSE](LICENSE).