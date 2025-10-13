using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class WebContentDownloadButton : MonoBehaviour
{
    private const string LogPrefix = "[WebContentDownloadButton]";
    [SerializeField]
    private RemoteWebContentInstaller installer;

    [SerializeField]
    private WebViewController webViewController;

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
    [Tooltip("버튼 상태 변화를 표시할 UI Text")]
    private Text statusText;

    [SerializeField]
    [Tooltip("다운로드가 끝난 뒤 WebView를 자동으로 보이게 할지 여부")]
    private bool showWebViewOnComplete = true;

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

    private void Awake()
    {
        button = GetComponent<Button>();
        AutoAssignReferences();
        button.onClick.AddListener(OnButtonClicked);
        Debug.Log($"{LogPrefix} Awake (button={name})");
    }

    private void Reset()
    {
        AutoAssignReferences();
    }

    private void AutoAssignReferences()
    {
        if (installer == null)
        {
            installer = GetComponentInParent<RemoteWebContentInstaller>();
            if (installer != null)
            {
                Debug.Log($"{LogPrefix} Auto-assigned installer: {installer.name}");
            }
        }

        if (webViewController == null)
        {
            webViewController = GetComponentInParent<WebViewController>();
            if (webViewController == null)
            {
                webViewController = GetComponentInChildren<WebViewController>(true);
            }
            if (webViewController != null)
            {
                Debug.Log($"{LogPrefix} Auto-assigned WebViewController: {webViewController.name}");
            }
        }
    }

    private void OnEnable()
    {
        SubscribeInstallerEvents();
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
            Debug.LogError("[WebContentDownloadButton] RemoteWebContentInstaller reference is missing.");
            return;
        }

        button.interactable = false;
        bool hasCache = installer.HasInstalledContent();
        bool force = forceDownloadEveryTime && hasCache;
        string labelToUse = downloadingLabel;
        if (hasCache && !force && !string.IsNullOrEmpty(cachedLabel))
        {
            labelToUse = cachedLabel;
        }

        UpdateStatusLabel(labelToUse);
        usingCachedContent = hasCache && !force;
        onDownloadStarted.Invoke();
        Debug.Log($"{LogPrefix} Install requested (hasCache={hasCache}, force={force})");
        installer.BeginInstall(force);
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
        button.gameObject.SetActive(false);
        onDownloadCompleted.Invoke();
        Debug.Log($"{LogPrefix} Install completed");

        if (showWebViewOnComplete && webViewController != null)
        {
            webViewController.SetVisible(true);
        }

        usingCachedContent = false;
    }

    private void HandleInstallFailed()
    {
        UpdateStatusLabel(failedLabel);
        button.interactable = true;
        onDownloadFailed.Invoke();
        Debug.LogWarning($"{LogPrefix} Install failed");
        usingCachedContent = false;
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
    }
}
