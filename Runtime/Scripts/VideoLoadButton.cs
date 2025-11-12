using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Muabe.WebView
{
    [RequireComponent(typeof(Button))]
    public class VideoLoadButton : WebViewButtonBase
    {
        private const string LogPrefix = "[VideoLoadButton]";

        [Header("필수 참조")]
        [SerializeField]
        private FlutterWebBridge bridge;

        [Header("로드 옵션")]
        [Tooltip("비디오 로드 완료 후 버튼을 비활성화합니다.")]
        [SerializeField]
        private bool disableAfterLoad = true;
        
        [Tooltip("비디오 로드를 기다릴 최대 시간(초)")]
        [SerializeField]
        private float loadTimeout = 30f;

        [Header("텍스트 설정")]
        [SerializeField]
        private string loadingLabel = "비디오 로딩 중...";

        [SerializeField]
        private string completedLabel = "로드 완료";

        [SerializeField]
        private string failedLabel = "로드 실패";

        [SerializeField]
        private string notReadyLabel = "브릿지 없음";

        [Header("이벤트")]
        public UnityEvent onLoadStarted;
        public UnityEvent onLoadCompleted;
        public UnityEvent onLoadFailed;

        private Coroutine loadRoutine;
        private bool wasSuccessful = false;

        protected override void Awake()
        {
            base.Awake();
            AutoAssignReferences();
            
            // 초기 상태 강제 설정
            if (button != null)
            {
                button.interactable = true;
            }
            
            WebViewUtility.Log(LogPrefix, $"VideoLoadButton initialized. Button: {(button != null ? "OK" : "NULL")}, Bridge: {(bridge != null ? bridge.name : "NULL")}");
        }
        
        private void Start()
        {
            // Start에서 한 번 더 확인 - 모든 초기화가 완료된 후
            if (button == null)
            {
                button = GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(OnButtonClicked);
                    WebViewUtility.Log(LogPrefix, "Button re-initialized in Start");
                }
                else
                {
                    WebViewUtility.LogError(LogPrefix, "Button still NULL in Start!");
                }
            }
            
            WebViewUtility.Log(LogPrefix, $"Start completed. Button interactable: {(button != null ? button.interactable.ToString() : "NULL")}");
        }

        private void Reset()
        {
            AutoAssignReferences();
            CacheOriginalLabels();
        }

        private void OnValidate()
        {
            AutoAssignReferences();
            if (!Application.isPlaying)
            {
                CacheOriginalLabels();
            }
        }

        private void AutoAssignReferences()
        {
            if (bridge == null)
            {
                bridge = GetComponentInParent<FlutterWebBridge>();
                if (bridge == null)
                {
                    bridge = WebViewUtility.FindObjectInScene<FlutterWebBridge>(true);
                }
                
                if (bridge != null && Application.isPlaying)
                {
                    WebViewUtility.Log(LogPrefix, $"Bridge auto-assigned: {bridge.name}");
                }
            }
        }

        protected override void OnButtonClicked()
        {
            WebViewUtility.Log(LogPrefix, "Button clicked!");

            // 이미 로딩 중이면 무시
            if (loadRoutine != null)
            {
                WebViewUtility.LogWarning(LogPrefix, "Already loading videos...");
                return;
            }

            // 이미 로드되었으면 무시
            if (wasSuccessful)
            {
                WebViewUtility.LogWarning(LogPrefix, "Videos already loaded.");
                return;
            }

            // Bridge 확인
            if (bridge == null)
            {
                WebViewUtility.LogError(LogPrefix, "FlutterWebBridge is not assigned!");
                UpdateStatusLabel(notReadyLabel);
                onLoadFailed?.Invoke();
                return;
            }

            // 로드 시작
            loadRoutine = StartCoroutine(LoadVideosRoutine());
        }

        private IEnumerator LoadVideosRoutine()
        {
            WebViewUtility.Log(LogPrefix, "Starting video load...");
            
            // UI 업데이트
            SetButtonInteractable(false);
            UpdateStatusLabel(loadingLabel);
            onLoadStarted?.Invoke();

            // 이벤트 구독
            bool eventReceived = false;
            System.Action<int, int> handler = (loaded, total) =>
            {
                WebViewUtility.Log(LogPrefix, $"Event received: {loaded}/{total} videos loaded");
                eventReceived = true;
            };
            
            bridge.OnVideosLoaded += handler;

            // Flutter로 명령 전송
            bridge.SendLoadVideosCommand();
            WebViewUtility.Log(LogPrefix, "Command sent to Flutter");

            // 이벤트 대기 (타임아웃 포함)
            float elapsed = 0f;
            while (!eventReceived && elapsed < loadTimeout)
            {
                yield return new WaitForSecondsRealtime(1f);
                elapsed += 1f;
            }

            // 이벤트 구독 해제
            bridge.OnVideosLoaded -= handler;

            // 결과 처리
            if (eventReceived)
            {
                WebViewUtility.Log(LogPrefix, "Video load completed successfully!");
                wasSuccessful = true;
                UpdateStatusLabel(completedLabel);
                SetButtonInteractable(!disableAfterLoad);
                onLoadCompleted?.Invoke();
            }
            else
            {
                WebViewUtility.LogError(LogPrefix, $"Video load timeout ({loadTimeout}s)");
                UpdateStatusLabel(failedLabel);
                SetButtonInteractable(true);
                onLoadFailed?.Invoke();
            }

            loadRoutine = null;
        }

        public void ResetState()
        {
            if (loadRoutine != null)
            {
                StopCoroutine(loadRoutine);
                loadRoutine = null;
            }
            
            wasSuccessful = false;
            SetButtonInteractable(true);
            ResetStatusLabel();
        }
    }
}
