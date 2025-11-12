using UnityEngine;
using UnityEngine.UI;

namespace Muabe.WebView
{
    [RequireComponent(typeof(Button))]
    public class WebViewTestButton : MonoBehaviour
    {
        [SerializeField]
        private WebViewController webViewController;

        [SerializeField]
        private string testMessage = "Test from Unity!";

        private Button button;

        private void Awake()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(OnButtonClick);

            if (webViewController == null)
            {
                webViewController = FindObjectOfType<WebViewController>();
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }

        private void OnButtonClick()
        {
            if (webViewController == null)
            {
                Debug.LogError("[WebViewTestButton] WebViewController not found!");
                return;
            }

            Debug.Log($"[WebViewTestButton] Sending test message: {testMessage}");

            // 간단한 alert 테스트
            string js1 = $"alert('Simple Alert Test: {testMessage}');";
            Debug.Log($"[WebViewTestButton] Executing JS: {js1}");
            webViewController.RunJavaScript(js1, queueUntilReady: false);

            // console.log 테스트
            string js2 = $"console.log('[Unity Test] {testMessage}');";
            webViewController.RunJavaScript(js2, queueUntilReady: false);

            // window.__unityBridge 존재 확인
            string js3 = @"
                if (window.__unityBridge) {
                    alert('Bridge EXISTS:\n' + JSON.stringify(Object.keys(window.__unityBridge)));
                } else {
                    alert('Bridge DOES NOT EXIST!');
                }
            ";
            webViewController.RunJavaScript(js3, queueUntilReady: false);

            // CustomEvent 발생 테스트
            string js4 = @"
                console.log('[Unity Test] Dispatching test event...');
                var testPayload = {type: 'manualTest', message: 'Manual test from button'};
                window.dispatchEvent(new CustomEvent('UnityToFlutter', {detail: testPayload}));
                alert('CustomEvent dispatched!');
            ";
            webViewController.RunJavaScript(js4, queueUntilReady: false);

            Debug.Log("[WebViewTestButton] All test scripts sent");
        }
    }
}
