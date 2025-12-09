using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Muabe.WebView
{
    public class WebViewController : MonoBehaviour
    {
        private const string LogPrefix = WebViewConstants.LogPrefixViewController;
        [Header("Local HTTP Server")]
        [Tooltip("로컬 서버 포트")]
        public int serverPort = WebViewConstants.DefaultServerPort;

    [Tooltip("웹 루트 경로 (예: /)")]
    [SerializeField]
    private string webRootPath = "/";

    [Header("WebView")]
    [Tooltip("초기 URL 자동 로드 여부")]
    public bool autoLoadOnStart = false;

    [Tooltip("iOS WKWebView 활성화")]
    public bool enableWKWebView = true;

    [Tooltip("WebView를 투명하게")]
    public bool transparent = true;

    [Tooltip("SafeArea를 무시하고 전체 화면으로 표시")]
    public bool ignoreSafeArea = false;

    private WebViewObject webViewObject;
    private LocalWebServer localServer;
    private string initialUrl;

    private bool isWebViewReady;
    private readonly Queue<string> pendingJavaScript = new Queue<string>();

    [Header("Overlay Margins (px)")]
    [SerializeField]
    private int overlayPaddingLeft;

    [SerializeField]
    private int overlayPaddingTop;

    [SerializeField]
    private int overlayPaddingRight;

    [SerializeField]
    private int overlayPaddingBottom;

    // 회전/해상도/세이프에어리어 변화 감지용 캐시
    private Rect lastSafeArea;
    private Vector2Int lastResolution;
    private ScreenOrientation lastOrientation;
    private bool applyingMargins;

    private void Awake()
    {
        localServer = GetComponent<LocalWebServer>();
        if (localServer == null)
        {
            localServer = gameObject.AddComponent<LocalWebServer>();
            Debug.Log($"{LogPrefix} LocalWebServer component auto-added.");
        }

        localServer.port = serverPort;
        Debug.Log($"{LogPrefix} Awake (port={serverPort}, autoLoadOnStart={autoLoadOnStart})");
    }

    private void Start()
    {
        StartCoroutine(InitializeWebView());
    }

    private void OnValidate()
    {
        overlayPaddingLeft = Mathf.Max(0, overlayPaddingLeft);
        overlayPaddingTop = Mathf.Max(0, overlayPaddingTop);
        overlayPaddingRight = Mathf.Max(0, overlayPaddingRight);
        overlayPaddingBottom = Mathf.Max(0, overlayPaddingBottom);
        
        if (Application.isPlaying && webViewObject != null)
        {
            ApplySafeAreaMargins();
        }
    }

        private IEnumerator InitializeWebView()
        {
            yield return new WaitForSeconds(WebViewConstants.WebViewInitDelay);

        initialUrl = BuildInitialUrl();
        Debug.Log($"{LogPrefix} InitializeWebView coroutine started. Platform: {Application.platform}, Initial URL: {initialUrl}");

        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        Debug.Log($"{LogPrefix} WebViewObject created.");
        isWebViewReady = false;

        webViewObject.Init(
            cb: (msg) =>
            {
                Debug.Log($"[WebView] Callback received: {msg}");
                HandleFlutterMessage(msg);
            },
            err: (msg) =>
            {
                Debug.LogError($"[WebView] Error: {msg}");
            },
            httpErr: (msg) =>
            {
                Debug.LogError($"[WebView] HTTP Error: {msg}");
            },
            started: (msg) =>
            {
                Debug.Log($"[WebView] Started: {msg}");
                isWebViewReady = false;
            },
            hooked: (msg) =>
            {
                Debug.Log($"[WebView] Hooked: {msg}");
            },
            ld: (msg) =>
            {
                Debug.Log($"[WebView] Loaded: {msg}");
                StartCoroutine(ApplyMarginsNextFrame());
                // WebView는 숨김 상태 유지 - WebViewShowButton에서 표시
                // webViewObject.SetVisibility(true); // 제거!
                isWebViewReady = true;
                
                // WebView 로드 직후 AVAudioSession 강제 재설정
                #if UNITY_IOS && !UNITY_EDITOR
                ResetAudioSessionForWebView();
                #endif
                
                InjectUnityCallFunction();
                FlushPendingJavaScript();
            },
            enableWKWebView: enableWKWebView,
            transparent: transparent,
            wkAllowsBackForwardNavigationGestures: false  // swipe back 제스처 비활성화
        );

        yield return null;
        ApplySafeAreaMargins();

        webViewObject.SetVisibility(false);

        if (autoLoadOnStart)
        {
            Debug.Log($"{LogPrefix} autoLoadOnStart enabled. Waiting for server to be ready...");
            yield return StartCoroutine(WaitForServerReady());
            Debug.Log($"{LogPrefix} Server is ready. Loading initial URL.");
            LoadInitialUrl();
        }

        webViewObject.SetCameraAccess(true);
        webViewObject.SetMicrophoneAccess(true);
    }
    
    private IEnumerator WaitForServerReady()
    {
        if (localServer == null)
        {
            Debug.LogWarning($"{LogPrefix} LocalWebServer not found. Proceeding without waiting.");
            yield break;
        }
        
        float timeout = 10f;
        float elapsed = 0f;
        
        while (!localServer.IsServerReady && elapsed < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            elapsed += 0.1f;
        }
        
        if (!localServer.IsServerReady)
        {
            Debug.LogWarning($"{LogPrefix} Server ready timeout after {timeout}s. Proceeding anyway.");
        }
    }

    public void LoadInitialUrl()
    {
        if (webViewObject == null)
        {
            Debug.LogWarning($"{LogPrefix} LoadInitialUrl called before WebView is ready.");
            return;
        }

        if (string.IsNullOrEmpty(initialUrl))
        {
            initialUrl = BuildInitialUrl();
        }

        Debug.Log($"{LogPrefix} Loading URL: {initialUrl}");
        webViewObject.LoadURL(initialUrl);
        isWebViewReady = false;
    }

    public void LoadUrl(string url)
    {
        if (webViewObject == null)
        {
            Debug.LogWarning($"{LogPrefix} LoadUrl called before WebView is ready.");
            return;
        }

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning($"{LogPrefix} LoadUrl called with empty URL.");
            return;
        }

        Debug.Log($"{LogPrefix} Loading URL: {url}");
        webViewObject.LoadURL(url);
        isWebViewReady = false;
    }

    public string GetInitialUrl()
    {
        if (string.IsNullOrEmpty(initialUrl))
        {
            initialUrl = BuildInitialUrl();
        }
        return initialUrl;
    }

    public string GetBaseUrl()
    {
        return $"http://localhost:{serverPort}";
    }

    public void SetWebRootPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            webRootPath = "/";
        }
        else
        {
            string normalized = path.Trim();
            if (!normalized.StartsWith("/"))
            {
                normalized = "/" + normalized;
            }
            if (!normalized.EndsWith("/"))
            {
                normalized += "/";
            }
            webRootPath = normalized;
        }

        initialUrl = BuildInitialUrl();
    }

    private string BuildInitialUrl()
    {
        string root = string.IsNullOrEmpty(webRootPath) ? "/" : webRootPath;
        if (!root.StartsWith("/"))
        {
            root = "/" + root;
        }
        if (!root.EndsWith("/"))
        {
            root += "/";
        }
        string url = $"http://localhost:{serverPort}{root}";
        Debug.Log($"{LogPrefix} BuildInitialUrl -> {url}");
        return url;
    }

    private void Update()
    {
        bool orientationChanged = lastOrientation != Screen.orientation;
        bool resolutionChanged = lastResolution.x != Screen.width || lastResolution.y != Screen.height;
        bool safeAreaChanged = !ignoreSafeArea && lastSafeArea != Screen.safeArea;

        if (orientationChanged || resolutionChanged || safeAreaChanged)
        {
            if (!applyingMargins)
            {
                StartCoroutine(ApplyMarginsWithStabilizeDelay());
            }
        }
    }

    private IEnumerator ApplyMarginsWithStabilizeDelay()
    {
        applyingMargins = true;
        yield return null;
        yield return new WaitForSeconds(0.08f);
        ApplySafeAreaMargins();
        applyingMargins = false;
    }

    public string WebRootPath => webRootPath;
    public bool IsWebViewReady => isWebViewReady;
    public int OverlayPaddingTop => overlayPaddingTop;

    private IEnumerator ApplyMarginsNextFrame()
    {
        yield return null;
        ApplySafeAreaMargins();
    }

    private void ApplySafeAreaMargins()
    {
        if (webViewObject == null)
        {
            if (Application.isPlaying)
            {
                Debug.Log($"{LogPrefix} ApplySafeAreaMargins skipped because WebView is not ready.");
            }
            return;
        }

        Rect safeArea = Screen.safeArea;

        int left, right, top, bottom;

        if (ignoreSafeArea)
        {
            // SafeArea 무시하고 오버레이 패딩만 적용
            left = overlayPaddingLeft;
            right = overlayPaddingRight;
            top = overlayPaddingTop;
            bottom = overlayPaddingBottom;
        }
        else
        {
            // SafeArea 계산
            left = Mathf.RoundToInt(safeArea.xMin);
            right = Mathf.RoundToInt(Screen.width - safeArea.xMax);
            top = Mathf.RoundToInt(Screen.height - safeArea.yMax);
            bottom = Mathf.RoundToInt(safeArea.yMin);

            // 오버레이 패딩 추가
            left = Mathf.Max(0, left + overlayPaddingLeft);
            right = Mathf.Max(0, right + overlayPaddingRight);
            top = Mathf.Max(0, top + overlayPaddingTop);
            bottom = Mathf.Max(0, bottom + overlayPaddingBottom);
        }

        webViewObject.SetMargins(left, top, right, bottom);

        lastSafeArea = safeArea;
        lastResolution = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Debug.Log($"{LogPrefix} Margins L:{left} T:{top} R:{right} B:{bottom} | ignoreSafeArea:{ignoreSafeArea} | overlay:{overlayPaddingLeft}/{overlayPaddingTop}/{overlayPaddingRight}/{overlayPaddingBottom} | res:{Screen.width}x{Screen.height} | safe:{safeArea} | orient:{lastOrientation}");
    }

    public void RunJavaScript(string js, bool queueUntilReady = true)
    {
        if (string.IsNullOrWhiteSpace(js))
        {
            Debug.LogWarning($"{LogPrefix} RunJavaScript called with empty script.");
            return;
        }

        if (queueUntilReady && (webViewObject == null || !isWebViewReady))
        {
            pendingJavaScript.Enqueue(js);
            Debug.Log($"{LogPrefix} Queued JS until WebView is ready. Queue size: {pendingJavaScript.Count}");
            return;
        }

        if (webViewObject == null)
        {
            Debug.LogWarning($"{LogPrefix} RunJavaScript called before WebView is created.");
            return;
        }

        webViewObject.EvaluateJS(js);
    }

    public void SetWebViewInteractionEnabled(bool enabled)
    {
        if (webViewObject == null)
        {
            Debug.LogWarning($"{LogPrefix} SetWebViewInteractionEnabled called before WebView is ready.");
            return;
        }

        webViewObject.SetInteractionEnabled(enabled);
        Debug.Log($"{LogPrefix} WebView interaction {(enabled ? "enabled" : "disabled")}.");
    }

    #if UNITY_IOS && !UNITY_EDITOR
    [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern void _ResetAudioSession();
    
    private void ResetAudioSessionForWebView()
    {
        try
        {
            _ResetAudioSession();
            Debug.Log($"{LogPrefix} AVAudioSession reset for WebView");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to reset audio session: {e.Message}");
        }
    }
    #endif
    
    private void InjectUnityCallFunction()
    {
        if (webViewObject == null)
        {
            Debug.LogWarning($"{LogPrefix} InjectUnityCallFunction called before WebView is ready.");
            return;
        }

        string js = @"
(function() {
    if (window.Unity && window.Unity.call) {
        console.log('[Unity Bridge] Unity.call already exists');
        return;
    }
    
    console.log('[Unity Bridge] Initialized');
    
    // Unity 메시지 전송 함수
    function sendToUnity(message) {
        var messageStr = typeof message === 'string' ? message : JSON.stringify(message);
        
        // iOS (WKWebView) 방식
        if (window.webkit && window.webkit.messageHandlers && window.webkit.messageHandlers.unityControl) {
            window.webkit.messageHandlers.unityControl.postMessage(messageStr);
            return true;
        }
        // Android 또는 URL scheme 방식
        else {
            window.location = 'unity:' + encodeURIComponent(messageStr);
            return true;
        }
    }
    
    // Console 로그를 Unity로 전달
    var originalLog = console.log;
    var originalWarn = console.warn;
    var originalError = console.error;
    
    console.log = function() {
        originalLog.apply(console, arguments);
        try {
            var message = Array.prototype.slice.call(arguments).map(function(arg) {
                return typeof arg === 'object' ? JSON.stringify(arg) : String(arg);
            }).join(' ');
            sendToUnity(JSON.stringify({type: '__console_log', level: 'log', message: message}));
        } catch(e) {}
    };
    
    console.warn = function() {
        originalWarn.apply(console, arguments);
        try {
            var message = Array.prototype.slice.call(arguments).map(function(arg) {
                return typeof arg === 'object' ? JSON.stringify(arg) : String(arg);
            }).join(' ');
            sendToUnity(JSON.stringify({type: '__console_log', level: 'warn', message: message}));
        } catch(e) {}
    };
    
    console.error = function() {
        originalError.apply(console, arguments);
        try {
            var message = Array.prototype.slice.call(arguments).map(function(arg) {
                return typeof arg === 'object' ? JSON.stringify(arg) : String(arg);
            }).join(' ');
            sendToUnity(JSON.stringify({type: '__console_log', level: 'error', message: message}));
        } catch(e) {}
    };
    
    // Unity.call 함수 설정
    window.Unity = window.Unity || {};
    window.Unity.call = function(message) {
        try {
            console.log('[Flutter→Unity] Unity.call invoked:', message);
            sendToUnity(message);
        } catch (err) {
            console.error('[Flutter→Unity] Unity.call error:', err);
        }
    };
    
    console.log('[Unity Bridge] Unity.call function and console forwarding injected');
})();
";
        
        webViewObject.EvaluateJS(js);
        Debug.Log($"{LogPrefix} Unity.call function and console forwarding injected into WebView");
    }

    private void OnDestroy()
    {
        if (webViewObject != null)
        {
            Destroy(webViewObject.gameObject);
            webViewObject = null;
        }

        pendingJavaScript.Clear();
        isWebViewReady = false;

        if (localServer != null)
        {
            try
            {
                localServer.StopServer();
            }
            catch
            {
                // ignored
            }
        }
    }

    public void SetVisible(bool visible)
    {
        if (webViewObject == null)
        {
            Debug.LogWarning($"{LogPrefix} SetVisible({visible}) called before WebView is ready.");
            return;
        }

        Debug.Log($"{LogPrefix} SetVisible({visible})");
        webViewObject.SetVisibility(visible);
    }

    private void FlushPendingJavaScript()
    {
        if (webViewObject == null || pendingJavaScript.Count == 0)
        {
            return;
        }

        while (pendingJavaScript.Count > 0)
        {
            var js = pendingJavaScript.Dequeue();
            webViewObject.EvaluateJS(js);
        }
    }

    public void SetOverlayPadding(int left, int top, int right, int bottom)
    {
        overlayPaddingLeft = Mathf.Max(0, left);
        overlayPaddingTop = Mathf.Max(0, top);
        overlayPaddingRight = Mathf.Max(0, right);
        overlayPaddingBottom = Mathf.Max(0, bottom);
        ApplySafeAreaMargins();
    }

    public void SetOverlayPaddingTop(int top)
    {
        top = Mathf.Max(0, top);
        if (overlayPaddingTop == top)
        {
            Debug.Log($"{LogPrefix} Overlay top unchanged ({overlayPaddingTop}).");
            return;
        }

        overlayPaddingTop = top;
        Debug.Log($"{LogPrefix} SetOverlayPaddingTop -> {overlayPaddingTop}");
        ApplySafeAreaMargins();
    }

    public void EnsureOverlayPaddingTop(int top)
    {
        top = Mathf.Max(0, top);
        if (top <= overlayPaddingTop)
        {
            Debug.Log($"{LogPrefix} EnsureOverlayPaddingTop ignored (current={overlayPaddingTop}, requested={top}).");
            return;
        }

        overlayPaddingTop = top;
        Debug.Log($"{LogPrefix} EnsureOverlayPaddingTop -> {overlayPaddingTop}");
        ApplySafeAreaMargins();
    }

        public void GetOverlayPadding(out int left, out int top, out int right, out int bottom)
        {
            left = overlayPaddingLeft;
            top = overlayPaddingTop;
            right = overlayPaddingRight;
            bottom = overlayPaddingBottom;
        }

        private void HandleFlutterMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return;
            }

            try
            {
                var data = JsonUtility.FromJson<FlutterMessage>(message);
                if (data == null || string.IsNullOrEmpty(data.type))
                {
                    Debug.LogWarning($"{LogPrefix} Received message without type: {message}");
                    return;
                }

                // Console 로그 처리
                if (data.type == "__console_log")
                {
                    var consoleData = JsonUtility.FromJson<ConsoleLogMessage>(message);
                    if (consoleData != null)
                    {
                        string logMessage = $"[WebView Console.{consoleData.level}] {consoleData.message}";
                        switch (consoleData.level)
                        {
                            case "error":
                                Debug.LogError(logMessage);
                                break;
                            case "warn":
                                Debug.LogWarning(logMessage);
                                break;
                            default:
                                Debug.Log(logMessage);
                                break;
                        }
                    }
                    return;
                }

                Debug.Log($"{LogPrefix} Received Flutter message: type={data.type}");

                // FlutterWebBridge에 이벤트 전달
                var bridge = GetComponent<FlutterWebBridge>();
                if (bridge != null)
                {
                    bridge.HandleFlutterToUnityMessage(data.type, message);
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} FlutterWebBridge not found. Cannot handle message: {data.type}");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"{LogPrefix} Failed to parse Flutter message: {ex.Message}, message={message}");
            }
        }

        [System.Serializable]
        private class FlutterMessage
        {
            public string type;
        }

        [System.Serializable]
        private class ConsoleLogMessage
        {
            public string type;
            public string level;
            public string message;
        }
    }
}
