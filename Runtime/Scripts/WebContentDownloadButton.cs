using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WebContentDownloadButton : MonoBehaviour
{
    private const string LogPrefix = "[WebContentDownloadButton]";
    [SerializeField]
    private WebContentDownloadManager installer;

    [SerializeField]
    private WebContentLaunchButton launchButton;

    [Header("다운로드 입력")]
    [SerializeField]
    [Tooltip("다운로드에 사용할 ZIP 파일 URL")]
    private string downloadUrl;

    [SerializeField]
    [Tooltip("콘텐츠 버전(비워두면 기존 설정 유지)")]
    private string remoteVersionOverride = string.Empty;

    [SerializeField]
    [Tooltip("다운로드 시작 시 표시할 텍스트 (선택 사항)")]
    private string downloadingLabel = "다운로드 중...";

    [SerializeField]
    [Tooltip("다운로드 완료 시 표시할 텍스트 (선택 사항)")]
    private string completedLabel = "다운로드 완료";

    [SerializeField]
    [Tooltip("다운로드 실패 시 표시할 텍스트 (선택 사항)")]
    private string failedLabel = "다운로드 실패";

    [SerializeField]
    [Tooltip("이미 다운로드된 상태일 때 표시할 텍스트")]
    private string alreadyDownloadedLabel = "이미 다운로드됨";

    [SerializeField]
    [Tooltip("버튼 상태 변화를 표시할 UI Text")]
    private Text statusText;

    [SerializeField]
    [Tooltip("버튼 자체에 표시할 텍스트 (선택 사항)")]
    private Text buttonLabel;

    [SerializeField]
    [Tooltip("항상 새로 다운로드할지 여부")]
    private bool forceDownloadEveryTime = false;

    [SerializeField]
    [Tooltip("캐시된 콘텐츠를 사용할 때 표시할 텍스트")]
    private string cachedLabel = "캐시에서 불러오는 중...";

    [SerializeField]
    private UnityEvent onDownloadStarted = new UnityEvent();

    [SerializeField]
    private UnityEvent onDownloadCompleted = new UnityEvent();

    [SerializeField]
    private UnityEvent onDownloadFailed = new UnityEvent();

    private Button button;
    private bool eventsSubscribed;
    private bool usingCachedContent;
    private string originalButtonLabel;
    private string originalStatusLabel;

    private void Awake()
    {
        button = GetComponent<Button>();
        AutoAssignReferences();
        ApplyConfigurationOverrides();
        ResetStatusLabel();
        button.onClick.AddListener(OnButtonClicked);
        Debug.Log($"{LogPrefix} Awake (button={name})");
    }

    private void Reset()
    {
        AutoAssignReferences();
        ApplyConfigurationOverrides();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
        ApplyConfigurationOverrides();
    }

    private void AutoAssignReferences()
    {
        if (installer == null)
        {
            installer = GetComponentInParent<WebContentDownloadManager>();
            if (installer != null)
            {
                Debug.Log($"{LogPrefix} Auto-assigned installer: {installer.name}");
            }
        }

        if (launchButton == null)
        {
            launchButton = GetComponentInParent<WebContentLaunchButton>();
        }

        if (buttonLabel == null)
        {
            buttonLabel = GetComponentInChildren<Text>(true);
        }

        if (statusText == null)
        {
            statusText = buttonLabel;
        }

        CacheOriginalButtonLabel();
    }

    private void OnEnable()
    {
        SubscribeInstallerEvents();
        ApplyConfigurationOverrides();
        RefreshButtonState();
    }

    private void OnDisable()
    {
        UnsubscribeInstallerEvents();
    }

    private void OnDestroy()
    {
        button.onClick.RemoveListener(OnButtonClicked);
        UnsubscribeInstallerEvents();
    }

    private void SubscribeInstallerEvents()
    {
        if (eventsSubscribed || installer == null)
        {
            if (installer == null)
            {
                Debug.LogWarning($"{LogPrefix} Installer reference missing. Button will not trigger downloads.");
            }
            return;
        }

        installer.onInstallStarted.AddListener(HandleInstallStarted);
        installer.onInstallCompleted.AddListener(HandleInstallCompleted);
        installer.onInstallFailed.AddListener(HandleInstallFailed);
        eventsSubscribed = true;
    }

    private void UnsubscribeInstallerEvents()
    {
        if (!eventsSubscribed || installer == null)
        {
            return;
        }

        installer.onInstallStarted.RemoveListener(HandleInstallStarted);
        installer.onInstallCompleted.RemoveListener(HandleInstallCompleted);
        installer.onInstallFailed.RemoveListener(HandleInstallFailed);
        eventsSubscribed = false;
    }

    private void OnButtonClicked()
    {
        if (installer == null)
        {
            Debug.LogError("[WebContentDownloadButton] WebContentDownloadManager reference is missing.");
            return;
        }

        ApplyConfigurationOverrides();
        button.interactable = false;
        bool hasCache = installer.HasInstalledContent();
        bool force = forceDownloadEveryTime && hasCache;
        if (hasCache && !force)
        {
            HandleAlreadyDownloaded();
            return;
        }

        string labelToUse = downloadingLabel;
        if (hasCache && !force && !string.IsNullOrEmpty(cachedLabel))
        {
            labelToUse = cachedLabel;
        }

        UpdateStatusLabel(labelToUse);
        usingCachedContent = hasCache && !force;
        onDownloadStarted.Invoke();
        string urlToUse = string.IsNullOrWhiteSpace(downloadUrl) ? null : downloadUrl.Trim();
        bool requiresUrl = force || !hasCache;
        if (requiresUrl && string.IsNullOrWhiteSpace(urlToUse))
        {
            Debug.LogError($"{LogPrefix} Download URL is empty. Cannot download new content.");
            UpdateStatusLabel(failedLabel);
            button.interactable = true;
            onDownloadFailed.Invoke();
            usingCachedContent = false;
            return;
        }

        Debug.Log($"{LogPrefix} Install requested (hasCache={hasCache}, force={force}, urlProvided={!string.IsNullOrWhiteSpace(urlToUse)})");
        installer.BeginInstall(force, urlToUse);
    }

    private void HandleInstallStarted()
    {
        if (!usingCachedContent)
        {
            UpdateStatusLabel(downloadingLabel);
        }
        Debug.Log($"{LogPrefix} Install started");
    }

    private void HandleInstallCompleted()
    {
        UpdateStatusLabel(completedLabel);
        button.interactable = !forceDownloadEveryTime;
        onDownloadCompleted.Invoke();
        Debug.Log($"{LogPrefix} Install completed");
        usingCachedContent = false;
        if (installer.HasInstalledContent())
        {
            HandleAlreadyDownloaded();
        }
    }

    private void HandleInstallFailed()
    {
        UpdateStatusLabel(failedLabel);
        button.interactable = true;
        onDownloadFailed.Invoke();
        Debug.LogWarning($"{LogPrefix} Install failed");
        usingCachedContent = false;
        RefreshButtonState();
    }

    private void UpdateStatusLabel(string label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return;
        }

        if (statusText != null)
        {
            statusText.text = label;
        }

        if (buttonLabel != null)
        {
            buttonLabel.text = label;
        }
    }

    private void CacheOriginalButtonLabel()
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

    public void ResetStatusLabel()
    {
        RefreshButtonState();
        if (buttonLabel != null && !string.IsNullOrEmpty(originalButtonLabel))
        {
            buttonLabel.text = originalButtonLabel;
        }

        if (statusText != null && !string.IsNullOrEmpty(originalStatusLabel))
        {
            statusText.text = originalStatusLabel;
        }
    }

    public void SetDownloadUrl(string url)
    {
        downloadUrl = string.IsNullOrWhiteSpace(url) ? string.Empty : url.Trim();
        ApplyConfigurationOverrides();
    }

    private void RefreshButtonState()
    {
        if (installer != null && installer.HasInstalledContent())
        {
            HandleAlreadyDownloaded();
        }
        else
        {
            button.interactable = true;
            if (!string.IsNullOrEmpty(originalButtonLabel))
            {
                UpdateStatusLabel(originalButtonLabel);
            }
        }
    }

    private void HandleAlreadyDownloaded()
    {
        button.interactable = false;
        UpdateStatusLabel(string.IsNullOrEmpty(alreadyDownloadedLabel) ? completedLabel : alreadyDownloadedLabel);
    }

    private void ApplyConfigurationOverrides()
    {
        if (installer != null)
        {
            installer.SetRemoteVersion(remoteVersionOverride);
        }

        if (launchButton != null)
        {
            launchButton.ApplyConfigurationOverrides();
        }
    }
}
