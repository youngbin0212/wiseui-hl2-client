# srt3d_uwp — 빌드 절차

`Assets/Plugins/WSA/ARM64/srt3d_uwp.dll` 을 소스에서 재빌드하기 위한 문서.

---

## ⏱️ 시작하기 전에

> ### ⚠️ **OpenCV UWP ARM64 정적 빌드가 전체 과정의 대부분을 차지한다.**
>
> 이 저장소의 `srt3d_uwp` 자체는 소스가 23개 파일(약 260KB)이라 금방 끝난다.
> 시간을 잡아먹는 것은 **사전 준비물인 OpenCV** 다. OpenCV 를 UWP/WindowsStore ARM64
> 정적 라이브러리로 **직접 빌드해야 하며**, 이건 한 번만 하면 되지만 오래 걸린다.
> 처음이라면 OpenCV 빌드를 먼저 걸어두고 다른 일을 하는 편이 낫다.

| 단계 | 소요 | 빈도 |
|---|---|---|
| OpenCV 4.11.0 UWP ARM64 정적 빌드 | **[확인 필요]** — 실측값 미기록. **여기가 가장 오래 걸린다** | 최초 1회 |
| Eigen 3.4.0 다운로드 (FetchContent) | 약 9초 (네트워크 의존) | 최초 1회 |
| `srt3d_uwp` configure | 14.0초 (다운로드 포함) / 5.1초 (Eigen 로컬 사본) | 매번 |
| `srt3d_uwp` 빌드 (Release) | 약 17초 | 매번 |
| **`build_uwp_arm64.ps1` 전체** (clean, 다운로드 포함) | **31.1초** | — |

> **측정 환경 (2026-08-03).**
> Intel Core i9-10980XE (18C/36T, 3.00GHz), RAM 64GB,
> Windows 11 Pro 10.0.26100, VS2022 v17 (MSVC 14.44.35207), CMake 4.2.0.
> 빌드 디렉터리를 완전히 삭제한 clean 상태에서 `-—parallel` 로 측정.
> Eigen 다운로드 9초는 `configure 14.0초 − 로컬사본 configure 5.1초` 로 산출한 값이며
> 네트워크 상태에 따라 크게 달라진다.
>
> OpenCV 빌드 시간만 **[확인 필요]** 로 남아 있다 — 이번 검증에서 재빌드하지 않았고
> 문서에도 기록이 없어 추정하지 않았다. 실제로 돌려본 뒤 채울 것.

---

## 1. 필요 도구

| 도구 | 버전 | 비고 |
|---|---|---|
| **Visual Studio 2022** | v17 (MSVC 14.4x) | **UWP 워크로드 + ARM64 툴셋** 필수 |
| **CMake** | **4.2.0 에서 검증** | 3.18 이상이면 동작해야 하나 검증한 건 4.2.0 |
| **Git** | — | Eigen FetchContent 가 사용 |
| Windows SDK | 10.0.26100.0 에서 검증 | VS 설치에 포함 |

---

## 2. 의존성

### 2.1 OpenCV 4.11.0 — ⚠️ 직접 빌드해야 함

**`core` 와 `imgproc` 2개 모듈만**, WindowsStore ARM64 **정적** 빌드가 필요하다.
NuGet/공식 배포판에는 이 구성이 없다.

핵심 CMake 옵션 (`docs/SRT3D_INTERFACE.md` §2.4 요약):

```powershell
cmake -S <opencv-src> -B <opencv-build> -G "Visual Studio 17 2022" -A ARM64 `
  -DCMAKE_SYSTEM_NAME=WindowsStore -DCMAKE_SYSTEM_VERSION="10.0" `
  -DBUILD_LIST="core,imgproc" `
  -DBUILD_SHARED_LIBS=OFF -DBUILD_WITH_STATIC_CRT=OFF `
  -DWITH_JPEG=OFF -DWITH_PNG=OFF -DWITH_TIFF=OFF    # imgcodecs 계열 전부 OFF
```

옵션별 이유 4가지:

- `CMAKE_SYSTEM_NAME=WindowsStore` — **필수.** UWP 금지 API 를 피한다.
- `BUILD_WITH_STATIC_CRT=OFF` — **필수.** Unity/IL2CPP 가 동적 CRT 를 쓰므로 맞춰야 한다.
- imgcodecs 계열 OFF — `srt3d_uwp` 는 raw RGB 만 받고 `cv::imdecode` 를 쓰지 않는다
  (`MODIFICATIONS.md` §2.1). JPEG 입력으로 되돌리려면 이 전제가 깨진다.
- 결과적으로 `features2d` 가 없다 → M3T 의 texture 모달리티는 이 빌드로 불가.

> 📖 **전체 맥락은 `docs/SRT3D_INTERFACE.md` §2.4 를 볼 것.**
> OpenCV 쪽 빌드 절차 전문은 여기 옮기지 않는다 — 이 저장소가 관리하는 대상이 아니라
> 중복 유지보수 부담만 늘기 때문이다.

#### 빌드 산출 경로를 넘기는 방법

> ⚠️ **install 루트가 아니라 arch/config 폴더를 직접 가리켜야 한다.**
> 루트 dispatcher 인 `OpenCVConfig.cmake` 는 static 구성을 인식하지 못한다.
> ```
> ❌ <install>
> ✅ <install>\ARM64\vc17\staticlib
> ```

우선순위 (위가 이김):

1. 스크립트 파라미터 — `.\build_uwp_arm64.ps1 -OpenCvDir "<install>\ARM64\vc17\staticlib"`
2. 환경변수 — `$env:SRT3D_OPENCV_DIR = "<install>\ARM64\vc17\staticlib"`
3. CMake 에 직접 — `cmake -DOpenCV_DIR="<install>/ARM64/vc17/staticlib" ...`
4. (fallback) 스크립트 기준 `..\..\..\opencv_uwp\install\ARM64\vc17\staticlib`

경로를 못 찾으면 즉시 해결법을 담은 에러가 난다.

### 2.2 Eigen 3.4.0 — ✅ 준비 불필요 (자동)

CMake `FetchContent` 가 `GIT_TAG 3.4.0` 으로 고정해 받는다. **아무것도 준비하지 않아도 된다.**
빌드 디렉터리 `_deps/eigen-src` 에 받아지므로 저장소는 오염되지 않는다.

헤더만 쓰므로 Eigen 의 CMake 프로젝트는 `add_subdirectory` 하지 않는다
(`SOURCE_SUBDIR` 우회 — 이유는 `CMakeLists.txt` 주석 참조).

**오프라인 빌드나 기존 사본 재사용**이 필요하면 우선순위 (위가 이김):

1. `.\build_uwp_arm64.ps1 -EigenDir "<eigen-root>"`
2. `cmake -DSRT3D_EIGEN_DIR="<eigen-root>" ...`
3. `$env:SRT3D_EIGEN_DIR = "<eigen-root>"`
4. `third_party/eigen3/` 에 배치 (있으면 자동 감지)
5. 위 어느 것도 없으면 → FetchContent 다운로드

`<eigen-root>` 는 **`Eigen/` 과 `unsupported/` 를 담은 폴더**다. 실사용 헤더는 3개뿐:
`<Eigen/Dense>`, `<Eigen/Geometry>`, `<unsupported/Eigen/MatrixFunctions>`.

### 2.3 tiny_obj_loader — ✅ 준비 불필요 (편입됨)

`third_party/tiny_obj_loader/tiny_obj_loader.h` 로 이 저장소에 포함돼 있다. 단일 헤더 80KB.

---

## 3. 빌드

```powershell
cd native\srt3d_uwp
powershell -ExecutionPolicy Bypass -File .\build_uwp_arm64.ps1 `
  -OpenCvDir "D:\path\to\opencv_uwp\install\ARM64\vc17\staticlib"
```

### 파라미터

| 파라미터 | 기본값 | 설명 |
|---|---|---|
| `-OpenCvDir` | `$env:SRT3D_OPENCV_DIR` → `..\..\..\opencv_uwp\install\ARM64\vc17\staticlib` | OpenCV static config 폴더 (§2.1) |
| `-EigenDir` | `$env:SRT3D_EIGEN_DIR` → 비움(FetchContent) | 로컬 Eigen 루트. 오프라인 빌드용 (§2.2) |
| `-BuildDir` | `<스크립트 위치>\build_arm64_uwp` | 빌드 디렉터리 |
| `-CopyTo` | 비움 | 지정하면 산출 DLL 을 이 폴더로 자동 복사 (§4) |

> ### ⚠️ 빌드 디렉터리는 반드시 저장소 안(또는 일반 경로)에 둘 것
> `-BuildDir` 을 `%TEMP%` 아래로 지정하면 MSBuild 가 실패한다:
> ```
> warning MSB8029: 중간 디렉터리 또는 출력 디렉터리는 임시 디렉터리 아래에 있을 수 없습니다.
> error MSB6003: ... System.IO.__Error.WinIOError
> ```
> 컴파일러 검사(`project()`) 단계에서 죽기 때문에 원인이 CMake 설정처럼 보이지만 아니다.
> 기본값(`build_arm64_uwp`)은 `.gitignore` 처리돼 있으니 그대로 쓰면 된다.

### 직접 configure 하려면

```powershell
cmake -S . -B build_arm64_uwp `
  -G "Visual Studio 17 2022" -A ARM64 `
  -DCMAKE_SYSTEM_NAME=WindowsStore -DCMAKE_SYSTEM_VERSION=10.0 `
  -DCMAKE_SYSTEM_PROCESSOR=ARM64 -DCMAKE_POLICY_VERSION_MINIMUM=3.5 `
  -DOpenCV_DIR="<install>/ARM64/vc17/staticlib"
cmake --build build_arm64_uwp --config Release --parallel
```

성공 시 다음 두 줄이 보여야 한다:
```
-- Found OpenCV: ... (found version "4.11.0") found components: core imgproc
-- Eigen: FetchContent 3.4.0 — .../_deps/eigen-src
```

### ✅ 재빌드 검증 (2026-08-03)

이 절차로 빌드한 DLL 이 저장소에 포함된 기존
`Assets/Plugins/WSA/ARM64/srt3d_uwp.dll` 과 동등함을 확인했다.

| 항목 | 결과 |
|---|---|
| 파일 크기 | **854,528 B — 바이트 단위 일치** |
| Export 심볼 | **6개 완전 일치** (`srt3d_init`, `srt3d_reset_pose`, `srt3d_track_rgb`, `srt3d_release`, `srt3d_last_error`, `srt3d_set_log_callback`) — 차집합 양방향 공집합 |
| Import DLL | **23개 완전 일치** (`MSVCP140_APP.dll`, `VCRUNTIME140_APP.dll`, `vccorlib140_app.DLL` 포함) |
| 아키텍처 | `AA64 machine (ARM64)`, `20B magic # (PE32+)` — 일치 |
| SHA256 | **불일치 (정상)** |

해시가 다른 것은 **PE 타임스탬프와 빌드 GUID** 때문이며, 바이너리 동일성은 기대 대상이 아니다.
크기가 바이트 단위로 같고 export/import/아키텍처가 모두 일치한다는 것이
"같은 소스에서 같은 물건이 나온다"의 실질적 근거다.

검증 명령:
```powershell
dumpbin /exports    <dll>    # 심볼 집합
dumpbin /dependents <dll>    # import DLL
dumpbin /headers    <dll>    # machine / magic
```

---

## 4. 산출물 배치

빌드 산출: `build_arm64_uwp\Release\srt3d_uwp.dll`

이걸 Unity 프로젝트로 복사한다:

```
native\srt3d_uwp\build_arm64_uwp\Release\srt3d_uwp.dll
  →  Assets\Plugins\WSA\ARM64\srt3d_uwp.dll
```

자동화:
```powershell
.\build_uwp_arm64.ps1 -OpenCvDir "<...>" -CopyTo "..\..\Assets\Plugins\WSA\ARM64"
```

복사 후 **Unity Inspector 에서 플랫폼을 `WSAPlayer` / `ARM64` 로 지정**해야 한다.
(기존 `srt3d_uwp.dll.meta` 가 이미 그렇게 설정돼 있으므로, 같은 이름으로 덮어쓰면 유지된다.)

> DLL 은 Unity Editor 에 로드되지 않는다. `Srt3dNative` 의 선언 자체는 무해(지연 로드)하지만
> 호출부는 `#if !UNITY_EDITOR` 로 감싸야 한다 — `docs/SRT3D_INTERFACE.md` §7.6.

---

## 5. Known issues

### 5.1 `-DOpenCV_STATIC=ON` 은 효과가 없다

`build_uwp_arm64.ps1` 이 이 인자를 넘기지만 `OpenCVConfig.cmake` 가 소비하지 않아
configure 때 경고가 뜬다:

```
CMake Warning: Manually-specified variables were not used by the project:
    OpenCV_STATIC
```

**정적 링크 여부는 OpenCV 를 빌드하는 시점에 결정되며**(§2.1 의 `BUILD_SHARED_LIBS=OFF`),
소비하는 쪽에서 바꿀 수 없다. 기존 스크립트에 남아 있으나 **무해하다.**

### 5.2 `warning C4819` 가 수십 줄 쏟아진다 — 무해

한국어 Windows(코드 페이지 949) 에서 빌드하면 다음 경고가 **대량으로** 발생한다:

```
warning C4819: 현재 코드 페이지(949)에서 표시할 수 없는 문자가 파일에 들어 있습니다.
```

발생처는 두 곳이다:

- **이 저장소의 소스** — `srt3d_uwp.cpp`, `srt3d/src/*.cpp` 의 한글 주석
- **Eigen 헤더** — `_deps/eigen-src/Eigen/src/Core/arch/Default/Half.h`,
  `BFloat16.h`, `GenericPacketMathFunctions.h`, `products/GeneralBlockPanelKernel.h` 등
  (저작권 표기의 비 ASCII 문자)

화면이 경고로 가득 차 **빌드가 실패한 것처럼 보이지만 실패가 아니다.**
마지막 줄에 `srt3d_uwp.vcxproj -> ...\Release\srt3d_uwp.dll` 이 나왔는지로 판단할 것.
코드 페이지가 UTF-8 인 환경(영문 Windows 등)에서는 나타나지 않는다.

### 5.3 `warning MSB8021` — 무해

```
warning MSB8021: 'CharacterSet' 변수의 'MultiByte' 값은
                 'WindowsAppContainer' 변수의 'true' 값과 호환되지 않습니다.
```

CMake 가 생성한 **`ZERO_CHECK` / `ALL_BUILD` 유틸리티 프로젝트**에서만 발생하며
`srt3d_uwp` 타깃과는 무관하다. 산출 DLL 에 영향이 없다.

### 5.4 PowerShell 5.1 에서 `2>&1` 을 쓰면 성공이 실패로 보인다

cmake 출력을 `2>&1` 로 리다이렉트하면 PowerShell 5.1 이 **경고를 ErrorRecord 로 승격**시켜
`NativeCommandError` 를 내고 `$?` 를 `$false` 로 만든다. cmake 자체는 exit 0 인데도 그렇다.

**리다이렉트 없이 실행할 것.** stderr 는 이미 콘솔에 나온다.

```powershell
❌ .\build_uwp_arm64.ps1 ... 2>&1 | Select-Object -Last 40
✅ .\build_uwp_arm64.ps1 ...
```

### 5.5 `.meta` 는 기기에서 만들 수 없다

GL-free 빌드라 `Model::GenerateModel()` 이 비활성이다
(`srt3d/src/model.cpp` — `[SRT3D_NO_GL] GenerateModel disabled — precomputed .meta required`).
모델 템플릿 `.meta` 는 **GL 이 되는 PC 에서 오프라인 생성**해
`Assets/StreamingAssets/srt3d/` 로 배포해야 한다.
상세: `MODIFICATIONS.md` §2.2, `docs/SRT3D_INTERFACE.md` §7.1.

---

## 6. 관련 문서

| 문서 | 내용 |
|---|---|
| [`MODIFICATIONS.md`](MODIFICATIONS.md) | 원본(DLR SRT3D / pysrt3d) 대비 수정 내역, 출처 체인, 라이선스 |
| [`../../docs/SRT3D_INTERFACE.md`](../../docs/SRT3D_INTERFACE.md) | C ABI 6함수 명세, 인자 타입, 좌표 규약, 온디바이스 제약, OpenCV 빌드 상세(§2.4), 빌드 구성(§7.7) |
