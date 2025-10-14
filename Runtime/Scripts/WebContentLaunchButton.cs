using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WebContentLaunchButton : MonoBehaviour
{
    private const string LogPrefix = "[WebContentLaunchButton]";

    [Header("필수 참조")]
    [SerializeField]
    private WebContentDownloadManager installer;

    [SerializeField]
    private LocalWebServer targetServer;

    [SerializeField]
    private WebViewController targetWebView;

    [Header("경로 입력")]
    [SerializeField]
    [Tooltip("설치 시 사용할 콘텐츠 하위 폴더(비워두면 기존 설정 유지)")]
    private string contentRootSubfolder = "flutter";

    [SerializeField]
    [Tooltip("서버 라우트 프리픽스(비워두면 기존 설정 유지)")]
    private string routePrefix = "flutter";


    [Header("로드 옵션")]
    [Tooltip("로드 시 서버에 콘텐츠 경로를 자동으로 적용합니다.")]
    [SerializeField]
    private bool configureServerOnLoad = true;

    [Tooltip("서버가 실행 중이 아니면 자동으로 시작합니다.")]
    [SerializeField]
    private bool startServerIfNeeded = true;

    [Tooltip("서버가 실행될 때까지 기다립니다.")]
    [SerializeField]
    private bool waitForServerReady = true;

    [Tooltip("서버 준비를 기다릴 최대 시간(초)")]
    [SerializeField]
    private float serverReadyTimeout = 5f;

    [Tooltip("웹뷰를 자동으로 보여줄지 여부")]
    [SerializeField]
    private bool showWebViewOnLoad = true;

    [Tooltip("이미 로드된 상태라면 버튼을 비활성화합니다.")]
    [SerializeField]
    private bool disableButtonAfterSuccess = false;

    [Header("텍스트 설정")]
    [SerializeField]
    private Text statusText;

    [SerializeField]
    private Text buttonLabel;

    [SerializeField]
    private string loadingLabel = "로드 중...";

    [SerializeField]
    private string waitingServerLabel = "서버 시작 중...";

    [SerializeField]
    private string completedLabel = "로드 완료";

    [SerializeField]
    private string failedLabel = "로드 실패";

    [SerializeField]
    private string notReadyLabel = "콘텐츠 없음";

    [Header("이벤트")]
    public UnityEvent onLoadStarted;
    public UnityEvent onLoadCompleted;
    public UnityEvent onLoadFailed;

    private Button button;
    private Coroutine loadRoutine;
    private string originalButtonLabel;
    private string originalStatusLabel;
    private bool wasSuccessful;

    private void Awake()
    {
        button = GetComponent<Button>();
        AutoAssignReferences();
        CacheOriginalLabels();
        NormalizeOverrides();
        ApplyConfigurationOverrides();
        button.onClick.AddListener(HandleButtonClicked);
        RefreshButtonState();
        Debug.Log($"{LogPrefix} Awake (configureServerOnLoad={configureServerOnLoad}, startServerIfNeeded={startServerIfNeeded})");
    }

    private void Reset()
    {
        AutoAssignReferences();
        CacheOriginalLabels();
        NormalizeOverrides();
        ApplyConfigurationOverrides();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
        if (!Application.isPlaying)
        {
            CacheOriginalLabels();
        }
        NormalizeOverrides();
        ApplyConfigurationOverrides();
    }

    private void AutoAssignReferences()
    {
        if (installer == null)
        {
            installer = GetComponentInParent<WebContentDownloadManager>();
        }

        if (targetServer == null)
        {
            targetServer = GetComponentInParent<LocalWebServer>();
        }

        if (targetWebView == null)
        {
            targetWebView = GetComponentInParent<WebViewController>();
            if (targetWebView == null)
            {
                targetWebView = GetComponentInChildren<WebViewController>(true);
            }
        }

        if (buttonLabel == null)
        {
            buttonLabel = GetComponentInChildren<Text>(true);
        }

        if (statusText == null)
        {
            statusText = buttonLabel;
        }

        NormalizeOverrides();
        ApplyConfigurationOverrides();

    }

    private void HandleButtonClicked()
    {
        if (loadRoutine != null)
        {
            return;
        }

        NormalizeOverrides();
        ApplyConfigurationOverrides();
        if (!HasInstallPrepared())
        {
            UpdateStatus(notReadyLabel);
            onLoadFailed?.Invoke();
            return;
        }

        loadRoutine = StartCoroutine(LoadRoutine());
    }

    private bool HasInstallPrepared()
    {
        if (installer == null)
        {
            return false;
        }

        if (installer.HasInstalledContent())
        {
            return true;
        }

        if (Directory.Exists(installer.ContentRootPath))
        {
            return true;
        }

        return false;
    }

    private IEnumerator LoadRoutine()
    {
        button.interactable = false;
        UpdateStatus(loadingLabel);
        onLoadStarted?.Invoke();
        wasSuccessful = false;

        if (configureServerOnLoad)
        {
            ConfigureServerContent();
        }

        if (startServerIfNeeded && targetServer != null && !targetServer.IsRunning)
        {
            UpdateStatus(waitingServerLabel);
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
                Debug.LogWarning($"{LogPrefix} Server did not start within timeout");
            }
        }

        if (targetWebView == null)
        {
            Debug.LogWarning($"{LogPrefix} targetWebView is not assigned. Cannot load WebView.");
            UpdateStatus(failedLabel);
            onLoadFailed?.Invoke();
            button.interactable = true;
            loadRoutine = null;
            yield break;
        }

        targetWebView.LoadInitialUrl();
        if (showWebViewOnLoad)
        {
            targetWebView.SetVisible(true);
        }

        UpdateStatus(completedLabel);
        onLoadCompleted?.Invoke();
        wasSuccessful = true;
        button.interactable = !disableButtonAfterSuccess;
        loadRoutine = null;
    }

    public void ConfigureServerContent()
    {
        if (targetServer == null)
        {
            Debug.LogWarning($"{LogPrefix} targetServer is not assigned. Cannot configure server.");
            return;
        }

        if (installer == null)
        {
            Debug.LogWarning($"{LogPrefix} installer is not assigned. Cannot resolve content root.");
            return;
        }

        string contentRoot = installer.ContentRootPath;
        if (!Directory.Exists(contentRoot))
        {
            Debug.LogWarning($"{LogPrefix} Content root does not exist yet: {contentRoot}");
        }

        targetServer.SetContentRootOverride(contentRoot);
        Debug.Log($"{LogPrefix} Applied content root {contentRoot}");
    }

    public void StartServer()
    {
        if (targetServer == null)
        {
            Debug.LogWarning($"{LogPrefix} targetServer is not assigned. Cannot start server.");
            return;
        }

        targetServer.StartServer();
    }

    public void StopServer()
    {
        if (targetServer == null)
        {
            Debug.LogWarning($"{LogPrefix} targetServer is not assigned. Cannot stop server.");
            return;
        }

        targetServer.StopServer();
    }

    public void ResetStatusLabel()
    {
        CacheOriginalLabels();
        NormalizeOverrides();
        ApplyConfigurationOverrides();
        RefreshButtonState();
    }

    private void RefreshButtonState()
    {
        if (wasSuccessful && disableButtonAfterSuccess)
        {
            button.interactable = false;
            UpdateStatus(completedLabel);
            return;
        }

        button.interactable = true;
        if (!string.IsNullOrEmpty(originalButtonLabel))
        {
            UpdateStatus(originalButtonLabel);
        }
        else if (!string.IsNullOrEmpty(originalStatusLabel))
        {
            UpdateStatus(originalStatusLabel);
        }
    }

    private void UpdateStatus(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        if (statusText != null)
        {
            statusText.text = message;
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = message;
        }
    }

    private void CacheOriginalLabels()
    {
        if (buttonLabel != null)
        {
            originalButtonLabel = buttonLabel.text;
        }

        if (statusText != null)
        {
            originalStatusLabel = statusText.text;
        }
    }

    private void NormalizeOverrides()
    {
        contentRootSubfolder = NormalizeSubfolder(contentRootSubfolder);
        routePrefix = NormalizeRoute(routePrefix);
    }

    private string NormalizeSubfolder(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string normalized = value.Trim().Replace('\\', '/');
        return normalized.Trim('/');
    }

    private string NormalizeRoute(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('/');
    }

    public void ApplyConfigurationOverrides()
    {
        NormalizeOverrides();
        if (installer != null && !string.IsNullOrWhiteSpace(contentRootSubfolder))
        {
            installer.SetContentRootSubfolder(contentRootSubfolder);
        }

        if (targetServer != null)
        {
            targetServer.SetRoutePrefix(routePrefix);
        }

        if (targetWebView != null)
        {
            targetWebView.SetWebRootPath(BuildWebRootPath(routePrefix));
        }
    }

    private string BuildWebRootPath(string route)
    {
        if (string.IsNullOrEmpty(route))
        {
            return "/";
        }

        return "/" + route.Trim('/') + "/";
    }

}
