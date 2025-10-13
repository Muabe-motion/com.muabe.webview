# WebView 패키지 설치 가이드

1. **패키지 추가**: Unity에서 `Window > Package Manager`를 열고 `+ > Add package from disk...`를 선택한 뒤 `Packages/com.muabe.webview/package.json`을 선택합니다.
2. **샘플 임포트**: Package Manager의 `Samples` 섹션에서 `Basic Setup`을 임포트하면 `RemoteWebContentInstaller` 구성 예시와 문서를 `Assets/Samples/...` 경로에서 확인할 수 있습니다.
3. **씬 구성**:
   - `LocalWebServer` 컴포넌트를 가진 GameObject를 하나 두고, `port`와 `routePrefix`(예: `flutter`)를 지정합니다. 기본값으로 `autoStartOnStart`는 꺼져 있고 `contentSource`는 `PersistentDataPath`로 설정됩니다.
   - 같은 객체에 `RemoteWebContentInstaller`를 붙이고 `downloadUrl`, `installFolderName`, `contentRootSubfolder`를 입력합니다. ZIP 내부 구조가 `contentRootSubfolder`와 일치해야 합니다. 이 컴포넌트는 동일 GameObject(또는 자식)에서 `LocalWebServer`와 `WebViewController`를 자동으로 찾아 채우지만, 필요하면 인스펙터에서 수동으로 지정할 수 있습니다. `autoStartServer`와 `autoLoadWebViewOnInstall`을 켜 두면 설치가 끝나는 즉시 서버를 기동하고 한 프레임 뒤에 웹뷰를 새로 로드합니다.
   - 다운로드 트리거를 수동으로 제공하려면 UI 버튼에 `WebContentDownloadButton`을 붙이고 `installer`(필수)와 `webViewController`(선택) 필드를 연결하세요. 기본적으로 캐시된 파일이 있으면 재다운로드 없이 서버를 재시작하고 웹뷰를 표시하며, 매번 새 ZIP을 받고 싶다면 `Force Download Every Time`을 켜면 됩니다. 상태 텍스트를 지정하면 “다운로드 중/캐시 로드/완료/실패” 문구가 자동으로 갱신됩니다.
   - `WebViewController`를 동일 GameObject 또는 별도 객체에 붙여 `serverPort`와 `webRootPath`(`/flutter/`)를 `LocalWebServer` 설정과 맞춥니다. 기본으로 `autoLoadOnStart`가 꺼져 있으므로 `RemoteWebContentInstaller`가 설치 완료 후 자동으로 `LoadInitialUrl()`을 호출하도록 합니다.
   - Android에서 카메라/마이크 권한이 필요하면 `PermissionRequester`를 추가합니다.
4. **원격 콘텐츠 준비**:
   - 웹 앱 빌드 결과(예: Flutter `build/web`)를 ZIP으로 묶습니다. ZIP 내부 최상위 폴더 이름이 `contentRootSubfolder`와 동일하도록 유지하세요.
   - Android 빌드에서 StreamingAssets를 사용할 경우, ZIP 안에 `android-preload.txt`와 같이 미리 로드할 파일 목록을 담은 텍스트를 넣어 두고 `LocalWebServer.androidPreloadListFile`에 상대 경로를 지정하면 `androidPreloadFiles` 대신 해당 목록을 사용합니다. (한 줄에 하나씩 경로를 적고, `#`로 시작하는 줄은 주석으로 무시됩니다.)
   - ZIP을 CDN, 개인 서버, GitHub Release 등에서 직접 내려받을 수 있는 URL로 업로드합니다.
   - 필요 시 버전 문자열을 바꿔 새 콘텐츠가 배포되면 다시 다운로드되도록 합니다.
5. **플랫폼별 주의사항**:
   - **Android**: 첫 실행에서 ZIP을 내려받으므로 네트워크 권한이 필요합니다. `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define은 `WebViewDefines`가 자동으로 추가합니다. HTTPS가 아니라면 네트워크 정책을 확인하세요.
   - **iOS**: `WebViewController.enableWKWebView` 옵션을 켜면 WKWebView가 활성화됩니다. App Transport Security 정책 때문에 HTTPS 사용을 권장합니다.
   - **WebGL**: `unity-webview-webgl-plugin.jslib`는 자동으로 포함됩니다. WebGL에서는 로컬 서버 대신 빌드된 페이지를 직접 호스팅하는 방식을 고려하세요.

## 문제 해결
- 웹뷰가 빈 화면인 경우 `LocalWebServer` 로그와 `RemoteWebContentInstaller` 설치 로그를 확인하세요.
- Android에서 HTTP 콘텐츠가 차단되면 `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define이 적용되었는지 확인하거나 HTTPS URL을 사용하세요.
- ZIP 구조가 올바르지 않으면 `RemoteWebContentInstaller`가 설치 폴더 바깥으로 파일을 추출하지 않고 경고를 남깁니다.
