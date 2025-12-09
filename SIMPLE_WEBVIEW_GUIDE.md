# SimpleWebViewManager 간편 사용 가이드

## 개요

`SimpleWebViewManager`는 기존의 복잡한 설정 과정을 간소화하여 **코드 몇 줄로 WebView를 쉽게 사용**할 수 있도록 만든 래퍼 클래스입니다.

### 기존 방식 vs 간편 방식

| 기존 방식 | 간편 방식 |
|---------|----------|
| 4개의 컴포넌트를 수동으로 추가 | 1개의 컴포넌트만 추가 |
| Inspector에서 복잡한 설정 | 최소한의 Inspector 설정 |
| 각 컴포넌트 간 참조 연결 필요 | 자동으로 컴포넌트 추가 및 연결 |
| 여러 개의 버튼 컴포넌트 필요 | 코드로 간단하게 제어 |

---

## 빠른 시작 (4단계)

### 1단계: GameObject 생성 및 컴포넌트 추가

```
Hierarchy > Create Empty GameObject
이름: "WebViewManager"

Add Component > SimpleWebViewManager
```

### 2단계: Inspector 설정

SimpleWebViewManager 컴포넌트의 필수 설정만 입력:

```
┌─ Simple Web View Manager (Script) ──────┐
│ ▼ 필수 설정                               │
│   Content Path: arpedia/dino/wj_demo     │
│   Server Port: 8088                      │
│                                          │
│ ▼ 다운로드 설정 (선택사항)                │
│   Download Url: (비워둠 또는 ZIP URL)      │
│   Content Version: 1.0.0                 │
│   Download Folder Path: arpedia/dino     │
│                                          │
│ ▼ WebView 옵션                           │
│   Enable WKWebView: ✅                   │
│   Transparent: ✅                        │
│   Ignore Safe Area: ❌                   │
│                                          │
│ ▼ 브릿지 설정                             │
│   Unity To Flutter Event: __unityBridge  │
└──────────────────────────────────────────┘
```

**필수 설정:**
- **Content Path**: `index.html`이 있는 폴더 경로 (persistentDataPath 기준)
- **Server Port**: 로컬 서버 포트 번호 (기본: 8088)

**선택 설정:**
- **Download Url**: 웹 콘텐츠 ZIP 파일 URL (앱 내 다운로드 사용 시만)
- **Content Version**: 다운로드할 콘텐츠 버전
- **Download Folder Path**: 다운로드 받을 폴더 경로

### 3단계: 스크립트에서 사용

#### 방법 A: 버튼으로 제어

```csharp
using UnityEngine;
using Muabe.WebView;

public class MyWebViewController : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;

    // 1. 서버 시작 및 WebView 로드 버튼
    public void OnLaunchButtonClicked()
    {
        webViewManager.StartServerAndLoadWebView();
    }

    // 2. WebView 표시 버튼 (단순 표시만)
    public void OnShowButtonClicked()
    {
        webViewManager.ShowWebView();
    }

    // 3. WebView 표시 + 페이지 이동 버튼 (권장)
    public void OnShowAndNavigateButtonClicked()
    {
        webViewManager.ShowWebViewAndNavigate("page30");
    }

    // 4. 페이지만 전환 버튼 (WebView는 이미 표시된 상태)
    public void OnNavigateButtonClicked()
    {
        webViewManager.NavigateToPage("page15");
    }

    // 5. WebView 숨김 버튼
    public void OnHideButtonClicked()
    {
        webViewManager.HideWebView();
    }
}
```

**사용 방법:**
1. UI 버튼 생성 (예: LaunchButton, ShowButton, HideButton)
2. 각 버튼의 OnClick 이벤트에 위 메서드 연결
3. Play 모드에서 버튼을 순서대로 클릭:
   - **LaunchButton** → 서버 시작 및 WebView 로드 (숨김 상태)
   - **ShowAndNavigateButton** → WebView 표시 + 특정 페이지로 이동
   - **NavigateButton** → 다른 페이지로 전환 (선택사항)
   - **HideButton** → WebView 숨김 (선택사항)

#### 방법 B: 코드로 자동 실행

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class AutoWebViewStarter : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;
    [SerializeField] private string targetPage = "page30";

    private void Start()
    {
        StartCoroutine(AutoStartWebView());
    }

    private IEnumerator AutoStartWebView()
    {
        // 1. 서버 시작 및 WebView 로드 (숨김 상태)
        webViewManager.StartServerAndLoadWebView();

        // 2. WebView 로드 완료 대기
        yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

        // 3. WebView 표시 및 페이지 이동
        webViewManager.ShowWebViewAndNavigate(targetPage);

        Debug.Log("WebView 자동 시작 완료!");
    }
}
```

**사용 방법:**
1. 새로운 C# 스크립트 생성
2. 위 코드 복사
3. WebViewManager GameObject에 스크립트 추가
4. Inspector에서 WebViewManager 참조 설정
5. Play 모드 실행 시 자동으로 WebView 표시

### 4단계: 실행 순서 이해하기

**전체 워크플로우:**

```
1단계: StartServerAndLoadWebView()
   → 로컬 서버 시작
   → WebView 초기화 및 로드
   → WebView는 숨김 상태로 준비 완료

2단계: WebView 표시 및 페이지 전환

   방법 A) ShowWebView()
   → WebView를 화면에 표시 (홈 페이지)

   방법 B) ShowWebViewAndNavigate("page30") ⭐ 권장
   → WebView를 화면에 표시
   → 0.3초 대기 (Flutter 앱 준비)
   → 특정 페이지로 이동

3단계: 페이지 전환 (선택사항)
   NavigateToPage("page15")
   → 다른 페이지로 전환 (WebView가 이미 표시된 상태)

4단계: WebView 숨김 (선택사항)
   HideWebView()
   → WebView 숨김 (백그라운드로 유지)
```

**핵심 포인트:**
- **1단계는 필수**: 항상 먼저 `StartServerAndLoadWebView()` 호출
- **2단계에서 선택**: 처음 표시할 때는 `ShowWebViewAndNavigate()` 권장
- **3단계는 반복 가능**: 이미 표시된 상태에서 `NavigateToPage()`로 페이지만 전환
- **4단계는 선택**: 필요할 때만 `HideWebView()`로 숨김

---

## 주요 기능 상세 설명

### 1. 콘텐츠 다운로드 (선택사항)

```csharp
// 일반 다운로드 (버전 확인)
webViewManager.DownloadContent();

// 강제 다운로드 (버전 무시)
webViewManager.DownloadContentForced();
```

**사용 시나리오:**
- Unity 앱 내에서 웹 콘텐츠를 ZIP 파일로 다운로드해야 하는 경우
- 버전 관리가 필요한 경우

**건너뛰기:**
- 콘텐츠 파일을 수동으로 기기에 복사하는 경우
- 콘텐츠를 Unity 빌드에 포함시키는 경우

### 2. 서버 시작 및 WebView 로드

```csharp
// 서버 시작 + WebView 로드 (한 번에)
webViewManager.StartServerAndLoadWebView();

// 로드 완료 확인
if (webViewManager.IsWebViewLoaded)
{
    Debug.Log("WebView 로드 완료!");
}
```

**내부 동작:**
1. LocalWebServer 시작 (Content Path 기준)
2. 서버 준비 대기 (최대 5초)
3. WebView 초기화 및 URL 로드
4. WebView 준비 완료 **(숨김 상태)**

> **⚠️ 중요**: 이 단계에서는 WebView가 **숨김 상태**로 로드됩니다. 화면에 표시하려면 `ShowWebView()` 또는 `ShowWebViewAndNavigate()`를 호출해야 합니다.

### 3. WebView 표시 및 페이지 전환

#### WebView 표시 방법

```csharp
// 방법 1: WebView 표시만 (홈 페이지)
webViewManager.ShowWebView();

// 방법 2: WebView 표시 + 특정 페이지로 이동 (권장)
webViewManager.ShowWebViewAndNavigate("page30");
```

**차이점:**
- **ShowWebView()**: WebView를 화면에 표시만 함 (index.html의 기본 페이지)
- **ShowWebViewAndNavigate(pagePath)**: 표시 + 특정 페이지로 이동 (0.3초 대기 후 페이지 전환)

#### 페이지 전환 방법

```csharp
// WebView가 이미 표시된 상태에서 다른 페이지로 이동
webViewManager.NavigateToPage("page15");
```

> **💡 참고**: `NavigateToPage()`는 WebView가 이미 화면에 표시되어 있을 때 사용합니다.

**페이지 경로 예시:**
- `"page30"` → Flutter에서 `/page30`으로 이동
- `"/video/player"` → 비디오 플레이어 페이지
- `"/gallery"` → 갤러리 페이지

**사용 시나리오:**
1. **처음 표시할 때**: `ShowWebViewAndNavigate("page30")` 사용 (표시 + 이동 한번에)
2. **이미 표시된 상태**: `NavigateToPage("page15")` 사용 (페이지만 전환)

### 4. WebView 숨김

```csharp
// WebView 숨김 (백그라운드로 유지)
webViewManager.HideWebView();
```

---

## 이벤트 활용

SimpleWebViewManager는 다양한 이벤트를 제공하여 진행 상황을 추적할 수 있습니다.

### Inspector에서 이벤트 연결

```
┌─ Simple Web View Manager (Script) ──────┐
│ ▼ 이벤트                                  │
│   On Download Started ()                 │
│     → MyScript.OnDownloadStarted         │
│   On Download Completed ()               │
│     → MyScript.OnDownloadCompleted       │
│   On Download Progress (Single)          │
│     → MyScript.UpdateProgressBar         │
│   On Server Started ()                   │
│     → MyScript.OnServerReady             │
│   On Web View Loaded ()                  │
│     → MyScript.OnWebViewReady            │
└──────────────────────────────────────────┘
```

### 코드에서 이벤트 구독

```csharp
using UnityEngine;
using Muabe.WebView;

public class EventExample : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;

    private void Start()
    {
        // 다운로드 이벤트
        webViewManager.onDownloadStarted.AddListener(() =>
        {
            Debug.Log("다운로드 시작!");
        });

        webViewManager.onDownloadCompleted.AddListener(() =>
        {
            Debug.Log("다운로드 완료!");
        });

        webViewManager.onDownloadProgress.AddListener((progress) =>
        {
            Debug.Log($"다운로드 진행: {progress * 100:F1}%");
        });

        // 서버 이벤트
        webViewManager.onServerStarted.AddListener(() =>
        {
            Debug.Log("서버 시작 완료!");
        });

        // WebView 이벤트
        webViewManager.onWebViewLoaded.AddListener(() =>
        {
            Debug.Log("WebView 로드 완료!");
        });
    }
}
```

---

## 상태 확인

```csharp
// 초기화 완료 여부
if (webViewManager.IsInitialized)
{
    Debug.Log("컴포넌트 초기화 완료");
}

// 서버 실행 여부
if (webViewManager.IsServerRunning)
{
    Debug.Log("서버 실행 중");
}

// WebView 로드 완료 여부
if (webViewManager.IsWebViewLoaded)
{
    Debug.Log("WebView 준비 완료");
}
```

---

## 전체 워크플로우 예제

### 예제 1: 기본 사용 (가장 간단)

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class SimpleExample : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;

    private void Start()
    {
        StartCoroutine(StartWebView());
    }

    private IEnumerator StartWebView()
    {
        // 1. 서버 시작 및 WebView 로드 (숨김 상태)
        webViewManager.StartServerAndLoadWebView();

        // 2. WebView 로드 완료 대기
        yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

        // 3. WebView 표시 및 페이지 이동
        webViewManager.ShowWebViewAndNavigate("page30");

        Debug.Log("WebView 시작 완료!");
    }
}
```

### 예제 2: 다운로드 포함

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class DownloadExample : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;
    private bool downloadCompleted = false;

    private void Start()
    {
        // 다운로드 완료 이벤트 구독
        webViewManager.onDownloadCompleted.AddListener(() => {
            downloadCompleted = true;
        });

        StartCoroutine(FullWorkflow());
    }

    private IEnumerator FullWorkflow()
    {
        // 1. 콘텐츠 다운로드 (선택사항)
        webViewManager.DownloadContent();
        yield return new WaitUntil(() => downloadCompleted);

        // 2. 서버 시작 및 WebView 로드 (숨김 상태)
        webViewManager.StartServerAndLoadWebView();
        yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

        // 3. WebView 표시 및 페이지 이동
        webViewManager.ShowWebViewAndNavigate("page30");

        Debug.Log("전체 워크플로우 완료!");
    }
}
```

### 예제 3: 버튼으로 단계별 제어

```csharp
using UnityEngine;
using UnityEngine.UI;
using Muabe.WebView;

public class ButtonControlExample : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;
    [SerializeField] private Button launchButton;
    [SerializeField] private Button showButton;
    [SerializeField] private Button navigateButton;
    [SerializeField] private Button hideButton;

    private void Start()
    {
        // 버튼 이벤트 연결
        launchButton.onClick.AddListener(OnLaunch);
        showButton.onClick.AddListener(OnShow);
        navigateButton.onClick.AddListener(OnNavigate);
        hideButton.onClick.AddListener(OnHide);

        // 초기 상태
        showButton.interactable = false;
        navigateButton.interactable = false;

        // WebView 로드 완료 시 Show 버튼 활성화
        webViewManager.onWebViewLoaded.AddListener(() =>
        {
            showButton.interactable = true;
        });
    }

    private void OnLaunch()
    {
        // 1. 서버 시작 및 WebView 로드 (숨김 상태)
        webViewManager.StartServerAndLoadWebView();
        launchButton.interactable = false;
    }

    private void OnShow()
    {
        // 2. WebView 표시 및 페이지 이동
        webViewManager.ShowWebViewAndNavigate("page30");
        navigateButton.interactable = true;
    }

    private void OnNavigate()
    {
        // 3. 다른 페이지로 전환
        webViewManager.NavigateToPage("page15");
    }

    private void OnHide()
    {
        // 4. WebView 숨김
        webViewManager.HideWebView();
    }
}
```

### 예제 4: 단순 표시와 페이지 이동 분리

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class SeparateShowExample : MonoBehaviour
{
    [SerializeField] private SimpleWebViewManager webViewManager;

    private void Start()
    {
        StartCoroutine(StartWebView());
    }

    private IEnumerator StartWebView()
    {
        // 1. 서버 시작 및 WebView 로드 (숨김 상태)
        webViewManager.StartServerAndLoadWebView();
        yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

        // 2. WebView 표시 (홈 페이지)
        webViewManager.ShowWebView();

        // 3. 잠시 대기
        yield return new WaitForSeconds(2f);

        // 4. 특정 페이지로 이동
        webViewManager.NavigateToPage("page30");

        Debug.Log("WebView 표시 및 페이지 전환 완료!");
    }
}
```

---

## 동적 설정 변경

### 런타임에 경로 변경

```csharp
// 콘텐츠 경로 변경
webViewManager.SetContentPath("arpedia/another/content");

// 다운로드 URL 변경
webViewManager.SetDownloadUrl("https://example.com/new-content.zip", "2.0.0");
```

---

## 고급 기능 접근

SimpleWebViewManager는 내부 컴포넌트에 직접 접근할 수 있는 속성을 제공합니다.

```csharp
// LocalWebServer 접근
LocalWebServer server = webViewManager.LocalWebServer;
server.StartServer();

// WebViewController 접근
WebViewController controller = webViewManager.WebViewController;
controller.SetVisible(true);

// FlutterWebBridge 접근
FlutterWebBridge bridge = webViewManager.FlutterWebBridge;
bridge.NavigateToPage("page15");
```

---

## 트러블슈팅

### 문제: "WebView is not loaded yet" 경고

**원인**: `StartServerAndLoadWebView()`를 호출하지 않았거나 아직 로드 중

**해결**:
```csharp
// WebView 로드 완료 대기
yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);
```

### 문제: "DownloadManager is not initialized" 에러

**원인**: Download Url이 설정되지 않음

**해결**:
```csharp
// Inspector에서 Download Url 설정
// 또는 코드로 설정
webViewManager.SetDownloadUrl("https://example.com/content.zip", "1.0.0");
```

### 문제: WebView 빈 화면

**원인**: Content Path가 잘못됨

**해결**:
1. Content Path 확인: `{persistentDataPath}/arpedia/dino/wj_demo/index.html`
2. 파일 구조 확인
3. Console 로그 확인

### 문제: 페이지 전환이 안 됨

**원인**: Flutter 앱에서 브릿지 리스너 미구현

**해결**:
1. Flutter 앱에서 `__unityBridge.handleMessage` 구현 확인
2. `type == 'navigate'` 처리 확인
3. `Navigator.pushNamed()` 호출 확인

---

## 비교: 기존 방식 vs SimpleWebViewManager

### 기존 방식 (복잡)

```
1. WebViewManager GameObject 생성
2. LocalWebServer 컴포넌트 추가 및 설정
3. WebContentDownloadManager 추가 및 설정
4. WebViewController 추가 및 설정
5. FlutterWebBridge 추가 및 설정
6. DownloadButton 생성 및 WebContentDownloadButton 추가
7. LaunchButton 생성 및 WebContentLaunchButton 추가
8. ShowButton 생성 및 WebViewShowButton 추가
9. 각 버튼에 참조 연결
10. 버튼 클릭으로 단계별 실행
```

### SimpleWebViewManager (간단)

```
1. WebViewManager GameObject 생성
2. SimpleWebViewManager 컴포넌트 추가
3. Content Path만 설정
4. 코드로 실행:
   // 1. 서버 시작 및 WebView 로드 (숨김)
   webViewManager.StartServerAndLoadWebView();
   yield return new WaitUntil(() => webViewManager.IsWebViewLoaded);

   // 2. WebView 표시 및 페이지 이동
   webViewManager.ShowWebViewAndNavigate("page30");

   // 3. 다른 페이지로 전환 (선택)
   webViewManager.NavigateToPage("page15");
```

**결과**: **10단계 → 4단계**, **복잡한 설정 → 최소 설정**, **여러 버튼 → 간단한 코드**

---

## 요약

### SimpleWebViewManager의 장점

1. **간단한 설정**: Inspector에서 최소한의 설정만 필요
2. **자동 초기화**: 필요한 컴포넌트를 자동으로 추가 및 연결
3. **직관적인 API**: 메서드 이름만 봐도 기능 파악 가능
4. **이벤트 지원**: 진행 상황 추적 및 UI 업데이트 용이
5. **유연성**: 버튼 또는 코드로 자유롭게 제어 가능

### 추천 사용 사례

- Unity 프로젝트에 WebView를 빠르게 통합하고 싶을 때
- Inspector 설정보다 코드로 제어하는 것을 선호할 때
- 복잡한 버튼 설정 없이 간단하게 사용하고 싶을 때
- 런타임에 동적으로 WebView를 제어해야 할 때

### 기존 방식을 사용해야 하는 경우

- UI 버튼으로만 제어하고 싶을 때 (코드 작성 없이)
- 각 컴포넌트를 세밀하게 커스터마이징해야 할 때
- 기존 프로젝트에 이미 설정이 완료되어 있을 때

---

## 다음 단계

- **예제 프로젝트 확인**: `Runtime/Scripts/Examples/SimpleWebViewExample.cs`
- **상세 가이드**: [WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md) (기존 방식 참고용)
- **스크립트 가이드**: [WEBVIEW_SCRIPT_GUIDE.md](WEBVIEW_SCRIPT_GUIDE.md) (고급 스크립트 제어)

---

**Happy Coding!**
