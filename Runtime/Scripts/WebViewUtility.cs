using System;
using System.IO;
using UnityEngine;

namespace Muabe.WebView
{
    public static class WebViewUtility
    {
        /// <summary>
        /// Normalizes a subfolder path by removing leading/trailing slashes and converting to forward slashes
        /// </summary>
        public static string NormalizeSubfolder(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string normalized = value.Trim().Replace('\\', '/');
            return normalized.Trim('/');
        }

        /// <summary>
        /// Normalizes a route by removing leading/trailing slashes
        /// </summary>
        public static string NormalizeRoute(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return value.Trim().Trim('/');
        }

        /// <summary>
        /// Normalizes a version string by trimming whitespace
        /// </summary>
        public static string NormalizeVersion(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        /// <summary>
        /// Normalizes a relative path by removing leading/trailing slashes and converting to forward slashes
        /// </summary>
        public static string NormalizeRelativePath(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            string trimmed = value.Trim();
            trimmed = trimmed.Replace('\\', '/');
            return trimmed.Trim('/');
        }

        /// <summary>
        /// Normalizes a full file system path
        /// </summary>
        public static string NormalizeFullPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            try
            {
                return Path.GetFullPath(path);
            }
            catch
            {
                return path;
            }
        }

        /// <summary>
        /// Combines a base URI with a relative path, ensuring proper separator
        /// </summary>
        public static string CombineUri(string baseUri, string relative)
        {
            if (string.IsNullOrEmpty(relative))
                return baseUri;

            if (!baseUri.EndsWith("/", StringComparison.Ordinal))
                baseUri += "/";

            return baseUri + relative;
        }

        /// <summary>
        /// Combines filesystem paths and normalizes the result
        /// </summary>
        public static string CombineFilesystemPath(string root, string subDirectory)
        {
            if (string.IsNullOrEmpty(subDirectory))
                return root;

            return Path.Combine(root, subDirectory.Trim('/').Replace('/', Path.DirectorySeparatorChar));
        }

        /// <summary>
        /// Gets MIME content type for a file based on extension
        /// </summary>
        public static string GetContentType(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            return extension switch
            {
                ".html" => "text/html; charset=utf-8",
                ".js" => "application/javascript; charset=utf-8",
                ".css" => "text/css; charset=utf-8",
                ".json" => "application/json; charset=utf-8",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                ".ico" => "image/x-icon",
                ".wasm" => "application/wasm",
                ".woff" => "font/woff",
                ".woff2" => "font/woff2",
                ".ttf" => "font/ttf",
                _ => "application/octet-stream"
            };
        }

        /// <summary>
        /// Gets HTTP status text for a status code
        /// </summary>
        public static string GetHttpStatusText(int statusCode)
        {
            return statusCode switch
            {
                WebViewConstants.HttpStatusOk => "OK",
                WebViewConstants.HttpStatusNotFound => "Not Found",
                WebViewConstants.HttpStatusInternalError => "Internal Server Error",
                WebViewConstants.HttpStatusServiceUnavailable => "Service Unavailable",
                _ => "OK"
            };
        }

        /// <summary>
        /// Checks if a path is within a root directory (prevents path traversal)
        /// </summary>
        public static bool IsPathWithinRoot(string path, string root)
        {
            string fullPath = NormalizeFullPath(path);
            string rootFull = NormalizeFullPath(root);

            return fullPath.StartsWith(rootFull, StringComparison.Ordinal);
        }

        /// <summary>
        /// Sanitizes event name by removing invalid characters
        /// </summary>
        public static string SanitizeEventName(string eventName, string defaultName = WebViewConstants.DefaultFlutterEventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return defaultName;

            return eventName.Trim().Replace("'", "\\'");
        }

        /// <summary>
        /// Gets the full hierarchy path of a transform for debugging
        /// </summary>
        public static string GetTransformPath(Transform transform)
        {
            if (transform == null)
                return "<null>";

            var segments = new System.Collections.Generic.List<string>();
            while (transform != null)
            {
                segments.Add(transform.name);
                transform = transform.parent;
            }
            segments.Reverse();
            return string.Join("/", segments);
        }

        /// <summary>
        /// Safely tries to find a component in the scene, handling Unity version differences
        /// </summary>
        public static T FindObjectInScene<T>(bool includeInactive = true) where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindFirstObjectByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#else
            return UnityEngine.Object.FindObjectOfType<T>();
#endif
        }

        /// <summary>
        /// Safely tries to find all components in the scene, handling Unity version differences
        /// </summary>
        public static T[] FindObjectsInScene<T>(bool includeInactive = true) where T : UnityEngine.Object
        {
#if UNITY_2022_2_OR_NEWER
            return UnityEngine.Object.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude, FindObjectsSortMode.None);
#else
            return UnityEngine.Object.FindObjectsOfType<T>();
#endif
        }

        /// <summary>
        /// Builds a web root path with proper formatting
        /// </summary>
        public static string BuildWebRootPath(string route)
        {
            if (string.IsNullOrEmpty(route))
                return "/";

            return "/" + route.Trim('/') + "/";
        }

        /// <summary>
        /// Logs a message with a prefix
        /// </summary>
        public static void Log(string prefix, string message, UnityEngine.Object context = null)
        {
            Debug.Log($"{prefix} {message}", context);
        }

        /// <summary>
        /// Logs a warning with a prefix
        /// </summary>
        public static void LogWarning(string prefix, string message, UnityEngine.Object context = null)
        {
            Debug.LogWarning($"{prefix} {message}", context);
        }

        /// <summary>
        /// Logs an error with a prefix
        /// </summary>
        public static void LogError(string prefix, string message, UnityEngine.Object context = null)
        {
            Debug.LogError($"{prefix} {message}", context);
        }
    }
}
