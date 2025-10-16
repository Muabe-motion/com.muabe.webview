using UnityEngine;
using UnityEngine.UI;

namespace Muabe.WebView
{
    [RequireComponent(typeof(Button))]
        public class FlutterWidgetButton : MonoBehaviour
    {
        private const string LogPrefix = WebViewConstants.LogPrefixFlutterButton;

        [SerializeField]
        private FlutterWebBridge bridge;

        [SerializeField]
        [Tooltip("상위에 없으면 씬 전체에서 FlutterWebBridge를 자동으로 찾습니다.")]
        private bool autoLocateBridgeInScene = true;

        [SerializeField]
        [Tooltip("디버그 로그 출력 활성화")] 
        private bool verboseLogging = false;

        [SerializeField]
        [Tooltip("제어할 플러터 위젯 ID")]
        private string widgetId;

    [SerializeField]
    [Tooltip("버튼 클릭 시 실행할 동작")]
        private ClickMode clickMode = ClickMode.Toggle;

    [SerializeField]
    [Tooltip("ClickMode가 ForceValue일 때 적용할 visible 값")]
        private bool visibleValue = true;

    [Header("WebView Overlay")]
    [SerializeField]
    [Tooltip("버튼 영역만큼 WebView 상단 여백을 자동으로 확보합니다.")]
        private bool reserveWebViewArea = false;

    [SerializeField]
    [Tooltip("자동 계산 시 추가로 확보할 여백 (픽셀)")]
        private float reservePaddingPixels = 12f;

    [SerializeField]
    [Tooltip("자동 계산 기준이 될 RectTransform (미지정 시 현재 버튼)")]
        private RectTransform reserveTarget;

    [SerializeField]
    [Tooltip("자동 계산 대신 명시적인 상단 여백을 지정합니다. 음수면 자동 계산 결과를 사용합니다.")]
        private int explicitTopMargin = -1;

        private Button cachedButton;
        private RectTransform cachedRectTransform;
        private Vector2Int cachedScreenSize;
        private bool overlayMarginDirty;
        private bool loggedMissingController;
        private bool loggedMissingBridge;
        private static readonly Vector3[] CornerBuffer = new Vector3[4];

        private void Awake()
        {
            cachedButton = GetComponent<Button>();
            cachedRectTransform = GetComponent<RectTransform>();

            TryResolveBridge();

            cachedButton.onClick.AddListener(HandleButtonClick);

            if (verboseLogging)
            {
                WebViewUtility.Log(LogPrefix, $"Awake (bridge={(bridge != null ? bridge.name : "null")}, widgetId={widgetId})", this);
            }
        }

        private void OnValidate()
    {
        if (!string.IsNullOrEmpty(widgetId))
        {
            widgetId = widgetId.Trim();
        }

        reservePaddingPixels = Mathf.Max(0f, reservePaddingPixels);
        if (explicitTopMargin < -1)
        {
            explicitTopMargin = -1;
        }

        overlayMarginDirty = true;
    }

        private void OnDestroy()
    {
        if (cachedButton != null)
        {
            cachedButton.onClick.RemoveListener(HandleButtonClick);
        }
    }

        private void OnEnable()
    {
        cachedScreenSize = new Vector2Int(Screen.width, Screen.height);
        overlayMarginDirty = true;
        TryResolveBridge();
    }

        private void Start()
    {
        if (reserveWebViewArea)
        {
            ScheduleOverlayMarginUpdate();
        }
    }

        private void Update()
    {
        if (!reserveWebViewArea)
        {
            return;
        }

        if (cachedRectTransform != null && cachedRectTransform.hasChanged)
        {
            cachedRectTransform.hasChanged = false;
            overlayMarginDirty = true;
        }

        if (loggedMissingController && bridge != null && bridge.WebViewController != null)
        {
            loggedMissingController = false;
            overlayMarginDirty = true;
        }

        if (cachedScreenSize.x != Screen.width || cachedScreenSize.y != Screen.height)
        {
            cachedScreenSize = new Vector2Int(Screen.width, Screen.height);
            overlayMarginDirty = true;
        }

        if (bridge == null && autoLocateBridgeInScene)
        {
            TryResolveBridge();
        }

        if (overlayMarginDirty)
        {
            overlayMarginDirty = false;
            UpdateOverlayMargin();
        }
    }

        private void HandleButtonClick()
    {
        TryResolveBridge();

        if (bridge == null)
        {
            if (!loggedMissingBridge)
            {
                Debug.LogWarning($"{LogPrefix} Bridge reference is missing.");
                loggedMissingBridge = true;
            }
            return;
        }

        if (string.IsNullOrWhiteSpace(widgetId))
        {
            Debug.LogWarning($"{LogPrefix} widgetId is empty.");
            return;
        }

        var normalizedId = widgetId.Trim();
        Debug.Log($"{LogPrefix} Clicked (widgetId={normalizedId}, mode={clickMode})");

        switch (clickMode)
        {
            case ClickMode.Toggle:
                bridge.ToggleWidgetVisibility(normalizedId);
                break;
            case ClickMode.Show:
                bridge.ShowWidget(normalizedId);
                break;
            case ClickMode.Hide:
                bridge.HideWidget(normalizedId);
                break;
            case ClickMode.ForceValue:
                bridge.SetWidgetVisibility(normalizedId, visibleValue);
                break;
        }
    }

        private void ScheduleOverlayMarginUpdate()
    {
        overlayMarginDirty = true;
    }

        private void UpdateOverlayMargin()
    {
        var controller = bridge != null ? bridge.WebViewController : null;
        if (controller == null)
        {
            if (!loggedMissingController)
            {
                Debug.LogWarning($"{LogPrefix} Unable to reserve WebView margin because WebViewController is missing.");
                loggedMissingController = true;
            }
            return;
        }
        loggedMissingController = false;

        if (explicitTopMargin >= 0)
        {
            controller.SetOverlayPaddingTop(explicitTopMargin);
            return;
        }

        var target = reserveTarget != null ? reserveTarget : cachedRectTransform;
        if (target == null)
        {
            Debug.LogWarning($"{LogPrefix} Cannot determine RectTransform for overlay margin calculation.");
            return;
        }

        var canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning($"{LogPrefix} Canvas not found for overlay margin calculation.");
            return;
        }

        target.GetWorldCorners(CornerBuffer);
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        float highestY = float.MinValue;
        for (int i = 0; i < CornerBuffer.Length; i++)
        {
            var screenPoint = RectTransformUtility.WorldToScreenPoint(eventCamera, CornerBuffer[i]);
            if (screenPoint.y > highestY)
            {
                highestY = screenPoint.y;
            }
        }

        if (highestY <= 0f)
        {
            return;
        }

        int topMargin = Mathf.Max(0, Mathf.RoundToInt(Screen.height - highestY + reservePaddingPixels));
        controller.SetOverlayPaddingTop(topMargin);
    }

        private void TryResolveBridge()
    {
        if (bridge != null)
        {
            return;
        }

        bridge = GetComponentInParent<FlutterWebBridge>();
        if (bridge == null && autoLocateBridgeInScene)
        {
            bridge = FindBridgeInScene();
        }

        if (bridge != null)
        {
            loggedMissingBridge = false;
            overlayMarginDirty = true;
            if (verboseLogging)
            {
                Debug.Log($"{LogPrefix} Resolved bridge -> {bridge.name}", this);
            }
        }
        else if (verboseLogging)
        {
            Debug.LogWarning($"{LogPrefix} TryResolveBridge failed (autoLocate={autoLocateBridgeInScene})", this);
            LogExistingBridges();
        }
    }

        private static FlutterWebBridge FindBridgeInScene()
        {
            return WebViewUtility.FindObjectInScene<FlutterWebBridge>(true);
        }

        private void LogExistingBridges()
        {
            var candidates = WebViewUtility.FindObjectsInScene<FlutterWebBridge>(true);
            if (candidates == null || candidates.Length == 0)
            {
                WebViewUtility.LogWarning(LogPrefix, "No FlutterWebBridge instances found in scene.", this);
                return;
            }

            foreach (var candidate in candidates)
            {
                var controller = candidate.WebViewController;
                var controllerName = controller != null ? controller.name : "null";
                var path = WebViewUtility.GetTransformPath(candidate.transform);
                WebViewUtility.Log(LogPrefix, $"Found FlutterWebBridge candidate at '{path}' (controller={controllerName})", candidate);
            }
        }

        private enum ClickMode
        {
            Toggle,
            Show,
            Hide,
            ForceValue
        }
    }
}
