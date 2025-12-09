using System;
using System.Collections.Generic;
using UnityEngine;

namespace Muabe.WebView
{
    [DisallowMultipleComponent]
    public class FlutterWebBridge : MonoBehaviour
    {
        private const string LogPrefix = WebViewConstants.LogPrefixFlutterBridge;
        private const string DefaultEventName = WebViewConstants.DefaultFlutterEventName;
        private const string CommandType = WebViewConstants.CommandTypeVisibility;

    [SerializeField]
    [Tooltip("명시하지 않으면 같은 게임오브젝트의 WebViewController를 자동으로 찾습니다.")]
    private WebViewController webViewController;

    [SerializeField]
    [Tooltip("플러터 측에서 수신할 CustomEvent 이름")]
    private string unityToFlutterEventName = DefaultEventName;

    private readonly Dictionary<string, bool> widgetVisibility = new Dictionary<string, bool>();
    private bool videosLoaded = false;
    private int totalVideos = 0;
    private int loadedVideos = 0;

    // 비디오 로드 완료 이벤트
    public event Action<int, int> OnVideosLoaded;
    
    // WebView 숨김/표시 이벤트
    public event Action OnWebViewHidden;
    public event Action OnWebViewShown;

    public bool AreVideosLoaded => videosLoaded;
    public int TotalVideos => totalVideos;
    public int LoadedVideos => loadedVideos;

    private void Awake()
    {
        if (webViewController == null)
        {
            webViewController = GetComponent<WebViewController>();
        }

        unityToFlutterEventName = SanitizeEventName(unityToFlutterEventName);

            if (webViewController == null)
            {
                WebViewUtility.LogError(LogPrefix, "WebViewController reference could not be resolved. Bridge will not function.");
            }
    }

    private void OnValidate()
    {
        unityToFlutterEventName = SanitizeEventName(unityToFlutterEventName);
    }

    public WebViewController WebViewController => webViewController;

    public void SetWidgetVisibility(string widgetId, bool visible)
    {
        if (string.IsNullOrWhiteSpace(widgetId))
        {
            Debug.LogWarning($"{LogPrefix} SetWidgetVisibility called with empty widgetId.");
            return;
        }

        widgetId = widgetId.Trim();
        widgetVisibility[widgetId] = visible;

        var payload = new WidgetVisibilityPayload
        {
            type = CommandType,
            widgetId = widgetId,
            visible = visible
        };

        DispatchToFlutter(payload);
    }

    public void ShowWidget(string widgetId) => SetWidgetVisibility(widgetId, true);

    public void HideWidget(string widgetId) => SetWidgetVisibility(widgetId, false);

    public void HideWebView()
    {
        if (webViewController == null)
        {
            Debug.LogWarning($"{LogPrefix} WebViewController is not assigned.");
            return;
        }

        Debug.Log($"{LogPrefix} Hiding WebView");
        webViewController.SetVisible(false);
        OnWebViewHidden?.Invoke();
    }

    public void ShowWebView()
    {
        if (webViewController == null)
        {
            Debug.LogWarning($"{LogPrefix} WebViewController is not assigned.");
            return;
        }

        Debug.Log($"{LogPrefix} Showing WebView");
        webViewController.SetVisible(true);
        OnWebViewShown?.Invoke();
    }

    public void ToggleWidgetVisibility(string widgetId)
    {
        if (string.IsNullOrWhiteSpace(widgetId))
        {
            Debug.LogWarning($"{LogPrefix} ToggleWidgetVisibility called with empty widgetId.");
            return;
        }

        widgetId = widgetId.Trim();
        if (!widgetVisibility.TryGetValue(widgetId, out var current))
        {
            current = true;
        }
        var nextVisible = !current;
        SetWidgetVisibility(widgetId, nextVisible);
    }

    public void NavigateToPage(string pagePath)
    {
        if (string.IsNullOrWhiteSpace(pagePath))
        {
            Debug.LogWarning($"{LogPrefix} NavigateToPage called with empty pagePath.");
            return;
        }

        var payload = new NavigationPayload
        {
            type = "navigateToPage",
            path = pagePath.Trim()
        };

        DispatchToFlutter(payload);
        Debug.Log($"{LogPrefix} NavigateToPage dispatched: {pagePath}");
    }

    public void SendLoadVideosCommand()
    {
        var payload = new SimplePayload
        {
            type = "loadVideos"
        };

        DispatchToFlutter(payload);
        Debug.Log($"{LogPrefix} SendLoadVideosCommand dispatched");
    }

    private void DispatchToFlutter(object payload)
    {
        if (webViewController == null)
        {
            Debug.LogWarning($"{LogPrefix} WebViewController is not assigned.");
            return;
        }

        var json = JsonUtility.ToJson(payload);
        var eventName = SanitizeEventName(unityToFlutterEventName);
        
        var js = $@"(function() {{
    var payload = {json};
    var bridge = window.__unityBridge = window.__unityBridge || {{}};
    bridge.queue = bridge.queue || [];
    
    if (bridge.handleMessage) {{
        try {{
            bridge.handleMessage(payload);
        }} catch (err) {{
            console.error('[Unity→Flutter] handleMessage error', err);
        }}
    }} else {{
        bridge.queue.push(payload);
    }}
    
    window.dispatchEvent(new CustomEvent('{eventName}', {{ detail: payload }}));
}})();";
        
        webViewController.RunJavaScript(js);
    }

        public void HandleFlutterToUnityMessage(string messageType, string messageJson)
        {
            Debug.Log($"{LogPrefix} Received message type: {messageType}");

            switch (messageType)
            {
                case "hideWebView":
                    HideWebView();
                    break;
                case "showWebView":
                    ShowWebView();
                    break;
                case "videosLoaded":
                    HandleVideosLoaded(messageJson);
                    break;
                default:
                    Debug.LogWarning($"{LogPrefix} Unknown message type from Flutter: {messageType}");
                    break;
            }
        }

        private void HandleVideosLoaded(string messageJson)
        {
            try
            {
                var message = JsonUtility.FromJson<VideosLoadedPayload>(messageJson);
                videosLoaded = true;
                totalVideos = message.totalVideos;
                loadedVideos = message.loadedVideos;
                
                Debug.Log($"{LogPrefix} Videos loaded: {loadedVideos}/{totalVideos}");
                Debug.Log($"{LogPrefix} Video keys: {string.Join(", ", message.videoKeys ?? new string[0])}");
                
                // 이벤트 발행
                Debug.Log($"{LogPrefix} Invoking OnVideosLoaded event. Subscribers: {(OnVideosLoaded != null ? OnVideosLoaded.GetInvocationList().Length : 0)}");
                OnVideosLoaded?.Invoke(loadedVideos, totalVideos);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LogPrefix} Failed to parse videosLoaded message: {ex.Message}");
            }
        }

        private static string SanitizeEventName(string eventName)
        {
            return WebViewUtility.SanitizeEventName(eventName, DefaultEventName);
        }

        [System.Serializable]
        private class WidgetVisibilityPayload
        {
            public string type;
            public string widgetId;
            public bool visible;
        }

        [System.Serializable]
        private class NavigationPayload
        {
            public string type;
            public string path;
        }

        [System.Serializable]
        private class SimplePayload
        {
            public string type;
        }

        [System.Serializable]
        private class VideosLoadedPayload
        {
            public string type;
            public int totalVideos;
            public int loadedVideos;
            public string[] videoKeys;
        }
    }
}
