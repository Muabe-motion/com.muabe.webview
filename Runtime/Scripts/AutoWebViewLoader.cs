using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Muabe.WebView
{
    /// <summary>
    /// 씬 시작 시 자동으로 WebView를 로드하고 페이지를 표시하는 컴포넌트.
    /// WebViewManager GameObject를 자동으로 찾아 연결합니다.
    /// </summary>
    public class AutoWebViewLoader : MonoBehaviour
    {
        private const string LogPrefix = "[AutoWebViewLoader]";

        [Header("참조 (자동으로 찾음)")]
        [SerializeField] private LocalWebServer server;
        [SerializeField] private WebViewController webViewController;
        [SerializeField] private FlutterWebBridge bridge;

        [Header("설정")]
        [Tooltip("WebViewManager GameObject 이름 (자동 검색용)")]
        [SerializeField] private string webViewManagerName = "WebViewManager";
        [Tooltip("이동할 페이지 경로")]
        [SerializeField] private string pagePath = "/page30";
        [Tooltip("비디오 로드 완료까지 대기")]
        [SerializeField] private bool waitForVideos = true;
        [Tooltip("비디오 로드 타임아웃 (초)")]
        [SerializeField] private float videoLoadTimeout = 30f;
        [Tooltip("씬 시작 시 자동 실행")]
        [SerializeField] private bool autoStartOnAwake = true;

        [Header("이벤트")]
        [Tooltip("WebView가 숨겨졌을 때 호출됩니다 (Flutter에서 hideWebView 호출 시)")]
        public UnityEvent onWebViewHidden;
        [Tooltip("WebView가 표시되었을 때 호출됩니다")]
        public UnityEvent onWebViewShown;
        [Tooltip("전체 워크플로우가 완료되었을 때 호출됩니다")]
        public UnityEvent onWorkflowCompleted;

        private bool isRunning = false;

        void Start()
        {
            FindComponents();
            SubscribeEvents();

            if (autoStartOnAwake)
            {
                StartWorkflow();
            }
        }

        void OnDestroy()
        {
            UnsubscribeEvents();
        }

        private void FindComponents()
        {
            if (server != null && webViewController != null && bridge != null)
                return;

            GameObject manager = GameObject.Find(webViewManagerName);
            if (manager != null)
            {
                if (server == null)
                    server = manager.GetComponent<LocalWebServer>();
                if (webViewController == null)
                    webViewController = manager.GetComponent<WebViewController>();
                if (bridge == null)
                    bridge = manager.GetComponent<FlutterWebBridge>();
            }

            if (server == null || webViewController == null || bridge == null)
            {
                Debug.LogWarning($"{LogPrefix} 일부 컴포넌트를 찾을 수 없습니다. '{webViewManagerName}' GameObject가 씬에 있는지 확인하세요.");
            }
        }

        private void SubscribeEvents()
        {
            if (bridge != null)
            {
                bridge.OnWebViewHidden += HandleWebViewHidden;
                bridge.OnWebViewShown += HandleWebViewShown;
            }
        }

        private void UnsubscribeEvents()
        {
            if (bridge != null)
            {
                bridge.OnWebViewHidden -= HandleWebViewHidden;
                bridge.OnWebViewShown -= HandleWebViewShown;
            }
        }

        private void HandleWebViewHidden()
        {
            Debug.Log($"{LogPrefix} WebView가 숨겨졌습니다.");
            onWebViewHidden?.Invoke();
        }

        private void HandleWebViewShown()
        {
            Debug.Log($"{LogPrefix} WebView가 표시되었습니다.");
            onWebViewShown?.Invoke();
        }

        /// <summary>
        /// 워크플로우를 수동으로 시작합니다.
        /// </summary>
        public void StartWorkflow()
        {
            if (isRunning)
            {
                Debug.LogWarning($"{LogPrefix} 워크플로우가 이미 실행 중입니다.");
                return;
            }

            StartCoroutine(FullWorkflow());
        }

        /// <summary>
        /// 페이지 경로를 설정하고 워크플로우를 시작합니다.
        /// </summary>
        public void StartWorkflow(string newPagePath)
        {
            pagePath = newPagePath;
            StartWorkflow();
        }

        IEnumerator FullWorkflow()
        {
            isRunning = true;
            Debug.Log($"{LogPrefix} ========== WebView 자동 로드 시작 ==========");

            // 1단계: 서버 시작
            yield return StartCoroutine(Step1_StartServer());

            // 2단계: WebView 로드
            yield return StartCoroutine(Step2_LoadWebView());

            // 3단계: 비디오 로드 대기
            if (waitForVideos)
            {
                yield return StartCoroutine(Step3_WaitForVideos());
            }

            // 4단계: 페이지 표시
            yield return StartCoroutine(Step4_ShowPage());

            Debug.Log($"{LogPrefix} ========== ✅ 모든 작업 완료! ==========");
            isRunning = false;
            onWorkflowCompleted?.Invoke();
        }

        IEnumerator Step1_StartServer()
        {
            Debug.Log($"{LogPrefix} [1/4] 서버 시작");

            if (server == null)
            {
                Debug.LogError($"{LogPrefix} LocalWebServer를 찾을 수 없습니다!");
                yield break;
            }

            if (server.IsRunning)
            {
                Debug.Log($"{LogPrefix} 서버가 이미 실행 중입니다.");
                yield break;
            }

            Debug.Log($"{LogPrefix} 서버 시작 중...");
            server.StartServer();

            float timeWaited = 0f;
            float timeout = 5f;

            while (!server.IsServerReady && timeWaited < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeWaited += 0.1f;
            }

            if (server.IsServerReady)
            {
                Debug.Log($"{LogPrefix} 서버 준비 완료!");
            }
            else
            {
                Debug.LogError($"{LogPrefix} 서버 시작 타임아웃!");
            }
        }

        IEnumerator Step2_LoadWebView()
        {
            Debug.Log($"{LogPrefix} [2/4] WebView 로드");

            if (webViewController == null)
            {
                Debug.LogError($"{LogPrefix} WebViewController를 찾을 수 없습니다!");
                yield break;
            }

            if (webViewController.IsWebViewReady)
            {
                Debug.Log($"{LogPrefix} WebView가 이미 준비되어 있습니다.");
                yield break;
            }

            Debug.Log($"{LogPrefix} WebView 초기화 중...");
            webViewController.LoadInitialUrl();

            float timeWaited = 0f;
            float timeout = 10f;

            while (!webViewController.IsWebViewReady && timeWaited < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeWaited += 0.1f;
            }

            if (webViewController.IsWebViewReady)
            {
                Debug.Log($"{LogPrefix} WebView 준비 완료!");
            }
            else
            {
                Debug.LogError($"{LogPrefix} WebView 초기화 타임아웃!");
            }
        }

        IEnumerator Step3_WaitForVideos()
        {
            Debug.Log($"{LogPrefix} [3/4] 비디오 로드 대기");

            if (bridge == null)
            {
                Debug.LogError($"{LogPrefix} FlutterWebBridge를 찾을 수 없습니다!");
                yield break;
            }

            if (bridge.AreVideosLoaded)
            {
                Debug.Log($"{LogPrefix} 비디오가 이미 로드되어 있습니다.");
                yield break;
            }

            Debug.Log($"{LogPrefix} 비디오 로드 대기 중...");
            float timeWaited = 0f;

            while (!bridge.AreVideosLoaded && timeWaited < videoLoadTimeout)
            {
                yield return new WaitForSeconds(0.1f);
                timeWaited += 0.1f;
            }

            if (bridge.AreVideosLoaded)
            {
                Debug.Log($"{LogPrefix} 비디오 로드 완료!");
            }
            else
            {
                Debug.LogWarning($"{LogPrefix} 비디오 로드 타임아웃 ({videoLoadTimeout}초), 계속 진행...");
            }
        }

        IEnumerator Step4_ShowPage()
        {
            Debug.Log($"{LogPrefix} [4/4] 페이지 표시");

            if (webViewController == null || bridge == null)
            {
                Debug.LogError($"{LogPrefix} WebViewController 또는 Bridge를 찾을 수 없습니다!");
                yield break;
            }

            if (!webViewController.IsWebViewReady)
            {
                Debug.LogError($"{LogPrefix} WebView가 준비되지 않았습니다!");
                yield break;
            }

            Debug.Log($"{LogPrefix} WebView 화면에 표시...");
            bridge.ShowWebView();

            Debug.Log($"{LogPrefix} Flutter 준비 대기 (0.3초)...");
            yield return new WaitForSecondsRealtime(0.3f);

            Debug.Log($"{LogPrefix} 페이지 전환: {pagePath}");
            bridge.NavigateToPage(pagePath);

            Debug.Log($"{LogPrefix} 페이지 표시 완료!");
        }
    }
}
