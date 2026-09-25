# PeakChatTranslator

**Português** | [English](https://github.com/gustavogordoni/PeakChatTranslator/blob/main/README.md)

Tradução automática de chat para **PEAK** (via [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/)).

## Funcionalidades

### Tradução de mensagens recebidas
- Traduz automaticamente mensagens de outros jogadores para o idioma configurado
- Exibe a mensagem original + tradução logo abaixo com prefixo colorido `[TR-XX]`
- Suporta **100+ idiomas** via Google Translate (primário) com fallback MyMemory (gratuito, sem API key, auto-detect)
- Inclui idiomas asiáticos: Coreano (ko), Japonês (ja), Chinês (zh), Russo (ru), Árabe (ar), Hindi (hi), Tailandês (th), Vietnamita (vi), e muitos mais

### Tradução de mensagens enviadas (comando)
- `/tr sua mensagem` → envia **apenas a mensagem traduzida** (sem duplicar o original)
- Evita tradução duplicada quando o destinatário também tem o mod
- Idioma alvo configurável em "Translate My Messages To" (Traduzir Minhas Mensagens Para)

### Tradução de sussurros (integração com TinyTweaks)
- `/tr /w jogador mensagem` → envia **apenas o sussurro traduzido** para o alvo
- Formatação roxa (`#8973a1`) com `(secret msg for you)` igual ao TinyTweaks
- Você vê a tradução localmente; o destinatário recebe formatado como sussurro privado

### Configurações (Mod Config / Gale)
| Seção | Opção | Tipo | Padrão |
|-------|-------|------|--------|
| **General** | Enabled | Toggle | On |
| **General** | Target Language | Dropdown (100+ idiomas) | pt |
| **Display** | Translation Prefix | String | TR |
| **Display** | Translation Color | Hex | #7FC8FF |
| **Outgoing** | Translate My Messages To | Dropdown (100+ idiomas) | en |

## Dependências
- [BepInExPack_PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/) 5.4.75301+
- [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/) 1.3.4+
- Opcional: [TinyTweaks](https://thunderstore.io/c/peak/p/YonDev/TinyTweaks/) (para sussurros `/w`)

## Instalação
1. Instale as dependências acima via Thunderstore/Gale
2. Baixe `afxgg-PeakChatTranslator-0.2.4.zip` do Thunderstore
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
GPLv3 — veja [LICENSE](LICENSE).
