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

        public enum ContentRootSource
        {
            StreamingAssets,
            PersistentDataPath,
            CustomAbsolute
        }

        private static readonly Dictionary<string, byte[]> AndroidStreamingCache = new Dictionary<string, byte[]>();
        private static readonly object AndroidCacheLock = new object();
        private static bool androidCacheReady;

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
        [Tooltip("콘텐츠 파일이 위치한 기본 위치")]
        private ContentRootSource contentSource = ContentRootSource.PersistentDataPath;

        [SerializeField]
        [Tooltip("ContentRootSource가 CustomAbsolute일 때 사용할 절대 경로")]
        private string customAbsoluteContentRoot = string.Empty;

        [SerializeField]
        [Tooltip("Android에서 사용할 파일 리스트 텍스트(선택). 상대 경로로 지정하며, 줄마다 파일 경로를 작성합니다.")]
        private string androidPreloadListFile = string.Empty;

        [SerializeField]
        [Tooltip("파일 리스트에서 주석으로 취급할 문자. 기본은 #")]
        private char androidPreloadListCommentChar = WebViewConstants.DefaultCommentChar;

        private TcpListener tcpListener;
        private Thread listenerThread;
        private bool isRunning;
        private string contentRootOverride;
        private string cachedContentRootFullPath;
        private string routePrefix = string.Empty;
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
        Debug.Log($"{LogPrefix} Start (autoStartOnStart={autoStartOnStart}, contentSource={contentSource})");
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

    public void SetContentRootOverride(string absolutePath)
    {
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            contentRootOverride = string.Empty;
            cachedContentRootFullPath = string.Empty;
            return;
        }

        contentRootOverride = NormalizeFullPath(absolutePath);
        cachedContentRootFullPath = contentRootOverride;
        Debug.Log($"{LogPrefix} Content root override set to {cachedContentRootFullPath}");
    }

    public void SetRoutePrefix(string prefix)
    {
        routePrefix = NormalizeRoute(prefix);
        cachedContentRootFullPath = string.Empty;
    }

    public string GetCurrentContentRoot()
    {
        if (!string.IsNullOrEmpty(cachedContentRootFullPath))
        {
            return cachedContentRootFullPath;
        }

        string basePath = string.Empty;
        switch (contentSource)
        {
            case ContentRootSource.StreamingAssets:
                basePath = Application.streamingAssetsPath;
                break;
            case ContentRootSource.PersistentDataPath:
                basePath = Application.persistentDataPath;
                break;
            case ContentRootSource.CustomAbsolute:
                basePath = customAbsoluteContentRoot;
                break;
        }

        if (string.IsNullOrEmpty(basePath))
        {
            return string.Empty;
        }

        if (contentSource != ContentRootSource.CustomAbsolute)
        {
        string prefix = NormalizeRoute(routePrefix);
        if (!string.IsNullOrEmpty(prefix))
        {
            basePath = CombineFilesystemPath(basePath, prefix);
        }
        }

        cachedContentRootFullPath = NormalizeFullPath(basePath);
        if (logRequests)
        {
            Debug.Log($"{LogPrefix} Current content root resolved to {cachedContentRootFullPath}");
        }
        return cachedContentRootFullPath;
    }

    public void StartServer()
    {
        if (isRunning)
        {
            return;
        }

        listenerThread = new Thread(Listen) { IsBackground = true };
        listenerThread.Start();
        Debug.Log($"{LogPrefix} Local web server starting on http://localhost:{port}");
        
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
        string route = NormalizeRoute(routePrefix);
        string normalizedPath = NormalizeRequestPath(url);

        if (!TryGetRelativePath(normalizedPath, route, out string relativePath))
        {
            WriteResponse(stream, 404, "text/plain", "Requested path is outside the configured route.");
            Debug.LogWarning($"{LogPrefix} Requested path outside route: {normalizedPath}");
            return;
        }

        if (string.IsNullOrEmpty(relativePath) || relativePath.EndsWith("/", StringComparison.Ordinal))
        {
            relativePath = AppendDefaultDocument(relativePath);
        }

        string cacheKey = BuildCacheKey(route, relativePath);
        byte[] fileBytes;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (ShouldUseAndroidStreamingAssets())
        {
            if (!androidCacheReady)
            {
                WriteResponse(stream, 503, "text/plain", "Server is preparing content...");
                Debug.LogWarning($"{LogPrefix} Cache not ready yet for {relativePath}");
                return;
            }

            lock (AndroidCacheLock)
            {
                if (!AndroidStreamingCache.TryGetValue(cacheKey, out fileBytes))
                {
                    WriteResponse(stream, 404, "text/plain", $"File not cached: {cacheKey}");
                    Debug.LogWarning($"{LogPrefix} Cache miss: {cacheKey}");
                    return;
                }
            }
        }
        else
#endif
        {
            if (!TryReadFileFromDisk(relativePath, out fileBytes))
            {
                WriteResponse(stream, 404, "text/plain", $"File not found: {relativePath}");
                Debug.LogWarning($"{LogPrefix} File not found on disk: {relativePath}");
                return;
            }
        }

        string contentType = GetContentType(relativePath);
        if (logRequests)
        {
            Debug.Log($"{LogPrefix} Serving {relativePath} ({contentType})");
        }
        WriteResponse(stream, 200, contentType, fileBytes);
    }

    private bool TryReadFileFromDisk(string relativePath, out byte[] data)
    {
        data = null;
        string root = GetCurrentContentRoot();
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

    private IEnumerator PreloadStreamingAssets()
    {
        string route = NormalizeRoute(routePrefix);
        string rootPath = GetCurrentContentRoot();
        if (string.IsNullOrEmpty(rootPath))
        {
            Debug.LogWarning("[LocalWebServer] Content root path is empty. Server will start without cached files.");
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
            string requestKey = BuildCacheKey(route, relative);
            string fileUri = CombineUri(baseUri, relative);

            using (UnityWebRequest request = UnityWebRequest.Get(fileUri))
            {
                yield return request.SendWebRequest();

                if (!request.isNetworkError && !request.isHttpError)
                {
                    lock (AndroidCacheLock)
                    {
                        AndroidStreamingCache[requestKey] = request.downloadHandler.data;
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

    private bool TryGetRelativePath(string requestPath, string route, out string relativePath)
    {
        string normalizedRoute = route;
        if (!string.IsNullOrEmpty(normalizedRoute))
        {
            normalizedRoute = normalizedRoute.Trim('/');
        }

        if (string.IsNullOrEmpty(normalizedRoute))
        {
            relativePath = requestPath;
            return true;
        }

        if (string.IsNullOrEmpty(requestPath))
        {
            relativePath = string.Empty;
            return true;
        }

        if (requestPath.Equals(normalizedRoute, StringComparison.Ordinal))
        {
            relativePath = string.Empty;
            return true;
        }

        string prefixWithSlash = normalizedRoute + "/";
        if (requestPath.StartsWith(prefixWithSlash, StringComparison.Ordinal))
        {
            relativePath = requestPath.Substring(prefixWithSlash.Length);
            return true;
        }

        relativePath = string.Empty;
        return false;
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

    private string BuildCacheKey(string route, string relativePath)
    {
        string normalizedRoute = string.IsNullOrEmpty(route) ? string.Empty : route.Trim('/');
        string normalizedRelative = NormalizeRelativePath(relativePath);

        if (string.IsNullOrEmpty(normalizedRoute))
        {
            return normalizedRelative;
        }

        if (string.IsNullOrEmpty(normalizedRelative))
        {
            return normalizedRoute;
        }

        return normalizedRoute + "/" + normalizedRelative;
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

    private string NormalizeRoute(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Trim().Trim('/');
    }


    private bool ShouldUseAndroidStreamingAssets()
    {
        return string.IsNullOrEmpty(contentRootOverride)
            && contentSource == ContentRootSource.StreamingAssets
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

    private string CombineFilesystemPath(string root, string subDirectory)
    {
        if (string.IsNullOrEmpty(subDirectory))
        {
            return root;
        }

        return Path.Combine(root, subDirectory.Trim('/').Replace('/', Path.DirectorySeparatorChar));
    }

    private string NormalizeFullPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return string.Empty;
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
