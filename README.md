# Muabe Interactive WebView

[![Unity Version](https://img.shields.io/badge/Unity-2019.4%2B-blue)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.0.3-orange)](package.json)

Muabe Interactive WebView 패키지는 Unity 프로젝트에서 네이티브 웹뷰, 로컬 웹 서버, 원격 콘텐츠 배포를 통합 구성할 수 있도록 도와줍니다. Flutter·React 등으로 제작한 웹 앱을 Android, iOS에서 동일한 워크플로로 배포하고, Unity와 웹 앱 간 양방향 통신을 지원합니다.

> **📖 상세 가이드**: 단계별 설정 가이드는 [WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md)를 참고하세요.

## ✨ 주요 기능

### 핵심 기능
- 🌐 **네이티브 WebView**: `gree/unity-webview` 기반 커스텀 WebView 구현 (Android/iOS)
- 🖥️ **로컬 HTTP 서버**: Unity 내장 경량 서버로 웹 콘텐츠 제공
- 📦 **원격 콘텐츠 관리**: ZIP 파일 다운로드, 버전 관리, 자동 업데이트
- 🔄 **Unity ↔ Flutter/React 브리지**: 양방향 메시지 통신 지원
- 🎬 **비디오 프리로드**: 영상을 미리 로드하여 즉시 재생
- 🎮 **UI 컴포넌트**: 드래그 앤 드롭으로 쉽게 구성 가능한 버튼들

### 지원 환경
- Unity 2019.4 LTS 이상
- 플랫폼: Android 7.0+, iOS 13+
- 의존성: [unity-webview](https://github.com/gree/unity-webview) (패키지에 포함)

> ⚠️ **Unity 2019.4 사용자**: IL2CPP 빌드 권장. [호환성 가이드](UNITY_2019_COMPATIBILITY.md) 참고

## GitHub에서 설치
프로젝트의 `Packages/manifest.json`에 Git URL을 추가하면 바로 사용할 수 있습니다. 태그를 지정해 안정된 버전을 고정하는 것을 권장합니다.

```json
{
  "dependencies": {
    "com.muabe.webview": "https://github.com/Muabe-motion/com.muabe.webview.git#Release-1.0.3"
  }
}
```

Unity 에디터에서는 `Window > Package Manager`를 열고 **+ > Add package from git URL...**을 선택해 동일한 주소를 입력하면 됩니다. 현재 저장소에는 `Release-1.0.3` 태그가 배포 버전으로 등록되어 있으므로 정확한 태그 이름을 사용하세요. 특정 브랜치나 커밋을 사용하고 싶다면 `#branch-name`, `#commit-hash`를 뒤에 붙여 주세요.

로컬 패키지로 쓰고 싶다면 이 저장소를 클론한 뒤 `Packages/com.muabe.webview` 경로를 선택해 `Add package from disk...`를 실행하면 됩니다.

## 📦 패키지 구성

### Core Components (핵심 컴포넌트)
- **`LocalWebServer`**: 로컬 HTTP 서버 (Port 8088, 퍼시스턴트 폴더 또는 StreamingAssets 호스팅)
- **`WebContentDownloadManager`**: ZIP 파일 다운로드, 버전 관리, 자동 업데이트
- **`WebViewController`**: 웹뷰 초기화, URL 로드, 표시/숨김 제어
- **`FlutterWebBridge`**: Unity ↔ Flutter/React 양방향 메시지 통신

### UI Components (UI 컴포넌트)
- **`WebContentDownloadButton`**: 원격 ZIP 다운로드 버튼 (버전 체크, 자동 업데이트)
- **`WebContentLaunchButton`**: 서버 시작 및 웹뷰 로드 버튼
- **`VideoLoadButton`**: 비디오 미리 로드 버튼 (Unity → Flutter 브리지 통신)
- **`WebViewShowButton`**: 웹뷰 표시 및 페이지 전환 버튼
- **`FlutterWidgetButton`**: Flutter 위젯 표시/숨김 제어 버튼

### Utilities (유틸리티)
- **`WebViewConstants`**: 모든 상수 통합 관리
- **`WebViewUtility`**: 15+ 공통 유틸리티 함수
- **`WebViewButtonBase`**: 버튼 베이스 클래스
- **`PermissionRequester`**: 카메라·마이크 등 런타임 권한 요청

### Editor Extensions
- **`WebViewDefines`**: Android defines 자동 관리
- **`UnityWebViewPostprocessBuild`**: 빌드 후처리 자동화

> 💡 **자동 참조**: 대부분의 컴포넌트 필드는 동일 GameObject 내에서 자동으로 참조됩니다.

## 🚀 빠른 시작

### 전체 워크플로우

```
다운로드 → 서버 시작 & 웹뷰 로드 → 비디오 프리로드 → 웹뷰 표시 & 영상 재생
```

### 1단계: WebView GameObject 설정

**Hierarchy에서 새 GameObject 생성:**
```
Create Empty GameObject → 이름: "WebViewManager"
```

**필수 컴포넌트 4개 추가:**
1. `LocalWebServer` (Port: 8088, Default Document: index.html)
2. `WebContentDownloadManager` (Install Folder Name: webview-content)
3. `WebViewController` (Server Port: 8088, Enable WKWebView: ✅)
4. `FlutterWebBridge` (Unity To Flutter Event: __unityBridge)

### 2단계: Download 버튼 설정

**UI Button 생성 → `WebContentDownloadButton` 컴포넌트 추가**

**Inspector 설정:**
- `Installer`: WebViewManager GameObject 할당
- `Download Url`: ZIP 파일 URL (예: `https://example.com/flutter-app.zip`)
- `Remote Version Override`: 버전 문자열 (예: `1.0.0`)

### 3단계: Launch 버튼 설정

**UI Button 생성 → `WebContentLaunchButton` 컴포넌트 추가**

**Inspector 설정:**
- `Installer`, `Target Server`, `Target Web View`: 모두 WebViewManager 할당
- `Content Root Subfolder`: ZIP 내 폴더명 (예: `flutter`)
- `Route Prefix`: 동일한 폴더명 (예: `flutter`)

### 4단계: Video Load 버튼 설정 (선택사항)

**UI Button 생성 → `VideoLoadButton` 컴포넌트 추가**

**Inspector 설정:**
- `Bridge`: WebViewManager GameObject 할당

> Flutter/React 앱에서 `window.__unityBridge.handleMessage` 리스너 구현 필요

### 5단계: Show 버튼 설정

**UI Button 생성 → `WebViewShowButton` 컴포넌트 추가**

**Inspector 설정:**
- `Target Web View`: WebViewManager 할당
- `Bridge`: WebViewManager 할당
- `Page Path`: 표시할 페이지 경로 (예: `page30`)
- `Use Bridge`: ✅ (권장)
- `Wait For Videos Loaded`: ✅ (4단계 사용 시)

### 실행 순서

1. **Download 버튼 클릭** → ZIP 다운로드 및 설치 완료
2. **Launch 버튼 클릭** → 서버 시작 및 웹뷰 로드 (숨김 상태)
3. **Video Load 버튼 클릭** → 비디오 미리 로드 (선택사항)
4. **Show 버튼 클릭** → 웹뷰 표시 및 페이지 전환 🎉

> 📖 **상세 가이드**: 각 컴포넌트의 상세 설정은 [WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md)를 참고하세요.

## 원격 콘텐츠 배포 워크플로
1. Flutter·React·Vue 등으로 제작한 웹 앱을 빌드한 다음 결과물을 ZIP으로 압축합니다. ZIP 루트 폴더 이름은 `contentRootSubfolder` 값과 일치해야 합니다.
2. Android에서 StreamingAssets를 사용할 경우 `android-preload.txt` 파일에 미리 패키징할 리소스를 한 줄씩 작성하고 `LocalWebServer.androidPreloadListFile`에 경로를 지정합니다. 주석은 `#`으로 시작합니다.
3. ZIP 파일을 HTTPS CDN, GitHub Release, 사내 서버 등에서 다운로드할 수 있도록 업로드합니다.
4. `WebContentDownloadButton` Inspector에서 `downloadUrl`과 `remoteVersion` 값을 입력하고, 업데이트 시 버전을 변경해 재다운로드를 트리거합니다.
5. 서버 응답이 큰 경우 `timeoutSeconds`와 `maxRedirects` 값을 조정해 안정성을 확보하세요.

## 플랫폼별 설정

### Android
- `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define이 자동으로 추가됩니다
- HTTP 사용 시 네트워크 정책 확인 필요
- 추가 권한이 필요한 경우 `PermissionRequester` 사용
- 포트 번호: 8088 (기본값)

### iOS
- `WebViewController`에서 `Enable WKWebView` 옵션 활성화 필수 (✅)
- HTTP 콘텐츠 사용 시:
  - `Edit > Project Settings > Player > iOS > Other Settings > Configuration`
  - **Allow downloads over HTTP** → **Always allowed** 설정
- WKWebView는 iOS 13+ 필수

## 문제 해결

### 웹뷰가 빈 화면
- `LocalWebServer` 로그와 `WebContentDownloadManager` 설치 로그 확인
- `WebViewController`의 `Server Port`와 `LocalWebServer`의 `Port`가 일치하는지 확인 (8088)
- `Route Prefix`와 `Content Root Subfolder`가 일치하는지 확인

### ZIP 구조 오류
- 폴더 이름이 `contentRootSubfolder`와 일치하는지 확인
- 예상 구조: `flutter-app.zip/flutter/index.html`
- ZIP 파일 압축 해제 후 경로 재확인

### 다운로드 실패
- Download Url이 정확한지 확인
- HTTPS 사용 권장 (Android HTTP 차단 방지)
- 네트워크 연결 상태 확인
- 브라우저에서 URL 직접 다운로드 테스트

### 비디오 로드 타임아웃
- Flutter/React 앱에서 `window.__unityBridge.handleMessage` 리스너 구현 확인
- `Load Timeout` 값 증가 (30초 → 60초)
- Flutter 콘솔에서 'load_videos' 메시지 수신 로그 확인

### 버전 업데이트 안 됨
- `Remote Version Override` 값 변경 (예: 1.0.0 → 1.0.1)
- `Force Download Every Time` 옵션 활성화
- 수동으로 폴더 삭제: `Application.persistentDataPath/webview-content/`

> 💡 **더 자세한 트러블슈팅**: [WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md)의 각 단계별 트러블슈팅 섹션 참고

## 📚 문서

- **[WEBVIEW_SETUP_GUIDE.md](WEBVIEW_SETUP_GUIDE.md)** - 단계별 상세 설정 가이드 (1~5단계)
- **[UNITY_2019_COMPATIBILITY.md](UNITY_2019_COMPATIBILITY.md)** - Unity 2019.4 호환성 가이드
- **[README.md](README.md)** - 이 문서 (빠른 시작 및 개요)

## 🔧 코드 예시

### Unity ↔ Flutter 통신

**Unity → Flutter (명령 전송)**:
```csharp
using Muabe.WebView;

public class MyController : MonoBehaviour
{
    [SerializeField] private FlutterWebBridge bridge;
    
    public void OnButtonClick()
    {
        // Flutter 위젯 제어
        bridge.HideWidget("lion");
        bridge.ShowWidget("cloud");
        bridge.ToggleWidgetVisibility("bird");
        
        // 페이지 전환
        bridge.NavigateToPage("/page30");
        
        // 비디오 로드 명령
        bridge.SendLoadVideosCommand();
    }
    
    void Start()
    {
        // Flutter로부터 이벤트 수신
        bridge.OnVideosLoaded += (loadedCount, totalCount) =>
        {
            Debug.Log($"비디오 로드 완료: {loadedCount}/{totalCount}");
        };
    }
}
```

**Flutter → Unity (메시지 수신 및 응답)**:
```dart
import 'dart:js' as js;

class UnityBridge {
  void init() {
    // Unity 메시지 수신 리스너 등록
    js.context['__unityBridge'] = js.JsObject.jsify({
      'handleMessage': (message) {
        var msg = js.JsObject.jsify(message);
        String type = msg['type'];
        
        if (type == 'navigate') {
          String page = msg['page'];
          Navigator.pushNamed(context, page);
        } else if (type == 'load_videos') {
          loadVideos();
        } else if (type == 'show_widget') {
          String widgetId = msg['widgetId'];
          showWidget(widgetId);
        }
      }
    });
  }
  
  // Unity로 비디오 로드 완료 전송
  void sendVideosLoaded(int loaded, int total) {
    js.context.callMethod('unityCallFunction', [
      'OnVideosLoaded',
      '$loaded,$total'
    ]);
  }
}
```

## 🤝 기여

기여를 환영합니다! 이슈나 풀 리퀘스트를 자유롭게 제출해주세요.

## 📞 지원

- **개발사**: Muabe Motion
- **이메일**: dev@muabe.com
- **웹사이트**: https://www.muabe.com/

## 📄 라이선스

이 패키지는 [Apache License 2.0](LICENSE) 하에 배포됩니다.

---

**Made with ❤️ by Muabe Motion**
