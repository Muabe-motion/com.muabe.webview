using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Muabe.WebView
{
    public class PermissionRequester : MonoBehaviour
    {
    [SerializeField]
    private bool requestMicrophone = true;

    [SerializeField]
    private bool requestCamera = true;

        private void Start()
        {
#if UNITY_ANDROID
            WebViewUtility.Log(WebViewConstants.LogPrefixPermission, "Permission Request Started");
            StartCoroutine(RequestPermissionsIfNeeded());
#endif
        }

#if UNITY_ANDROID
        private IEnumerator RequestPermissionsIfNeeded()
        {
            if (requestMicrophone)
            {
                yield return RequestPermissionRoutine(Permission.Microphone);
            }

            if (requestCamera)
            {
                yield return RequestPermissionRoutine(Permission.Camera);
            }
        }

        private IEnumerator RequestPermissionRoutine(string permission)
        {
            if (Permission.HasUserAuthorizedPermission(permission))
            {
                WebViewUtility.Log(WebViewConstants.LogPrefixPermission, $"Already authorized: {permission}");
                yield break;
            }

            bool completed = false;
            bool granted = false;
            bool dontAskAgain = false;

            var callbacks = new PermissionCallbacks();
            callbacks.PermissionGranted += (_) =>
            {
                granted = true;
                completed = true;
            };
            callbacks.PermissionDenied += (_) =>
            {
                completed = true;
            };
            callbacks.PermissionDeniedAndDontAskAgain += (_) =>
            {
                dontAskAgain = true;
                completed = true;
            };

            Permission.RequestUserPermission(permission, callbacks);

            while (!completed)
            {
                yield return null;
            }

            if (!granted)
            {
                string suffix = dontAskAgain ? " (다시 묻지 않음)" : string.Empty;
                WebViewUtility.LogWarning(WebViewConstants.LogPrefixPermission, $"Permission denied{suffix}: {permission}");
            }
            else
            {
                WebViewUtility.Log(WebViewConstants.LogPrefixPermission, $"Permission granted: {permission}");
            }
        }
#endif
    }
}
