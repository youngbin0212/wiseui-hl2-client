using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public static class WiseUiRuntimeConfig
{
    public const string DefaultInitServerUrl = "http://127.0.0.1:8002";
    public const string FileName = "wiseui.runtime.json";

    [Serializable]
    class Payload
    {
        public string initServerUrl;
    }

    public static string NormalizeBaseUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return DefaultInitServerUrl;

        string normalized = value.Trim().TrimEnd('/');
        Uri uri;
        if (!Uri.TryCreate(normalized, UriKind.Absolute, out uri) ||
            uri.Scheme != Uri.UriSchemeHttp ||
            string.IsNullOrWhiteSpace(uri.Host) ||
            !string.IsNullOrEmpty(uri.Query) ||
            !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new ArgumentException("Initialization server URL must be an absolute HTTP URL.", nameof(value));
        }
        return normalized;
    }

    public static string BuildJson(string value)
    {
        return JsonUtility.ToJson(new Payload { initServerUrl = NormalizeBaseUrl(value) });
    }

    public static IEnumerator Load(Action<string, string> completed)
    {
        string path = Path.Combine(Application.streamingAssetsPath, FileName);
        using (var request = UnityWebRequest.Get(path))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var payload = JsonUtility.FromJson<Payload>(request.downloadHandler.text);
                    completed(NormalizeBaseUrl(payload == null ? null : payload.initServerUrl), FileName);
                    yield break;
                }
                catch (Exception error)
                {
                    Debug.LogWarning("[WiseUiRuntimeConfig] invalid packaged config: " + error.Message);
                }
            }
            else
            {
                Debug.Log("[WiseUiRuntimeConfig] packaged config unavailable; using loopback fallback");
            }
        }
        completed(DefaultInitServerUrl, "fallback");
    }
}
