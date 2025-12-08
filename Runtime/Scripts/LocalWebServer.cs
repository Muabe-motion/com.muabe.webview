using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace Muabe.WebView
{
    public class LocalWebServer : MonoBehaviour
    {
        private const string LogPrefix = WebViewConstants.LogPrefixServer;

#if UNITY_ANDROID && !UNITY_EDITOR
        private static readonly Dictionary<string, byte[]> AndroidStreamingCache = new Dictionary<string, byte[]>();
        private static readonly object AndroidCacheLock = new object();
        private static bool androidCacheReady;
#endif

        private static LocalWebServer instance;

        [Header("Server")]
        [Tooltip("로컬 서버가 사용할 포트 번호")]
        public int port = WebViewConstants.DefaultServerPort;

        [SerializeField]
        [Tooltip("씬 시작 시 자동으로 서버를 기동할지 여부")]
        private bool autoStartOnStart = false;

        [SerializeField]
        [Tooltip("요청 경로가 폴더이거나 빈 문자열일 때 제공할 기본 문서 이름")]
        private string defaultDocument = WebViewConstants.DefaultDocument;

        [SerializeField]
        [Tooltip("요청과 응답 정보를 콘솔에 로그로 출력할지 여부")]
        private bool logRequests = false;

        [Header("콘텐츠 경로 설정")]
        [SerializeField]
        [Tooltip("index.html이 있는 폴더의 상대 경로 (persistentDataPath 기준, 예: arpedia/dino/wj_demo). 절대 경로도 지원합니다.")]
        private string contentPath = string.Empty;

        [SerializeField]
        [Tooltip("Android에서 사용할 파일 리스트 텍스트(선택). 상대 경로로 지정하며, 줄마다 파일 경로를 작성합니다.")]
        private string androidPreloadListFile = string.Empty;

        [SerializeField]
        [Tooltip("파일 리스트에서 주석으로 취급할 문자. 기본은 #")]
        private char androidPreloadListCommentChar = WebViewConstants.DefaultCommentChar;

        private TcpListener tcpListener;
        private Thread listenerThread;
        private bool isRunning;
        private string cachedContentRootFullPath;
        private bool isServerReady;

        public bool IsRunning => isRunning;
        public bool IsServerReady => isServerReady;

        public event System.Action OnServerReady;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.Log("LocalWebServer already exists. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"{LogPrefix} Awake on {name}");
        }

        private void Start()
        {
            Debug.Log($"{LogPrefix} Start (autoStartOnStart={autoStartOnStart}, contentPath={contentPath})");
            if (!autoStartOnStart)
            {
                return;
            }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ShouldUseAndroidStreamingAssets())
        {
            StartCoroutine(PreloadStreamingAssets());
            return;
        }
#endif
            StartServer();
        }

        /// <summary>
        /// index.html이 있는 폴더의 경로를 설정합니다.
        /// 상대 경로 입력 시 persistentDataPath 기준으로 자동 변환됩니다.
        /// 예: "arpedia/dino/wj_demo" → "{persistentDataPath}/arpedia/dino/wj_demo"
        /// </summary>
        public void SetContentPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                contentPath = string.Empty;
                cachedContentRootFullPath = string.Empty;
                Debug.LogWarning($"{LogPrefix} Content path cleared");
                return;
            }

            contentPath = NormalizeFullPath(path);
            contentPath = EnsureDirectoryContentPath(contentPath);

            // 캐시 무효화
            cachedContentRootFullPath = string.Empty;

            Debug.Log($"{LogPrefix} Content path set to: {contentPath}");

            // 경로 유효성 검증
            if (!Directory.Exists(contentPath))
            {
                Debug.LogWarning($"{LogPrefix} Warning: Directory does not exist: {contentPath}");
            }
            else
            {
                string indexPath = Path.Combine(contentPath, defaultDocument);
                if (File.Exists(indexPath))
                {
                    Debug.Log($"{LogPrefix} ✓ Found {defaultDocument} at {indexPath}");
                }
                else
                {
                    Debug.LogWarning($"{LogPrefix} Warning: {defaultDocument} not found at {indexPath}");
                }
            }
        }

        /// <summary>
        /// 현재 설정된 콘텐츠 경로를 반환합니다.
        /// </summary>
        public string GetContentPath()
        {
            if (!string.IsNullOrEmpty(cachedContentRootFullPath))
            {
                return cachedContentRootFullPath;
            }

            if (string.IsNullOrEmpty(contentPath))
            {
                Debug.LogWarning($"{LogPrefix} Content path is empty");
                return string.Empty;
            }

            cachedContentRootFullPath = NormalizeFullPath(contentPath);
            cachedContentRootFullPath = EnsureDirectoryContentPath(cachedContentRootFullPath);

            if (logRequests)
            {
                Debug.Log($"{LogPrefix} Content path resolved to {cachedContentRootFullPath}");
            }

            return cachedContentRootFullPath;
        }

        public void StartServer()
        {
            if (isRunning)
            {
                Debug.LogWarning($"{LogPrefix} Server is already running");
                return;
            }

            // 서버 시작 전 경로 유효성 검증
            string currentContentPath = GetContentPath();
            if (string.IsNullOrEmpty(currentContentPath))
            {
                Debug.LogError($"{LogPrefix} Cannot start server: Content path is empty. Use SetContentPath() to set the path.");
                return;
            }

            if (!Directory.Exists(currentContentPath))
            {
                Debug.LogError($"{LogPrefix} Cannot start server: Directory does not exist: {currentContentPath}");
                return;
            }

            listenerThread = new Thread(Listen) { IsBackground = true };
            listenerThread.Start();
            Debug.Log($"{LogPrefix} Local web server starting on http://localhost:{port}");
            Debug.Log($"{LogPrefix} Serving content from: {currentContentPath}");

            StartCoroutine(NotifyServerReady());
        }

        private IEnumerator NotifyServerReady()
        {
            yield return new WaitForSeconds(0.5f);
            isServerReady = true;
            OnServerReady?.Invoke();
            Debug.Log($"{LogPrefix} Server ready notification sent");
        }

        public void StopServer()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            isServerReady = false;

            if (tcpListener != null)
            {
                tcpListener.Stop();
            }

            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(1000);
            }
            Debug.Log($"{LogPrefix} Local web server stopped");
        }

        private void Listen()
        {
            try
            {
                isRunning = true;
                tcpListener = new TcpListener(IPAddress.Loopback, port);
                tcpListener.Start(10); // 백로그 큐 크기 제한 (동시 연결 대기 수)

                while (isRunning)
                {
                    if (!tcpListener.Pending())
                    {
                        Thread.Sleep(10);
                        continue;
                    }

                    try
                    {
                        TcpClient client = tcpListener.AcceptTcpClient();
                        Thread clientThread = new Thread(() => HandleClient(client)) { IsBackground = true };
                        clientThread.Start();
                    }
                    catch (Exception e)
                    {
                        if (isRunning)
                        {
                            Debug.LogError($"{LogPrefix} Error accepting client: {e.Message}");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} Failed to start listener: {e.Message}");
            }
        }

        private void HandleClient(TcpClient client)
        {
            try
            {
                // 타임아웃 설정 (읽기/쓰기 각 5초)
                client.ReceiveTimeout = 5000;
                client.SendTimeout = 5000;

                using (NetworkStream stream = client.GetStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead <= 0)
                    {
                        return;
                    }

                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    string[] requestLines = request.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
                    if (requestLines.Length == 0)
                    {
                        return;
                    }

                    string[] requestParts = requestLines[0].Split(' ');
                    if (requestParts.Length < 2)
                    {
                        return;
                    }

                    string method = requestParts[0];
                    string url = requestParts[1];

                    if (logRequests)
                    {
                        Debug.Log($"{LogPrefix} {method} {url}");
                    }

                    if (method == "GET")
                    {
                        ProcessGetRequest(stream, url);
                    }
                }
            }
            catch (System.IO.IOException ioEx)
            {
                // 네트워크 I/O 에러는 경고로만 처리 (클라이언트가 연결을 끊은 경우 등)
                if (logRequests)
                {
                    Debug.LogWarning($"{LogPrefix} Network I/O error: {ioEx.Message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"{LogPrefix} Error handling client: {e.Message}");
            }
            finally
            {
                try
                {
                    client.Close();
                }
                catch
                {
                    // 이미 닫힌 연결은 무시
                }
            }
        }

        private void ProcessGetRequest(NetworkStream stream, string url)
        {
            string normalizedPath = NormalizeRequestPath(url);

            if (string.IsNullOrEmpty(normalizedPath) || normalizedPath.EndsWith("/", StringComparison.Ordinal))
            {
                normalizedPath = AppendDefaultDocument(normalizedPath);
            }

            byte[] fileBytes;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ShouldUseAndroidStreamingAssets())
        {
            if (!androidCacheReady)
            {
                WriteResponse(stream, 503, "text/plain", "Server is preparing content...");
                Debug.LogWarning($"{LogPrefix} Cache not ready yet for {normalizedPath}");
                return;
            }

            lock (AndroidCacheLock)
            {
                if (!AndroidStreamingCache.TryGetValue(normalizedPath, out fileBytes))
                {
                    WriteResponse(stream, 404, "text/plain", $"File not cached: {normalizedPath}");
                    Debug.LogWarning($"{LogPrefix} Cache miss: {normalizedPath}");
                    return;
                }
            }
        }
        else
#endif
            {
                if (!TryReadFileFromDisk(normalizedPath, out fileBytes))
                {
                    WriteResponse(stream, 404, "text/plain", $"File not found: {normalizedPath}");
                    Debug.LogWarning($"{LogPrefix} File not found on disk: {normalizedPath}");
                    return;
                }
            }

            string contentType = GetContentType(normalizedPath);
            if (logRequests)
            {
                Debug.Log($"{LogPrefix} Serving {normalizedPath} ({contentType})");
            }
            WriteResponse(stream, 200, contentType, fileBytes);
        }

        private bool TryReadFileFromDisk(string relativePath, out byte[] data)
        {
            data = null;
            string root = GetContentPath();
            if (string.IsNullOrEmpty(root))
            {
                return false;
            }

            string safeRelative = relativePath.Replace('/', Path.DirectorySeparatorChar);
            string combined = Path.Combine(root, safeRelative);
            string fullPath = NormalizeFullPath(combined);
            string rootFull = NormalizeFullPath(root);

            if (!fullPath.StartsWith(rootFull, StringComparison.Ordinal))
            {
                Debug.LogWarning($"{LogPrefix} Blocked path traversal attempt: {relativePath}");
                return false;
            }

            if (!File.Exists(fullPath))
            {
                return false;
            }

            data = File.ReadAllBytes(fullPath);
            return true;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    private IEnumerator PreloadStreamingAssets()
    {
        string rootPath = GetContentPath();
        if (string.IsNullOrEmpty(rootPath))
        {
            Debug.LogWarning("[LocalWebServer] Content path is empty. Server will start without cached files.");
            StartServer();
            yield break;
        }

        string baseUri = BuildStreamingAssetsUri(rootPath);

        List<string> filesToPreload = null;
        bool listFromFile = false;

        if (!string.IsNullOrEmpty(androidPreloadListFile))
        {
            filesToPreload = new List<string>();
            string listRelative = NormalizeRelativePath(androidPreloadListFile);

#if UNITY_ANDROID && !UNITY_EDITOR
            if (ShouldUseAndroidStreamingAssets())
            {
                if (!string.IsNullOrEmpty(listRelative))
                {
                    string listUri = CombineUri(baseUri, listRelative);
                    using (UnityWebRequest listRequest = UnityWebRequest.Get(listUri))
                    {
                        yield return listRequest.SendWebRequest();

                        if (!listRequest.isNetworkError && !listRequest.isHttpError)
                        {
                            ParsePreloadList(listRequest.downloadHandler.text, filesToPreload);
                            listFromFile = filesToPreload.Count > 0;
                            Debug.Log($"{LogPrefix} Loaded {filesToPreload.Count} preload entries from {listUri}");
                        }
                        else
                        {
                            Debug.LogWarning($"{LogPrefix} Failed to load preload list {listUri}: {listRequest.error}");
                        }
                    }
                }
            }
            else
#endif
            {
                LoadPreloadListFromDisk(rootPath, listRelative, filesToPreload);
                listFromFile = filesToPreload.Count > 0;
            }
        }

        if (filesToPreload == null)
        {
            filesToPreload = new List<string>();
        }

        if (filesToPreload.Count == 0)
        {
            Debug.LogWarning($"{LogPrefix} No files listed for Android preload. Server will run uncached.");
            StartServer();
            yield break;
        }

        lock (AndroidCacheLock)
        {
            AndroidStreamingCache.Clear();
            androidCacheReady = false;
        }

        foreach (string entry in filesToPreload)
        {
            if (string.IsNullOrWhiteSpace(entry))
            {
                continue;
            }

            string relative = NormalizeRelativePath(entry);
            string fileUri = CombineUri(baseUri, relative);

            using (UnityWebRequest request = UnityWebRequest.Get(fileUri))
            {
                yield return request.SendWebRequest();

                if (!request.isNetworkError && !request.isHttpError)
                {
                    lock (AndroidCacheLock)
                    {
                        AndroidStreamingCache[relative] = request.downloadHandler.data;
                    }
                }
                else
                {
                    Debug.LogError($"{LogPrefix} Failed to preload {fileUri}: {request.error}");
                }
            }
        }

        lock (AndroidCacheLock)
        {
            androidCacheReady = true;
        }

        Debug.Log($"{LogPrefix} Android preload completed. Entries: {filesToPreload.Count}, source={(listFromFile ? "file" : "inspector")}");
        StartServer();
    }
#endif

        private void LoadPreloadListFromDisk(string rootPath, string listRelative, List<string> output)
        {
            if (string.IsNullOrEmpty(listRelative))
            {
                return;
            }

            try
            {
                string fullPath = NormalizeFullPath(Path.Combine(rootPath, listRelative));
                if (!File.Exists(fullPath))
                {
                    Debug.LogWarning($"{LogPrefix} Preload list file not found: {fullPath}");
                    return;
                }

                string content = File.ReadAllText(fullPath);
                ParsePreloadList(content, output);
                Debug.Log($"{LogPrefix} Loaded {output.Count} preload entries from {fullPath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogPrefix} Failed to read preload list: {e.Message}");
            }
        }

        private void ParsePreloadList(string raw, List<string> output)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return;
            }

            using (StringReader reader = new StringReader(raw))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    if (androidPreloadListCommentChar != '\0' && line.Length > 0 && line[0] == androidPreloadListCommentChar)
                    {
                        continue;
                    }

                    string normalized = NormalizeRelativePath(line);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        output.Add(normalized);
                    }
                }
            }
        }

        private string NormalizeRequestPath(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return string.Empty;
            }

            string path = url;
            int queryIndex = path.IndexOf('?');
            if (queryIndex >= 0)
            {
                path = path.Substring(0, queryIndex);
            }

            if (path.StartsWith("/", StringComparison.Ordinal))
            {
                path = path.Substring(1);
            }

            return path.Replace('\\', '/');
        }

        private string AppendDefaultDocument(string relativePath)
        {
            string sanitized = relativePath?.Trim('/') ?? string.Empty;
            if (string.IsNullOrEmpty(sanitized))
            {
                return defaultDocument;
            }

            return sanitized + "/" + defaultDocument;
        }

        private string NormalizeRelativePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            string trimmed = value.Trim();
            trimmed = trimmed.Replace('\\', '/');
            trimmed = trimmed.Trim('/');
            return trimmed;
        }

        private bool ShouldUseAndroidStreamingAssets()
        {
            // Android StreamingAssets는 더 이상 자동으로 사용하지 않음
            // contentPath가 StreamingAssets를 가리키고 있고 Android 플랫폼이면 preload 사용
            return !string.IsNullOrEmpty(contentPath)
                && contentPath.Contains("StreamingAssets")
                && Application.platform == RuntimePlatform.Android;
        }

        private string CombineUri(string baseUri, string relative)
        {
            if (string.IsNullOrEmpty(relative))
            {
                return baseUri;
            }

            if (!baseUri.EndsWith("/", StringComparison.Ordinal))
            {
                baseUri += "/";
            }

            return baseUri + relative;
        }

        private string BuildStreamingAssetsUri(string fullPath)
        {
            string uri = fullPath;
            if (!uri.Contains("://"))
            {
                uri = "file://" + fullPath;
            }

            return uri.Replace('\\', '/');
        }

        private string NormalizeFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            path = path.Trim();

            // 절대 경로인지 확인 (Windows: C:\ 또는 D:\, Unix: /)
            bool isAbsolute = Path.IsPathRooted(path);

            if (!isAbsolute)
            {
                // 상대 경로인 경우 persistentDataPath 기준으로 변환
                path = path.Replace('\\', '/').Trim('/');
                path = Path.Combine(Application.persistentDataPath, path);
            }

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        private string EnsureDirectoryContentPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (Directory.Exists(path))
            {
                return path;
            }

            string normalized = path.Replace('\\', '/');
            bool endsWithDefaultDocument = !string.IsNullOrEmpty(defaultDocument)
                && normalized.EndsWith("/" + defaultDocument, StringComparison.OrdinalIgnoreCase);

            bool looksLikeFile = endsWithDefaultDocument || Path.HasExtension(path);

            if (looksLikeFile)
            {
                string parent = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(parent))
                {
                    Debug.Log($"{LogPrefix} Provided path appears to reference a file. Using parent directory: {parent}");
                    return parent;
                }
            }

            return path;
        }

        private void WriteResponse(NetworkStream stream, int statusCode, string contentType, string message)
        {
            byte[] payload = Encoding.UTF8.GetBytes(message);
            WriteResponse(stream, statusCode, contentType, payload);
        }

        private void WriteResponse(NetworkStream stream, int statusCode, string contentType, byte[] payload)
        {
            if (!stream.CanWrite)
            {
                Debug.LogWarning($"{LogPrefix} Cannot write to stream (stream closed or not writable)");
                return;
            }

            try
            {
                string statusText;
                switch (statusCode)
                {
                    case 200:
                        statusText = "OK";
                        break;
                    case 404:
                        statusText = "Not Found";
                        break;
                    case 500:
                        statusText = "Internal Server Error";
                        break;
                    case 503:
                        statusText = "Service Unavailable";
                        break;
                    default:
                        statusText = "OK";
                        break;
                }

                string header = $"HTTP/1.1 {statusCode} {statusText}\r\n" +
                                $"Content-Type: {contentType}\r\n" +
                                $"Content-Length: {payload.Length}\r\n" +
                                "Access-Control-Allow-Origin: *\r\n" +
                                "Connection: close\r\n\r\n";

                byte[] headerBytes = Encoding.UTF8.GetBytes(header);
                stream.Write(headerBytes, 0, headerBytes.Length);

                if (payload.Length > 0)
                {
                    // 큰 파일은 청크로 나눠서 전송
                    const int chunkSize = 8192; // 8KB 청크
                    int offset = 0;

                    while (offset < payload.Length)
                    {
                        int bytesToWrite = Math.Min(chunkSize, payload.Length - offset);
                        stream.Write(payload, offset, bytesToWrite);
                        offset += bytesToWrite;
                    }
                }

                stream.Flush();
            }
            catch (System.IO.IOException ioEx)
            {
                // 클라이언트가 연결을 끊은 경우 등의 I/O 에러
                if (logRequests)
                {
                    Debug.LogWarning($"{LogPrefix} Failed to write response: {ioEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"{LogPrefix} Unexpected error writing response: {ex.Message}");
            }
        }

        private string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            switch (extension)
            {
                case ".html":
                    return "text/html; charset=utf-8";
                case ".js":
                    return "application/javascript; charset=utf-8";
                case ".css":
                    return "text/css; charset=utf-8";
                case ".json":
                    return "application/json; charset=utf-8";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".gif":
                    return "image/gif";
                case ".svg":
                    return "image/svg+xml";
                case ".ico":
                    return "image/x-icon";
                case ".wasm":
                    return "application/wasm";
                case ".woff":
                    return "font/woff";
                case ".woff2":
                    return "font/woff2";
                case ".ttf":
                    return "font/ttf";
                case ".mp4":
                    return "video/mp4";
                case ".webm":
                    return "video/webm";
                case ".ogg":
                    return "video/ogg";
                case ".ogv":
                    return "video/ogg";
                case ".mov":
                    return "video/quicktime";
                case ".avi":
                    return "video/x-msvideo";
                case ".m4v":
                    return "video/x-m4v";
                default:
                    return "application/octet-stream";
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                StopServer();
            }
        }

        private void OnApplicationQuit()
        {
            StopServer();
        }
    }
}
