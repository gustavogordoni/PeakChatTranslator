using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using PeakTextChat;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakChatTranslator.Patches;

[HarmonyPatch(typeof(TextChatManager), nameof(TextChatManager.SendChatMessage))]
internal static class SendChatMessagePatch
{
    private static readonly Regex WhisperTranslateRegex = new(@"^/w\s+(\S+)\s+/tr\s+(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void Postfix(string message)
    {
        if (!Plugin.Enabled.Value)
            return;
        if (string.IsNullOrWhiteSpace(message))
            return;
        if (Plugin.IsTranslationMessage(message))
            return;

        var match = WhisperTranslateRegex.Match(message.Trim());
        if (match.Success)
        {
            var targetName = match.Groups[1].Value;
            var textToTranslate = match.Groups[2].Value;
            Plugin.Instance?.SendWhisperTranslation(targetName, textToTranslate);
            return;
        }

        if (!Plugin.TryExtractCommandMessage(message, out var toTranslate))
            return;

        Plugin.Instance?.SendOutgoingTranslation(toTranslate);
    }
}