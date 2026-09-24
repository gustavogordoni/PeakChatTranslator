using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PeakChatTranslator.Patches;
using PeakTextChat;

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

    private void Awake()
    {
        Instance = this;
        Log = Logger;

        Enabled = Config.Bind("General", "Enabled", true, "Enable/disable chat translation");
        TargetLanguage = Config.Bind("General", "TargetLanguage", "pt", "The language to translate chat messages into (e.g. pt, en, es, de, fr, ja)");
        TranslateOwnMessages = Config.Bind("General", "TranslateOwnMessages", true, "Also translate your own sent messages");
        Provider = Config.Bind("General", "Provider", TranslatorProvider.MyMemory, "Translation provider to use (MyMemory is free and needs no API key)");
        LibreTranslateUrl = Config.Bind("General", "LibreTranslateUrl", "https://libretranslate.com/translate", "LibreTranslate API endpoint");
        LibreTranslateApiKey = Config.Bind("General", "LibreTranslateApiKey", "", "Optional LibreTranslate api key");
        TranslationPrefix = Config.Bind("Display", "TranslationPrefix", "TR", "Prefix shown before the translated text, e.g. [TR]");
        TranslationColor = Config.Bind("Display", "TranslationColor", "#7FC8FF", "Color (hex) used for the translated line, e.g. #7FC8FF");

        Harmony.CreateAndPatchAll(typeof(TextChatDisplayPatch), Id);

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
}