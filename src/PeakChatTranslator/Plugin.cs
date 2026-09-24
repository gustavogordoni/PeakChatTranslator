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

    internal static ConfigEntry<string> TranslationPrefix = null!;
    internal static ConfigEntry<string> TranslationColor = null!;

    internal static ConfigEntry<string> OutgoingTargetLanguage = null!;

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        var allLanguages = GetSupportedLanguages();
        var langOptions = new AcceptableValueList<string>(allLanguages);

        Enabled = Config.Bind("General", "Enabled", true, "Enable/disable chat translation");

        TargetLanguage = Config.Bind("General", "Target Language", "pt",
            new ConfigDescription("Translate incoming messages into this language", langOptions));

        TranslationPrefix = Config.Bind("Display", "Translation Prefix", "TR",
            "Prefix shown before translated text (e.g. [TR])");
        TranslationColor = Config.Bind("Display", "Translation Color", "#7FC8FF",
            "Color (hex) for translated lines");

        OutgoingTargetLanguage = Config.Bind("Outgoing", "Translate My Messages To", "en",
            new ConfigDescription("When using /tr, translate your messages into this language", langOptions));

        Harmony.CreateAndPatchAll(typeof(TextChatDisplayPatch), Id);
        Harmony.CreateAndPatchAll(typeof(SendChatMessagePatch), Id);

        Log.LogInfo($"Plugin {Name} loaded!");
    }

    private void OnDestroy()
    {
        Harmony.UnpatchID(Id);
    }

    internal void HandleTranslationResult(TranslationResult result, TextChatManager.Message messageData)
    {
        if (!Enabled.Value) return;
        if (string.IsNullOrWhiteSpace(result.Text)) return;

        var target = TargetLanguage.Value?.Trim() ?? "pt";
        if (string.IsNullOrEmpty(target)) return;
        if (IsSameLanguage(result.SourceLang, target)) return;

        if (messageData != null && messageData.character != null && messageData.character == Character.localCharacter)
            return;

        if (TextChatDisplay.instance == null) return;

        var prefix = string.IsNullOrWhiteSpace(TranslationPrefix.Value) ? "TR" : TranslationPrefix.Value.Trim();
        var color = TranslationColor.Value;
        if (string.IsNullOrWhiteSpace(color)) color = "#7FC8FF";
        if (!color.StartsWith('#')) color = "#" + color;

        TextChatDisplay.instance.AddMessage($"<color={color}>[{prefix}] {result.Text}</color>");
    }

    internal static bool IsTranslationMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message)) return false;
        var stripped = TranslationService.StripRichText(message).TrimStart();
        return stripped.StartsWith("[TR-", StringComparison.OrdinalIgnoreCase)
            || stripped.StartsWith("[TR:", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSameLanguage(string detected, string target)
    {
        if (string.IsNullOrWhiteSpace(detected)) return false;
        var d = detected.Trim().ToLowerInvariant();
        if (d.Length > 2 && d[2] == '-') d = d.Substring(0, 2);
        var t = target.Trim().ToLowerInvariant();
        if (t.Length > 2 && t[2] == '-') t = t.Substring(0, 2);
        return d == t;
    }

    internal static bool TryExtractCommandMessage(string message, out string extracted)
    {
        extracted = null;
        const string prefix = "/tr";
        var trimmed = message.TrimStart();
        if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return false;
        var rest = trimmed.Substring(prefix.Length).TrimStart();
        if (string.IsNullOrWhiteSpace(rest)) return false;
        extracted = rest;
        return true;
    }

    internal void SendOutgoingTranslation(string textToTranslate)
    {
        if (!Enabled.Value) return;
        if (IsTranslationMessage(textToTranslate)) return;
        if (string.IsNullOrWhiteSpace(textToTranslate)) return;

        var targetLang = OutgoingTargetLanguage.Value?.Trim() ?? "en";
        var sourceLang = TargetLanguage.Value?.Trim() ?? "pt";
        if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase)) return;

        Action<TranslationResult> onResult = result =>
        {
            if (!Enabled.Value || string.IsNullOrWhiteSpace(result.Text)) return;
            if (IsSameLanguage(result.SourceLang, targetLang)) return;

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
            onResult,
            onError));
    }

    internal void SendWhisperTranslation(string targetName, string textToTranslate)
    {
        if (!Enabled.Value) return;
        if (IsTranslationMessage(textToTranslate)) return;
        if (string.IsNullOrWhiteSpace(textToTranslate)) return;

        var targetPlayer = FindPlayerByName(targetName);
        if (targetPlayer == null)
        {
            Log.LogWarning($"Whisper translation: player '{targetName}' not found");
            return;
        }

        var targetLang = OutgoingTargetLanguage.Value?.Trim() ?? "en";
        var sourceLang = TargetLanguage.Value?.Trim() ?? "pt";
        if (string.Equals(sourceLang, targetLang, StringComparison.OrdinalIgnoreCase)) return;

        Action<TranslationResult> onResult = result =>
        {
            if (!Enabled.Value || string.IsNullOrWhiteSpace(result.Text)) return;
            if (IsSameLanguage(result.SourceLang, targetLang)) return;

            var prefix = $"TR-{targetLang.ToUpperInvariant()}";
            var translatedText = result.Text;

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

    private static string[] GetSupportedLanguages()
    {
        return new[]
        {
            "auto", "pt", "en", "es", "fr", "de", "it", "ru", "ja", "ko", "zh", "zh-TW",
            "ar", "hi", "tr", "pl", "nl", "sv", "da", "no", "fi", "cs", "hu", "ro",
            "bg", "hr", "sk", "sl", "et", "lv", "lt", "mt", "ga", "cy", "eu", "ca",
            "gl", "he", "th", "vi", "id", "ms", "tl", "sw", "af", "sq", "mk", "sr",
            "bs", "is", "mk", "ka", "hy", "az", "be", "kk", "ky", "uz", "mn", "my",
            "km", "lo", "si", "ne", "bn", "gu", "pa", "ta", "te", "kn", "ml", "or",
            "as", "mr", "sa", "ur", "fa", "ps", "sd", "ku", "ug", "yi", "ji", "zh-CN"
        };
    }
}