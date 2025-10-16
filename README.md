# Muabe Interactive WebView

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue)](https://unity.com/)
[![License](https://img.shields.io/badge/License-Apache%202.0-green.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-1.0.3-orange)](package.json)

Muabe Interactive WebView 패키지는 Unity 프로젝트에서 네이티브 웹뷰, 로컬 웹 서버, 원격 콘텐츠 배포 흐름을 한 번에 구성할 수 있도록 도와줍니다. 하나의 패키지로 Android, iOS 환경에서 동일한 워크플로를 유지하면서 Flutter·React 등으로 제작한 웹 앱을 손쉽게 배포할 수 있습니다.

> **📖 상세 문서**: 전체 아키텍처와 컴포넌트 설명은 [ARCHITECTURE.md](ARCHITECTURE.md)를 참고하세요.

## ✨ 주요 기능

### 핵심 기능
- 🌐 **네이티브 WebView**: `gree/unity-webview` 기반 커스텀 WebView 구현
- 🖥️ **로컬 HTTP 서버**: Unity 내장 경량 서버로 웹 콘텐츠 제공
- 📦 **원격 콘텐츠 관리**: ZIP 파일 다운로드, 버전 관리, 자동 업데이트
- 🔄 **Unity ↔ Flutter 브리지**: 양방향 메시지 통신 지원
- 🎮 **UI 컴포넌트**: 드래그 앤 드롭으로 쉽게 구성 가능한 버튼들

## 지원 환경
- Unity 2021.3 이상
- 플랫폼: Android 7.0+, iOS 13+
- 의존성: [unity-webview](https://github.com/gree/unity-webview) (패키지에 포함)

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
- **`LocalWebServer`**: 퍼시스턴트 폴더 또는 StreamingAssets를 호스팅하는 경량 HTTP 서버
- **`WebContentDownloadManager`**: ZIP 아카이브 다운로드, 버전 및 캐시 관리
- **`WebViewController`**: 웹뷰 초기 URL 로드, 서버 상태 감지, 마진 관리
- **`FlutterWebBridge`**: Unity ↔ Flutter 양방향 메시지 브리지

### UI Components (UI 컴포넌트)
- **`WebContentDownloadButton`**: 콘텐츠 다운로드 버튼
- **`WebContentLaunchButton`**: 서버 시작 및 WebView 로드 버튼
- **`FlutterWidgetButton`**: Flutter 위젯 제어 버튼

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

### 1단계: GameObject 생성 및 컴포넌트 추가

```csharp
// 새로운 GameObject 생성
GameObject webViewManager = new GameObject("WebViewManager");

// 핵심 컴포넌트 추가
webViewManager.AddComponent<Muabe.WebView.LocalWebServer>();
webViewManager.AddComponent<Muabe.WebView.WebContentDownloadManager>();
webViewManager.AddComponent<Muabe.WebView.WebViewController>();
webViewManager.AddComponent<Muabe.WebView.FlutterWebBridge>(); // 선택사항

// 씬 전환 시에도 유지
DontDestroyOnLoad(webViewManager);
```

### 2단계: UI 버튼 설정

1. **다운로드 버튼**
   - UI Button 생성 → `WebContentDownloadButton` 컴포넌트 추가
   - Inspector에서 설정:
     - `Download Url`: ZIP 파일 URL (예: `https://example.com/app.zip`)
     - `Remote Version Override`: 버전 문자열 (예: `1.0.0`)

2. **실행 버튼**
   - UI Button 생성 → `WebContentLaunchButton` 컴포넌트 추가
   - Inspector에서 설정:
     - `Content Root Subfolder`: ZIP 내 웹 앱 폴더명 (예: `flutter`)
     - `Route Prefix`: URL 경로 (예: `flutter`)

### 3단계: 실행

1. 다운로드 버튼 클릭 → ZIP 다운로드 및 설치
2. 실행 버튼 클릭 → 서버 시작 및 WebView 로드
3. 완료! 웹 앱이 실행됩니다 🎉

> 📖 **자세한 가이드**: [설치 및 설정 가이드](Documentation~/setup.md) 참고

## 원격 콘텐츠 배포 워크플로
1. Flutter·React·Vue 등으로 제작한 웹 앱을 빌드한 다음 결과물을 ZIP으로 압축합니다. ZIP 루트 폴더 이름은 `contentRootSubfolder` 값과 일치해야 합니다.
2. Android에서 StreamingAssets를 사용할 경우 `android-preload.txt` 파일에 미리 패키징할 리소스를 한 줄씩 작성하고 `LocalWebServer.androidPreloadListFile`에 경로를 지정합니다. 주석은 `#`으로 시작합니다.
3. ZIP 파일을 HTTPS CDN, GitHub Release, 사내 서버 등에서 다운로드할 수 있도록 업로드합니다.
4. `WebContentDownloadButton` Inspector에서 `downloadUrl`과 `remoteVersion` 값을 입력하고, 업데이트 시 버전을 변경해 재다운로드를 트리거합니다.
5. 서버 응답이 큰 경우 `timeoutSeconds`와 `maxRedirects` 값을 조정해 안정성을 확보하세요.

## 플랫폼별 체크리스트
- **Android**: `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define이 자동으로 추가됩니다. HTTP를 사용하면 네트워크 정책을 확인하고, 추가 권한이 필요하면 `PermissionRequester`를 활용하세요.
- **iOS**: `enableWKWebView` 옵션을 켜면 WKWebView가 활성화됩니다. HTTP를 이용한 콘텐츠 사용을 위해서는 `Edit > Project Settings > Player > iOS > Other Settings > Configuration` 섹션에서 **Allow downloads over HTTP** 값을 **Always allowed**로 설정하세요.

## 문제 해결
- 웹뷰가 빈 화면일 경우: `LocalWebServer` 로그와 `WebContentDownloadManager`의 설치 로그(에디터 콘솔)를 확인하고 포트/라우트가 일치하는지 점검하세요.
- ZIP 구조 오류: 폴더 이름이 `contentRootSubfolder`와 다르면 설치되지 않습니다. 압축을 풀어 경로를 다시 확인하세요.
- Android에서 HTTP 요청 차단: HTTPS URL을 사용하거나 네트워크 보안 정책(네트워크 보안 구성)을 조정하세요.
- 캐시 무효화: 새 버전을 강제로 받으려면 `WebContentDownloadButton`의 `remoteVersion` 값을 증가시키거나 `Force Download Every Time` 옵션을 활성화합니다.

## 📚 문서

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - 전체 아키텍처 및 컴포넌트 상세 설명
- **[설치 및 설정 가이드](Documentation~/setup.md)** - 단계별 설치 및 설정 방법
- **[README.md](README.md)** - 이 문서

## 🔧 코드 예시

### Unity ↔ Flutter 통신

**Unity 측**:
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
    }
}
```

**Flutter 측**:
```dart
import 'package:your_app/common/unity_bridge/unity_bridge.dart';

class MyPage extends StatefulWidget {
  @override
  void initState() {
    super.initState();
    unityBridge.addVisibilityListener((widgetId, visible) {
      print('Unity says: $widgetId should be ${visible ? "visible" : "hidden"}');
    });
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
