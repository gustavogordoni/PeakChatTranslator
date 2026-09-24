# Changelog

## [2.1.0] - 2026-09-24
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