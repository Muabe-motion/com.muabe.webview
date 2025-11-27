# Unity 2019.4 호환성 가이드

이 문서는 Unity 2019.4 LTS에서 `com.muabe.webview` 패키지를 사용할 때 알아야 할 호환성 정보와 주의사항을 설명합니다.

## ✅ 호환성 확인 완료

`com.muabe.webview` 패키지는 Unity 2019.4 LTS 이상에서 정상 작동하도록 설계되었습니다.

### 주요 호환성 수정 사항 (v1.0.8+)

1. **System.IO.Compression 스트리핑 방지**
   - `Runtime/link.xml` 파일 추가
   - IL2CPP 빌드(Android, iOS)에서 ZIP 압축 해제 기능 보장

2. **속성 문법 호환성**
   - `[SerializeField, HideInInspector]` → 별도 줄로 분리
   - Unity 2019.4의 Inspector 안정성 향상

3. **API 호환성**
   - Unity 버전별 조건부 컴파일 지시자 활용
   - 2019.4~2023.x 모든 버전에서 작동

## 🔧 프로젝트 설정

### 필수 설정

#### 1. .NET Standard 2.0 사용 (기본값)
```
Edit > Project Settings > Player > Other Settings > Api Compatibility Level
→ .NET Standard 2.0 (권장)
```

#### 2. IL2CPP 빌드 (Android/iOS)
- `link.xml` 파일이 자동으로 포함됨
- System.IO.Compression 어셈블리가 보존됨

### Android 설정

```
Edit > Project Settings > Player > Android > Other Settings
- Scripting Backend: IL2CPP (권장) 또는 Mono
- Target API Level: 29 이상 권장
- Minimum API Level: 24 (Android 7.0)
```

### iOS 설정

```
Edit > Project Settings > Player > iOS > Other Settings
- Target minimum iOS Version: 13.0 이상
- Enable WKWebView: 필수 (WebViewController 컴포넌트에서 설정)
```

## 🐛 알려진 제한사항

### 1. Android Mono 빌드 제한
- **증상**: Mono 스크립팅 백엔드에서 ZIP 압축 해제 실패 가능
- **해결책**: IL2CPP 사용 권장 (link.xml이 자동 적용됨)

### 2. WebGL 플랫폼
- 로컬 HTTP 서버는 WebGL에서 작동하지 않음 (브라우저 제한)
- WebView는 WebGL에서 제한적으로 지원 (iframe 기반)

### 3. Unity Editor OSX
- WebView는 Editor OSX에서 제한적으로 지원됨
- 실제 디바이스 테스트 권장

## 📋 버전별 API 차이

패키지는 자동으로 Unity 버전을 감지하고 적절한 API를 사용합니다:

| Unity 버전 | 사용 API | 비고 |
|-----------|---------|------|
| 2019.4 | `FindObjectOfType<T>()` | 기본 검색 |
| 2020.2+ | `Permission.RequestUserPermissions()` | 신규 권한 API |
| 2021.1+ | `forceBringToFront` (Android) | WebView 최상위 표시 |
| 2022.2+ | `FindFirstObjectByType<T>()` | 성능 개선 |

모든 버전에서 코드가 자동으로 적절한 API를 선택하므로, 사용자가 직접 수정할 필요 없습니다.

## 🔍 빌드 오류 해결

### "Type or namespace System.IO.Compression could not be found"

**원인**: IL2CPP 스트리핑으로 System.IO.Compression 어셈블리 제거

**해결책**:
1. `Runtime/link.xml` 파일 존재 확인
2. 프로젝트 재빌드
3. 여전히 문제 발생 시, 다음 내용으로 `Assets/link.xml` 생성:
   ```xml
   <linker>
     <assembly fullname="System.IO.Compression" preserve="all"/>
     <assembly fullname="System.IO.Compression.FileSystem" preserve="all"/>
   </linker>
   ```

### Android 빌드 실패: "Unhandled Exception: System.TypeLoadException"

**원인**: Mono 스크립팅 백엔드에서 Type 로딩 실패

**해결책**:
```
Player Settings > Android > Other Settings > Scripting Backend
→ IL2CPP로 변경
```

### iOS 빌드 실패: Framework 누락

**원인**: WebKit.framework가 프로젝트에 포함되지 않음

**해결책**:
- 패키지의 빌드 후처리가 자동으로 프레임워크 추가
- 수동 추가 필요 시: Xcode 프로젝트에서 `WebKit.framework` 추가

## 💡 성능 최적화 팁

### Unity 2019.4 권장 설정

1. **Managed Stripping Level**
   ```
   Player Settings > Other Settings > Managed Stripping Level
   → Low (권장) 또는 Medium
   ```
   - High/Aggressive는 link.xml에도 불구하고 문제 발생 가능

2. **Android IL2CPP**
   ```
   Player Settings > Android > Other Settings
   - ARM64: ✅ (필수)
   - ARMv7: 선택적 (하위 호환)
   ```

3. **Build Settings**
   - Development Build: 테스트 시 활성화
   - Script Debugging: 디버깅 필요 시만 활성화

## 🧪 테스트 체크리스트

Unity 2019.4에서 빌드 전 확인 사항:

- [ ] .NET Standard 2.0 사용 확인
- [ ] IL2CPP 스크립팅 백엔드 설정 (Android/iOS)
- [ ] `Runtime/link.xml` 파일 존재 확인
- [ ] Minimum API Level 24 이상 (Android)
- [ ] Target iOS Version 13.0 이상
- [ ] 실제 디바이스에서 ZIP 다운로드 및 압축 해제 테스트
- [ ] WebView 표시 및 로컬 서버 통신 테스트

## 📞 문제 발생 시

1. **Unity Console 로그 확인**
   - `[WebViewConstants.LogPrefix*]` 태그로 필터링

2. **디바이스 로그 확인**
   - Android: `adb logcat -s Unity`
   - iOS: Xcode > Window > Devices and Simulators > View Device Logs

3. **버전 정보 제공**
   - Unity 버전: 2019.4.x
   - 패키지 버전: package.json 확인
   - 플랫폼: Android/iOS
   - 스크립팅 백엔드: Mono/IL2CPP
   - 빌드 설정: Development/Release

## 🔄 업그레이드 가이드

Unity 2019.4에서 최신 버전으로 업그레이드 시:

1. **Unity 2020.x로 업그레이드**
   - 코드 변경 불필요
   - 새로운 Permission API 자동 사용

2. **Unity 2021.x로 업그레이드**
   - Android forceBringToFront 자동 활성화
   - 성능 향상

3. **Unity 2022.x로 업그레이드**
   - FindFirstObjectByType API 자동 사용
   - 추가 최적화

모든 업그레이드는 하위 호환성을 유지합니다.

---

**최종 업데이트**: 2024-11-27
**패키지 버전**: 1.0.8
**지원 Unity 버전**: 2019.4 LTS ~ 2023.x
