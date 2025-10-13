# Basic Setup Sample

이 샘플은 `LocalWebServer`, `RemoteWebContentInstaller`, `WebViewController` 조합을 설정하는 방법을 설명합니다. (실제 웹 리소스는 포함되지 않습니다.)

## 사용 방법
1. Package Manager에서 `Import` 버튼을 눌러 샘플을 프로젝트로 복사합니다.
2. `Assets/Samples/Local WebView/1.0.0/BasicSetup` 폴더에 포함된 README를 참고해 씬에 컴포넌트를 배치합니다.
3. 웹 앱 빌드 결과를 ZIP으로 묶어 원격에 업로드한 뒤, `RemoteWebContentInstaller.downloadUrl`에 해당 링크를 입력합니다.
4. `LocalWebServer.routePrefix`와 `WebViewController.webRootPath`(`/flutter/`)가 일치하는지 확인합니다. 기본으로 `LocalWebServer.autoStartOnStart`와 `WebViewController.autoLoadOnStart`가 꺼져 있으므로 `RemoteWebContentInstaller`의 `autoStartServer`/`autoLoadWebViewOnInstall` 옵션을 사용하세요. 필요하면 인스펙터에서 `targetServer`와 `targetWebView`에 컴포넌트를 명시적으로 할당할 수 있습니다.
5. UI 버튼에 `WebContentDownloadButton`을 붙이면 사용자가 직접 “다운로드” 버튼을 눌러 ZIP을 강제로 재다운로드하고, 완료 후 버튼을 숨기거나 상태 메시지를 표시할 수 있습니다. 버튼을 사용할 때는 `RemoteWebContentInstaller.installOnStart`를 끈 상태로 두세요.
6. Android StreamingAssets를 활용한다면 ZIP 안에 `android-preload.txt` 같은 파일 목록을 함께 넣고 `LocalWebServer.androidPreloadListFile`에 지정하면 미리 읽어 둘 목록을 손쉽게 관리할 수 있습니다.

테스트용으로는 간단한 HTML 페이지를 ZIP으로 묶어 업로드한 뒤 URL을 지정하면 됩니다.
