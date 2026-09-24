using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace PeakChatTranslator;

public enum TranslatorProvider
{
    MyMemory,
    Google,
    LibreTranslate
}

public readonly struct TranslationResult
{
    public readonly string Text;
    public readonly string SourceLang;

    public TranslationResult(string text, string sourceLang)
    {
        Text = text;
        SourceLang = sourceLang;
    }
}

internal static class TranslationService
{
    private static readonly Regex RichTextRegex = new("<.*?>", RegexOptions.Compiled);
    private static readonly ConcurrentDictionary<string, string> Cache = new();

    public static string StripRichText(string text)
    {
        return RichTextRegex.Replace(text, string.Empty);
    }

    public static IEnumerator TranslateCoroutine(
        string text,
        string targetLang,
        TranslatorProvider provider,
        string libreBaseUrl,
        string libreApiKey,
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        yield return TranslateWithSourceCoroutine(text, "auto", targetLang, provider, libreBaseUrl, libreApiKey, onResult, onError);
    }

    public static IEnumerator TranslateWithSourceCoroutine(
        string text,
        string sourceLang,
        string targetLang,
        TranslatorProvider provider,
        string libreBaseUrl,
        string libreApiKey,
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        var effectiveSource = string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase) ? "auto" : sourceLang;
        var cacheKey = $"{provider}|{effectiveSource}|{targetLang}|{text}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            onResult(new TranslationResult(cached, cached == text ? targetLang : effectiveSource));
            yield break;
        }

        using var request = BuildRequest(text, effectiveSource, targetLang, provider, libreBaseUrl, libreApiKey);
        if (request == null)
            yield break;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke($"HTTP {(int)request.responseCode} {request.error}");
            yield break;
        }

        TranslationResult result;
        try
        {
            result = provider switch
            {
                TranslatorProvider.MyMemory => ParseMyMemory(request.downloadHandler.text),
                TranslatorProvider.Google => ParseGoogle(request.downloadHandler.text, targetLang),
                TranslatorProvider.LibreTranslate => ParseLibre(request.downloadHandler.text),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(result.Text))
            Cache[cacheKey] = result.Text;

        onResult(result);
    }

    private static UnityWebRequest BuildRequest(string text, string sourceLang, string targetLang, TranslatorProvider provider, string libreBaseUrl, string libreApiKey)
    {
        return provider switch
        {
            TranslatorProvider.MyMemory => BuildMyMemoryRequest(text, sourceLang, targetLang),
            TranslatorProvider.Google => BuildGoogleRequest(text, targetLang),
            TranslatorProvider.LibreTranslate => BuildLibreRequest(text, targetLang, libreBaseUrl, libreApiKey),
            _ => throw new ArgumentOutOfRangeException(nameof(provider))
        };
    }

    private static UnityWebRequest BuildMyMemoryRequest(string text, string sourceLang, string targetLang)
    {
        var src = string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase) ? "Autodetect" : sourceLang;
        var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(text)
                + $"&langpair={Uri.EscapeDataString(src)}|{Uri.EscapeDataString(targetLang)}";

        var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        return request;
    }

    private static TranslationResult ParseMyMemory(string json)
    {
        var root = JObject.Parse(json);
        var data = (JObject?)root["responseData"];
        var translated = data?["translatedText"]?.ToString() ?? string.Empty;
        var source = data?["detectedLanguage"]?.ToString() ?? "?";
        return new TranslationResult(translated, source);
    }

    private static UnityWebRequest BuildGoogleRequest(string text, string targetLang)
    {
        var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto"
                + $"&tl={Uri.EscapeDataString(targetLang)}&dt=t&q={Uri.EscapeDataString(text)}";

        var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        return request;
    }

    private static UnityWebRequest BuildLibreRequest(string text, string targetLang, string baseUrl, string apiKey)
    {
        var payload = new JObject
        {
            ["q"] = text,
            ["source"] = "auto",
            ["target"] = targetLang,
            ["format"] = "text"
        };
        if (!string.IsNullOrEmpty(apiKey))
            payload["api_key"] = apiKey;

        var request = new UnityWebRequest(baseUrl, UnityWebRequest.kHttpVerbPOST)
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(payload.ToString())),
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 15
        };
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }

    private static TranslationResult ParseGoogle(string json, string targetLang)
    {
        var root = JArray.Parse(json);
        var sb = new System.Text.StringBuilder();
        foreach (var part in (JArray)root[0]!)
            sb.Append(part![0]!.ToString());
        var source = root[2]?.ToString() ?? "?";
        return new TranslationResult(sb.ToString(), source);
    }

    private static TranslationResult ParseLibre(string json)
    {
        var root = JObject.Parse(json);
        return new TranslationResult(root["translatedText"]?.ToString() ?? string.Empty, "?");
    }
}