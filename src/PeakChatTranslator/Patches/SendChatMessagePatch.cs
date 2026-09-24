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

    // Prefix runs BEFORE original method. Return false to CANCEL original message.
    private static bool Prefix(string message)
    {
        if (!Plugin.Enabled.Value) return true;
        if (string.IsNullOrWhiteSpace(message)) return true;
        if (Plugin.IsTranslationMessage(message)) return true;

        var whisperMatch = WhisperTranslateRegex.Match(message.Trim());
        if (whisperMatch.Success)
        {
            var targetName = whisperMatch.Groups[1].Value;
            var textToTranslate = whisperMatch.Groups[2].Value;
            Plugin.Instance?.SendWhisperTranslation(targetName, textToTranslate);
            return false; // CANCEL original whisper message
        }

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