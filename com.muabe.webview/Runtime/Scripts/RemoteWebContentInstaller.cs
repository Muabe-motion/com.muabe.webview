using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public class RemoteWebContentInstaller : MonoBehaviour
{
    private const string LogPrefix = "[RemoteWebContentInstaller]";
    [Header("다운로드 설정")]
    [SerializeField]
    [Tooltip("원격에서 받을 ZIP 파일 URL")]
    private string downloadUrl = "https://pub-8573e57a18ed403493bfc401bcca451b.r2.dev/flutter.zip";

    [SerializeField]
    [Tooltip("다운로드한 콘텐츠를 저장할 하위 폴더 이름 (persistentDataPath 기준)")]
    private string installFolderName = "webview-content";

    [SerializeField]
    [Tooltip("ZIP 내부에서 웹 앱이 들어 있는 하위 폴더 (예: 'flutter'). 빈 값이면 ZIP 최상위가 곧 콘텐츠 루트")]
    private string contentRootSubfolder = "flutter";

    [SerializeField]
    [Tooltip("콘텐츠 버전. 버전이 다르면 다시 다운로드합니다.")]
    private string remoteVersion = "1.0.0";

    [SerializeField]
    [Tooltip("버전 정보를 저장할 파일 이름")]
    private string versionFileName = ".webcontent-version";

    [SerializeField]
    [Tooltip("컴포넌트가 시작될 때 자동으로 설치를 시도합니다.")]
    private bool installOnStart = false;

    [SerializeField]
    [Tooltip("새 버전을 설치하기 전에 기존 폴더를 삭제합니다.")]
    private bool clearFolderBeforeInstall = true;

    [SerializeField]
    [Tooltip("설치가 끝나면 LocalWebServer에 콘텐츠 경로를 지정하고 서버를 자동으로 시작합니다.")]
    private bool autoStartServer = true;

    [SerializeField]
    [Tooltip("콘텐츠를 제공할 LocalWebServer")]
    private LocalWebServer targetServer;

    [SerializeField]
    [Tooltip("설치가 끝난 뒤 자동으로 URL을 로드할 WebViewController")]
    private WebViewController targetWebView;

    [SerializeField]
    [Tooltip("설치 완료 시 targetWebView.LoadInitialUrl()을 자동으로 호출합니다.")]
    private bool autoLoadWebViewOnInstall = true;

    [Header("이벤트")]
    public UnityEvent onInstallStarted;
    public UnityEvent onInstallCompleted;
    public UnityEvent onInstallFailed;

    private Coroutine installRoutine;
    private bool forceInstallRequested;

    public string InstallPath => Path.Combine(Application.persistentDataPath, installFolderName);

    private void Awake()
    {
        AutoAssignComponents();
        Debug.Log($"{LogPrefix} Awake (installOnStart={installOnStart}, autoStartServer={autoStartServer}, autoLoadWebViewOnInstall={autoLoadWebViewOnInstall})");
    }

    private void Reset()
    {
        AutoAssignComponents();
    }

    private void OnValidate()
    {
        AutoAssignComponents();
    }

    private void AutoAssignComponents()
    {
        if (targetServer == null)
        {
            targetServer = GetComponent<LocalWebServer>();
        }

        if (targetWebView == null)
        {
            targetWebView = GetComponent<WebViewController>();
            if (targetWebView == null)
            {
                targetWebView = GetComponentInChildren<WebViewController>(true);
            }
        }
    }

    private void Start()
    {
        Debug.Log($"{LogPrefix} Start (installOnStart={installOnStart})");
        if (installOnStart)
        {
            BeginInstall();
        }
    }

    public void BeginInstallIfNeeded()
    {
        BeginInstall(false);
    }

    public void BeginInstall(bool forceRedownload = false)
    {
        if (installRoutine != null)
        {
            Debug.LogWarning($"{LogPrefix} Installation already in progress.");
            return;
        }

        forceInstallRequested = forceRedownload;
        Debug.Log($"{LogPrefix} BeginInstall(force={forceRedownload})");
        installRoutine = StartCoroutine(EnsureContentRoutine());
    }

    public bool HasInstalledContent()
    {
        bool pathExists = false;
        try
        {
            string installPath = InstallPath;
            string versionFilePath = Path.Combine(installPath, versionFileName);
            pathExists = Directory.Exists(installPath) && File.Exists(versionFilePath);
            if (!pathExists)
            {
                return false;
            }

            string existingVersion = File.ReadAllText(versionFilePath).Trim();
            bool matches = existingVersion == remoteVersion;
            Debug.Log($"{LogPrefix} HasInstalledContent? pathExists={pathExists} version={existingVersion} matches={matches}");
            return matches;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"{LogPrefix} HasInstalledContent check failed: {e.Message}");
            return false;
        }
    }

    private IEnumerator EnsureContentRoutine()
    {
        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            Debug.LogWarning($"{LogPrefix} Download URL is empty. Skipping installation.");
            installRoutine = null;
            yield break;
        }

        string installPath = InstallPath;
        string versionFilePath = Path.Combine(installPath, versionFileName);

        bool forceInstall = forceInstallRequested;
        forceInstallRequested = false;

        bool needsDownload = true;
        if (!forceInstall && Directory.Exists(installPath) && File.Exists(versionFilePath))
        {
            try
            {
                string existingVersion = File.ReadAllText(versionFilePath).Trim();
                if (existingVersion == remoteVersion)
                {
                    needsDownload = false;
                    Debug.Log($"{LogPrefix} Existing content version {existingVersion} matches remoteVersion. Skipping download.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogPrefix} Failed to read version file. Forcing download. {e.Message}");
            }
        }

        if (!needsDownload)
        {
            HandlePostInstall(installPath);
            onInstallCompleted?.Invoke();
            installRoutine = null;
            yield break;
        }

        onInstallStarted?.Invoke();
        Debug.Log($"{LogPrefix} Downloading from {downloadUrl}");

        using UnityWebRequest request = UnityWebRequest.Get(downloadUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"{LogPrefix} Download failed: {request.error}");
            onInstallFailed?.Invoke();
            installRoutine = null;
            yield break;
        }

        byte[] data = request.downloadHandler.data;
        if (data == null || data.Length == 0)
        {
            Debug.LogError($"{LogPrefix} Download returned empty data.");
            onInstallFailed?.Invoke();
            installRoutine = null;
            yield break;
        }

        try
        {
            InstallFromZip(data, installPath);
            File.WriteAllText(versionFilePath, remoteVersion);
            Debug.Log($"{LogPrefix} Installation finished. Extracted files to {installPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"{LogPrefix} Failed to install content: {e}");
            onInstallFailed?.Invoke();
            installRoutine = null;
            yield break;
        }

        HandlePostInstall(installPath);
        onInstallCompleted?.Invoke();
        installRoutine = null;
    }

    private void InstallFromZip(byte[] zipData, string installPath)
    {
        if (clearFolderBeforeInstall && Directory.Exists(installPath))
        {
            Directory.Delete(installPath, true);
        }

        Directory.CreateDirectory(installPath);

        using MemoryStream memoryStream = new MemoryStream(zipData);
        using ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Read);

        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string destinationPath = Path.Combine(installPath, entry.FullName);
            string fullDestinationPath = Path.GetFullPath(destinationPath);
            string installRoot = Path.GetFullPath(installPath);

            if (!fullDestinationPath.StartsWith(installRoot, StringComparison.Ordinal))
            {
                Debug.LogWarning($"{LogPrefix} Skipping entry outside target directory: {entry.FullName}");
                continue;
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(fullDestinationPath);
                continue;
            }

            string directory = Path.GetDirectoryName(fullDestinationPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using Stream entryStream = entry.Open();
            using FileStream fileStream = File.Create(fullDestinationPath);
            entryStream.CopyTo(fileStream);
        }
        Debug.Log($"{LogPrefix} Extracted {archive.Entries.Count} entries.");
    }

    private void HandlePostInstall(string installPath)
    {
        if (targetServer != null)
        {
            ApplyToServer(installPath);
        }
        else if (autoStartServer)
        {
            Debug.LogWarning($"{LogPrefix} targetServer is not assigned. Server will not start automatically.");
        }

        if (autoLoadWebViewOnInstall && targetWebView != null)
        {
            targetWebView.StartCoroutine(LoadWebViewAfterServer());
        }
        else if (autoLoadWebViewOnInstall && targetWebView == null)
        {
            Debug.LogWarning($"{LogPrefix} targetWebView is not assigned. WebView will not auto-load.");
        }
    }

    private void ApplyToServer(string installPath)
    {
        if (targetServer == null)
        {
            return;
        }

        string contentRoot = installPath;
        if (!string.IsNullOrEmpty(contentRootSubfolder))
        {
            contentRoot = Path.Combine(contentRoot, contentRootSubfolder);
        }

        targetServer.SetContentRootOverride(contentRoot);
        Debug.Log($"{LogPrefix} Applied content root {contentRoot}");
        if (autoStartServer)
        {
            targetServer.StartServer();
        }
    }

    private IEnumerator LoadWebViewAfterServer()
    {
        if (targetServer != null)
        {
            float timeout = 5f;
            while (!targetServer.IsRunning && timeout > 0f)
            {
                yield return null;
                timeout -= Time.unscaledDeltaTime;
            }
            if (!targetServer.IsRunning)
            {
                Debug.LogWarning($"{LogPrefix} Server did not start within timeout; loading WebView anyway");
            }
            else
            {
                Debug.Log($"{LogPrefix} Server is running; proceeding to load WebView");
            }
        }

        yield return null;
        targetWebView.LoadInitialUrl();
        Debug.Log($"{LogPrefix} LoadInitialUrl invoked");
    }

    public void SetTargetWebView(WebViewController controller)
    {
        targetWebView = controller;
    }
}
