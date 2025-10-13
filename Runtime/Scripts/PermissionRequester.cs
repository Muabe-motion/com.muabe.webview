using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class PermissionRequester : MonoBehaviour
{
    [SerializeField]
    private bool requestMicrophone = true;

    [SerializeField]
    private bool requestCamera = true;

    private void Start()
    {
#if UNITY_ANDROID
        Debug.Log($"Permission Request Start!!!!!!!!!!!!!!!!!!!!!!!!!");
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
            Debug.Log($"[{nameof(PermissionRequester)}] Already authorized: {permission}.");
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
            Debug.LogWarning($"[{nameof(PermissionRequester)}] Permission denied{suffix}: {permission}.");
        }
        else
        {
            Debug.Log($"[{nameof(PermissionRequester)}] Permission granted: {permission}.");
        }
    }
#endif
}
