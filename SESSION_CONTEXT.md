# PeakChatTranslator - Session Context Document

## Project Overview

**PeakChatTranslator** is a BepInEx plugin for the game **PEAK** that provides automatic chat translation via the [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/) mod.

**GitHub**: https://github.com/gustavogordoni/PeakChatTranslator
**Thunderstore**: https://thunderstore.io/c/peak/p/afxgg/PeakChatTranslator/ (team: afxgg)
**Current Thunderstore version**: 0.2.4 | **Local development**: 0.2.6

---

## Core Functionality

### 1. Incoming Message Translation (Automatic)
- Every message from other players is automatically translated to the user's configured language
- Original message remains visible; translation appears below with `[TR-XX]` prefix
- Uses Google Translate (primary) with MyMemory fallback
- Configuration: **General → Target Language** = user's native/preferred language

### 2. Outgoing Message Translation (Command `/tr`)
- User types `/tr message` → sends **only the translated version** (original is blocked/cancelled)
- Prevents double-translation when both parties have the mod
- Source language = **General → Target Language** (user's language)
- Target language = **Outgoing → Translate My Messages To** (what others receive)
- Format: `[TR-EN] Translated message`

### 3. Whisper Translation (TinyTweaks Integration)
- Command: `/tr /w player message` (inverted format - `/tr` first, then `/w`)
- Sends **only translated whisper** to target player
- Purple formatting (`#8973a1`) with `(secret msg for you)` matching TinyTweaks style
- User sees translation locally; recipient receives formatted private whisper

---

## Technical Architecture

### Core Components

| File | Purpose |
|------|---------|
| `src/PeakChatTranslator/Plugin.cs` | Main plugin entry, config bindings, translation orchestration |
| `src/PeakChatTranslator/TranslationService.cs` | HTTP translation logic (Google + MyMemory with fallback) |
| `src/PeakChatTranslator/Patches/TextChatDisplayPatch.cs` | Harmony Postfix on `AddMessage(Message)` - handles incoming |
| `src/PeakChatTranslator/Patches/SendChatMessagePatch.cs` | Harmony Prefix on `SendChatMessage` - intercepts `/tr` commands |

### Key Design Decisions

#### 1. Harmony Prefix for Outgoing (Cancels Original)
```csharp
// SendChatMessagePatch.cs - Prefix returns false to CANCEL original
private static bool Prefix(string message) {
    if (message.StartsWith("/tr")) {
        SendOutgoingTranslation(extractedText);
        return false; // CANCEL original message from being sent
    }
    return true; // Allow normal messages
}
```
This prevents the "triple message" problem: original + recipient's auto-translate + sender's translation.

#### 2. Translation Pipeline (Google → MyMemory Fallback)
```
TranslateCoroutine(text, targetLang, provider)
    → TryTranslate with primary provider (Google)
    → On HTTP error / parse error → TryTranslate with fallback (MyMemory)
    → Cache results (key: provider|sourceLang|targetLang|text)
    → Callback with TranslationResult { Text, SourceLang }
```

#### 3. Language Handling
- **Codes**: ISO 639-1 (en, pt-BR, es, ja, ko, zh, ru, etc.) - 100+ supported
- **Display names**: "English (en)", "Português do Brasil (pt-BR)", "Japanese / 日本語 (ja)" 
- **Internal**: Extract code from display name via regex `\(([a-zA-Z0-9\-]+)\)$`
- **Target Language default**: English (en) - matches code default
- **Outgoing default**: English (en)

#### 4. Translation Providers
| Provider | Endpoint | Auth | Quality | Notes |
|----------|----------|------|---------|-------|
| **Google** (primary) | `translate.googleapis.com/translate_a/single?client=gtx&sl=auto&tl={tl}&dt=t&q={q}` | None | High | Unofficial endpoint, needs User-Agent header |
| **MyMemory** (fallback) | `api.mymemory.translated.net/get?q={q}&langpair={source}|{target}` | None | Medium | Free tier, rate limited |

---

## Configuration System

### Config Entries (BepInEx Config)

| Section | Key | Type | Default | Description |
|---------|-----|------|---------|-------------|
| **General** | Enabled | Toggle | true | Master switch |
| **General** | Target Language | Dropdown (100+) | English (en) | User's native language (incoming target) |
| **General** | Translation Provider | Enum | Google | Google / MyMemory |
| **Display** | Translation Prefix | String | TR | Prefix in `[TR-XX]` |
| **Display** | Translation Color | Hex | #7FC8FF | Color for translation lines |
| **Outgoing** | Translate My Messages To | Dropdown (100+) | English (en) | Target for `/tr` messages |

### Config Paths
- **Linux (Gale)**: `~/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/config/Gordoni.PeakChatTranslator.cfg`
- **Auto-generated** on first run

---

## Build System

### Project Structure
```
PeakChatTranslator/
├── artifacts/
│   ├── bin/PeakChatTranslator/release/Gordoni.PeakChatTranslator.dll
│   └── thunderstore/release/afxgg-PeakChatTranslator-0.2.6.zip
├── src/PeakChatTranslator/
│   ├── PeakChatTranslator.csproj
│   ├── Plugin.cs
│   ├── TranslationService.cs
│   ├── Patches/
│   │   ├── TextChatDisplayPatch.cs
│   │   └── SendChatMessagePatch.cs
│   └── screenshots/ (3 PNG files)
├── Config.Build.user.props (local paths)
├── Directory.Build.props / .targets (shared build config)
├── README.md / README.ptbr.md
├── CHANGELOG.md
└── LICENSE
```

### Build Commands
```bash
# Standard build (debug)
dotnet build

# Release build
dotnet build -c Release

# Release + Thunderstore package
dotnet build -c Release
# Output: artifacts/thunderstore/release/afxgg-PeakChatTranslator-0.2.6.zip

# Deploy to BepInEx (auto via DeployModFiles=true)
dotnet build -c Release
# Copies to: ~/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/plugins/afxgg-PeakChatTranslator/
```

### Key Build Files

**Config.Build.user.props** (local, gitignored):
```xml
<PEAKGameRootDir>$(HOME)/.local/share/Steam/steamapps/common/PEAK/</PEAKGameRootDir>
<PEAKBepInExDir>$(HOME)/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/</PEAKBepInExDir>
<GalePluginsDir>$(HOME)/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/plugins/</GalePluginsDir>
<DeployModFiles>true</DeployModFiles>
```

**PeakChatTranslator.csproj** references:
```xml
<Reference Include="PeakTextChat">
  <HintPath>$(GalePluginsDir)borealityy-PeakTextChat/PeakTextChat.dll</HintPath>
  <Private>false</Private>
</Reference>
```

---

## Version History

| Version | Date | Key Changes |
|---------|------|-------------|
| **0.2.6** | 2026-10-03 | Absolute GitHub URLs for screenshots (Thunderstore compatibility) |
| **0.2.5** | 2026-10-03 | Docs: generalized screenshots, removed PT-specific examples, defaults fixed |
| **0.2.4** | 2026-09-24 | Google Translate default, `/tr /w` format, removed unused configs |
| **0.2.3** | 2026-09-24 | Google Translate provider, fallback chain, `/tr` cancels original |
| **0.2.2** | 2026-09-24 | README absolute URLs |
| **0.2.1** | 2026-09-24 | MyMemory default, dropdown languages, `/tr` command |
| **0.2.0** | 2026-09-23 | Initial MyMemory, dropdowns, whisper `/w /tr` |

**Current Thunderstore**: 0.2.4 | **Local**: 0.2.6 ready

---

## Development Practices

### 1. Code Style
- **No comments** unless explicitly requested
- **Nullable warnings** enabled but tolerated
- **C# latest** (langversion: latest)
- **Nullable reference types** enabled

### 2. Git Workflow
- **Conventional commits**: `docs:`, `feat:`, `fix:`, `chore:`
- **Version bump** in `.csproj` before Thunderstore build
- **CHANGELOG.md** maintained manually (Keep a Changelog format)
- **No auto-commit** unless explicitly requested

### 3. Testing Approach
- **Manual in-game testing** via Gale profile
- **Log monitoring**: `tail -f ~/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/LogOutput.log | rg "PeakChatTranslator|Translation"`
- **Debug via UnityEngine.Debug.Log** in TranslationService (appears in BepInEx log)

### 4. Debugging Translation Issues
```bash
# Monitor in real-time
tail -f ~/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/LogOutput.log | rg "PeakChatTranslator|Translation|fallback|Google|MyMemory"

# Key log patterns:
# "Plugin PeakChatTranslator loaded!" - plugin loaded
# "Primary provider Google failed: HTTP 403... Trying fallback: MyMemory" - fallback working
# "Translation failed for '...': HTTP 403" - provider error
```

### 4. Thunderstore Publishing
```bash
# 1. Bump version in csproj
<Version>0.2.X</Version>

# 2. Build
dotnet build -c Release

# 3. Verify manifest.json in ZIP
unzip -p artifacts/thunderstore/release/afxgg-PeakChatTranslator-0.2.X.zip manifest.json

# 4. Upload at https://thunderstore.io/package/create/peak/
# Select team: afxgg
# Upload ZIP
```

---

## Key Files Quick Reference

| File | Lines | Purpose |
|------|-------|---------|
| `Plugin.cs` | ~370 | Main plugin, config, translation orchestration, whisper/outgoing logic |
| `TranslationService.cs` | ~197 | HTTP requests, Google/MyMemory parsing, fallback logic, caching |
| `TextChatDisplayPatch.cs` | ~35 | Incoming message interception (Postfix) |
| `SendChatMessagePatch.cs` | ~56 | Outgoing `/tr` interception (Prefix - cancels original) |
| `README.md` | ~99 | English docs with absolute image URLs |
| `README.ptbr.md` | ~99 | Portuguese docs with absolute image URLs |
| `CHANGELOG.md` | ~55 | Version history |
| `Config.Build.user.props` | ~123 | Local build paths (gitignored) |

---

## Screenshots (in `src/PeakChatTranslator/screenshots/`)

| File | Description | GitHub URL |
|------|-------------|------------|
| `englishTranslatedInto Portuguese.png` | Incoming: English message → Portuguese translation | `.../englishTranslatedInto%20Portuguese.png` |
| `originalSentencePortuguese.png` | Outgoing before: `/tr estou escrevendo...` | `.../originalSentencePortuguese.png` |
| `translatedIntoEnglish.png` | Outgoing after: `[TR-EN] I am writing...` | `.../translatedIntoEnglish.png` |

**GitHub Raw URLs** (used in READMEs):
```
https://raw.githubusercontent.com/gustavogordoni/PeakChatTranslator/refs/heads/main/screenshots/englishTranslatedInto%20Portuguese.png
https://raw.githubusercontent.com/gustavogordoni/PeakChatTranslator/refs/heads/main/screenshots/originalSentencePortuguese.png
https://raw.githubusercontent.com/gustavogordoni/PeakChatTranslator/refs/heads/main/screenshots/translatedIntoEnglish.png
```

---

## Thunderstore Package Details

**Team**: afxgg
**Package**: afxgg-PeakChatTranslator
**Current published**: 0.2.4
**Next to publish**: 0.2.6

**Manifest (from 0.2.6 ZIP)**:
```json
{
  "name": "PeakChatTranslator",
  "version_number": "0.2.6",
  "description": "Translates PeakTextChat via Google Translate (primary) with MyMemory fallback. 100+ languages. /tr command sends only translation. /tr /w player msg for whispers.",
  "website_url": "https://github.com/gustavogordoni/PeakChatTranslator",
  "dependencies": [
    "BepInEx-BepInExPack_PEAK-5.4.75301",
    "borealityy-PeakTextChat-1.3.4"
  ]
}
```

---

## Known Issues / Limitations

1. **TinyTweaks `/w player /tr msg` not interceptable** - TinyTweaks uses direct `RaiseEvent`, bypasses `SendChatMessage`. Use `/tr /w player msg` instead.
2. **Google endpoint unofficial** - May break if Google changes API; fallback to MyMemory handles this.
3. **MyMemory rate limits** - Free tier has daily quota; fallback may exhaust.
4. **Config default Target Language = English** - Change to user's language in Mod Config.
5. **Images in Thunderstore** - Require absolute GitHub raw URLs (fixed in 0.2.6).

---

## Commands Quick Reference

| Command | Description | Intercepted? |
|---------|-------------|--------------|
| `/tr message` | Send translated message (original blocked) | ✅ Yes (Prefix) |
| `/tr /w player message` | Send translated whisper to player | ✅ Yes (Prefix) |
| `/w player /tr message` | Legacy format | ❌ No (TinyTweaks direct RaiseEvent) |
| Normal message | Sent normally, auto-translated on receive | N/A |

---

## AI Development Context

**Tool**: opencode (software engineering CLI)
**Model**: Nemotron 3 Ultra Free (NVIDIA)
**Human role**: Requirements, testing, UX decisions, publishing
**AI role**: All C# code, Harmony patches, BepInEx/Photon integration, configs, ThunderPipe packaging, documentation

---

## Quick Start for New Session

```bash
cd /home/gordoni/dev/PeakChatTranslator

# Build & deploy to local Gale
dotnet build -c Release

# Verify build
ls -la artifacts/thunderstore/release/afxgg-PeakChatTranslator-0.2.6.zip

# Check logs
tail -f ~/.local/share/com.kesomannen.gale/peak/profiles/Default/BepInEx/LogOutput.log | rg "PeakChatTranslator"

# Test in game: join lobby, type /tr hello world
```

---

## Pending / Next Steps

- [ ] Publish 0.2.6 to Thunderstore (upload ZIP)
- [ ] Test whisper translation with TinyTweaks in multiplayer
- [ ] Consider adding LibreTranslate as third provider option
- [ ] Add config for translation prefix/color per-language
- [ ] Investigate Google endpoint stability for production

---

*Last updated: 2026-10-03 | Session: v0.2.6 documentation update*