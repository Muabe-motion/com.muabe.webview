using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class FlutterWebBridge : MonoBehaviour
{
    private const string LogPrefix = "[FlutterWebBridge]";
    private const string DefaultEventName = "UnityToFlutter";
    private const string CommandType = "setWidgetVisibility";

    [SerializeField]
    [Tooltip("명시하지 않으면 같은 게임오브젝트의 WebViewController를 자동으로 찾습니다.")]
    private WebViewController webViewController;

    [SerializeField]
    [Tooltip("플러터 측에서 수신할 CustomEvent 이름")]
    private string unityToFlutterEventName = DefaultEventName;

    private readonly Dictionary<string, bool> widgetVisibility = new Dictionary<string, bool>();

    private void Awake()
    {
        if (webViewController == null)
        {
            webViewController = GetComponent<WebViewController>();
        }

        unityToFlutterEventName = SanitizeEventName(unityToFlutterEventName);

        if (webViewController == null)
        {
            Debug.LogError($"{LogPrefix} WebViewController reference could not be resolved. Bridge will not function.");
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

    private void DispatchToFlutter(WidgetVisibilityPayload payload)
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
            console.error('{LogPrefix} handleMessage error', err);
        }}
    }} else {{
        bridge.queue.push(payload);
    }}
    window.dispatchEvent(new CustomEvent('{eventName}', {{ detail: payload }}));
}})();";
        webViewController.RunJavaScript(js);
    }

    private static string SanitizeEventName(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            return DefaultEventName;
        }

        return eventName.Trim().Replace("'", "\\'");
    }

    [System.Serializable]
    private class WidgetVisibilityPayload
    {
        public string type;
        public string widgetId;
        public bool visible;
    }
}
