# Muabe Interactive WebView 설치 및 설정 가이드

이 문서는 Muabe Interactive WebView 패키지를 프로젝트에 추가하고, 씬을 구성하며, 원격 웹 콘텐츠를 배포하는 전체 흐름을 설명합니다.

## 준비 사항
- Unity 2021.3 LTS 이상
- 지원 플랫폼: Android 7.0+, iOS 13+, WebGL
- 빌드할 웹 애플리케이션(Flutter, React, Vue 등)과 ZIP 압축 도구
- HTTPS에서 접근 가능한 다운로드 서버 (GitHub Release, S3, 자체 서버 등)

## 설치 방법

### GitHub 패키지 의존성 추가
1. 프로젝트의 `Packages/manifest.json` 파일에 다음 의존성을 추가합니다. (현재 배포 태그는 `Release-1.0.0`입니다.)
   ```json
   {
     "dependencies": {
       "com.muabe.webview": "https://github.com/Muabe-motion/com.muabe.webview.git#Release-1.0.0"
     }
   }
   ```
2. Unity를 다시 열거나 `Window > Package Manager`에서 **+ > Add package from git URL...**을 선택해 동일한 주소를 입력합니다. 태그 이름이 정확히 `Release-1.0.0`인지 확인하세요.
3. Package Manager 목록에 **Muabe Interactive WebView**가 표시되면 설치가 완료된 것입니다.

### 로컬 패키지로 추가
1. 이 저장소를 프로젝트 외부 또는 `Packages/` 아래에 클론합니다.
   ```
   git clone https://github.com/Muabe-motion/com.muabe.webview.git
   ```
2. Unity에서 `Window > Package Manager`를 열고 **+ > Add package from disk...**를 선택한 뒤 클론한 폴더의 `package.json`을 지정합니다.

## 패키지 구성 요소
- `LocalWebServer`  
  퍼시스턴트 데이터 또는 StreamingAssets를 로컬 HTTP 서버로 노출합니다. 포트와 라우트 접두어를 지정할 수 있으며, 자동 시작 여부와 콘텐츠 소스를 설정할 수 있습니다.
- `WebContentDownloadManager`  
  원격 ZIP을 다운로드해 압축을 해제하고 버전 캐시를 관리합니다.
- `WebContentLaunchButton`  
  설치된 콘텐츠를 서버와 웹뷰에 적용하는 UI 버튼입니다.
- `WebViewController`  
  웹뷰를 초기화하고 `http://127.0.0.1:{port}/{routePrefix}`와 같은 URL을 로드합니다. 서버 상태를 감지해 재로딩을 처리합니다.
- `WebContentDownloadButton`  
  UI 버튼에서 콘텐츠 다운로드/설치를 트리거합니다. 진행 메시지 출력을 지원합니다.
- `PermissionRequester`  
  Android/iOS에서 카메라, 마이크 등 네이티브 권한을 요청합니다.

## 씬 구성 절차 (요약)
1. **기본 오브젝트 준비**  
   - 빈 GameObject에 `LocalWebServer`, `WebContentDownloadManager`, `WebViewController`를 함께 붙입니다.  
   - 서버 포트, 라우트 이름, 설치 폴더(`installFolderName`) 정도만 지정하면 됩니다.

2. **다운로드 버튼**  
   - UI 버튼에 `WebContentDownloadButton`을 붙이고 위 오브젝트의 컴포넌트와 `WebContentLaunchButton`을 참조로 연결합니다.  
   - Inspector에서 `downloadUrl`, `remoteVersion`을 입력하면 됩니다. 나머지 경로 관련 값은 로드 버튼에서 관리합니다.  
   - `statusText`/`buttonLabel` 필드에 UI 텍스트를 연결해 진행 상황을 표시할 수 있습니다.

3. **로드 버튼**  
   - 다른 UI 버튼에 `WebContentLaunchButton`을 붙이고 동일한 컴포넌트들을 연결합니다.  
   - Inspector에서 다음 값을 입력하면 서버와 웹뷰가 동시에 갱신됩니다.  
     - `contentRootSubfolder`: ZIP 내부에서 실제 웹 앱이 들어 있는 하위 폴더 이름 (예: `flutter`, `build/web`).  
     - `routePrefix`: 로컬 서버에서 제공할 URL 경로 접두사. 최종 주소는 `http://localhost:포트/routePrefix/` 형태가 되며, 웹뷰 루트 경로도 동일한 값으로 자동 설정됩니다(예: `/routePrefix/`).  

4. **선택 항목**  
   - 권한 안내가 필요하면 `PermissionRequester`를 추가합니다.  
   - StreamingAssets 사전 로드가 필요하면 `LocalWebServer`의 Android 설정을 확인하세요.

5. **플레이 테스트**  
   - 플레이 모드에서 다운로드 버튼을 눌러 ZIP을 설치한 뒤, 로드 버튼으로 서버를 실행하고 웹뷰를 표시합니다.  
   - 콘솔 로그로 설치 경로와 서버 상태를 확인할 수 있습니다.

## 원격 콘텐츠 준비
1. 웹 애플리케이션을 빌드합니다. (예: Flutter `flutter build web`, React `npm run build`)
2. 빌드 결과 폴더가 `contentRootSubfolder`와 동일한 이름이 되도록 정리합니다.
3. 폴더 전체를 ZIP으로 압축합니다.
4. (선택) Android에서 StreamingAssets를 선로딩하려면 ZIP 안에 `android-preload.txt` 파일을 만들어 줄마다 사전 로드할 상대 경로를 기록합니다. `#`으로 시작하는 줄은 주석으로 무시됩니다.
5. ZIP을 CDN, GitHub Release, 사내 스토리지 등 HTTPS로 제공 가능한 위치에 업로드합니다.
6. Unity 인스펙터에서 `WebContentDownloadButton`의 `downloadUrl`을 업로드한 주소로, `remoteVersion`을 배포 버전과 동일한 문자열로 입력합니다. 경로 관련 값은 `WebContentLaunchButton`에서 설정합니다.

## 고급 설정 팁
- **캐시 초기화**: 개발 중 변경 사항을 바로 반영하려면 `WebContentDownloadButton`의 `remoteVersion` 값을 증가시키거나 `Force Download Every Time`을 켰다가 배포 시 끕니다.
- **타임아웃 조정**: 대용량 ZIP을 다룰 때 `timeoutSeconds`, `maxRedirects`, `maxRetries`(필요 시 스크립트 확장) 값을 조정해 네트워크 안정성을 확보합니다.
- **설치 위치 변경**: `installFolderName`을 환경별로 다르게 두고 싶다면 스크립트를 확장해 `Application.persistentDataPath` 하위 다른 경로를 사용할 수 있습니다.
- **커스텀 이벤트**: 설치 완료, 실패, 서버 시작 등을 UnityEvent로 제공하므로 다른 로직과 연결해 후속 처리를 자동화하세요.

## 플랫폼별 체크리스트
- **Android**
  - `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define이 자동 적용됩니다. HTTP를 사용할 경우 네트워크 보안 구성을 확인하세요.
  - 필요한 권한(저장소, 카메라 등)을 `PermissionRequester`로 요청하거나 Android Manifest에 직접 추가합니다.
  - 대용량 ZIP은 첫 실행에서 다운로드되므로 사용자 안내 UI를 준비하세요.
- **iOS**
  - `enableWKWebView`를 활성화해 최신 WKWebView를 사용합니다.
  - App Transport Security(ATS) 정책에 맞춰 HTTPS URL을 사용하거나 Info.plist 예외를 등록합니다.
  - 백그라운드 다운로드가 필요한 경우 `WebContentDownloadManager`를 확장하거나 네이티브 코루틴을 고려하세요.
- **WebGL**
  - `unity-webview-webgl-plugin.jslib`가 자동 포함됩니다.
  - WebGL에서는 로컬 HTTP 서버를 사용할 수 없으므로, `WebContentDownloadManager` 대신 직접 호스팅 경로를 로드하는 구성을 고려해야 합니다.
  - CORS 정책 때문에 CDN 설정을 사전에 확인하세요.

## 문제 해결
- **웹뷰가 빈 화면**: 콘솔에서 `LocalWebServer` 로그로 서버가 실행 중인지 확인하고, `WebViewController`가 올바른 포트와 라우트를 사용하고 있는지 검토하세요.
- **ZIP 설치 실패**: ZIP 내부 폴더 구조가 `contentRootSubfolder`로 지정한 값과 일치하는지 확인하고, 압축이 깨지지 않았는지 다시 업로드합니다.
- **HTTP 차단**: Android는 HTTP가 막힐 수 있으므로 HTTPS로 전환하거나 네트워크 보안 설정을 수정합니다. iOS는 ATS 정책을 확인하세요.
- **캐시된 오래된 파일**: `WebContentDownloadButton`의 `remoteVersion` 값을 증가시키거나 퍼시스턴트 폴더에서 기존 설치 폴더를 삭제한 뒤 재실행합니다.
- **권한 거부**: `PermissionRequester` 이벤트를 활용해 권한이 거부되었을 때 대체 UI를 표시합니다.

## 관련 자료
- 패키지 루트의 `README.md`에서는 기능 개요와 사용 흐름을 요약하고 있습니다.
