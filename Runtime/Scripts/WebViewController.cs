using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WebViewController : MonoBehaviour
{
    private const string LogPrefix = "[WebViewController]";
    [Header("Local HTTP Server")]
    [Tooltip("로컬 서버 포트")]
    public int serverPort = 8088;

    [Tooltip("플러터 웹이 서비스되는 경로(예: /flutter/)"), HideInInspector]
    [SerializeField]
    private string webRootPath = "/flutter/";

    [Header("WebView")]
    [Tooltip("초기 URL 자동 로드 여부")]
    public bool autoLoadOnStart = false;

    [Tooltip("iOS WKWebView 활성화")]
    public bool enableWKWebView = true;

    [Tooltip("WebView를 투명하게")]
    public bool transparent = true;

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
        yield return new WaitForSeconds(0.5f);

        initialUrl = BuildInitialUrl();
        Debug.Log($"{LogPrefix} InitializeWebView coroutine started. Platform: {Application.platform}, Initial URL: {initialUrl}");

        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        Debug.Log($"{LogPrefix} WebViewObject created.");
        isWebViewReady = false;

        webViewObject.Init(
            cb: (msg) =>
            {
                Debug.Log($"[WebView] Callback: {msg}");
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
                webViewObject.SetVisibility(true);
                isWebViewReady = true;
                FlushPendingJavaScript();
            },
            enableWKWebView: enableWKWebView,
            transparent: transparent
        );

        yield return null;
        ApplySafeAreaMargins();

        webViewObject.SetVisibility(false);

        if (autoLoadOnStart)
        {
            Debug.Log($"{LogPrefix} autoLoadOnStart enabled. Loading initial URL immediately.");
            LoadInitialUrl();
        }

        webViewObject.SetCameraAccess(true);
        webViewObject.SetMicrophoneAccess(true);
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
        if (lastOrientation != Screen.orientation ||
            lastResolution.x != Screen.width || lastResolution.y != Screen.height ||
            lastSafeArea != Screen.safeArea)
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

        int left = Mathf.RoundToInt(safeArea.xMin);
        int right = Mathf.RoundToInt(Screen.width - safeArea.xMax);
        int top = Mathf.RoundToInt(Screen.height - safeArea.yMax);
        int bottom = Mathf.RoundToInt(safeArea.yMin);

        left = Mathf.Max(0, left + overlayPaddingLeft);
        right = Mathf.Max(0, right + overlayPaddingRight);
        top = Mathf.Max(0, top + overlayPaddingTop);
        bottom = Mathf.Max(0, bottom + overlayPaddingBottom);

        webViewObject.SetMargins(left, top, right, bottom);

        lastSafeArea = safeArea;
        lastResolution = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Debug.Log($"{LogPrefix} Margins L:{left} T:{top} R:{right} B:{bottom} | overlay:{overlayPaddingLeft}/{overlayPaddingTop}/{overlayPaddingRight}/{overlayPaddingBottom} | res:{Screen.width}x{Screen.height} | safe:{safeArea} | orient:{lastOrientation}");
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
}
