# PeakChatTranslator

Tradução automática de chat para **PEAK** (via [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/)).

**Português** | [🇺🇸 English](https://github.com/gustavogordoni/PeakChatTranslator/blob/main/README.md)

## Funcionalidades

### Tradução de mensagens recebidas
- Traduz automaticamente mensagens de outros jogadores para o idioma configurado
- Exibe a mensagem original + tradução logo abaixo com prefixo colorido `[TR-XX]`
- Suporta **PT**, **EN**, **ES** (seleção via dropdown no Mod Config)
- Provedor padrão: **MyMemory** (gratuito, sem API key, com auto-detect de idioma)

### Tradução de mensagens enviadas (comando)
- `/tr sua mensagem` → envia original + tradução pública para todos
- Idiomas configuráveis: `OutgoingSourceLanguage` (seu idioma) → `OutgoingTargetLanguage` (idioma alvo)

### Whisper traduzido (integração com TinyTweaks)
- `/w jogador /tr mensagem` → envia whisper original + whisper traduzido **apenas para o alvo**
- Formatação roxa (`#8973a1`) com `(secret msg for you)` igual ao TinyTweaks
- Você vê a tradução localmente; o destinatário recebe formatado como whisper privado

### Configurações (Mod Config / Gale)
| Seção | Opção | Tipo | Padrão |
|-------|-------|------|--------|
| General | Enabled | Toggle | On |
| General | TargetLanguage | Dropdown (pt/en/es) | pt |
| General | TranslateOwnMessages | Toggle | Off |
| General | Provider | Dropdown | MyMemory |
| Display | TranslationPrefix | String | TR |
| Display | TranslationColor | Hex | #7FC8FF |
| Outgoing | OutgoingCommandPrefix | String | /tr |
| Outgoing | OutgoingTargetLanguage | Dropdown (pt/en/es) | en |
| Outgoing | OutgoingSourceLanguage | Dropdown (pt/en/es) | pt |

## Dependências
- [BepInExPack_PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) 5.4.75301+
- [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/) 1.3.4+
- Opcional: [TinyTweaks](https://thunderstore.io/c/peak/p/YonDev/TinyTweaks/) (para whispers `/w`)

## Instalação
1. Instale as dependências acima via Thunderstore/Gale
2. Baixe o `afxgg-PeakChatTranslator-0.2.0.zip` do Thunderstore
3. Instale via Gale ou extraia em `BepInEx/plugins/`

## Build local
```bash
dotnet build -c Release
# DLL em artifacts/bin/PeakChatTranslator/release/Gordoni.PeakChatTranslator.dll
# Pacote Thunderstore em artifacts/thunderstore/release/
```

---

## Créditos / Divulgação de IA

> **Este projeto foi desenvolvido praticamente inteiro com IA.**
>
> - **Ferramenta**: [opencode](https://opencode.ai/) (CLI de engenharia de software)
> - **Modelo**: Nemotron 3 Ultra Free (NVIDIA)
> - Todo o código C#, patches Harmony, integração BepInEx/Photon, configs, build/packaging ThunderPipe, e documentação foram gerados/iterados via prompts no opencode.
> - A IA analisou os mods base (PeakTextChat, TinyTweaks) para acoplar corretamente via APIs públicas e eventos Photon.

Código humano: revisão de requisitos, testes no jogo, decisões de UX (dropdowns, comandos, cores), publicação.

## Licença
MIT — veja [LICENSE](LICENSE).