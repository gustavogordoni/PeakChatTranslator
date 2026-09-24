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
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        yield return TranslateWithSourceCoroutine(text, "auto", targetLang, onResult, onError);
    }

    public static IEnumerator TranslateWithSourceCoroutine(
        string text,
        string sourceLang,
        string targetLang,
        Action<TranslationResult> onResult,
        Action<string> onError)
    {
        var effectiveSource = string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase) ? "auto" : sourceLang;
        var cacheKey = $"{effectiveSource}|{targetLang}|{text}";
        if (Cache.TryGetValue(cacheKey, out var cached))
        {
            onResult(new TranslationResult(cached, cached == text ? targetLang : effectiveSource));
            yield break;
        }

        using var request = BuildMyMemoryRequest(text, effectiveSource, targetLang);
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
            result = ParseMyMemory(request.downloadHandler.text);
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

    private static UnityWebRequest BuildMyMemoryRequest(string text, string sourceLang, string targetLang)
    {
        var src = string.Equals(sourceLang, "auto", StringComparison.OrdinalIgnoreCase) ? "auto" : sourceLang;
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
}