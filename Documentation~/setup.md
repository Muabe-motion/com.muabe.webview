# Muabe Interactive WebView 설치 및 설정 가이드

> **📖 전체 문서**: 아키텍처와 컴포넌트 상세 설명은
> [ARCHITECTURE.md](../ARCHITECTURE.md)를 참고하세요.

이 문서는 Muabe Interactive WebView 패키지를 프로젝트에 추가하고, 씬을 구성하며,
원격 웹 콘텐츠를 배포하는 전체 흐름을 단계별로 설명합니다.

## 📋 준비 사항

### 필수 요구사항

- **Unity**: 2019.4 LTS 이상
  - Unity 2019.4 사용 시 IL2CPP 빌드 권장 (상세:
    [UNITY_2019_COMPATIBILITY.md](../UNITY_2019_COMPATIBILITY.md))
- **플랫폼**: Android 7.0+ (API Level 24), iOS 13+
- **Android**:
  - Minimum API Level 24 이상
  - Target API Level 30 이상 권장
  - Scripting Backend: IL2CPP 권장 (ARM64 지원 필수)
- **iOS**:
  - Target minimum iOS Version 13.0 이상
  - WKWebView 활성화 필수
- **웹 앱**: Flutter, React, Vue 등으로 제작한 빌드 결과물
- **배포 서버**: HTTPS 지원 서버 (GitHub Release, AWS S3, CDN 등)

### 권장 사항

- Git 설치 (GitHub 패키지 사용 시)
- 기본 Unity UI 지식
- 웹 개발 경험 (Flutter/React 등)
- .NET Standard 2.0 API Compatibility Level

## 📦 설치 방법

### 방법 1: GitHub URL로 설치 (권장)

1. **Packages/manifest.json 편집**
   ```json
   {
      "dependencies": {
         "com.muabe.webview": "https://github.com/Muabe-motion/com.muabe.webview.git#v1.0.13"
      }
   }
   ```

2. **또는 Package Manager 사용**
   - Unity Editor에서 `Window > Package Manager` 열기
   - **+ > Add package from git URL...** 선택
   - 입력: `https://github.com/Muabe-motion/com.muabe.webview.git#v1.0.13`
   - **Add** 클릭

3. **설치 확인**
   - Package Manager 목록에 **Muabe Interactive WebView** 표시
   - 버전: 1.0.8

### 방법 2: 로컬 패키지로 설치

1. **저장소 클론**
   ```bash
   git clone https://github.com/Muabe-motion/com.muabe.webview.git
   ```

2. **Unity에서 추가**
   - `Window > Package Manager` 열기
   - **+ > Add package from disk...** 선택
   - `package.json` 파일 선택

### 설치 확인

패키지가 정상적으로 설치되면 다음 네임스페이스를 사용할 수 있습니다:

```csharp
using Muabe.WebView;  // 모든 컴포넌트 접근

// 사용 가능한 클래스들
- LocalWebServer
- WebContentDownloadManager
- WebViewController
- FlutterWebBridge
- WebViewConstants
- WebViewUtility
// ... 등
```

## 🧩 주요 컴포넌트 개요

### Core Components

#### `LocalWebServer`

- **역할**: 퍼시스턴트 데이터 또는 StreamingAssets를 로컬 HTTP 서버로 제공
- **주요 설정**: 포트, 라우트 접두사, 콘텐츠 소스
- **네임스페이스**: `Muabe.WebView`

#### `WebContentDownloadManager`

- **역할**: 원격 ZIP 다운로드, 압축 해제, 버전 관리
- **주요 설정**: 설치 폴더명, 콘텐츠 루트 서브폴더, 버전
- **네임스페이스**: `Muabe.WebView`

#### `WebViewController`

- **역할**: 웹뷰 초기화, URL 로드, 마진 관리
- **주요 설정**: 서버 포트, 웹 루트 경로, 오버레이 마진
- **네임스페이스**: `Muabe.WebView`

#### `FlutterWebBridge`

- **역할**: Unity ↔ Flutter 양방향 메시지 통신
- **주요 기능**: 위젯 표시/숨김 제어
- **네임스페이스**: `Muabe.WebView`

### UI Components

#### `WebContentDownloadButton`

- **역할**: 콘텐츠 다운로드 트리거 버튼
- **상속**: `WebViewButtonBase`
- **주요 설정**: 다운로드 URL, 버전, 상태 라벨

#### `WebContentLaunchButton`

- **역할**: 서버 시작 및 웹뷰 로드 버튼
- **상속**: `WebViewButtonBase`
- **주요 설정**: 콘텐츠 경로, 라우트 접두사

#### `FlutterWidgetButton`

- **역할**: Flutter 위젯 제어 버튼
- **주요 기능**: Toggle, Show, Hide, ForceValue 모드

### Utilities

- **`WebViewConstants`**: 모든 상수 통합 관리
- **`WebViewUtility`**: 15+ 공통 유틸리티 함수
- **`WebViewButtonBase`**: 버튼 베이스 클래스
- **`PermissionRequester`**: 런타임 권한 요청

## 🎮 씬 구성 단계

### Step 1: GameObject 생성 및 핵심 컴포넌트 추가

1. **GameObject 생성**
   - Hierarchy에서 우클릭 → Create Empty
   - 이름: `WebViewManager`

2. **컴포넌트 추가**
   ```
   Add Component 검색:
   - LocalWebServer
   - WebContentDownloadManager
   - WebViewController
   - FlutterWebBridge (선택사항)
   ```

3. **기본 설정**

   **LocalWebServer**:
   - `Port`: 8082 (또는 원하는 포트)
   - `Content Source`: Persistent Data Path

   **WebContentDownloadManager**:
   - `Install Folder Name`: webview-content
   - `Content Root Subfolder`: flutter (빈 칸으로 두면 나중에 설정)

   **WebViewController**:
   - `Server Port`: 8082 (LocalWebServer와 동일)
   - `Auto Load On Start`: false (버튼으로 제어)
   - `Enable WKWebView`: true (iOS 필수)
   - `Transparent`: true (웹뷰 배경 투명)
   - `Ignore Safe Area`: false (Safe Area 존중, true로 설정 시 전체 화면)

4. **코드로 생성 (선택사항)**
   ```csharp
   using Muabe.WebView;

   GameObject manager = new GameObject("WebViewManager");
   manager.AddComponent<LocalWebServer>();
   manager.AddComponent<WebContentDownloadManager>();
   manager.AddComponent<WebViewController>();
   manager.AddComponent<FlutterWebBridge>();
   DontDestroyOnLoad(manager);
   ```

### Step 2: UI 버튼 구성

#### 다운로드 버튼 생성

1. **UI Button 생성**
   - Hierarchy 우클릭 → UI → Button
   - 이름: `DownloadButton`

2. **컴포넌트 추가**
   - `Add Component` → `WebContentDownloadButton`

3. **Inspector 설정**
   ```
   WebContentDownloadButton:
   ├─ Installer: WebViewManager의 WebContentDownloadManager 드래그
   ├─ Launch Button: (다음에 만들 LaunchButton 드래그)
   └─ Download Input:
      ├─ Download Url: https://example.com/flutter-app.zip
      └─ Remote Version Override: 1.0.0

   Label Settings:
   ├─ Downloading Label: "다운로드 중..."
   ├─ Completed Label: "다운로드 완료"
   └─ Failed Label: "다운로드 실패"
   ```

#### 실행 버튼 생성

1. **UI Button 생성**
   - Hierarchy 우클릭 → UI → Button
   - 이름: `LaunchButton`

2. **컴포넌트 추가**
   - `Add Component` → `WebContentLaunchButton`

3. **Inspector 설정**
   ```
   WebContentLaunchButton:
   ├─ Installer: WebViewManager의 WebContentDownloadManager 드래그
   ├─ Target Server: WebViewManager의 LocalWebServer 드래그
   ├─ Target WebView: WebViewManager의 WebViewController 드래그
   └─ Path Input:
      ├─ Content Root Subfolder: flutter
      └─ Route Prefix: flutter

   Load Options:
   ├─ Configure Server On Load: true
   ├─ Start Server If Needed: true
   └─ Show WebView On Load: true
   ```

4. **DownloadButton에 참조 연결**
   - DownloadButton의 `Launch Button` 필드에 LaunchButton 드래그

### Step 3: 권한 설정 (Android/iOS)

**Android/iOS용 권한 요청**:

- WebViewManager에 `Add Component` → `PermissionRequester`
- Inspector 설정:
  - `Request Microphone`: true (필요 시)
  - `Request Camera`: true (필요 시)

> **💡 Unity 버전별 동작**:
>
> - **Unity 2020.2+**: `PermissionCallbacks`를 사용한 상세한 권한 결과 처리
>   (승인/거부/다시 묻지 않음)
> - **Unity 2019.4**: 기본 권한 요청 API 사용 (호환성 모드)

### Step 4: 테스트

1. **Play Mode 진입**
2. **DownloadButton 클릭** → ZIP 다운로드 및 설치
3. **LaunchButton 클릭** → 서버 시작 및 WebView 로드
4. **Console 확인**:
   ```
   [LocalWebServer] Local web server starting on http://localhost:8082
   [WebContentDownloadManager] Installation finished
   [WebViewController] InitializeWebView coroutine started
   [WebView] Loaded: http://localhost:8082/flutter/
   ```

### 자동 참조 기능

> 💡 **Tip**: 대부분의 컴포넌트는 동일 GameObject나 부모에서 자동으로 참조를
> 찾습니다. Inspector에서 `None`으로 표시되더라도, Play 시 `Auto-assigned`
> 로그가 나오면 정상입니다.

## 📁 웹 콘텐츠 준비 및 배포

### 1. 웹 앱 빌드

**Flutter 예시**:

```bash
cd your-flutter-project
flutter build web
# 결과물: build/web/
```

**React 예시**:

```bash
cd your-react-project
npm run build
# 결과물: build/
```

**Vue 예시**:

```bash
cd your-vue-project
npm run build
# 결과물: dist/
```

### 2. ZIP 압축

**중요**: 폴더 이름이 `contentRootSubfolder` 설정과 일치해야 합니다!

**올바른 구조**:

```
flutter-app.zip
└── flutter/              ← contentRootSubfolder
    ├── index.html
    ├── main.dart.js
    ├── assets/
    └── ...
```

**압축 명령어**:

```bash
# Flutter
cd build
zip -r flutter-app.zip web/
# web 폴더를 flutter로 이름 변경 후:
mv web flutter
zip -r flutter-app.zip flutter/

# React/Vue
cd build  # 또는 dist
mv build flutter  # 폴더명을 flutter로 변경
zip -r flutter-app.zip flutter/
```

### 3. Android StreamingAssets 프리로드 (선택사항)

**android-preload.txt 생성**:

```
# 프리로드할 파일 목록
flutter/index.html
flutter/main.dart.js
flutter/assets/icon.png
# 주석은 # 으로 시작
```

ZIP에 포함:

```bash
zip -u flutter-app.zip android-preload.txt
```

### 4. 서버 업로드

**옵션 1: GitHub Release**

```bash
gh release create v1.0.0 flutter-app.zip
# URL: https://github.com/user/repo/releases/download/v1.0.0/flutter-app.zip
```

**옵션 2: AWS S3**

```bash
aws s3 cp flutter-app.zip s3://your-bucket/flutter-app.zip --acl public-read
# URL: https://your-bucket.s3.amazonaws.com/flutter-app.zip
```

**옵션 3: 자체 서버**

- HTTPS 지원 필수
- CORS 헤더 설정 권장

### 5. Unity 설정 업데이트

**WebContentDownloadButton Inspector**:

- `Download Url`: `https://your-server.com/flutter-app.zip`
- `Remote Version Override`: `1.0.0`

**WebContentLaunchButton Inspector**:

- `Content Root Subfolder`: `flutter`
- `Route Prefix`: `flutter`

### 6. 버전 업데이트

새 버전 배포 시:

1. 웹 앱 재빌드
2. ZIP 압축 (동일한 폴더 구조)
3. 서버 업로드
4. Unity에서 `Remote Version Override` 증가 (예: `1.0.0` → `1.0.1`)
5. 빌드 및 배포

> ✅ **버전만 바꾸면 자동으로 새 ZIP을 다운로드합니다!**

## 고급 설정 팁

### 개발 및 디버깅

- **캐시 초기화**: 개발 중 변경 사항을 바로 반영하려면
  `WebContentDownloadButton`의 `remoteVersion` 값을 증가시키거나
  `Force Download Every Time`을 켰다가 배포 시 끕니다.
- **로그 활성화**:
  - `FlutterWebBridge`의 `Enable Debug Logs` 체크
  - Console에서 `[WebView*]` 태그로 필터링
- **Safe Area 테스트**: 다양한 디바이스에서 `Ignore Safe Area` 옵션 테스트

### 성능 최적화

- **타임아웃 조정**: 대용량 ZIP을 다룰 때 `timeoutSeconds`, `maxRedirects` 값을
  조정해 네트워크 안정성을 확보합니다.
- **StreamingAssets 프리로드**: Android에서 `android-preload.txt`로 자주
  사용하는 리소스를 미리 패키징
- **IL2CPP 빌드**: Unity 2019.4에서는 IL2CPP 사용 시 성능과 안정성 향상

### 커스터마이징

- **설치 위치 변경**: `installFolderName`을 환경별로 다르게 두고 싶다면
  스크립트를 확장해 `Application.persistentDataPath` 하위 다른 경로를 사용할 수
  있습니다.
- **커스텀 이벤트**: 설치 완료, 실패, 서버 시작 등을 UnityEvent로 제공하므로
  다른 로직과 연결해 후속 처리를 자동화하세요.
- **Unity ↔ Web 통신 확장**: `FlutterWebBridge`를 확장하여 커스텀 메시지 타입
  추가 가능

## 플랫폼별 체크리스트

### Android

- **필수 설정**:
  - Minimum API Level: 24 이상
  - Target API Level: 30 이상 권장
  - Scripting Backend: IL2CPP (ARM64 지원 필수)
  - Target Architectures: ARM64 체크 ✅
- **권한 설정**:
  - 필요한 권한(카메라, 마이크 등)을 `PermissionRequester`로 요청하거나 Android
    Manifest에 직접 추가
  - `UNITYWEBVIEW_ANDROID_USES_CLEARTEXT_TRAFFIC` define이 자동 적용됨 (HTTP
    사용 시)
- **주의사항**:
  - 대용량 ZIP은 첫 실행에서 다운로드되므로 사용자 안내 UI 준비 권장
  - debug.keystore 손상 시 백업 후 재생성 필요

### iOS

- **필수 설정**:
  - Target minimum iOS Version: 13.0 이상
  - `WebViewController`에서 `Enable WKWebView` 활성화 ✅
- **HTTP 콘텐츠 사용 시**:
  - `Edit > Project Settings > Player > iOS > Other Settings > Configuration`
  - **Allow downloads over HTTP** → **Always allowed** 설정
- **주의사항**:
  - 백그라운드 다운로드가 필요한 경우 `WebContentDownloadManager` 확장 고려
  - WKWebView는 iOS 13+ 필수

### Unity 2019.4 LTS 사용자

- **권장 설정**:
  - Scripting Backend: IL2CPP
  - API Compatibility Level: .NET Standard 2.0
  - Managed Stripping Level: Low 또는 Medium
- **주의사항**:
  - `Runtime/link.xml`이 포함되어 System.IO.Compression 보존
  - `PermissionRequester`는 Unity 버전별로 자동 최적화됨
- **상세 가이드**: [UNITY_2019_COMPATIBILITY.md](../UNITY_2019_COMPATIBILITY.md)
  참고

## 문제 해결

### 일반 문제

- **웹뷰가 빈 화면**: 콘솔에서 `LocalWebServer` 로그로 서버가 실행 중인지
  확인하고, `WebViewController`가 올바른 포트와 라우트를 사용하고 있는지
  검토하세요.
- **ZIP 설치 실패**: ZIP 내부 폴더 구조가 `contentRootSubfolder`로 지정한 값과
  일치하는지 확인하고, 압축이 깨지지 않았는지 다시 업로드합니다.
- **HTTP 차단**: Android는 HTTP가 막힐 수 있으므로 HTTPS로 전환하거나 네트워크
  보안 설정을 수정합니다. iOS는 ATS 정책을 확인하세요.
- **캐시된 오래된 파일**: `WebContentDownloadButton`의 `remoteVersion` 값을
  증가시키거나 퍼시스턴트 폴더에서 기존 설치 폴더를 삭제한 뒤 재실행합니다.
- **권한 거부**: `PermissionRequester` 이벤트를 활용해 권한이 거부되었을 때 대체
  UI를 표시합니다.

### Android 빌드 문제

#### "Invalid keystore format" 에러

**증상**:

```
Failed to read key AndroidDebugKey from store "/Users/[username]/.android/debug.keystore": Invalid keystore format
```

**해결 방법**:

```bash
# 손상된 debug.keystore 백업
mv ~/.android/debug.keystore ~/.android/debug.keystore.backup

# Unity에서 다시 빌드하면 새 keystore가 자동 생성됨
```

#### "You are trying to install ARMv7 APK to ARM64 device" 에러

**증상**: ARM64 디바이스에 ARMv7 빌드 설치 실패

**해결 방법**:

1. `Edit > Project Settings > Player > Android > Other Settings`
2. `Scripting Backend`: **IL2CPP**로 변경
3. `Target Architectures`: **ARM64** 체크 ✅ (ARMv7은 선택사항)

또는 `ProjectSettings/ProjectSettings.asset` 직접 수정:

```yaml
AndroidTargetArchitectures: 3 # ARMv7(1) + ARM64(2) = 3
scriptingBackend:
   Android: 1 # IL2CPP
```

#### "adb: device not found" 에러

**해결 방법**:

```bash
# adb daemon 재시작
adb kill-server
adb start-server
adb devices  # 디바이스 확인
```

### Safe Area 및 화면 표시 문제

#### Safe Area 밖이 투명해서 Unity 화면이 보임

**해결 방법 1 (권장)**: WebView를 전체 화면으로 확장

- `WebViewController` Inspector에서 `Ignore Safe Area` ✅ 체크

**해결 방법 2**: Unity 카메라 배경색 변경

- Main Camera > Background 색상을 웹뷰 배경과 동일하게 설정

**해결 방법 3**: UI로 Safe Area 밖 가리기

- Canvas에 Panel 추가하여 Safe Area 밖 영역을 원하는 색으로 채움

### Unity 2019.4 호환성 문제

#### "Type or namespace 'PermissionCallbacks' could not be found" 컴파일 에러

**원인**: Unity 2019.4에서 Unity 2020.2+ 전용 API 사용

**해결 방법**:

- 패키지를 최신 버전(1.0.8+)으로 업데이트
- 패키지에 Unity 버전별 조건부 컴파일이 적용되어 있음

#### IL2CPP 빌드에서 "System.IO.Compression could not be found" 에러

**해결 방법**:

1. `Runtime/link.xml` 파일 존재 확인 (패키지에 포함됨)
2. 여전히 문제 발생 시 `Assets/link.xml` 생성:

```xml
<linker>
  <assembly fullname="System.IO.Compression" preserve="all"/>
  <assembly fullname="System.IO.Compression.FileSystem" preserve="all"/>
</linker>
```

> 📖 **상세 가이드**: Unity 2019.4 관련 자세한 내용은
> [UNITY_2019_COMPATIBILITY.md](../UNITY_2019_COMPATIBILITY.md) 참고

## 📚 추가 자료

- **[ARCHITECTURE.md](../ARCHITECTURE.md)**: 전체 아키텍처 및 컴포넌트 상세 설명
- **[README.md](../README.md)**: 패키지 개요 및 빠른 시작
- **[UNITY_2019_COMPATIBILITY.md](../UNITY_2019_COMPATIBILITY.md)**: Unity
  2019.4 LTS 호환성 가이드
- **[WEBVIEW_SETUP_GUIDE.md](../WEBVIEW_SETUP_GUIDE.md)**: 상세한 단계별 설정
  가이드
- **[LICENSE](../LICENSE)**: Apache License 2.0

## 💬 지원

문제가 발생하거나 질문이 있으면:

- **Email**: dev@muabe.com
- **Website**: https://www.muabe.com/

---

**Made with ❤️ by Muabe Motion**
