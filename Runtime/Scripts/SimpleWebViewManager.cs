using System;
using System.Collections;
using UnityEngine;

namespace Muabe.WebView
{
    /// <summary>
    /// WebView의 모든 기능을 간단하게 사용할 수 있도록 래핑한 관리 클래스
    /// Inspector 설정 대신 코드로 쉽게 WebView를 제어할 수 있습니다.
    /// </summary>
    public class SimpleWebViewManager : MonoBehaviour
    {
        private const string LogPrefix = "[SimpleWebViewManager]";

        [Header("필수 설정")]
        [Tooltip("persistentDataPath 기준 상대 경로 (예: arpedia/dino/wj_demo)")]
        [SerializeField] private string contentPath = "arpedia/dino/wj_demo";

        [Tooltip("서버 포트 번호")]
        [SerializeField] private int serverPort = 8088;

        [Header("다운로드 설정 (선택사항)")]
        [Tooltip("웹 콘텐츠 ZIP 파일 다운로드 URL")]
        [SerializeField] private string downloadUrl = "";

        [Tooltip("다운로드할 콘텐츠의 버전")]
        [SerializeField] private string contentVersion = "1.0.0";

        [Tooltip("다운로드 받을 폴더 경로 (persistentDataPath 기준)")]
        [SerializeField] private string downloadFolderPath = "arpedia/dino";

        [Header("WebView 옵션")]
        [Tooltip("iOS에서 WKWebView 사용")]
        [SerializeField] private bool enableWKWebView = true;

        [Tooltip("WebView 배경 투명화")]
        [SerializeField] private bool transparent = true;

        [Tooltip("Safe Area 무시")]
        [SerializeField] private bool ignoreSafeArea = false;

        [Header("브릿지 설정")]
        [Tooltip("Unity → Flutter 이벤트 이름")]
        [SerializeField] private string unityToFlutterEvent = "__unityBridge";

        [Header("이벤트")]
        public UnityEngine.Events.UnityEvent onDownloadStarted;
        public UnityEngine.Events.UnityEvent onDownloadCompleted;
        public UnityEngine.Events.UnityEvent onDownloadFailed;
        public UnityEngine.Events.UnityEvent<float> onDownloadProgress;
        public UnityEngine.Events.UnityEvent onServerStarted;
        public UnityEngine.Events.UnityEvent onWebViewLoaded;
        public UnityEngine.Events.UnityEvent<int, int> onVideosLoaded;

        // 내부 컴포넌트
        private LocalWebServer localWebServer;
        private WebContentDownloadManager downloadManager;
        private WebViewController webViewController;
        private FlutterWebBridge flutterWebBridge;

        // 상태
        private bool isInitialized = false;
        private bool isServerRunning = false;
        private bool isWebViewLoaded = false;

        // 공개 속성
        public bool IsInitialized => isInitialized;
        public bool IsServerRunning => isServerRunning;
        public bool IsWebViewLoaded => isWebViewLoaded;
        public bool AreVideosLoaded => flutterWebBridge != null && flutterWebBridge.AreVideosLoaded;
        public LocalWebServer LocalWebServer => localWebServer;
        public WebViewController WebViewController => webViewController;
        public FlutterWebBridge FlutterWebBridge => flutterWebBridge;

        private void Awake()
        {
            InitializeComponents();
        }

        /// <summary>
        /// 모든 필요한 컴포넌트를 자동으로 추가하고 설정합니다.
        /// </summary>
        private void InitializeComponents()
        {
            if (isInitialized)
            {
                Debug.LogWarning($"{LogPrefix} Already initialized.");
                return;
            }

            Debug.Log($"{LogPrefix} Initializing components...");

            // LocalWebServer 추가
            localWebServer = GetComponent<LocalWebServer>();
            if (localWebServer == null)
            {
                localWebServer = gameObject.AddComponent<LocalWebServer>();
            }
            localWebServer.port = serverPort;
            localWebServer.OnServerReady += OnServerReadyHandler;

            // WebContentDownloadManager 추가 (다운로드 URL이 설정된 경우만)
            if (!string.IsNullOrEmpty(downloadUrl))
            {
                downloadManager = GetComponent<WebContentDownloadManager>();
                if (downloadManager == null)
                {
                    downloadManager = gameObject.AddComponent<WebContentDownloadManager>();
                }

                // 이벤트 연결
                downloadManager.onInstallStarted.AddListener(() => onDownloadStarted?.Invoke());
                downloadManager.onInstallCompleted.AddListener(() => onDownloadCompleted?.Invoke());
                downloadManager.onInstallFailed.AddListener(() => onDownloadFailed?.Invoke());
                downloadManager.onDownloadProgress.AddListener((progress) => onDownloadProgress?.Invoke(progress));
            }

            // WebViewController 추가
            webViewController = GetComponent<WebViewController>();
            if (webViewController == null)
            {
                webViewController = gameObject.AddComponent<WebViewController>();
            }
            webViewController.serverPort = serverPort;
            webViewController.enableWKWebView = enableWKWebView;
            webViewController.transparent = transparent;
            webViewController.ignoreSafeArea = ignoreSafeArea;

            // FlutterWebBridge 추가
            flutterWebBridge = GetComponent<FlutterWebBridge>();
            if (flutterWebBridge == null)
            {
                flutterWebBridge = gameObject.AddComponent<FlutterWebBridge>();
            }
            flutterWebBridge.OnVideosLoaded += OnVideosLoadedHandler;

            isInitialized = true;
            Debug.Log($"{LogPrefix} Components initialized successfully.");
        }

        private void OnServerReadyHandler()
        {
            isServerRunning = true;
            onServerStarted?.Invoke();
            Debug.Log($"{LogPrefix} Server is ready.");
        }

        private void OnVideosLoadedHandler(int total, int loaded)
        {
            onVideosLoaded?.Invoke(total, loaded);
            Debug.Log($"{LogPrefix} Videos loaded: {loaded}/{total}");
        }

        /// <summary>
        /// 웹 콘텐츠를 다운로드합니다. (선택사항)
        /// </summary>
        public void DownloadContent()
        {
            if (downloadManager == null)
            {
                Debug.LogError($"{LogPrefix} DownloadManager is not initialized. Set downloadUrl first.");
                return;
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                Debug.LogError($"{LogPrefix} Download URL is not set.");
                return;
            }

            Debug.Log($"{LogPrefix} Starting download from: {downloadUrl}");
            downloadManager.DownloadContent(downloadUrl);
        }

        /// <summary>
        /// 웹 콘텐츠를 강제로 다시 다운로드합니다.
        /// </summary>
        public void DownloadContentForced()
        {
            if (downloadManager == null)
            {
                Debug.LogError($"{LogPrefix} DownloadManager is not initialized. Set downloadUrl first.");
                return;
            }

            if (string.IsNullOrEmpty(downloadUrl))
            {
                Debug.LogError($"{LogPrefix} Download URL is not set.");
                return;
            }

            Debug.Log($"{LogPrefix} Starting forced download from: {downloadUrl}");
            downloadManager.DownloadContentForced(downloadUrl);
        }

        /// <summary>
        /// 로컬 웹 서버를 시작하고 WebView를 로드합니다.
        /// </summary>
        public void StartServerAndLoadWebView()
        {
            StartCoroutine(StartServerAndLoadWebViewCoroutine());
        }

        private IEnumerator StartServerAndLoadWebViewCoroutine()
        {
            if (!isInitialized)
            {
                Debug.LogError($"{LogPrefix} Not initialized. Call InitializeComponents first.");
                yield break;
            }

            // 서버 시작
            if (!isServerRunning)
            {
                Debug.Log($"{LogPrefix} Starting server...");
                localWebServer.SetContentPath(contentPath);
                localWebServer.StartServer();

                // 서버 준비 대기
                float timeout = 5f;
                float elapsed = 0f;
                while (!localWebServer.IsServerReady && elapsed < timeout)
                {
                    yield return new WaitForSeconds(0.1f);
                    elapsed += 0.1f;
                }

                if (!localWebServer.IsServerReady)
                {
                    Debug.LogWarning($"{LogPrefix} Server start timeout after {timeout}s");
                }
            }

            // WebView 로드
            Debug.Log($"{LogPrefix} Loading WebView...");
            webViewController.LoadInitialUrl();

            // WebView 준비 대기
            float webViewTimeout = 5f;
            float webViewElapsed = 0f;
            while (!webViewController.IsWebViewReady && webViewElapsed < webViewTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                webViewElapsed += 0.1f;
            }

            if (webViewController.IsWebViewReady)
            {
                isWebViewLoaded = true;
                onWebViewLoaded?.Invoke();
                Debug.Log($"{LogPrefix} WebView loaded successfully.");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} WebView load timeout after {webViewTimeout}s");
            }
        }

        /// <summary>
        /// WebView를 화면에 표시합니다.
        /// </summary>
        public void ShowWebView()
        {
            if (!isWebViewLoaded)
            {
                Debug.LogWarning($"{LogPrefix} WebView is not loaded yet. Call StartServerAndLoadWebView first.");
                return;
            }

            Debug.Log($"{LogPrefix} Showing WebView...");
            webViewController.SetVisible(true);
        }

        /// <summary>
        /// WebView를 숨깁니다.
        /// </summary>
        public void HideWebView()
        {
            Debug.Log($"{LogPrefix} Hiding WebView...");
            webViewController.SetVisible(false);
        }

        /// <summary>
        /// WebView를 표시하고 특정 페이지로 이동합니다.
        /// </summary>
        /// <param name="pagePath">이동할 페이지 경로 (예: "page30")</param>
        /// <param name="delay">WebView 표시 후 페이지 전환까지의 대기 시간 (기본 0.3초)</param>
        public void ShowWebViewAndNavigate(string pagePath, float delay = 0.3f)
        {
            StartCoroutine(ShowWebViewAndNavigateCoroutine(pagePath, delay));
        }

        private IEnumerator ShowWebViewAndNavigateCoroutine(string pagePath, float delay)
        {
            if (!isWebViewLoaded)
            {
                Debug.LogWarning($"{LogPrefix} WebView is not loaded. Starting server and loading WebView...");
                yield return StartServerAndLoadWebViewCoroutine();
            }

            Debug.Log($"{LogPrefix} Showing WebView and navigating to: {pagePath}");
            webViewController.SetVisible(true);

            // Flutter 앱 준비 대기
            yield return new WaitForSeconds(delay);

            // 페이지 전환
            flutterWebBridge.NavigateToPage(pagePath);
            Debug.Log($"{LogPrefix} Navigation command sent.");
        }

        /// <summary>
        /// Flutter 페이지로 이동합니다. (WebView는 이미 표시된 상태여야 함)
        /// </summary>
        public void NavigateToPage(string pagePath)
        {
            if (!isWebViewLoaded)
            {
                Debug.LogWarning($"{LogPrefix} WebView is not loaded yet.");
                return;
            }

            Debug.Log($"{LogPrefix} Navigating to: {pagePath}");
            flutterWebBridge.NavigateToPage(pagePath);
        }

        /// <summary>
        /// 비디오를 미리 로드합니다.
        /// </summary>
        public void LoadVideos()
        {
            if (!isWebViewLoaded)
            {
                Debug.LogWarning($"{LogPrefix} WebView is not loaded yet.");
                return;
            }

            Debug.Log($"{LogPrefix} Loading videos...");
            flutterWebBridge.SendLoadVideosCommand();
        }

        /// <summary>
        /// Flutter 위젯의 표시 여부를 설정합니다.
        /// </summary>
        public void SetWidgetVisibility(string widgetId, bool visible)
        {
            flutterWebBridge.SetWidgetVisibility(widgetId, visible);
        }

        /// <summary>
        /// 콘텐츠 경로를 동적으로 설정합니다.
        /// </summary>
        public void SetContentPath(string path)
        {
            contentPath = path;
            if (localWebServer != null)
            {
                localWebServer.SetContentPath(path);
            }
        }

        /// <summary>
        /// 다운로드 URL을 동적으로 설정합니다.
        /// </summary>
        public void SetDownloadUrl(string url, string version = "1.0.0")
        {
            downloadUrl = url;
            contentVersion = version;

            // DownloadManager가 없으면 추가
            if (downloadManager == null && !string.IsNullOrEmpty(url))
            {
                downloadManager = gameObject.AddComponent<WebContentDownloadManager>();
                downloadManager.onInstallStarted.AddListener(() => onDownloadStarted?.Invoke());
                downloadManager.onInstallCompleted.AddListener(() => onDownloadCompleted?.Invoke());
                downloadManager.onInstallFailed.AddListener(() => onDownloadFailed?.Invoke());
                downloadManager.onDownloadProgress.AddListener((progress) => onDownloadProgress?.Invoke(progress));
            }
        }

        private void OnDestroy()
        {
            if (localWebServer != null)
            {
                localWebServer.OnServerReady -= OnServerReadyHandler;
            }

            if (flutterWebBridge != null)
            {
                flutterWebBridge.OnVideosLoaded -= OnVideosLoadedHandler;
            }
        }
    }
}
