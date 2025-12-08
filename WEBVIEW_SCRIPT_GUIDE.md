# WebView 스크립트 사용 가이드

> **UI 버튼 없이 C# 스크립트만으로 WebView를 제어하는 방법을 상세히 설명합니다.**

---

## 개요

### 이 가이드의 대상

다음과 같은 상황에 해당하는 개발자를 위한 가이드입니다:

- UI 버튼 대신 스크립트로 WebView를 자동 실행하려는 경우
- 씬 시작 시 자동으로 WebView를 로드하려는 경우
- "웹뷰 로드 완료!" 로그가 출력되었으나 실제로는 WebView가 준비되지 않은 문제를 겪는 경우

## 버튼 방식과 스크립트 방식의 차이점

### UI 버튼 방식의 특징

```
사용자가 버튼 클릭
   ↓
버튼이 자동으로 준비 상태 확인
   ↓
준비되면 자동 실행
   ↓
완료!
```

**장점:**
- 타이밍 걱정 없음
- 자동으로 대기
- 초보자 친화적

### 스크립트 직접 호출 방식의 특징

```
개발자가 메서드 호출
   ↓
메서드가 즉시 반환됨
   ↓
실제 작업은 백그라운드에서 진행 중...
   ↓
개발자가 직접 완료 대기해야 함 (실수가 발생하기 쉬운 지점)
```

**단점:**
- 타이밍을 직접 관리해야 함
- 준비 상태를 직접 확인해야 함
- 실수 가능성이 높음

**핵심 차이점:**

스크립트 방식에서는 메서드 호출 후 **실제 완료를 직접 대기**해야 합니다. 이 가이드는 올바른 대기 방법과 타이밍 관리 방법을 설명합니다.

---

## 단계별 완전 가이드

### 단계 0: 준비물 확인

먼저 씬에 다음이 있어야 합니다:
- **WebViewManager** GameObject
  - LocalWebServer 컴포넌트
  - WebViewController 컴포넌트
  - FlutterWebBridge 컴포넌트

> 이 설정은 [WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md)의 1단계를 참고하세요.

### 단계 1: 서버 시작 및 대기

**작업 내용:**
1. 서버 시작
2. 서버가 실제로 준비될 때까지 대기

**구현 코드:**

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class Step1_StartServer : MonoBehaviour
{
    [SerializeField] private LocalWebServer server;

    void Start()
    {
        StartCoroutine(StartServerAndWait());
    }

    IEnumerator StartServerAndWait()
    {
        // 1. 서버 시작
        Debug.Log("🔵 서버 시작 중...");
        server.StartServer();

        // 2. 준비될 때까지 대기 (타임아웃 5초)
        float timeWaited = 0f;
        float timeout = 5f;

        while (!server.IsServerReady && timeWaited < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            timeWaited += 0.1f;
        }

        // 3. 결과 확인
        if (server.IsServerReady)
        {
            Debug.Log("서버 준비 완료!");
        }
        else
        {
            Debug.LogError("서버 시작 타임아웃!");
        }
    }
}
```

**핵심 사항:**
- `server.StartServer()`: 서버 시작 (즉시 반환)
- `server.IsServerReady`: 준비 완료 여부 확인
- `while` 루프: 준비될 때까지 반복 확인
- `timeout`: 무한 대기 방지

### 단계 2: WebView 로드 및 대기

**작업 내용:**
1. WebView 초기화 시작
2. WebView가 실제로 준비될 때까지 대기

**구현 코드:**

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class Step2_LoadWebView : MonoBehaviour
{
    [SerializeField] private WebViewController webViewController;

    void Start()
    {
        StartCoroutine(LoadWebViewAndWait());
    }

    IEnumerator LoadWebViewAndWait()
    {
        // 1. WebView 로드 시작
        Debug.Log("🔵 WebView 초기화 중...");
        webViewController.LoadInitialUrl();

        // 2. 준비될 때까지 대기 (타임아웃 10초)
        float timeWaited = 0f;
        float timeout = 10f;

        while (!webViewController.IsWebViewReady && timeWaited < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            timeWaited += 0.1f;
        }

        // 3. 결과 확인
        if (webViewController.IsWebViewReady)
        {
            Debug.Log("WebView 준비 완료!");
        }
        else
        {
            Debug.LogError("WebView 초기화 타임아웃!");
        }
    }
}
```

**핵심 사항:**
- `LoadInitialUrl()`: WebView 초기화 시작
- `IsWebViewReady`: WebView 준비 여부 확인
- 0.5초 후부터 초기화 시작 (내부 지연)
- 약 1~2초 후 완료

### 단계 3: WebView 표시 (Flutter 대기 포함)

**작업 내용:**
1. WebView를 화면에 표시
2. Flutter 앱이 준비될 때까지 0.3초 대기
3. 페이지 전환 메시지 전송

**구현 코드:**

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

public class Step3_ShowWebView : MonoBehaviour
{
    [SerializeField] private WebViewController webViewController;
    [SerializeField] private FlutterWebBridge bridge;

    public void ShowPage(string pagePath)
    {
        StartCoroutine(ShowPageRoutine(pagePath));
    }

    IEnumerator ShowPageRoutine(string pagePath)
    {
        // 1. WebView 준비 확인
        if (!webViewController.IsWebViewReady)
        {
            Debug.LogError("WebView가 준비되지 않았습니다!");
            yield break;
        }

        // 2. WebView 표시
        Debug.Log("WebView 표시...");
        webViewController.SetVisible(true);

        // 3. Flutter 앱 준비 대기 (필수)
        Debug.Log("Flutter 준비 대기 (0.3초)...");
        yield return new WaitForSecondsRealtime(0.3f);

        // 4. 페이지 전환
        Debug.Log($"페이지 전환: {pagePath}");
        bridge.NavigateToPage(pagePath);

        Debug.Log("완료!");
    }
}
```

**핵심 사항:**
- `SetVisible(true)`: WebView 표시
- `yield return new WaitForSecondsRealtime(0.3f)`: Flutter 대기
- 0.3초 대기는 필수 (메시지 손실 방지)
- `NavigateToPage()`: Flutter 페이지 전환

**0.3초 대기가 필요한 이유:**

```
0.0초: SetVisible(true) → WebView 화면에 표시
0.0초: Flutter 앱 로딩 시작...
0.1초: Flutter 앱 초기화 중...
0.2초: Flutter 앱 이벤트 리스너 등록 중...
0.3초: Flutter 앱 준비 완료
0.3초: NavigateToPage() → 메시지 전송 성공
```

만약 0초에 바로 전송하는 경우:
```
0.0초: NavigateToPage() 전송 → Flutter가 못 받음 (아직 준비 안 됨)
```

---

## 전체 워크플로우 (통합 구현)

모든 단계를 하나의 스크립트로 통합한 예제입니다.

```csharp
using System.Collections;
using UnityEngine;
using Muabe.WebView;

/// <summary>
/// 씬 시작 시 자동으로 WebView를 로드하고 페이지를 표시하는 완전한 구현 예제
/// </summary>
public class AutoWebViewLoader : MonoBehaviour
{
    [Header("참조 (자동으로 찾음)")]
    [SerializeField] private LocalWebServer server;
    [SerializeField] private WebViewController webViewController;
    [SerializeField] private FlutterWebBridge bridge;

    [Header("설정")]
    [SerializeField] private string pagePath = "/page30";

    void Start()
    {
        // WebViewManager 자동 찾기
        GameObject manager = GameObject.Find("WebViewManager");
        if (manager != null)
        {
            server = manager.GetComponent<LocalWebServer>();
            webViewController = manager.GetComponent<WebViewController>();
            bridge = manager.GetComponent<FlutterWebBridge>();
        }

        // 전체 프로세스 시작
        StartCoroutine(FullWorkflow());
    }

    IEnumerator FullWorkflow()
    {
        Debug.Log("========== WebView 자동 로드 시작 ==========");

        // 1단계: 서버 시작
        yield return StartCoroutine(Step1_StartServer());

        // 2단계: WebView 로드
        yield return StartCoroutine(Step2_LoadWebView());

        // 3단계: 페이지 표시
        yield return StartCoroutine(Step3_ShowPage());

        Debug.Log("========== ✅ 모든 작업 완료! ==========");
    }

    // 1단계: 서버 시작
    IEnumerator Step1_StartServer()
    {
        Debug.Log("\n[1/3] 서버 시작");
        Debug.Log("─────────────────");

        if (server == null)
        {
            Debug.LogError("LocalWebServer를 찾을 수 없습니다!");
            yield break;
        }

        // 이미 실행 중인지 확인
        if (server.IsRunning)
        {
            Debug.Log("서버가 이미 실행 중입니다.");
            yield break;
        }

        // 서버 시작
        Debug.Log("서버 시작 중...");
        server.StartServer();

        // 준비 대기
        float timeWaited = 0f;
        float timeout = 5f;

        while (!server.IsServerReady && timeWaited < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            timeWaited += 0.1f;
        }

        if (server.IsServerReady)
        {
            Debug.Log("서버 준비 완료!");
        }
        else
        {
            Debug.LogError("서버 시작 타임아웃!");
        }
    }

    // 2단계: WebView 로드
    IEnumerator Step2_LoadWebView()
    {
        Debug.Log("\n[2/3] WebView 로드");
        Debug.Log("─────────────────");

        if (webViewController == null)
        {
            Debug.LogError("WebViewController를 찾을 수 없습니다!");
            yield break;
        }

        // 이미 준비되었는지 확인
        if (webViewController.IsWebViewReady)
        {
            Debug.Log("WebView가 이미 준비되어 있습니다.");
            yield break;
        }

        // WebView 초기화
        Debug.Log("WebView 초기화 중...");
        webViewController.LoadInitialUrl();

        // 준비 대기
        float timeWaited = 0f;
        float timeout = 10f;

        while (!webViewController.IsWebViewReady && timeWaited < timeout)
        {
            yield return new WaitForSeconds(0.1f);
            timeWaited += 0.1f;
        }

        if (webViewController.IsWebViewReady)
        {
            Debug.Log("WebView 준비 완료!");
        }
        else
        {
            Debug.LogError("WebView 초기화 타임아웃!");
        }
    }

    // 3단계: 페이지 표시
    IEnumerator Step3_ShowPage()
    {
        Debug.Log("\n[3/3] 페이지 표시");
        Debug.Log("─────────────────");

        if (webViewController == null || bridge == null)
        {
            Debug.LogError("WebViewController 또는 Bridge를 찾을 수 없습니다!");
            yield break;
        }

        // WebView 준비 확인
        if (!webViewController.IsWebViewReady)
        {
            Debug.LogError("WebView가 준비되지 않았습니다!");
            yield break;
        }

        // WebView 표시
        Debug.Log("WebView 화면에 표시...");
        webViewController.SetVisible(true);

        // Flutter 앱 준비 대기 (필수)
        Debug.Log("Flutter 준비 대기 (0.3초)...");
        yield return new WaitForSecondsRealtime(0.3f);

        // 페이지 전환
        Debug.Log($"페이지 전환: {pagePath}");
        bridge.NavigateToPage(pagePath);

        Debug.Log("페이지 표시 완료!");
    }
}
```

### 사용 방법

1. 빈 GameObject 생성 (이름: "AutoLoader")
2. 위 스크립트를 추가
3. Inspector에서 설정:
   - Page Path: `/page30` (원하는 페이지)
4. Play 버튼 클릭

**Console 출력 예시:**

```
========== WebView 자동 로드 시작 ==========

[1/3] 서버 시작
─────────────────
서버 시작 중...
서버 준비 완료!

[2/3] WebView 로드
─────────────────
WebView 초기화 중...
WebView 준비 완료!

[3/3] 페이지 표시
─────────────────
WebView 화면에 표시...
Flutter 준비 대기 (0.3초)...
페이지 전환: /page30
페이지 표시 완료!

========== 모든 작업 완료! ==========
```

---

## 타이밍 비교표

| 작업 | 잘못된 방법 | 올바른 방법 |
|------|-----------|----------|
| 서버 시작 | `StartServer()` 호출만 | `StartServer()` + `IsServerReady` 대기 |
| WebView 로드 | `LoadInitialUrl()` 호출만 | `LoadInitialUrl()` + `IsWebViewReady` 대기 |
| 페이지 전환 | 즉시 호출 | WebView 표시 + 0.3초 대기 + 호출 |

---

## 주요 속성 및 메서드 요약

### LocalWebServer

| 이름 | 타입 | 설명 |
|------|------|------|
| `IsRunning` | 속성 (bool) | 서버 실행 여부 |
| `IsServerReady` | 속성 (bool) | 서버 준비 완료 여부 (필수 확인) |
| `StartServer()` | 메서드 | 서버 시작 |

### WebViewController

| 이름 | 타입 | 설명 |
|------|------|------|
| `IsWebViewReady` | 속성 (bool) | WebView 준비 완료 여부 (필수 확인) |
| `LoadInitialUrl()` | 메서드 | 초기 URL 로드 (비동기) |
| `SetVisible(bool)` | 메서드 | WebView 표시/숨김 |

### FlutterWebBridge

| 이름 | 타입 | 설명 |
|------|------|------|
| `NavigateToPage(string)` | 메서드 | Flutter 페이지 전환 (필수 사용) |

---

## 자주하는 실수 TOP 3

### 실수 1: "완료" 로그를 즉시 출력

```csharp
// 잘못된 방법:
webViewController.LoadInitialUrl();
Debug.Log("완료!");  // 실제로는 완료되지 않음

// 올바른 방법:
webViewController.LoadInitialUrl();
yield return new WaitUntil(() => webViewController.IsWebViewReady);
Debug.Log("완료!");  // 실제 완료 시점
```

### 실수 2: Flutter 준비 대기 없음

```csharp
// 잘못된 방법:
webViewController.SetVisible(true);
bridge.NavigateToPage("/page");  // Flutter가 메시지를 못 받을 수 있음

// 올바른 방법:
webViewController.SetVisible(true);
yield return new WaitForSecondsRealtime(0.3f);  // Flutter 대기
bridge.NavigateToPage("/page");  // 이제 전송 성공
```

### 실수 3: 준비 상태 확인 안 함

```csharp
// 잘못된 방법:
webViewController.SetVisible(true);  // WebView가 준비 안 됨

// 올바른 방법:
if (!webViewController.IsWebViewReady)
{
    Debug.LogError("WebView가 준비되지 않았습니다!");
    return;
}
webViewController.SetVisible(true);  // 안전한 실행
```

---

## 버튼 vs 스크립트 선택 가이드

### UI 버튼 사용이 적합한 경우 (권장)

- 사용자가 원하는 시점에 실행
- 타이밍 자동 관리
- 초보자에게 안전한 방법
- 빠른 프로토타이핑

### 스크립트 사용이 적합한 경우

- 씬 시작 시 자동 실행 필요
- 복잡한 조건부 로직 구현
- 커스텀 UI 구현
- 타이밍을 직접 관리할 수 있는 경우

**권장사항:** 불확실한 경우 UI 버튼 사용을 권장합니다.

---

## 추가 도움말

### 디버깅 방법

로그가 너무 많을 경우, 단계별로 나누어 테스트하세요:

1. **1단계 테스트**: 서버 시작만 확인
2. **2단계 테스트**: WebView 로드만 확인
3. **3단계 테스트**: 페이지 표시만 확인

### 타임아웃 조절

- 서버: 기본 5초 (일반적으로 충분)
- WebView: 기본 10초 (일반적으로 충분)
- Flutter 대기: 기본 0.3초 (필요시 0.5초까지 조정 가능)

### 추가 참고 자료

- [WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md): UI 버튼 사용법
- GitHub Issues: 문제 발생 시 이슈 등록
