using System;
using System.Text.RegularExpressions;
using HarmonyLib;
using PeakTextChat;

namespace PeakChatTranslator.Patches;

[HarmonyPatch(typeof(TextChatDisplay), nameof(TextChatDisplay.AddMessage), new[] { typeof(TextChatManager.Message) })]
internal static class TextChatDisplayPatch
{
    private static readonly Regex RichTextRegex = new("<.*?>", RegexOptions.Compiled);

    private static void Postfix(TextChatManager.Message messageData)
    {
        if (!Plugin.Enabled.Value) return;
        if (messageData == null) return;

        var targetLang = Plugin.TargetLanguage.Value?.Trim() ?? "pt";
        if (string.IsNullOrWhiteSpace(targetLang)) return;

        var rawMessage = RichTextRegex.Replace(messageData.message, string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(rawMessage)) return;

        if (Plugin.IsTranslationMessage(rawMessage)) return;

        if (Plugin.Instance == null) return;

        Plugin.Instance.StartCoroutine(TranslationService.TranslateCoroutine(
            rawMessage,
            targetLang,
            result => Plugin.Instance.HandleTranslationResult(result, messageData),
            error => Plugin.Log.LogWarning($"Translation failed for \"{rawMessage}\": {error}")));
    }
}