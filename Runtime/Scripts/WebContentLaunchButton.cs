using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Muabe.WebView
{
    [RequireComponent(typeof(Button))]
    public class WebContentLaunchButton : WebViewButtonBase
    {
        private const string LogPrefix = WebViewConstants.LogPrefixLaunchButton;

    [Header("필수 참조")]
    [SerializeField]
    private LocalWebServer targetServer;

    [SerializeField]
    private WebViewController targetWebView;

    [Header("로드 옵션")]
    [Tooltip("서버가 실행 중이 아니면 자동으로 시작합니다.")]
    [SerializeField]
    private bool startServerIfNeeded = true;

    [Tooltip("서버가 실행될 때까지 기다립니다.")]
    [SerializeField]
    private bool waitForServerReady = true;

    [Tooltip("서버 준비를 기다릴 최대 시간(초)")]
    [SerializeField]
    private float serverReadyTimeout = 5f;

    [Tooltip("이미 로드된 상태라면 버튼을 비활성화합니다.")]
    [SerializeField]
    private bool disableButtonAfterSuccess = false;

        [Header("텍스트 설정")]

    [SerializeField]
    private string loadingLabel = "로드 중...";

    [SerializeField]
    private string waitingServerLabel = "서버 시작 중...";

    [SerializeField]
    private string completedLabel = "로드 완료";

    [SerializeField]
    private string failedLabel = "로드 실패";

    [Header("이벤트")]
    public UnityEvent onLoadStarted;
    public UnityEvent onLoadCompleted;
    public UnityEvent onLoadFailed;

        private Coroutine loadRoutine;
        private bool wasSuccessful;

        protected override void Awake()
        {
            base.Awake();
            AutoAssignReferences();
            RefreshButtonState();
            WebViewUtility.Log(LogPrefix, $"Awake (startServerIfNeeded={startServerIfNeeded})");
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
        }
    }

        private void AutoAssignReferences()
        {
            if (targetServer == null)
            {
                targetServer = GetComponentInParent<LocalWebServer>();
                if (targetServer == null)
                {
                    targetServer = WebViewUtility.FindObjectInScene<LocalWebServer>(true);
                }
            }

            if (targetWebView == null)
            {
                targetWebView = GetComponentInParent<WebViewController>();
                if (targetWebView == null)
                {
                    targetWebView = GetComponentInChildren<WebViewController>(true);
                }
                if (targetWebView == null)
                {
                    targetWebView = WebViewUtility.FindObjectInScene<WebViewController>(true);
                }
            }

            if (Application.isPlaying)
            {
                if (targetServer != null)
                {
                    WebViewUtility.Log(LogPrefix, $"Server assigned: {targetServer.name}");
                }
                else
                {
                    WebViewUtility.LogWarning(LogPrefix, "Server not found!");
                }

                if (targetWebView != null)
                {
                    WebViewUtility.Log(LogPrefix, $"WebView assigned: {targetWebView.name}");
                }
                else
                {
                    WebViewUtility.LogError(LogPrefix, "WebView not found! Please add WebViewController to your scene or manually assign it.");
                }
            }
        }

        protected override void OnButtonClicked()
        {
        if (loadRoutine != null)
        {
            WebViewUtility.LogWarning(LogPrefix, "Load already in progress");
            return;
        }

        loadRoutine = StartCoroutine(LoadRoutine());
    }

        private IEnumerator LoadRoutine()
        {
            SetButtonInteractable(false);
            UpdateStatusLabel(loadingLabel);
            onLoadStarted?.Invoke();
            wasSuccessful = false;

        if (startServerIfNeeded && targetServer != null && !targetServer.IsRunning)
        {
            UpdateStatusLabel(waitingServerLabel);
            WebViewUtility.Log(LogPrefix, "Starting server...");
            targetServer.StartServer();
        }

        if (waitForServerReady && targetServer != null)
        {
            float timeout = serverReadyTimeout;
            while (!targetServer.IsRunning && timeout > 0f)
            {
                yield return null;
                timeout -= Time.unscaledDeltaTime;
            }

            if (!targetServer.IsRunning)
            {
                WebViewUtility.LogWarning(LogPrefix, "Server did not start within timeout");
                UpdateStatusLabel(failedLabel);
                onLoadFailed?.Invoke();
                SetButtonInteractable(true);
                loadRoutine = null;
                yield break;
            }
        }

        if (targetWebView != null)
        {
            WebViewUtility.Log(LogPrefix, "Loading initial WebView (hidden)");
            targetWebView.LoadInitialUrl();
        }
        else
        {
            WebViewUtility.LogWarning(LogPrefix, "targetWebView is not assigned. Skipping WebView load.");
        }

        UpdateStatusLabel(completedLabel);
        onLoadCompleted?.Invoke();
        wasSuccessful = true;
        SetButtonInteractable(!disableButtonAfterSuccess);
        loadRoutine = null;
    }

    public void StartServer()
    {
        if (targetServer == null)
        {
            WebViewUtility.LogWarning(LogPrefix, "targetServer is not assigned. Cannot start server.");
            return;
        }

        targetServer.StartServer();
    }

    public void StopServer()
    {
        if (targetServer == null)
        {
            WebViewUtility.LogWarning(LogPrefix, "targetServer is not assigned. Cannot stop server.");
            return;
        }

        targetServer.StopServer();
    }

    public new void ResetStatusLabel()
    {
        CacheOriginalLabels();
        RefreshButtonState();
    }

        private void RefreshButtonState()
        {
            if (wasSuccessful && disableButtonAfterSuccess)
            {
                SetButtonInteractable(false);
                UpdateStatusLabel(completedLabel);
                return;
            }

            SetButtonInteractable(true);
            if (!string.IsNullOrEmpty(originalButtonLabel))
            {
                UpdateStatusLabel(originalButtonLabel);
            }
            else if (!string.IsNullOrEmpty(originalStatusLabel))
            {
                UpdateStatusLabel(originalStatusLabel);
            }
        }
    }
}
