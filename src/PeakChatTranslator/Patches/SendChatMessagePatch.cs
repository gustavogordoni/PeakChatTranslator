using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using PeakTextChat;

namespace PeakChatTranslator.Patches;

[HarmonyPatch(typeof(TextChatManager), nameof(TextChatManager.SendChatMessage))]
internal static class SendChatMessagePatch
{
    private static readonly Regex WhisperTranslateRegex = new(@"^/w\s+(\S+)\s+/tr\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex TranslateCommandRegex = new(@"^/tr\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Novo formato invertido: /tr /w Nome mensagem
    private static readonly Regex TranslateWhisperRegex = new(@"^/tr\s+/w\s+(\S+)\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool Prefix(string message)
    {
        if (!Plugin.Enabled.Value) return true;
        if (string.IsNullOrWhiteSpace(message)) return true;
        if (Plugin.IsTranslationMessage(message)) return true;

        // Formato invertido: /tr /w Nome mensagem (PRIORIDADE - mais específico)
        var trWhisperMatch = TranslateWhisperRegex.Match(message.Trim());
        if (trWhisperMatch.Success)
        {
            var targetName = trWhisperMatch.Groups[1].Value;
            var textToTranslate = trWhisperMatch.Groups[2].Value;
            Plugin.Instance?.SendWhisperTranslation(targetName, textToTranslate);
            return false; // CANCEL original
        }

        // Whisper padrão do TinyTweaks: /w Nome /tr mensagem (não interceptável, TinyTweaks usa RaiseEvent direto)
        var whisperMatch = WhisperTranslateRegex.Match(message.Trim());
        if (whisperMatch.Success)
        {
            // TinyTweaks envia direto via RaiseEvent, não passa por aqui
            // Não podemos interceptar - usar formato invertido /tr /w ...
            // Mas tentamos processar mesmo assim caso passe por aqui
            var targetName = whisperMatch.Groups[1].Value;
            var textToTranslate = whisperMatch.Groups[2].Value;
            Plugin.Instance?.SendWhisperTranslation(targetName, textToTranslate);
            return false;
        }

        // Comando normal: /tr mensagem
        var translateMatch = TranslateCommandRegex.Match(message.Trim());
        if (translateMatch.Success)
        {
            var textToTranslate = translateMatch.Groups[1].Value;
            Plugin.Instance?.SendOutgoingTranslation(textToTranslate);
            return false; // CANCEL original message with /tr
        }

        return true; // Allow normal messages through
    }
}