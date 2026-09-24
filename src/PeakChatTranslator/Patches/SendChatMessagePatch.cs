using System;
using HarmonyLib;
using PeakTextChat;

namespace PeakChatTranslator.Patches;

[HarmonyPatch(typeof(TextChatManager), nameof(TextChatManager.SendChatMessage))]
internal static class SendChatMessagePatch
{
    private static void Postfix(string message)
    {
        if (!Plugin.Enabled.Value)
            return;
        if (string.IsNullOrWhiteSpace(message))
            return;
        if (Plugin.IsTranslationMessage(message))
            return;
        if (!Plugin.TryExtractCommandMessage(message, out var toTranslate))
            return;

        Plugin.Instance?.SendOutgoingTranslation(toTranslate);
    }
}