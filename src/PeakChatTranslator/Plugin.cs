using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        var displayNames = allLanguages.Select(GetLanguageDisplayName).ToArray();
        var langOptions = new AcceptableValueList<string>(displayNames);

        Enabled = Config.Bind("General", "Enabled", true, "Enable/disable chat translation");

        TargetLanguage = Config.Bind("General", "Target Language", GetLanguageDisplayName("en"),
            new ConfigDescription("Translate incoming messages into this language", langOptions));

        TranslationPrefix = Config.Bind("Display", "Translation Prefix", "TR",
            "Prefix shown before translated text (e.g. [TR])");
        TranslationColor = Config.Bind("Display", "Translation Color", "#7FC8FF",
            "Color (hex) for translated lines");

        OutgoingTargetLanguage = Config.Bind("Outgoing", "Translate My Messages To", GetLanguageDisplayName("en"),
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

        var target = GetLanguageCode(TargetLanguage.Value ?? "pt");
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

        var targetLang = GetLanguageCode(OutgoingTargetLanguage.Value ?? "en");
        var sourceLang = GetLanguageCode(TargetLanguage.Value ?? "pt");
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

        var targetLang = GetLanguageCode(OutgoingTargetLanguage.Value ?? "en");
        var sourceLang = GetLanguageCode(TargetLanguage.Value ?? "pt");
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

    private static readonly Dictionary<string, string> LanguageDisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pt-BR"] = "Português do Brasil (pt-BR)",
        ["pt-PT"] = "Português de Portugal (pt-PT)",
        ["en"] = "English (en)",
        ["es"] = "Spanish / Español (es)",
        ["fr"] = "French / Français (fr)",
        ["de"] = "German / Deutsch (de)",
        ["it"] = "Italian / Italiano (it)",
        ["ru"] = "Russian / Русский (ru)",
        ["ja"] = "Japanese / 日本語 (ja)",
        ["ko"] = "Korean / 한국어 (ko)",
        ["zh"] = "Chinese Simplified / 中文简体 (zh)",
        ["zh-TW"] = "Chinese Traditional / 中文繁體 (zh-TW)",
        ["zh-CN"] = "Chinese Simplified / 中文简体 (zh-CN)",
        ["ar"] = "Arabic / العربية (ar)",
        ["hi"] = "Hindi / हिन्दी (hi)",
        ["tr"] = "Turkish / Türkçe (tr)",
        ["pl"] = "Polish / Polski (pl)",
        ["nl"] = "Dutch / Nederlands (nl)",
        ["sv"] = "Swedish / Svenska (sv)",
        ["da"] = "Danish / Dansk (da)",
        ["no"] = "Norwegian / Norsk (no)",
        ["fi"] = "Finnish / Suomi (fi)",
        ["cs"] = "Czech / Čeština (cs)",
        ["hu"] = "Hungarian / Magyar (hu)",
        ["ro"] = "Romanian / Română (ro)",
        ["bg"] = "Bulgarian / Български (bg)",
        ["hr"] = "Croatian / Hrvatski (hr)",
        ["sk"] = "Slovak / Slovenčina (sk)",
        ["sl"] = "Slovenian / Slovenščina (sl)",
        ["et"] = "Estonian / Eesti (et)",
        ["lv"] = "Latvian / Latviešu (lv)",
        ["lt"] = "Lithuanian / Lietuvių (lt)",
        ["mt"] = "Maltese / Malti (mt)",
        ["ga"] = "Irish / Gaeilge (ga)",
        ["cy"] = "Welsh / Cymraeg (cy)",
        ["eu"] = "Basque / Euskara (eu)",
        ["ca"] = "Catalan / Català (ca)",
        ["gl"] = "Galician / Galego (gl)",
        ["he"] = "Hebrew / עברית (he)",
        ["th"] = "Thai / ไทย (th)",
        ["vi"] = "Vietnamese / Tiếng Việt (vi)",
        ["id"] = "Indonesian / Bahasa Indonesia (id)",
        ["ms"] = "Malay / Bahasa Melayu (ms)",
        ["tl"] = "Tagalog / Tagalog (tl)",
        ["sw"] = "Swahili / Kiswahili (sw)",
        ["af"] = "Afrikaans / Afrikaans (af)",
        ["sq"] = "Albanian / Shqip (sq)",
        ["mk"] = "Macedonian / Македонски (mk)",
        ["sr"] = "Serbian / Српски (sr)",
        ["bs"] = "Bosnian / Bosanski (bs)",
        ["is"] = "Icelandic / Íslenska (is)",
        ["ka"] = "Georgian / ქართული (ka)",
        ["hy"] = "Armenian / Հայերեն (hy)",
        ["az"] = "Azerbaijani / Azərbaycanca (az)",
        ["be"] = "Belarusian / Беларуская (be)",
        ["kk"] = "Kazakh / Қазақша (kk)",
        ["ky"] = "Kyrgyz / Кыргызча (ky)",
        ["uz"] = "Uzbek / O'zbekcha (uz)",
        ["mn"] = "Mongolian / Монгол (mn)",
        ["my"] = "Burmese / မြန်မာဘာသာ (my)",
        ["km"] = "Khmer / ខ្មែរ (km)",
        ["lo"] = "Lao / ລາວ (lo)",
        ["si"] = "Sinhala / සිංහල (si)",
        ["ne"] = "Nepali / नेपाली (ne)",
        ["bn"] = "Bengali / বাংলা (bn)",
        ["gu"] = "Gujarati / ગુજરાતી (gu)",
        ["pa"] = "Punjabi / ਪੰਜਾਬੀ (pa)",
        ["ta"] = "Tamil / தமிழ் (ta)",
        ["te"] = "Telugu / తెలుగు (te)",
        ["kn"] = "Kannada / ಕನ್ನಡ (kn)",
        ["ml"] = "Malayalam / മലയാളം (ml)",
        ["or"] = "Odia / ଓଡ଼ିଆ (or)",
        ["as"] = "Assamese / অসমীয়া (as)",
        ["mr"] = "Marathi / मराठी (mr)",
        ["sa"] = "Sanskrit / संस्कृतम् (sa)",
        ["ur"] = "Urdu / اردو (ur)",
        ["fa"] = "Persian / فارسی (fa)",
        ["ps"] = "Pashto / پښتو (ps)",
        ["sd"] = "Sindhi / سنڌي (sd)",
        ["ku"] = "Kurdish / کوردی (ku)",
        ["ug"] = "Uyghur / ئۇيغۇرچە (ug)",
        ["yi"] = "Yiddish / ייִדיש (yi)",
        ["ji"] = "Yiddish / ייִדיש (ji)",
    };

    private static string[] GetSupportedLanguages()
    {
        return LanguageDisplayNames.Keys.OrderBy(k => k).ToArray();
    }

    internal static string GetLanguageDisplayName(string code)
    {
        if (LanguageDisplayNames.TryGetValue(code, out var name))
            return name;
        return code;
    }

    internal static string GetLanguageCode(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return "en";
        var match = Regex.Match(displayName, @"\(([a-zA-Z0-9\-]+)\)$");
        if (match.Success)
            return match.Groups[1].Value;
        return LanguageDisplayNames.FirstOrDefault(kvp => kvp.Value.Equals(displayName, StringComparison.OrdinalIgnoreCase)).Key ?? "en";
    }
}