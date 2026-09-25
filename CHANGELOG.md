# Changelog

## [0.2.4] - 2026-09-24
### Documentation
- Updated README: command format is `/tr /w player message` (inverted format)
- Updated README: Google Translate is now the default translation provider
- Updated version references to 0.2.4

## [0.2.3] - 2026-09-24
### Added
- Full MyMemory language support (100+ languages including Asian: ko, ja, zh, ru, etc.)
- Dropdown language selectors for both incoming and outgoing translation
- `/tr` now sends ONLY translated message (no duplicate original) to prevent double-translation

### Changed
- Removed unused configs: Provider, LibreTranslateUrl, LibreTranslateApiKey, TranslateOwnMessages, OutgoingCommandPrefix, OutgoingSourceLanguage
- Simplified UI: General (Enabled, Target Language), Display (Prefix, Color), Outgoing (Translate My Messages To)
- `/tr` command hardcoded, source language auto-uses Target Language
- Outgoing whisper (`/w player /tr`) sends only translated whisper with purple formatting

### Fixed
- Duplicate translation issue: recipients no longer receive both original + translated
- Whisper translations now use correct purple color (#8973a1) with "(secret msg for you)"

## [0.2.2] - 2026-09-24
### Fixed
- README cross-links now use absolute GitHub URLs (work on Thunderstore page)
### Added
- MyMemory provider (free, no API key, auto-detect) as default
- Outgoing translation command `/tr` with dropdown language selection (PT/EN/ES)
- Whisper translation combo: `/w player /tr message` — sends original + translated whisper
- Whisper translations styled with TinyTweaks purple color and "(secret msg for you)"
- Local preview of outgoing whisper translations
- README links PT↔EN

### Changed
- `TranslateOwnMessages` default to `false`
- Language configs now use dropdown selectors (PT/EN/ES)
- Switched from Google Translate (blocked) to MyMemory

### Fixed
- Incoming whisper translations now work
- Translation loop prevention for own messages

## [0.1.0] - 2026-09-23
### Added
- Initial release
- Incoming message translation with `[TR]` prefix
- Google/LibreTranslate providers