# PeakChatTranslator

**Português** | [English](https://github.com/gustavogordoni/PeakChatTranslator/blob/main/README.md)

Tradução automática de chat para **PEAK** (via [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/)).

## Capturas de tela

### Tradução de mensagens recebidas
Um jogador com **Português** como idioma nativo recebe uma mensagem em inglês, com a tradução aparecendo logo abaixo:

**Configurações usadas na imagem:**
- **General → Target Language**: `Português do Brasil (pt-BR)` (seu idioma nativo)
- O outro jogador escreve em inglês → você vê traduzido para português automaticamente

![Tradução de entrada](screenshots/englishTranslatedInto%20Portuguese.png)

### Tradução de mensagens enviadas (comando `/tr`)
Digite `/tr` seguido da sua mensagem para enviar **apenas a versão traduzida**:

**Configurações usadas na imagem:**
- **General → Target Language**: `Português do Brasil (pt-BR)` (seu idioma nativo — origem)
- **Outgoing → Translate My Messages To**: `English (en)` (idioma que os outros recebem)

| Antes (você digita no seu idioma) | Depois (enviado ao chat traduzido) |
|-----------------------------------|----------------------------------|
| ![Frase original](screenshots/originalSentencePortuguese.png) | ![Mensagem traduzida](screenshots/translatedIntoEnglish.png) |

## Como funciona

### Mensagens recebidas (automático)
- Toda mensagem de outros jogadores é traduzida automaticamente para **seu** idioma
- Mensagem original permanece visível; tradução aparece abaixo com prefixo `[TR-XX]`
- Configure: **General → Target Language** = seu idioma nativo/preferido

### Mensagens enviadas (comando `/tr`)
- Digite `/tr sua mensagem` → envia **apenas a versão traduzida** (original é bloqueada)
- Idioma de origem = **General → Target Language** (seu idioma)
- Idioma alvo = **Outgoing → Translate My Messages To** (o que os outros recebem)
- Funciona para qualquer par de idiomas suportado pelo Google Translate/MyMemory

### Tradução de sussurros (TinyTweaks)
- `/tr /w jogador mensagem` → envia **apenas o sussurro traduzido** para o alvo
- Formatação roxa com `(secret msg for you)` igual ao TinyTweaks

## Configurações (Mod Config)
| Seção | Opção | Tipo | Padrão |
|---------|--------|------|--------|
| **General** | Enabled | Toggle | On |
| **General** | Target Language | Dropdown (100+ idiomas) | English (en) |
| **Display** | Translation Prefix | String | TR |
| **Display** | Translation Color | Hex | #7FC8FF |
| **Outgoing** | Translate My Messages To | Dropdown (100+ idiomas) | English (en) |

## Guia rápido
1. **General → Target Language** = o idioma que VOCÊ lê/escreve (seu idioma nativo)
2. **Outgoing → Translate My Messages To** = o idioma que OS OUTROS devem receber suas mensagens
3. Use `/tr mensagem` para enviar mensagens traduzidas
4. Use `/tr /w jogador mensagem` para sussurros traduzidos

## Funcionalidades
- **100+ idiomas** via Google Translate (primário) com fallback MyMemory (gratuito, sem API key)
- Tradução automática de entrada + manual de saída via `/tr`
- Tradução de sussurros com integração TinyTweaks
- Prevenção de tradução duplicada quando ambos têm o mod
- Suporte a idiomas asiáticos: Coreano, Japonês, Chinês, Russo, Árabe, Hindi, Tailandês, Vietnamita, etc.

## Dependências
- [BepInExPack_PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/)
- [PeakTextChat](https://thunderstore.io/c/peak/p/borealityy/PeakTextChat/)
- Opcional: [TinyTweaks](https://thunderstore.io/c/peak/p/YonDev/TinyTweaks/) (para sussurros `/w`)

## Instalação
1. Instale as dependências acima via Thunderstore/Gale
2. Baixe `afxgg-PeakChatTranslator-0.2.5.zip` do Thunderstore
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