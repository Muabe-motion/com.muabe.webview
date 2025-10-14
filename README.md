# Muabe Interactive WebView

Muabe Interactive WebView 패키지는 Unity 프로젝트에서 네이티브 웹뷰, 로컬 웹 서버, 원격 콘텐츠 배포 흐름을 한 번에 구성할 수 있도록 도와줍니다. 하나의 패키지로 Android, iOS, WebGL 환경에서 동일한 워크플로를 유지하면서 Flutter·React 등으로 제작한 웹 앱을 손쉽게 배포할 수 있습니다.

## 주요 기능
- `gree/unity-webview` 기반 커스텀 `WebViewObject` 프리팹 및 네이티브 플러그인 제공
- `LocalWebServer`로 퍼시스턴트 스토리지에 있는 정적 파일을 로컬 HTTP 서버로 노출
- `WebContentDownloadManager`가 원격 ZIP 콘텐츠를 다운로드·검증·설치하고 버전 관리
- `WebContentDownloadButton`, `PermissionRequester` 등 UI/권한 유틸리티 포함
- Editor 확장으로 스크립팅 define 설정과 빌드 후 처리 자동화

## 지원 환경
- Unity 2021.3 이상
- 플랫폼: Android 7.0+, iOS 13+, WebGL
- 의존성: [unity-webview](https://github.com/gree/unity-webview) (패키지에 포함)

## GitHub에서 설치
프로젝트의 `Packages/manifest.json`에 Git URL을 추가하면 바로 사용할 수 있습니다. 태그를 지정해 안정된 버전을 고정하는 것을 권장합니다.

```json
{
  "dependencies": {
    "com.muabe.webview": "https://github.com/Muabe-motion/com.muabe.webview.git#Release-1.0.0"
  }
}
```

Unity 에디터에서는 `Window > Package Manager`를 열고 **+ > Add package from git URL...**을 선택해 동일한 주소를 입력하면 됩니다. 현재 저장소에는 `Release-1.0.0` 태그가 배포 버전으로 등록되어 있으므로 정확한 태그 이름을 사용하세요. 특정 브랜치나 커밋을 사용하고 싶다면 `#branch-name`, `#commit-hash`를 뒤에 붙여 주세요.

로컬 패키지로 쓰고 싶다면 이 저장소를 클론한 뒤 `Packages/com.muabe.webview` 경로를 선택해 `Add package from disk...`를 실행하면 됩니다.

## 패키지 구성
- `Runtime/LocalWebServer` : 퍼시스턴트 폴더 또는 StreamingAssets를 호스팅하는 경량 HTTP 서버
- `Runtime/WebContentDownloadManager` : ZIP 아카이브 다운로드, 버전 및 캐시 관리
- `Runtime/WebContentLaunchButton` : 설치된 콘텐츠를 서버/웹뷰에 적용하는 UI 버튼
- `Runtime/WebViewController` : 웹뷰 초기 URL 로드, 서버 상태 감지, 메시지 브리지
- `Runtime/UI/WebContentDownloadButton` : UI 버튼으로 설치/갱신 이벤트 트리거
- `Runtime/PermissionRequester` : 카메라·마이크 등 런타임 권한 요청
- `Editor` 스크립트 : Android defines 관리와 빌드 파이프라인 훅 제공

각 구성 요소의 필드는 동일 GameObject 내 다른 컴포넌트를 자동으로 참조하지만, Inspector에서 직접 연결해 명시적으로 구성할 수 있습니다.

## 빠른 시작
자세한 단계별 설명은 [설치 및 설정 가이드](Documentation~/setup.md)에서 확인할 수 있습니다. 아래 순서만 따라도 기본 구성이 완료됩니다.
1. **기본 컴포넌트 배치** – 하나의 GameObject에 `LocalWebServer`, `WebContentDownloadManager`, `WebViewController`를 붙입니다. 서버 포트와 설치 폴더(`installFolderName`) 정도만 설정하면 됩니다.
2. **다운로드 버튼** – UI 버튼에 `WebContentDownloadButton`을 붙이고 위 GameObject의 컴포넌트들을 연결합니다. Inspector에서 `downloadUrl`, `remoteVersion`만 입력하면 나머지 값은 자동으로 관리됩니다.
3. **로드 버튼** – 다른 UI 버튼에 `WebContentLaunchButton`을 붙이고 동일한 컴포넌트들을 연결합니다. 아래 값들을 Inspector에서 입력하면 서버·웹뷰가 함께 갱신됩니다:
   - `contentRootSubfolder`: ZIP 내부에서 실제 웹 앱이 위치한 폴더 이름 (예: `flutter`, `build/web`).
   - `routePrefix`: 로컬 서버가 제공할 URL 경로 접두사. `http://localhost:포트/routePrefix/` 형태로 노출되며, 웹뷰 루트 경로도 동일한 값으로 자동 구성됩니다(예: `/routePrefix/`).
4. **실행** – 플레이 모드에서 다운로드 버튼을 눌러 ZIP을 설치한 뒤, 로드 버튼으로 로컬 서버를 기동하고 웹뷰를 노출합니다. 권한 안내가 필요하면 `PermissionRequester`를 추가하세요.

## 원격 콘텐츠 배포 워크플로
1. Flutter·React·Vue 등으로 제작한 웹 앱을 빌드한 다음 결과물을 ZIP으로 압축합니다. ZIP 루트 폴더 이름은 `contentRootSubfolder` 값과 일치해야 합니다.
2. Android에서 StreamingAssets를 사용할 경우 `android-preload.txt` 파일에 미리 패키징할 리소스를 한 줄씩 작성하고 `LocalWebServer.androidPreloadListFile`에 경로를 지정합니다. 주석은 `#`으로 시작합니다.
3. ZIP 파일을 HTTPS CDN, GitHub Release, 사내 서버 등에서 다운로드할 수 있도록 업로드합니다.
4. `WebContentDownloadButton` Inspector에서 `downloadUrl`과 `remoteVersion` 값을 입력하고, 업데이트 시 버전을 변경해 재다운로드를 트리거합니다.
5. 서버 응답이 큰 경우 `timeoutSeconds`와 `maxRedirects` 값을 조정해 안정성을 확보하세요.

## 플랫폼별 체크리스트
- **Android**: `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define이 자동으로 추가됩니다. HTTP를 사용하면 네트워크 정책을 확인하고, 추가 권한이 필요하면 `PermissionRequester`를 활용하세요.
- **iOS**: `enableWKWebView` 옵션을 켜면 WKWebView가 활성화됩니다. ATS 정책에 맞춰 HTTPS 콘텐츠를 사용하세요.
- **WebGL**: `unity-webview-webgl-plugin.jslib`가 자동 포함됩니다. WebGL 빌드는 로컬 서버 대신 호스팅 환경에 맞춰 정적 파일을 직접 서빙하는 방식도 고려할 수 있습니다.

## 문제 해결
- 웹뷰가 빈 화면일 경우: `LocalWebServer` 로그와 `WebContentDownloadManager`의 설치 로그(에디터 콘솔)를 확인하고 포트/라우트가 일치하는지 점검하세요.
- ZIP 구조 오류: 폴더 이름이 `contentRootSubfolder`와 다르면 설치되지 않습니다. 압축을 풀어 경로를 다시 확인하세요.
- Android에서 HTTP 요청 차단: HTTPS URL을 사용하거나 네트워크 보안 정책(네트워크 보안 구성)을 조정하세요.
- 캐시 무효화: 새 버전을 강제로 받으려면 `WebContentDownloadButton`의 `remoteVersion` 값을 증가시키거나 `Force Download Every Time` 옵션을 활성화합니다.

## 추가 문서
- [설치 및 설정 가이드](Documentation~/setup.md)

## 라이선스
이 패키지는 [Apache License 2.0](LICENSE) 하에 배포됩니다.
