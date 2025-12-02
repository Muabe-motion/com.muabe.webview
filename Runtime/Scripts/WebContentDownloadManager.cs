using System;
using System.Collections;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace Muabe.WebView
{
    public class WebContentDownloadManager : MonoBehaviour
    {
        private const string LogPrefix = WebViewConstants.LogPrefixDownloadManager;
        [Header("다운로드 설정")]
        [SerializeField]
        [HideInInspector]
        [Tooltip("원격에서 받을 ZIP 파일 URL (선택). 비워두면 DownloadContent 호출 시 전달된 URL을 사용합니다.")]
        private string defaultDownloadUrl = string.Empty;

        [SerializeField]
        [Tooltip("다운로드한 콘텐츠를 저장할 상대 경로 (persistentDataPath 기준, 예: arpedia/dino/wj_demo)")]
        private string installFolderPath = "webview-content";

        [SerializeField]
        [HideInInspector]
        private string remoteVersion = string.Empty;

        [SerializeField]
        [Tooltip("버전 정보를 저장할 파일 이름")]
        private string versionFileName = WebViewConstants.DefaultVersionFileName;

        [SerializeField]
        [Tooltip("컴포넌트가 시작될 때 자동으로 설치를 시도합니다.")]
        private bool installOnStart = false;

        [SerializeField]
        [Tooltip("새 버전을 설치하기 전에 기존 폴더를 삭제합니다.")]
        private bool clearFolderBeforeInstall = true;

        [SerializeField]
        [Header("이벤트")]
        public UnityEvent onInstallStarted;
        public UnityEvent onInstallCompleted;
        public UnityEvent onInstallFailed;
        public UnityEvent<float> onDownloadProgress;

        private Coroutine installRoutine;
        private bool forceInstallRequested;
        private string activeDownloadUrl;
        private string lastDownloadUrl;

        public string InstallPath => Path.Combine(Application.persistentDataPath, NormalizePath(installFolderPath));
        public string ContentRootPath => InstallPath;

        public string LastInstallPath { get; private set; }

        private void Awake()
        {
            installFolderPath = NormalizePath(installFolderPath);
            remoteVersion = NormalizeVersion(remoteVersion);
            Debug.Log($"{LogPrefix} Awake (installOnStart={installOnStart})");
        }

        private void Start()
        {
            installFolderPath = NormalizePath(installFolderPath);
            remoteVersion = NormalizeVersion(remoteVersion);
            Debug.Log($"{LogPrefix} Start (installOnStart={installOnStart})");
            if (installOnStart)
            {
                BeginInstall();
            }
        }

        private void OnValidate()
        {
            installFolderPath = NormalizePath(installFolderPath);
            remoteVersion = NormalizeVersion(remoteVersion);
        }

        public void DownloadContent()
        {
            BeginInstall(false);
        }

        public void DownloadContentForced()
        {
            BeginInstall(true);
        }

        public void DownloadContent(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"{LogPrefix} DownloadContent(string) called with empty URL.");
                return;
            }
            BeginInstall(false, url);
        }

        public void DownloadContentForced(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"{LogPrefix} DownloadContentForced(string) called with empty URL.");
                return;
            }
            BeginInstall(true, url);
        }

        public void BeginInstallIfNeeded(string overrideDownloadUrl = null)
        {
            BeginInstall(false, overrideDownloadUrl);
        }

        public void BeginInstall(bool forceRedownload = false, string overrideDownloadUrl = null)
        {
            if (installRoutine != null)
            {
                Debug.LogWarning($"{LogPrefix} Installation already in progress.");
                return;
            }

            forceInstallRequested = forceRedownload;
            if (!string.IsNullOrWhiteSpace(overrideDownloadUrl))
            {
                activeDownloadUrl = overrideDownloadUrl.Trim();
                lastDownloadUrl = activeDownloadUrl;
            }
            else if (!string.IsNullOrWhiteSpace(lastDownloadUrl))
            {
                activeDownloadUrl = lastDownloadUrl;
            }
            else
            {
                activeDownloadUrl = defaultDownloadUrl;
                if (!string.IsNullOrWhiteSpace(activeDownloadUrl))
                {
                    lastDownloadUrl = activeDownloadUrl;
                }
            }
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
                if (string.IsNullOrEmpty(remoteVersion))
                {
                    remoteVersion = NormalizeVersion(existingVersion);
                    Debug.Log($"{LogPrefix} HasInstalledContent? pathExists={pathExists} version={existingVersion} matches=True (remoteVersion empty, adopting existing)");
                    return true;
                }

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

        public bool TryGetInstalledContentRoot(out string contentRoot)
        {
            contentRoot = ContentRootPath;
            return Directory.Exists(contentRoot);
        }

        public string RemoteVersion => remoteVersion;

        public void SetRemoteVersion(string version)
        {
            remoteVersion = NormalizeVersion(version);
        }

        public void SetInstallFolderPath(string relativePath)
        {
            installFolderPath = NormalizePath(relativePath);
        }

        private IEnumerator EnsureContentRoutine()
        {
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
                LastInstallPath = installPath;
                onInstallCompleted?.Invoke();
                installRoutine = null;
                activeDownloadUrl = null;
                yield break;
            }

            string resolvedUrl = activeDownloadUrl;
            if (string.IsNullOrWhiteSpace(resolvedUrl))
            {
                Debug.LogWarning($"{LogPrefix} Download URL is empty. Skipping installation.");
                onInstallFailed?.Invoke();
                installRoutine = null;
                activeDownloadUrl = null;
                yield break;
            }

            onInstallStarted?.Invoke();
            Debug.Log($"{LogPrefix} Downloading from {resolvedUrl}");

            byte[] data;
            using (UnityWebRequest request = UnityWebRequest.Get(resolvedUrl))
            {
                var operation = request.SendWebRequest();

                // 다운로드 진행률 업데이트
                while (!operation.isDone)
                {
                    float progress = request.downloadProgress;
                    onDownloadProgress?.Invoke(progress);
                    yield return null;
                }

                // 완료 시 100%
                onDownloadProgress?.Invoke(1f);

                if (request.isNetworkError || request.isHttpError)
                {
                    Debug.LogError($"{LogPrefix} Download failed: {request.error}");
                    onInstallFailed?.Invoke();
                    installRoutine = null;
                    activeDownloadUrl = null;
                    yield break;
                }

                data = request.downloadHandler.data;
            }

            if (data == null || data.Length == 0)
            {
                Debug.LogError($"{LogPrefix} Download returned empty data.");
                onInstallFailed?.Invoke();
                installRoutine = null;
                activeDownloadUrl = null;
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
                activeDownloadUrl = null;
                yield break;
            }

            LastInstallPath = installPath;
            onInstallCompleted?.Invoke();
            installRoutine = null;
            activeDownloadUrl = null;
        }

        private string NormalizeVersion(string value)
        {
            return WebViewUtility.NormalizeVersion(value);
        }

        private string NormalizePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return "webview-content";
            }

            value = value.Trim();

            // 백슬래시를 슬래시로 변환 (Windows 경로 대응)
            value = value.Replace('\\', '/');

            // 앞뒤 슬래시 제거
            value = value.Trim('/');

            // 연속된 슬래시 제거
            while (value.Contains("//"))
            {
                value = value.Replace("//", "/");
            }

            return string.IsNullOrEmpty(value) ? "webview-content" : value;
        }

        private void InstallFromZip(byte[] zipData, string installPath)
        {
            if (clearFolderBeforeInstall && Directory.Exists(installPath))
            {
                Directory.Delete(installPath, true);
            }

            Directory.CreateDirectory(installPath);

            using (MemoryStream memoryStream = new MemoryStream(zipData))
            using (ZipArchive archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
            {
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

                    using (Stream entryStream = entry.Open())
                    using (FileStream fileStream = File.Create(fullDestinationPath))
                    {
                        entryStream.CopyTo(fileStream);
                    }
                }
                Debug.Log($"{LogPrefix} Extracted {archive.Entries.Count} entries.");
            }
        }
    }
}
