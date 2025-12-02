using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Muabe.WebView
{
    [RequireComponent(typeof(Button))]
    public class WebViewShowButton : WebViewButtonBase
    {
        private const string LogPrefix = "[WebViewShowButton]";

        [Header("필수 참조")]
        [SerializeField]
        private WebViewController targetWebView;

        [SerializeField]
        private FlutterWebBridge bridge;

        [SerializeField]
        private LocalWebServer targetServer;

        [Header("대기 옵션")]
        [SerializeField]
        [Tooltip("로컬 서버가 준비될 때까지 버튼을 비활성화합니다.")]
        private bool waitForServerReady = true;

        [Header("페이지 설정")]
        [SerializeField]
        [Tooltip("Flutter 페이지 경로 (예: page30, /page30)")]
        private string pagePath = "";

        [SerializeField]
        [Tooltip("true: 브릿지로 페이지 전환, false: 직접 URL 로드")]
        private bool useBridge = true;

        [SerializeField]
        [Tooltip("웹뷰가 준비되지 않았을 때 자동으로 로드합니다 (브릿지 모드에서만)")]
        private bool autoLoadWebViewIfNeeded = true;

        [SerializeField]
        [Tooltip("웹뷰 표시 후 브릿지 메시지 전송까지 대기 시간 (초)")]
        private float bridgeMessageDelay = 0.3f;

        [Header("직접 로드 모드 옵션")]
        [SerializeField]
        [Tooltip("직접 로드 시 사용할 URL 경로 (useBridge=false일 때만 사용)")]
        private string urlPath = "/";

        [Header("표시 옵션")]
        [Tooltip("버튼 클릭 시 웹뷰를 자동으로 보여줍니다.")]
        [SerializeField]
        private bool showWebView = true;

        [Tooltip("URL을 로드합니다. (useBridge=false일 때만 적용)")]
        [SerializeField]
        private bool loadUrl = true;

        [Header("Videos Loaded 체크")]
        [Tooltip("브릿지 모드에서 videosLoaded가 완료될 때까지 버튼을 비활성화합니다.")]
        [SerializeField]
        private bool waitForVideosLoaded = true;

        [Header("텍스트 설정")]
        [SerializeField]
        private string loadingLabel = "로딩 중...";

        [SerializeField]
        private string completedLabel = "로드 완료";

        [SerializeField]
        private string failedLabel = "로드 실패";

#pragma warning disable 0414
        [SerializeField]
        private string notReadyLabel = "웹뷰 없음";
#pragma warning restore 0414

        [SerializeField]
        private string waitingVideosLabel = "비디오 로딩 중...";
        
        [SerializeField]
        private string waitingServerLabel = "서버 준비 중...";

        [Header("이벤트")]
        public UnityEvent onShowStarted;
        public UnityEvent onShowCompleted;
        public UnityEvent onShowFailed;

        private bool serverReady = true;
        private bool videosReady = true;
        private bool waitingLabelActive;
        private Image cachedButtonImage;

        protected override void Awake()
        {
            base.Awake();
            cachedButtonImage = button != null ? button.GetComponent<Image>() : null;
            serverReady = !waitForServerReady;
            videosReady = !(useBridge && waitForVideosLoaded);
            AutoAssignReferences();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromVideosLoadedEvent();
            UnsubscribeFromServerReadyEvent();
        }

        private void SubscribeToVideosLoadedEvent()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            bool shouldWait = ShouldWaitForVideos();
            if (!shouldWait)
            {
                WebViewUtility.Log(LogPrefix, "Videos loaded check skipped (no loader or disabled)");
                videosReady = true;
                UpdateButtonInteractableState();
                return;
            }

            if (bridge == null)
            {
                WebViewUtility.LogWarning(LogPrefix, "Bridge is null, trying to auto-assign...");
                AutoAssignReferences();
            }

            if (bridge == null)
            {
                WebViewUtility.LogError(LogPrefix, "Cannot subscribe to videos loaded event: Bridge is null!");
                videosReady = true;
                UpdateButtonInteractableState();
                return;
            }

            bridge.OnVideosLoaded -= HandleVideosLoadedEvent;

            if (bridge.AreVideosLoaded)
            {
                WebViewUtility.Log(LogPrefix, $"Videos already loaded! ({bridge.LoadedVideos}/{bridge.TotalVideos})");
                videosReady = true;
                UpdateButtonInteractableState();
                return;
            }

            videosReady = false;
            bridge.OnVideosLoaded += HandleVideosLoadedEvent;
            WebViewUtility.Log(LogPrefix, $"Subscribed to OnVideosLoaded event (button: {name})");
            UpdateButtonInteractableState();
        }

        private void UnsubscribeFromVideosLoadedEvent()
        {
            if (bridge != null)
            {
                bridge.OnVideosLoaded -= HandleVideosLoadedEvent;
                WebViewUtility.Log(LogPrefix, "Unsubscribed from OnVideosLoaded event");
            }
        }

        private void HandleVideosLoadedEvent(int loadedCount, int totalCount)
        {
            WebViewUtility.Log(LogPrefix, $"HandleVideosLoadedEvent called! Loaded: {loadedCount}/{totalCount}");
            videosReady = true;
            UpdateButtonInteractableState();
            WebViewUtility.Log(LogPrefix, "Button activated by videos loaded event");
        }

        private void SubscribeToServerReadyEvent()
        {
            if (!Application.isPlaying || !waitForServerReady)
            {
                serverReady = true;
                return;
            }

            if (targetServer == null)
            {
                AutoAssignReferences();
            }

            if (targetServer == null)
            {
                WebViewUtility.LogWarning(LogPrefix, "LocalWebServer not found. Skipping server ready wait.");
                serverReady = true;
                return;
            }

            targetServer.OnServerReady -= HandleServerReady;

            if (targetServer.IsServerReady)
            {
                serverReady = true;
                UpdateButtonInteractableState();
                return;
            }

            serverReady = false;
            targetServer.OnServerReady += HandleServerReady;
            WebViewUtility.Log(LogPrefix, $"Waiting for server ready event (button: {name})");
            UpdateButtonInteractableState();
        }

        private void HandleServerReady()
        {
            serverReady = true;
            if (targetServer != null)
            {
                targetServer.OnServerReady -= HandleServerReady;
            }
            WebViewUtility.Log(LogPrefix, "Local server ready detected");
            UpdateButtonInteractableState();
        }

        private void UnsubscribeFromServerReadyEvent()
        {
            if (targetServer != null)
            {
                targetServer.OnServerReady -= HandleServerReady;
            }
        }

        private bool ShouldWaitForVideos()
        {
            if (!useBridge || !waitForVideosLoaded)
            {
                return false;
            }

            if (bridge == null)
            {
                AutoAssignReferences();
            }

            if (bridge == null)
            {
                WebViewUtility.LogWarning(LogPrefix, "WaitForVideosLoaded enabled but bridge is missing. Skipping wait.");
                return false;
            }

            var loaders = WebViewUtility.FindObjectsInScene<VideoLoadButton>(true);
            if (loaders == null || loaders.Length == 0)
            {
                WebViewUtility.LogWarning(LogPrefix, $"WaitForVideosLoaded is enabled but no VideoLoadButton exists for bridge '{bridge.name}'. Skipping wait.");
                return false;
            }

            foreach (var loader in loaders)
            {
                if (loader == null || !loader.isActiveAndEnabled)
                {
                    continue;
                }

                if (loader.IsUsingBridge(bridge))
                {
                    return true;
                }
            }

            WebViewUtility.LogWarning(LogPrefix, $"WaitForVideosLoaded is enabled but no active VideoLoadButton uses bridge '{bridge.name}'. Skipping wait.");
            return false;
        }

        private void UpdateButtonInteractableState()
        {
            bool waitingServer = waitForServerReady && !serverReady;
            bool waitingVideos = useBridge && waitForVideosLoaded && !videosReady;
            bool interactable = !(waitingServer || waitingVideos);

            SetButtonInteractable(interactable);
            UpdateRaycastState(interactable);

            if (!interactable)
            {
                if (waitingServer)
                {
                    SetWaitingLabel(waitingServerLabel);
                }
                else if (waitingVideos)
                {
                    SetWaitingLabel(waitingVideosLabel);
                }
            }
            else
            {
                RestoreWaitingLabel();
            }
        }

        private void UpdateRaycastState(bool interactable)
        {
            if (cachedButtonImage == null && button != null)
            {
                cachedButtonImage = button.GetComponent<Image>();
            }

            if (cachedButtonImage != null)
            {
                cachedButtonImage.raycastTarget = interactable;
            }
        }

        private void SetWaitingLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            waitingLabelActive = true;
            UpdateStatusLabel(label);
        }

        private void RestoreWaitingLabel()
        {
            if (!waitingLabelActive)
            {
                return;
            }

            waitingLabelActive = false;

            if (!string.IsNullOrEmpty(originalButtonLabel))
            {
                UpdateStatusLabel(originalButtonLabel);
            }
            else if (!string.IsNullOrEmpty(originalStatusLabel))
            {
                UpdateStatusLabel(originalStatusLabel);
            }
        }

        private void Reset()
        {
            AutoAssignReferences();
            CacheOriginalLabels();
        }

        private void OnValidate()
        {
            AutoAssignReferences();
            if (!Application.isPlaying)
            {
                CacheOriginalLabels();
                
                // Inspector에서 useBridge가 true인데 bridge가 없으면 경고
                if (useBridge && bridge == null)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[WebViewShowButton] '{name}': 'Use Bridge' is enabled but FlutterWebBridge is not assigned. " +
                        "Please add FlutterWebBridge component to your scene or manually assign it in the Inspector.", 
                        this);
                }
            }
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            AutoAssignReferences();
            SubscribeToServerReadyEvent();
            SubscribeToVideosLoadedEvent();
            UpdateButtonInteractableState();
        }

        private void OnDisable()
        {
            UnsubscribeFromVideosLoadedEvent();
            UnsubscribeFromServerReadyEvent();
        }

        private void AutoAssignReferences()
        {
            if (targetWebView == null)
            {
                targetWebView = GetComponentInParent<WebViewController>();
                if (targetWebView == null)
                {
                    targetWebView = GetComponentInChildren<WebViewController>(true);
                }
                
                if (targetWebView == null && useBridge == false)
                {
                    targetWebView = WebViewUtility.FindObjectInScene<WebViewController>(true);
                }
            }

            if (targetServer == null)
            {
                if (targetWebView != null)
                {
                    targetServer = targetWebView.GetComponent<LocalWebServer>();
                }

                if (targetServer == null)
                {
                    targetServer = GetComponentInParent<LocalWebServer>();
                }

                if (targetServer == null)
                {
                    targetServer = WebViewUtility.FindObjectInScene<LocalWebServer>(true);
                }

                if (targetServer != null)
                {
                    WebViewUtility.Log(LogPrefix, $"Server auto-assigned: {targetServer.name}");
                }
            }

            if (useBridge && bridge == null)
            {
                bridge = GetComponentInParent<FlutterWebBridge>();
                if (bridge == null)
                {
                    bridge = WebViewUtility.FindObjectInScene<FlutterWebBridge>(true);
                }

                if (bridge == null)
                {
                    WebViewUtility.LogWarning(LogPrefix, 
                        "FlutterWebBridge not found in scene. useBridge is enabled but no bridge component exists. " +
                        "Please add FlutterWebBridge component to your scene or set useBridge to false.");
                }
                else
                {
                    WebViewUtility.Log(LogPrefix, $"Bridge auto-assigned: {bridge.name}");
                }
            }
        }

        protected override void OnButtonClicked()
        {
            UpdateStatusLabel(loadingLabel);
            onShowStarted?.Invoke();

            if (useBridge)
            {
                StartCoroutine(HandleBridgeModeCoroutine());
            }
            else
            {
                try
                {
                    HandleDirectLoadMode();
                    UpdateStatusLabel(completedLabel);
                    onShowCompleted?.Invoke();
                }
                catch (System.Exception ex)
                {
                    WebViewUtility.LogError(LogPrefix, $"Failed: {ex.Message}");
                    UpdateStatusLabel(failedLabel);
                    onShowFailed?.Invoke();
                }
            }
        }

        private System.Collections.IEnumerator HandleBridgeModeCoroutine()
        {
            // 다시 한 번 브릿지 찾기 시도
            if (bridge == null)
            {
                bridge = WebViewUtility.FindObjectInScene<FlutterWebBridge>(true);
            }

            if (bridge == null)
            {
                WebViewUtility.LogError(LogPrefix, 
                    "FlutterWebBridge is not assigned and could not be found in scene.\n" +
                    "Solution 1: Add FlutterWebBridge component to your WebViewController GameObject\n" +
                    "Solution 2: Manually assign the Bridge field in Inspector\n" +
                    "Solution 3: Set 'Use Bridge' to false to use direct URL loading instead");
                UpdateStatusLabel(failedLabel);
                onShowFailed?.Invoke();
                yield break;
            }

            if (string.IsNullOrWhiteSpace(pagePath))
            {
                WebViewUtility.LogWarning(LogPrefix, "pagePath is empty.");
                UpdateStatusLabel(failedLabel);
                onShowFailed?.Invoke();
                yield break;
            }

            // 웹뷰가 준비되지 않았으면 먼저 로드 (선택 옵션)
            if (autoLoadWebViewIfNeeded && targetWebView != null && !targetWebView.IsWebViewReady)
            {
                WebViewUtility.Log(LogPrefix, "WebView is not ready. Loading initial URL first...");
                targetWebView.LoadInitialUrl();
                WebViewUtility.Log(LogPrefix, "Note: Consider using WebContentLaunchButton to load WebView before using this button.");
                
                // 웹뷰 로드 완료 대기 (최대 5초)
                float timeout = 5f;
                while (!targetWebView.IsWebViewReady && timeout > 0f)
                {
                    yield return null;
                    timeout -= Time.unscaledDeltaTime;
                }

                if (!targetWebView.IsWebViewReady)
                {
                    WebViewUtility.LogWarning(LogPrefix, "WebView did not become ready within timeout");
                }
            }

            // 1단계: 웹뷰 먼저 표시
            if (showWebView && targetWebView != null)
            {
                WebViewUtility.Log(LogPrefix, "Showing WebView first...");
                targetWebView.SetVisible(true);
            }

            // 2단계: 지연 대기 (브릿지가 완전히 준비될 때까지)
            WebViewUtility.Log(LogPrefix, $"Waiting {bridgeMessageDelay}s for bridge to be ready...");
            yield return new WaitForSecondsRealtime(bridgeMessageDelay);

            // 3단계: 브릿지 메시지 전송
            string normalizedPath = pagePath.Trim();
            if (!normalizedPath.StartsWith("/"))
            {
                normalizedPath = "/" + normalizedPath;
            }

            WebViewUtility.Log(LogPrefix, $"Navigating via bridge to: {normalizedPath}");
            bridge.NavigateToPage(normalizedPath);

            // 4단계: WebView 표시
            if (showWebView && targetWebView != null)
            {
                WebViewUtility.Log(LogPrefix, "Showing WebView...");
                targetWebView.SetVisible(true);
            }

            UpdateStatusLabel(completedLabel);
            onShowCompleted?.Invoke();
        }

        private void HandleBridgeMode()
        {
            // Legacy method - 코루틴으로 대체됨
            StartCoroutine(HandleBridgeModeCoroutine());
        }

        private void HandleDirectLoadMode()
        {
            if (targetWebView == null)
            {
                WebViewUtility.LogWarning(LogPrefix, "targetWebView is not assigned.");
                throw new System.Exception("WebView not found");
            }

            if (loadUrl)
            {
                string fullUrl = BuildFullUrl();
                WebViewUtility.Log(LogPrefix, $"Loading URL: {fullUrl}");
                targetWebView.LoadUrl(fullUrl);
            }

            if (showWebView)
            {
                targetWebView.SetVisible(true);
            }
        }

        private string BuildFullUrl()
        {
            if (string.IsNullOrWhiteSpace(urlPath))
            {
                return targetWebView.GetInitialUrl();
            }

            string normalized = urlPath.Trim();
            if (normalized.StartsWith("http://", System.StringComparison.OrdinalIgnoreCase) ||
                normalized.StartsWith("https://", System.StringComparison.OrdinalIgnoreCase))
            {
                return normalized;
            }

            string baseUrl = targetWebView.GetBaseUrl();
            string webRoot = targetWebView.WebRootPath;

            if (!string.IsNullOrEmpty(webRoot))
            {
                webRoot = webRoot.Trim('/');
                if (!normalized.StartsWith("/", System.StringComparison.Ordinal))
                {
                    normalized = "/" + normalized;
                }
                return $"{baseUrl}/{webRoot}{normalized}";
            }

            if (!normalized.StartsWith("/", System.StringComparison.Ordinal))
            {
                normalized = "/" + normalized;
            }

            return baseUrl + normalized;
        }

        public void SetUrlPath(string path)
        {
            urlPath = path;
        }

        public string GetUrlPath()
        {
            return urlPath;
        }

        public void ShowWebView()
        {
            OnButtonClicked();
        }
    }
}
