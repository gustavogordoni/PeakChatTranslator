using System;
using System.Collections;
using System.Text.RegularExpressions;
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
        
        var langOptions = new AcceptableValueList<string>("pt", "en", "es");
        
        TargetLanguage = Config.Bind("General", "TargetLanguage", "pt",
            new ConfigDescription("Language to translate incoming chat messages into", langOptions));
        TranslateOwnMessages = Config.Bind("General", "TranslateOwnMessages", false, 
            "Also translate your own received messages in the chat display");
        Provider = Config.Bind("General", "Provider", TranslatorProvider.MyMemory, 
            "Translation provider to use (MyMemory is free and needs no API key)");
        LibreTranslateUrl = Config.Bind("General", "LibreTranslateUrl", "https://libretranslate.com/translate", 
            "LibreTranslate API endpoint");
        LibreTranslateApiKey = Config.Bind("General", "LibreTranslateApiKey", "", 
            "Optional LibreTranslate api key");
        TranslationPrefix = Config.Bind("Display", "TranslationPrefix", "TR", 
            "Prefix shown before the translated text, e.g. [TR]");
        TranslationColor = Config.Bind("Display", "TranslationColor", "#7FC8FF", 
            "Color (hex) used for the translated line, e.g. #7FC8FF");

        OutgoingCommandPrefix = Config.Bind("Outgoing", "OutgoingCommandPrefix", "/tr", 
            "Command prefix to trigger outgoing translation (e.g. /tr, /translate). Message after prefix gets translated and sent as second line.");
        OutgoingTargetLanguage = Config.Bind("Outgoing", "OutgoingTargetLanguage", "en",
            new ConfigDescription("Language to translate your outgoing messages into", langOptions));
        OutgoingSourceLanguage = Config.Bind("Outgoing", "OutgoingSourceLanguage", "pt",
            new ConfigDescription("Your language (source for outgoing translation)", langOptions));

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

        var targetLang = OutgoingTargetLanguage.Value?.Trim() ?? "en";
        var sourceLang = OutgoingSourceLanguage.Value?.Trim() ?? "pt";
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

    internal void SendWhisperTranslation(string targetName, string textToTranslate)
    {
        if (!Enabled.Value)
            return;
        if (IsTranslationMessage(textToTranslate))
            return;
        if (string.IsNullOrWhiteSpace(textToTranslate))
            return;

        var targetPlayer = FindPlayerByName(targetName);
        if (targetPlayer == null)
        {
            Log.LogWarning($"Whisper translation: player '{targetName}' not found");
            return;
        }

        var targetLang = OutgoingTargetLanguage.Value?.Trim() ?? "en";
        var sourceLang = OutgoingSourceLanguage.Value?.Trim() ?? "pt";
        if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase))
            return;

        Action<TranslationResult> onResult = result =>
        {
            if (!Enabled.Value || string.IsNullOrWhiteSpace(result.Text))
                return;
            if (IsSameLanguage(result.SourceLang, targetLang))
                return;

            var prefix = $"TR-{targetLang.ToUpperInvariant()}";
            var translatedText = result.Text;

            // Formato whisper igual ao TinyTweaks: cor roxa #8973a1 + (secret msg for you)
            var whisperColor = "#8973a1";
            var translatedMsg = $"<color={whisperColor}>[{prefix}] {translatedText} <size=16><i>(secret msg for you)</i></size></color>";

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
                    new RaiseEventOptions { TargetActors = new[] { targetPlayer.ActorNumber } },
                    SendOptions.SendReliable
                );

                // Mostrar a tradução localmente para quem enviou
                if (TextChatDisplay.instance != null)
                {
                    TextChatDisplay.instance.AddMessage(translatedMsg);
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Failed to send whisper translation: {ex.Message}");
            }
        };

        Action<string> onError = error => Plugin.Log.LogWarning($"Whisper translation failed for '{textToTranslate}': {error}");

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

    private static Photon.Realtime.Player FindPlayerByName(string name)
    {
        var cleanName = Regex.Replace(name.ToLower(), @"</?color(=\w+|=[#\w]+)?>", string.Empty, RegexOptions.IgnoreCase);
        foreach (var plr in PhotonNetwork.PlayerList)
        {
            var plrClean = Regex.Replace(plr.NickName.ToLower(), @"</?color(=\w+|=[#\w]+)?>", string.Empty, RegexOptions.IgnoreCase);
            if (plrClean.Contains(cleanName, StringComparison.OrdinalIgnoreCase))
                return plr;
        }
        return null;
    }
}