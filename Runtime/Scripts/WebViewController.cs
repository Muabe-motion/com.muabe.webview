using UnityEngine;
using System.Collections;

public class WebViewController : MonoBehaviour
{
    private const string LogPrefix = "[WebViewController]";
    [Header("Local HTTP Server")]
    [Tooltip("로컬 서버 포트")]
    public int serverPort = 8088;

    [Tooltip("플러터 웹이 서비스되는 경로(예: /flutter/)")]
    public string webRootPath = "/flutter/";

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

    private IEnumerator InitializeWebView()
    {
        yield return new WaitForSeconds(0.5f);

        initialUrl = BuildInitialUrl();
        Debug.Log($"{LogPrefix} InitializeWebView coroutine started. Platform: {Application.platform}, Initial URL: {initialUrl}");

        webViewObject = (new GameObject("WebViewObject")).AddComponent<WebViewObject>();
        Debug.Log($"{LogPrefix} WebViewObject created.");

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

    private IEnumerator ApplyMarginsNextFrame()
    {
        yield return null;
        ApplySafeAreaMargins();
    }

    private void ApplySafeAreaMargins()
    {
        if (webViewObject == null)
        {
            return;
        }

        Rect safeArea = Screen.safeArea;

        int left = Mathf.RoundToInt(safeArea.xMin);
        int right = Mathf.RoundToInt(Screen.width - safeArea.xMax);
        int top = Mathf.RoundToInt(Screen.height - safeArea.yMax);
        int bottom = Mathf.RoundToInt(safeArea.yMin);

        webViewObject.SetMargins(left, top, right, bottom);

        lastSafeArea = safeArea;
        lastResolution = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        Debug.Log($"{LogPrefix} Margins L:{left} T:{top} R:{right} B:{bottom} | res:{Screen.width}x{Screen.height} | safe:{safeArea} | orient:{lastOrientation}");
    }

    private void OnDestroy()
    {
        if (webViewObject != null)
        {
            Destroy(webViewObject.gameObject);
            webViewObject = null;
        }

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
}
