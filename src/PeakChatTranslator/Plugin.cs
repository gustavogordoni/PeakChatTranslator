using System;
using System.Collections;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExitGames.Client.Photon;
using HarmonyLib;
using PeakChatTranslator.Patches;
using PeakTextChat;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace PeakChatTranslator;

[BepInDependency("com.borealityy.peaktextchat")]
[BepInAutoPlugin]
public partial class Plugin : BaseUnityPlugin
{
    internal static Plugin Instance = null!;
    internal static ManualLogSource Log { get; private set; } = null!;

    internal static ConfigEntry<bool> Enabled = null!;
    internal static ConfigEntry<string> TargetLanguage = null!;
    internal static ConfigEntry<bool> TranslateOwnMessages = null!;
    internal static ConfigEntry<TranslatorProvider> Provider = null!;
    internal static ConfigEntry<string> LibreTranslateUrl = null!;
    internal static ConfigEntry<string> LibreTranslateApiKey = null!;
    internal static ConfigEntry<string> TranslationPrefix = null!;
    internal static ConfigEntry<string> TranslationColor = null!;

    internal static ConfigEntry<string> OutgoingCommandPrefix = null!;
    internal static ConfigEntry<string> OutgoingTargetLanguage = null!;
    internal static ConfigEntry<string> OutgoingSourceLanguage = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        Enabled = Config.Bind("General", "Enabled", true, "Enable/disable chat translation");
        TargetLanguage = Config.Bind("General", "TargetLanguage", "pt", "The language to translate incoming chat messages into (e.g. pt, en, es, de, fr, ja)");
        TranslateOwnMessages = Config.Bind("General", "TranslateOwnMessages", false, "Also translate your own received messages in the chat display");
        Provider = Config.Bind("General", "Provider", TranslatorProvider.MyMemory, "Translation provider to use (MyMemory is free and needs no API key)");
        LibreTranslateUrl = Config.Bind("General", "LibreTranslateUrl", "https://libretranslate.com/translate", "LibreTranslate API endpoint");
        LibreTranslateApiKey = Config.Bind("General", "LibreTranslateApiKey", "", "Optional LibreTranslate api key");
        TranslationPrefix = Config.Bind("Display", "TranslationPrefix", "TR", "Prefix shown before the translated text, e.g. [TR]");
        TranslationColor = Config.Bind("Display", "TranslationColor", "#7FC8FF", "Color (hex) used for the translated line, e.g. #7FC8FF");

        OutgoingCommandPrefix = Config.Bind("Outgoing", "OutgoingCommandPrefix", "/tr", "Command prefix to trigger outgoing translation (e.g. /tr, /translate). Message after prefix gets translated and sent as second line.");
        OutgoingTargetLanguage = Config.Bind("Outgoing", "OutgoingTargetLanguage", "en", "Language to translate your outgoing messages into (e.g. en, es, de, fr)");
        OutgoingSourceLanguage = Config.Bind("Outgoing", "OutgoingSourceLanguage", "pt", "Your language (source for outgoing translation). Use 'auto' for auto-detect");

        Harmony.CreateAndPatchAll(typeof(TextChatDisplayPatch), Id);
        Harmony.CreateAndPatchAll(typeof(SendChatMessagePatch), Id);

        Log.LogInfo($"Plugin {Name} is loaded!");
    }

    private void OnDestroy()
    {
        Harmony.UnpatchID(Id);
    }

    internal void HandleTranslationResult(TranslationResult result, TextChatManager.Message messageData)
    {
        if (!Enabled.Value)
            return;
        if (string.IsNullOrWhiteSpace(result.Text))
            return;

        var target = TargetLanguage.Value.Trim();
        if (string.IsNullOrEmpty(target))
            return;
        if (IsSameLanguage(result.SourceLang, target))
            return;

        if (messageData != null
            && !TranslateOwnMessages.Value
            && messageData.character != null
            && messageData.character == Character.localCharacter)
            return;

        if (TextChatDisplay.instance == null)
            return;

        var prefix = string.IsNullOrWhiteSpace(TranslationPrefix.Value) ? "TR" : TranslationPrefix.Value.Trim();
        var color = TranslationColor.Value;
        if (string.IsNullOrWhiteSpace(color))
            color = "#7FC8FF";
        if (!color.StartsWith('#'))
            color = "#" + color;

        TextChatDisplay.instance.AddMessage($"<color={color}>[{prefix}] {result.Text}</color>");
    }

    internal static bool IsTranslationMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;
        var stripped = TranslationService.StripRichText(message).TrimStart();
        return stripped.StartsWith("[TR-", StringComparison.OrdinalIgnoreCase)
            || stripped.StartsWith("[TR:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameLanguage(string detected, string target)
    {
        if (string.IsNullOrWhiteSpace(detected))
            return false;

        var d = detected.Trim().ToLowerInvariant();
        if (d.Length > 2 && d[2] == '-')
            d = d.Substring(0, 2);

        var t = target.Trim().ToLowerInvariant();
        if (t.Length > 2 && t[2] == '-')
            t = t.Substring(0, 2);

        return d == t;
    }

    internal static bool TryExtractCommandMessage(string message, out string extracted)
    {
        extracted = null;
        var prefix = OutgoingCommandPrefix.Value?.Trim();
        if (string.IsNullOrWhiteSpace(prefix))
            return false;
        var trimmed = message.TrimStart();
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return false;
        var rest = trimmed.Substring(prefix.Length).TrimStart();
        if (string.IsNullOrWhiteSpace(rest))
            return false;
        extracted = rest;
        return true;
    }

    internal void SendOutgoingTranslation(string textToTranslate)
    {
        if (!Enabled.Value)
            return;
        if (IsTranslationMessage(textToTranslate))
            return;
        if (string.IsNullOrWhiteSpace(textToTranslate))
            return;

        var targetLang = OutgoingTargetLanguage.Value?.Trim();
        var sourceLang = OutgoingSourceLanguage.Value?.Trim();
        if (string.IsNullOrEmpty(targetLang) || string.IsNullOrEmpty(sourceLang))
            return;
        if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase))
            return;

        Action<TranslationResult> onResult = result =>
        {
            if (!Enabled.Value || string.IsNullOrWhiteSpace(result.Text))
                return;
            if (IsSameLanguage(result.SourceLang, targetLang))
                return;

            var prefix = $"TR-{targetLang.ToUpperInvariant()}";
            var translatedMsg = $"[{prefix}] {result.Text}";

            try
            {
                var chatEventCode = (byte)81;
                var payload = new object[]
                {
                    PhotonNetwork.LocalPlayer.NickName,
                    translatedMsg,
                    PhotonNetwork.LocalPlayer.UserId,
                    false
                };

                PhotonNetwork.RaiseEvent(
                    chatEventCode,
                    payload,
                    new RaiseEventOptions { Receivers = ReceiverGroup.All },
                    SendOptions.SendReliable
                );
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to send outgoing translation: {ex.Message}");
            }
        };

        Action<string> onError = error => Plugin.Log.LogWarning($"Outgoing translation failed for '{textToTranslate}': {error}");

        StartCoroutine(TranslationService.TranslateWithSourceCoroutine(
            textToTranslate,
            sourceLang,
            targetLang,
            Provider.Value,
            LibreTranslateUrl.Value,
            LibreTranslateApiKey.Value,
            onResult,
            onError));
    }
}