using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace PeakChatTranslator;

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
        TranslationProvider provider,
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        yield return TranslateWithSourceCoroutine(text, "en", targetLang, provider, onResult, onError);
    }

    public static IEnumerator TranslateWithSourceCoroutine(
        string text,
        string sourceLang,
        string targetLang,
        TranslationProvider provider,
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        // Try primary provider first
        yield return TryTranslate(text, sourceLang, targetLang, provider, onResult, onError);
    }

    private static IEnumerator TryTranslate(
        string text,
        string sourceLang,
        string targetLang,
        TranslationProvider provider,
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        var cacheKey = $"{provider}|{sourceLang}|{targetLang}|{text}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            onResult(new TranslationResult(cached, sourceLang));
            yield break;
        }

        using var request = BuildRequest(text, sourceLang, targetLang, provider);
        if (request == null)
        {
            onError?.Invoke($"Unknown provider: {provider}");
            yield break;
        }

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            var errorMsg = $"HTTP {(int)request.responseCode} {request.error}";
            var fallbackProvider = GetFallbackProvider(provider);
            
            if (fallbackProvider != TranslationProvider.Google && fallbackProvider != provider)
            {
                // Try fallback
                LogDebug($"Primary provider {provider} failed: {errorMsg}. Trying fallback: {fallbackProvider}");
                yield return TryTranslate(text, sourceLang, targetLang, fallbackProvider, onResult, onError);
            }
            else
            {
                onError?.Invoke(errorMsg);
            }
            yield break;
        }

        TranslationResult result = default;
        Exception parseException = null;
        try
        {
            result = provider switch
            {
                TranslationProvider.Google => ParseGoogle(request.downloadHandler.text, targetLang),
                TranslationProvider.MyMemory => ParseMyMemory(request.downloadHandler.text),
                _ => throw new ArgumentOutOfRangeException(nameof(provider))
            };
        }
        catch (Exception ex)
        {
            parseException = ex;
        }

        if (parseException != null)
        {
            var fallbackProvider = GetFallbackProvider(provider);
            if (fallbackProvider != TranslationProvider.Google && fallbackProvider != provider)
            {
                LogDebug($"Primary provider {provider} parse error: {parseException.Message}. Trying fallback: {fallbackProvider}");
                yield return TryTranslate(text, sourceLang, targetLang, fallbackProvider, onResult, onError);
            }
            else
            {
                onError?.Invoke(parseException.Message);
            }
            yield break;
        }

        if (!string.IsNullOrWhiteSpace(result.Text))
            Cache[cacheKey] = result.Text;

        onResult(result);
    }

    private static TranslationProvider GetFallbackProvider(TranslationProvider provider)
    {
        return provider == TranslationProvider.Google ? TranslationProvider.MyMemory : TranslationProvider.Google;
    }

    private static void LogDebug(string msg)
    {
        // Use Unity Debug.Log since we don't have Plugin.Log here
        UnityEngine.Debug.Log($"[PeakChatTranslator] {msg}");
    }

    private static UnityWebRequest BuildRequest(string text, string sourceLang, string targetLang, TranslationProvider provider)
    {
        return provider switch
        {
            TranslationProvider.Google => BuildGoogleRequest(text, targetLang),
            TranslationProvider.MyMemory => BuildMyMemoryRequest(text, sourceLang, targetLang),
            _ => null
        };
    }

    private static UnityWebRequest BuildGoogleRequest(string text, string targetLang)
    {
        var url = "https://translate.googleapis.com/translate_a/single?client=gtx&sl=auto"
                + $"&tl={Uri.EscapeDataString(targetLang)}&dt=t&q={Uri.EscapeDataString(text)}";

        var request = UnityWebRequest.Get(url);
        request.timeout = 10;
        // Add User-Agent to avoid 403
        request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        return request;
    }

    private static UnityWebRequest BuildMyMemoryRequest(string text, string sourceLang, string targetLang)
    {
        var url = "https://api.mymemory.translated.net/get?q=" + Uri.EscapeDataString(text)
                + $"&langpair={Uri.EscapeDataString(sourceLang)}|{Uri.EscapeDataString(targetLang)}";

        var request = UnityWebRequest.Get(url);
        request.timeout = 15;
        return request;
    }

    private static TranslationResult ParseGoogle(string json, string targetLang)
    {
        // Google returns: [[["translated","original",...],...],"detected_lang",...]
        var root = JArray.Parse(json);
        var sb = new System.Text.StringBuilder();
        foreach (var part in (JArray)root[0]!)
            sb.Append(part![0]!.ToString());

        var detected = root[2]?.ToString() ?? "en";
        return new TranslationResult(sb.ToString(), detected);
    }

    private static TranslationResult ParseMyMemory(string json)
    {
        var root = JObject.Parse(json);
        var data = (JObject?)root["responseData"];
        var translated = data?["translatedText"]?.ToString() ?? string.Empty;
        var source = data?["detectedLanguage"]?.ToString() ?? "en";
        return new TranslationResult(translated, source);
    }
}