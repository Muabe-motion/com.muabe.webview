namespace Muabe.WebView
{
    public static class WebViewConstants
    {
        // Logging
        public const string LogPrefixServer = "[LocalWebServer]";
        public const string LogPrefixDownloadManager = "[WebContentDownloadManager]";
        public const string LogPrefixViewController = "[WebViewController]";
        public const string LogPrefixDownloadButton = "[WebContentDownloadButton]";
        public const string LogPrefixLaunchButton = "[WebContentLaunchButton]";
        public const string LogPrefixFlutterBridge = "[FlutterWebBridge]";
        public const string LogPrefixFlutterButton = "[FlutterWidgetButton]";
        public const string LogPrefixPermission = "[PermissionRequester]";

        // Server defaults
        public const int DefaultServerPort = 8088;
        public const string DefaultDocument = "index.html";
        public const int DefaultBufferSize = 4096;
        public const int DefaultThreadJoinTimeout = 1000;
        public const int DefaultThreadSleepMs = 10;

        // WebView initialization
        public const float WebViewInitDelay = 0.5f;
        public const float MarginStabilizeDelay = 0.08f;

        // Server timeouts
        public const float DefaultServerReadyTimeout = 5f;

        // File names
        public const string DefaultVersionFileName = ".webcontent-version";
        public const char DefaultCommentChar = '#';

        // Flutter bridge
        public const string DefaultFlutterEventName = "UnityToFlutter";
        public const string CommandTypeVisibility = "setWidgetVisibility";

        // HTTP status codes
        public const int HttpStatusOk = 200;
        public const int HttpStatusNotFound = 404;
        public const int HttpStatusInternalError = 500;
        public const int HttpStatusServiceUnavailable = 503;

        // Default paths
        public const string DefaultContentRoot = "root";
        public const string DefaultRoutePrefix = "flutter";
        public const string DefaultInstallFolder = "webview-content";
    }
}
