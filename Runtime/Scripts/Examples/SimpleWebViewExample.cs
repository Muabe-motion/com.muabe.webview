using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Muabe.WebView.Examples
{
    /// <summary>
    /// SimpleWebViewManager 사용 예제
    /// 버튼 클릭으로 간단하게 WebView를 제어하는 방법을 보여줍니다.
    /// </summary>
    public class SimpleWebViewExample : MonoBehaviour
    {
        [Header("SimpleWebViewManager 참조")]
        [SerializeField] private SimpleWebViewManager webViewManager;

        [Header("UI 버튼 (선택사항)")]
        [SerializeField] private Button downloadButton;
        [SerializeField] private Button launchButton;
        [SerializeField] private Button showButton;
        [SerializeField] private Button hideButton;

        [Header("페이지 설정")]
        [SerializeField] private string targetPage = "page30";

        private void Start()
        {
            // WebViewManager가 설정되지 않았다면 자동으로 찾기
            if (webViewManager == null)
            {
                webViewManager = FindObjectOfType<SimpleWebViewManager>();
            }

            // 버튼 이벤트 연결
            if (downloadButton != null)
            {
                downloadButton.onClick.AddListener(OnDownloadButtonClicked);
            }

            if (launchButton != null)
            {
                launchButton.onClick.AddListener(OnLaunchButtonClicked);
            }

            if (showButton != null)
            {
                showButton.onClick.AddListener(OnShowButtonClicked);
            }

            if (hideButton != null)
            {
                hideButton.onClick.AddListener(OnHideButtonClicked);
            }

            // 이벤트 구독
            SubscribeToEvents();
        }

        private void SubscribeToEvents()
        {
            if (webViewManager == null) return;

            webViewManager.onDownloadStarted.AddListener(() =>
            {
                Debug.Log("[Example] Download started");
                if (downloadButton != null) downloadButton.interactable = false;
            });

            webViewManager.onDownloadCompleted.AddListener(() =>
            {
                Debug.Log("[Example] Download completed");
                if (downloadButton != null) downloadButton.interactable = true;
                if (launchButton != null) launchButton.interactable = true;
            });

            webViewManager.onDownloadFailed.AddListener(() =>
            {
                Debug.Log("[Example] Download failed");
                if (downloadButton != null) downloadButton.interactable = true;
            });

            webViewManager.onDownloadProgress.AddListener((progress) =>
            {
                Debug.Log($"[Example] Download progress: {progress * 100:F1}%");
            });

            webViewManager.onServerStarted.AddListener(() =>
            {
                Debug.Log("[Example] Server started");
            });

            webViewManager.onWebViewLoaded.AddListener(() =>
            {
                Debug.Log("[Example] WebView loaded");
                if (showButton != null) showButton.interactable = true;
            });

            webViewManager.onVideosLoaded.AddListener((total, loaded) =>
            {
                Debug.Log($"[Example] Videos loaded: {loaded}/{total}");
            });
        }

        /// <summary>
        /// 다운로드 버튼 클릭 (선택사항)
        /// </summary>
        private void OnDownloadButtonClicked()
        {
            Debug.Log("[Example] Download button clicked");
            webViewManager.DownloadContent();
        }

        /// <summary>
        /// 서버 시작 및 WebView 로드 버튼 클릭
        /// </summary>
        private void OnLaunchButtonClicked()
        {
            Debug.Log("[Example] Launch button clicked");
            webViewManager.StartServerAndLoadWebView();
        }

        /// <summary>
        /// WebView 표시 버튼 클릭
        /// </summary>
        private void OnShowButtonClicked()
        {
            Debug.Log("[Example] Show button clicked");
            // 간단하게 표시만
            // webViewManager.ShowWebView();

            // 또는 특정 페이지로 이동하면서 표시
            webViewManager.ShowWebViewAndNavigate(targetPage);
        }

        /// <summary>
        /// WebView 숨김 버튼 클릭
        /// </summary>
        private void OnHideButtonClicked()
        {
            Debug.Log("[Example] Hide button clicked");
            webViewManager.HideWebView();
        }

        // ========== 코드로만 사용하는 예제 (버튼 없이) ==========

        /// <summary>
        /// 예제 1: 전체 워크플로우를 코드로 실행
        /// </summary>
        [ContextMenu("Run Full Workflow")]
        public void RunFullWorkflow()
        {
            StartCoroutine(FullWorkflowCoroutine());
        }

        private IEnumerator FullWorkflowCoroutine()
        {
            Debug.Log("[Example] Starting full workflow...");

            // 1. 다운로드 (선택사항)
            if (!string.IsNullOrEmpty(webViewManager.LocalWebServer.ToString()))
            {
                webViewManager.DownloadContent();

                // 다운로드 완료 대기
                yield return new WaitUntil(() => webViewManager.onDownloadCompleted != null);
            }

            // 2. 서버 시작 및 WebView 로드
            webViewManager.StartServerAndLoadWebView();

            // WebView 로드 완료 대기
            yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

            // 3. 비디오 로드 (선택사항)
            webViewManager.LoadVideos();

            // 비디오 로드 대기
            yield return new WaitUntil(() => webViewManager.AreVideosLoaded);

            // 4. WebView 표시 및 페이지 이동
            webViewManager.ShowWebViewAndNavigate(targetPage);

            Debug.Log("[Example] Full workflow completed!");
        }

        /// <summary>
        /// 예제 2: 간단한 시작 (다운로드 없이)
        /// </summary>
        [ContextMenu("Quick Start (No Download)")]
        public void QuickStart()
        {
            StartCoroutine(QuickStartCoroutine());
        }

        private IEnumerator QuickStartCoroutine()
        {
            Debug.Log("[Example] Quick start...");

            // 서버 시작 및 WebView 로드
            webViewManager.StartServerAndLoadWebView();

            // WebView 로드 완료 대기
            yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

            // WebView 표시 및 페이지 이동
            webViewManager.ShowWebViewAndNavigate(targetPage);

            Debug.Log("[Example] Quick start completed!");
        }

        /// <summary>
        /// 예제 3: 페이지 전환만 (WebView는 이미 표시된 상태)
        /// </summary>
        [ContextMenu("Navigate to Another Page")]
        public void NavigateToAnotherPage()
        {
            string anotherPage = "page15"; // 다른 페이지 예시
            webViewManager.NavigateToPage(anotherPage);
        }
    }
}
