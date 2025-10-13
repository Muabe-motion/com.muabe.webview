# Local WebView Package

이 패키지는 Unity 프로젝트에서 웹뷰와 로컬 웹 서버를 함께 구성할 수 있도록 도와줍니다.

## 제공 기능
- gree/unity-webview 기반 `WebViewObject` 스크립트 및 네이티브 플러그인
- Android/iOS/WebGL 지원 설정
- `LocalWebServer`로 HTTP 서버를 실행하고, `RemoteWebContentInstaller`로 원격 ZIP을 받아 콘텐츠를 배포
- 권한 요청 및 빌드 후 처리(Editor 스크립트)

## 설치
1. 프로젝트의 `Packages/manifest.json`에 이 패키지를 추가하거나 `Add package from disk...`로 `Packages/com.muabe.webview/package.json`을 선택합니다.
2. Unity가 임포트를 완료하면 Package Manager에서 `Local WebView` 항목을 확인할 수 있습니다.

## 사용 방법
1. Package Manager의 `Samples` 탭에서 **Basic Setup**을 임포트하면 `RemoteWebContentInstaller` 구성 예시와 안내서를 확인할 수 있습니다.
2. 씬에 `LocalWebServer`, `RemoteWebContentInstaller`, `WebViewController`를 붙이고 포트/라우트(`/flutter/`)가 서로 일치하도록 설정합니다. `RemoteWebContentInstaller`는 같은 GameObject(또는 자식)에서 `LocalWebServer`/`WebViewController`를 자동으로 찾지만, 필요하면 인스펙터에서 명시적으로 지정하세요. `LocalWebServer.autoStartOnStart`와 `WebViewController.autoLoadOnStart`는 기본으로 꺼져 있으며, `RemoteWebContentInstaller`의 `autoStartServer`/`autoLoadWebViewOnInstall` 옵션을 켜 두면 설치 완료 후 서버를 기동하고 한 프레임 뒤에 `LoadInitialUrl()`을 자동 호출합니다.
3. 수동 동작이 필요하면 UI 버튼에 `WebContentDownloadButton` 컴포넌트를 붙여 두세요. 버튼은 기본적으로 캐시된 콘텐츠가 있으면 재다운로드 없이 서버를 다시 기동하고 웹뷰를 띄웁니다. 매번 새 ZIP을 받고 싶다면 `Force Download Every Time` 옵션을 켜면 됩니다. 상태 텍스트를 연결하면 진행 상황을 간단히 보여줄 수 있습니다.
4. Android용 StreamingAssets을 쓸 계획이라면 Flutter 빌드 결과와 함께 `android-preload.txt` 같은 텍스트 파일을 넣고, `LocalWebServer.androidPreloadListFile`에 해당 경로를 적어 주면 됩니다(줄마다 상대 경로 한 줄씩). 파일이 없으면 인스펙터에 적어 둔 기본 목록을 사용합니다.
5. 배포용 웹 앱을 ZIP으로 압축해 HTTPS 등에서 내려받을 수 있는 URL을 `RemoteWebContentInstaller.downloadUrl`에 입력합니다. 새 버전을 배포할 때는 `remoteVersion`을 변경하면 자동으로 다시 다운로드됩니다.
6. Android 빌드의 경우 `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` 정의가 자동으로 적용되는지 확인하고, 필요한 경우 `PermissionRequester`를 사용해 권한 팝업을 띄우세요.

## 문서
추가 설정 방법은 `Documentation~/setup.md`를 참고하세요.
