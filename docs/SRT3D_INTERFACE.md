# 온디바이스 3D 트래커 인터페이스 명세 (HoloLens 2 / SRT3D)

HoloLens 2(UWP/ARM64, IL2CPP)에서 Unity C#이 네이티브 트래커를 호출하는 규약.
같은 구조를 재현하거나 트래커를 교체할 때 필요한 것 전부.

**문서 구성은 받은 질문 4개에 1:1로 대응한다.**

| 절 | 질문 |
|---|---|
| [§1](#1-질문-1--srt3d인가-icg인가) | srt3d인지 ICG+인지 |
| [§2](#2-질문-2--라이브러리) | 라이브러리 |
| [§3](#3-질문-3--호출하는-타입-c-익스포트-6개) | 호출하는 타입 |
| [§4](#4-질문-4--데이터를-어떤-타입으로-넘기는지) | 데이터를 어떤 타입으로 넘기는지 |
| [§5](#5-좌표-규약--가장-많이-막히는-지점)~[§8](#8-다른-트래커로-교체하려면) | (참고) 좌표 규약 · 렌즈 왜곡 · 온디바이스 제약 · 교체 가이드 |

문서의 모든 내용은 아래 실제 파일에서 뽑았다. **확인 못 한 것은 그렇다고 명시했다.**

| 역할 | 파일 |
|---|---|
| C# 바인딩 | `Assets/Scripts/Srt3dNative.cs` |
| 호출부 | `Assets/Scripts/Srt3dTracker.cs` |
| 캡처 | `Assets/Scripts/Srt3dPvCapture.cs` |
| C 브리지 | `native/srt3d_uwp/srt3d_uwp.cpp` |
| 빌드 | `native/srt3d_uwp/CMakeLists.txt`, `native/srt3d_uwp/build_uwp_arm64.ps1` |
| 코어(vendored) | `native/srt3d_uwp/srt3d/` (헤더 12 + .cpp 6) |
| 수정 내역 | `native/srt3d_uwp/MODIFICATIONS.md` |
| 빌드 절차 | `native/srt3d_uwp/README.md` |
| OpenCV | 별도 트리에서 직접 빌드 — `native/srt3d_uwp/README.md` §2.1 참조 |
| upstream 대조 | `DLR-RM/3DObjectTracking` (커밋 `11ae750`) |
| 산출물 | `Assets/Plugins/WSA/ARM64/srt3d_uwp.dll` (854,528 B) |

> 2026-08-03: 위 소스가 이 저장소 `native/srt3d_uwp/` 로 편입됐다.
> 이전에는 저장소 밖의 별도 트리에 있었고 DLL 만 커밋돼 있었다.

---

## 1. 질문 1 — srt3d인가 ICG+인가

### 1.1 결론: 기기마다 다르다

| 기기 | 트래커 | 코드베이스 | 래퍼 |
|---|---|---|---|
| **HoloLens 2** | **SRT3D** | pysrt3d 포크 (vendored) | `srt3d_uwp.cpp` (C ABI / P Invoke) |
| **Galaxy XR** | **ICG+** | **M3T** | `m3t_jni.cpp` (JNI) |

"ICG+인데 왜 파일명이 M3T인가"는 오해가 아니라 **upstream이 그렇게 하라고 지시한 것**이다.
ICG+ 폴더에는 코드가 한 줄도 없다 — `readme.md`와 동영상 썸네일 2개가 전부다.

```
$ git ls-tree -r --name-only f021061 -- "ICG+/"
ICG+/dlr_icg+_video.png
ICG+/readme.md
```

그 readme가 직접 M3T로 보낸다 (`3DObjectTracking/ICG+/readme.md`, "## Code"):

> The *ICG+* tracker was **fully integrated into** the multi-body, multi-modality, and
> multi-camera tracking library ***M3T***. ... it was ensured that the evaluation results
> for *ICG+* and *M3T* are **exactly the same**. If you would like to reproduce results from
> our publication or use the tracker in your own application, **please use the code from *M3T***.

→ **ICG+ = 알고리즘/논문 이름, M3T = 그 알고리즘이 들어 있는 유일한 코드베이스.**
   따라서 "ICG+를 쓴다"와 "m3t_jni.cpp"는 모순이 아니다. 정확히 말하면
   **"M3T 라이브러리로 ICG+ 구성을 돌린다"**가 맞다.

### 1.2 왜 두 기기가 다른 트래커를 쓰나

핵심은 **모달리티(modality)** 다. SRT3D는 region 하나뿐이고, M3T/ICG+는 region + depth + texture를 융합한다.

**코드 근거 — upstream 헤더 목록이 그대로 말해준다.**

```
$ git ls-tree --name-only f021061 -- SRT3D/include/srt3d/ | grep modality
SRT3D/include/srt3d/region_modality.h        ← 이거 하나뿐

$ ls 3DObjectTracking/M3T/include/m3t/ | grep modality
depth_modality.h
region_modality.h
texture_modality.h                            ← 3개
```

M3T readme 서두도 같은 말을 한다:

> It allows to fuse information from **depth, region, and texture** modalities to
> simultaneously predict the pose and configuration of multiple multi-body objects.

**우리가 vendoring한 SRT3D 코어도 region 하나뿐이다** (`srt3d_uwp/srt3d/src/`):

```
body.cpp  camera.cpp  common.cpp  model.cpp  region_modality.cpp  tracker.cpp
```

즉 **SRT3D는 구조적으로 RGB 단일 모달리티**다. depth를 줄 수도, 받을 수도 없다.

**그리고 HoloLens 추적 단계엔 애초에 depth가 없다.** 이건 SRT3D의 제약이 아니라
우리 파이프라인 설계의 결과다 (`Srt3dTracker.cs:1-9`, `Hl2ssBootstrap.cs:1`):

```
1. 앱 시작 → hl2ss 가 PV+depth+EET 스트리밍 ON        ← depth 는 여기서만 쓴다
2. PC 가 SAM+FoundationPose 로 초기 pose 계산 (depth 사용)
3. Hl2ssBootstrap.Stop() → 카메라 반납                 ← depth 스트림 종료
4. MediaFrameReader(PV RGB24 896x504) ON → 매 프레임 srt3d_track_rgb
```

`Hl2ssBootstrap.cs:1` 주석이 이 시퀀스를 명시한다:
> 시작 시 hl2ss(PV+depth) ON → FP init 후 Stop() 으로 카메라 반납

HL2는 PV 카메라를 **배타 점유(ExclusiveControl)** 하므로 hl2ss와 MFR이 동시에 못 돈다.
추적 루프에 들어간 시점에 손에 쥔 것은 **RGB 프레임 하나**뿐이다.

> **정리하면 사실 관계는 이렇다.** SRT3D는 region 하나, M3T는 region+depth+texture 셋.
> 그리고 HoloLens 추적 단계엔 depth가 없다(위 시퀀스 3~4). 그래서 region-only인 SRT3D가
> 파이프라인과 맞는다.
>
> ⚠️ **여기서 회복 능력에 대한 인과 주장은 하지 않는다.** "RGB-only라 초기 자세를 못 끌어온다"는
> 성립하지 않는다 — **Galaxy XR도 M3T 이전엔 SRT3D를 썼고 그때 초기 자세를 끌어왔다**(사용자 증언).
> 같은 트래커인데 HL2에서만 안 끌려온 이유는 알고리즘이 아니라 조건 차이이며, 아직 미확정이다
> (부록 A #5).

> ⚠️ **미확인 항목.** Galaxy XR의 `m3t_jni.cpp`가 **실제로 어느 모달리티를 켜는지는 확인 못 했다.**
> 그 프로젝트(XRHandEyeTracker)가 이 머신에 없다 — 작업 트리 전체를 검색해도
> `m3t_jni` / `XRHandEyeTracker` 파일이 나오지 않는다.
> 위 서술은 **M3T가 제공하는 모달리티**까지만 코드로 확인한 것이고,
> "Galaxy XR이 depth/texture를 실제로 켰다"는 **아직 추측이다.**
> 확정하려면 `m3t_jni.cpp`에서 `DepthModality` / `TextureModality` 생성 여부와
> `Optimizer`에 어떤 modality를 `Add`하는지 한 번만 보면 된다.

### 1.3 세대 관계 — "왜 구버전을 쓰나"에 대한 답

DLR-RM/3DObjectTracking은 **한 연구실의 연작**이고, 새 논문마다 이전 것을 포함해 갱신한다
(repo `readme.md`).

| 연도 | 알고리즘 | 발표 | 성격 |
|---|---|---|---|
| 2020 | RBGT | ACCV | region, sparse Gaussian |
| **2022** | **SRT3D** | **IJCV** | **region-only, RGB. ← HoloLens 2가 쓰는 것** |
| 2022 | ICG | CVPR | region + **depth** 융합 |
| 2023 | **ICG+** | IROS | ICG + **texture**(keypoint) + multi-region **← Galaxy XR** |
| 2023 | Mb-ICG | TPAMI | multi-body / kinematic structure |
| 2023 | **M3T** | Dissertation | 위를 전부 흡수한 최신 통합 라이브러리 |

repo readme의 안내:
> Note that the code for each new paper also includes an updated version of previous work.
> If you want to use our tracker in your own project, **please use the code from the latest
> publication.** Currently, the latest version of our code can be found in the folder **M3T**.

→ **SRT3D는 4세대쯤 뒤처진 게 맞다.** 다만 "버려진 코드"가 아니라
   **RGB-only 조건에서는 여전히 이 계열의 정답**이다. 이후 세대가 추가한 것은 전부
   depth/texture/multi-body이고, HL2 추적 단계엔 셋 다 없다(§1.2).

> ⚠️ **기존 문서의 오류 정정.** 이전 판에는 *"f021061 리비전엔 M3T만 있고 SRT3D 디렉터리가 없다"*
> 고 적혀 있었는데 **사실이 아니다.** 커밋 트리에는 6개가 전부 있다:
> ```
> $ git ls-tree --name-only f021061
> ICG+  ICG  LICENSE  M3T  Mb-ICG  RBGT  SRT3D  readme.md
> ```
> 디스크에 `M3T/`만 보이는 이유는 **sparse checkout** 이다:
> ```
> $ git config core.sparseCheckout      → true
> $ cat .git/info/sparse-checkout       → /*  !/*/  /M3T/
> ```
> 사수님께 "레포에 SRT3D가 없어서 옛날 걸 썼다"고 말하면 안 된다. **있다.** 고른 것이다.

### 1.4 되물음 대비: HoloLens도 M3T/ICG+로 통일하는 게 나은가

**지금은 아니다. 다만 영구적 결론은 아니다.**

통일의 이점은 분명하다 — 코드베이스가 하나로 줄고, 유지되는 최신 라이브러리를 따라가게 되고,
두 기기의 성능 수치를 같은 축에서 비교할 수 있다(지금은 conf 정의부터 달라 비교가 불가능하다, §2.3).
M3T는 텍스처 있는 물체에서 SRT3D보다 확실히 낫고, ICG+ 논문은 YCB-Video/OPT에서 SOTA를 주장한다.
문제는 **그 이점이 전부 HL2가 지금 못 먹이는 입력에서 나온다**는 것이다. depth 모달리티는 추적 중
depth 스트림을 요구하는데 HL2에선 PV 배타 점유 때문에 hl2ss(depth)와 MFR(PV RGB)이 동시에 못 돈다(§1.2)
— 이걸 풀려면 캡처 아키텍처를 통째로 다시 설계해야 한다. texture 모달리티는 keypoint feature detector가
필요해 OpenCV `features2d`를 추가로 빌드해야 하는데, 현재 UWP OpenCV는 `core,imgproc` **2개만**으로
잘라 놓은 상태다(§2.4). 게다가 M3T는 빌드 의존성에 **GLEW/GLFW**가 들어가고 렌더러 표면적이 SRT3D보다
넓다(`silhouette_renderer`, `basic_depth_renderer`, `normal_renderer`) — SRT3D 포팅 때 이미 하루를 태운
GL 제거 작업(§7.1)을 더 큰 규모로 반복해야 한다는 뜻이다. 요약하면 **비용은 확정적으로 크고, 이득은
HL2에서 대부분 실현되지 않는다.** region-only로 쓸 거면 M3T로 갈아타도 알고리즘적으로 얻는 게 거의 없다.

**갈아탈 조건은 명확하다.** (a) HL2 추적 루프에 depth를 넣는 데 성공하거나, (b) 추적 대상이 텍스처가
풍부한 물체로 바뀌거나, (c) 두 기기 성능을 같은 지표로 비교해야 하는 요구가 생기면 — 그때는 통일이 맞다.
그전까지는 **인터페이스만 맞춰 두는 것**이 실질적인 대비다. §3의 C ABI 6개 함수는 트래커 중립적이라
백엔드를 M3T로 바꿔도 `Srt3dTracker.cs`는 거의 그대로 쓸 수 있다(§8).

---

## 2. 질문 2 — 라이브러리

### 2.1 출처 체인 (⚠️ 리비전 일부 미고정)

```
DLR-RM/3DObjectTracking · SRT3D          원본 알고리즘 (2021-10-26, 커밋 11ae750)
  └→ pysrt3d (Jianxff, MIT, 2024)        수정본 ← 우리가 쓰는 실제 물건
       └→ XRHandEyeTracker source_nogl   Galaxy XR 프로젝트에서 GL 제거
            └→ srt3d_uwp                 UWP/ARM64 포팅 (현재)
```

- 원본: <https://github.com/DLR-RM/3DObjectTracking/tree/master/SRT3D>
- pysrt3d: <https://github.com/Jianxff/pysrt3d> (MIT, 2024)

**upstream SRT3D는 사실상 단일 리비전이다** — 확인했다:
```
$ git log --oneline f021061 -- SRT3D/include SRT3D/src
11ae750 feat(SRT3D): initial commit          (2021-10-26)
```
초기 커밋 이후 **소스가 한 번도 수정되지 않았다.** 이후 커밋은 readme뿐이다.
→ DLR 원본 쪽 재현성 위험은 없다. 어느 시점에 받았든 같은 코드다.

> ⚠️ **남은 재현성 공백: pysrt3d 포크 시점.**
> `srt3d_uwp\srt3d\`에는 VCS 메타데이터가 없다. `CMakeLists.txt:19` 주석이
> "XRHandEyeTracker에서 복사한 검증본"이라고만 밝힌다.
> **어느 pysrt3d 커밋에서 갈라졌는지 특정할 수 없다.**
> 새로 시작한다면 서브모듈이나 커밋 해시를 반드시 남길 것.

### 2.2 stock SRT3D가 아니다 — pysrt3d 수정 내역 (diff로 확인)

vendored 코어를 upstream `f021061:SRT3D/`와 직접 diff했다. pysrt3d 수정은 소스에
**`==== Additional ====` 블록으로 명시**되어 있어 식별이 쉽다.

**(a) KL divergence 기반 confidence — 원본에 없다.**
`region_modality.h` diff:
```diff
+  // ==================== Additional ====================
+  float FunctionKL(float u, float std);
+  float EvaluateDistribution();
+  void set_kl_threshold(float kl_threshold);
+  float kl_threshold_ = 1.0;
+  // ==================== Additional ====================
```

**(b) body diameter 자동 계산** — 원본은 생성자 인자로 수동 지정. `body.h` diff:
```diff
   Body(const std::string &name, const std::filesystem::path &geometry_path,
        float geometry_unit_in_meter, bool geometry_counterclockwise,
-       bool geometry_enable_culling, float maximum_body_diameter,
-       const Transform3fA &geometry2body_pose, int occlusion_id = 0);
+       bool geometry_enable_culling, const Transform3fA &geometry2body_pose = Transform3fA::Identity());
+  // Auto calculate body diameter
+  void calculate_maximum_body_diameter();
```
→ **생성자 시그니처가 원본과 다르다.** stock SRT3D 예제 코드를 그대로 붙이면 컴파일이 안 된다.

**(c) 그 외**: 초기화/추적 임계값 분리, 독립적 다중 모델 추적, `ResetOcclusionMask()` 추가.

### 2.3 ⚠️ conf는 다른 트래커와 숫자 비교가 불가능하다

`out17[16]`으로 나오는 conf는 **pysrt3d가 추가한 지표**이고 stock SRT3D에도, M3T에도 없다.
정의를 정확히 알아야 오해가 없다 (`srt3d/src/region_modality.cpp:19-41`):

```cpp
float RegionModality::FunctionKL(float u, float std) {
  // Consider Standard Distribution ~ N(0, min_variance_)
  float eq1 = std::log(min_standard_deviation_ / std);
  float eq2 = powf(std, 2.0f) + powf(u - 0.0f, 2.0f);
  float eq3 = 2.0f * min_variance_;
  return (eq1 + eq2 / eq3) - 0.5f;
}

float RegionModality::EvaluateDistribution() {
  ...
  float valid_cnt = 0.f;
  for (auto &data_line : data_lines_) {
    float kl = FunctionKL(data_line.mean, data_line.standard_deviation);
    if (kl < kl_threshold_) valid_cnt += 1.f;      // kl_threshold_ = 1.0
  }
  return valid_cnt / data_lines_.size();
}
```

**conf = (KL divergence가 임계값 1.0 미만인 correspondence line 수) / (전체 line 수)**

| 성질 | 값 |
|---|---|
| 범위 | **0.0 ~ 1.0** (비율이므로 유계) |
| 의미 | 윤곽선 대응 중 "잘 맞은" 것의 **비율** |
| 확률인가 | **아니다.** pose 정확도의 확률적 추정치가 아니다 |
| 재투영 오차와의 관계 | **없다.** 픽셀 단위 잔차가 아니다 |
| 임계값 | `kl_threshold_ = 1.0` 하드코딩 (`srt3d_uwp.cpp:84`) |

> ⚠️ **다른 트래커의 confidence/score와 절대 직접 비교하지 말 것.**
> M3T에는 이 지표가 아예 없고, FoundationPose의 score와도 정의가 무관하다.
> **"HoloLens conf 0.7 vs Galaxy XR score 0.7"은 아무 의미가 없는 비교다.**
> 기기 간 비교가 필요하면 양쪽에 공통 지표(예: 동일 시퀀스에서의 ADD-S, 재투영 오차)를
> 따로 계산해야 한다.

> 💡 **숨은 비용.** `EvaluateDistribution()`은 내부에서 `CalculateBeforeCameraUpdate()`와
> `CalculateCorrespondences(0)`를 **다시 호출한다.** 즉 conf를 읽는 대가로
> **프레임당 correspondence 계산이 1회 추가된다.** conf가 필요 없어지면 여기가 바로 삭감 지점이다.

### 2.4 의존성

| 항목 | 버전 | 구성 |
|---|---|---|
| **OpenCV** | **4.11.0** | `core`, `imgproc` **2개만**. static, WindowsStore ARM64 |
| **Eigen** | **3.4.0** | vendored (`third_party/eigen3`) |
| **tiny_obj_loader** | vendored | `third_party/tiny_obj_loader/tiny_obj_loader.h` |
| C++ 표준 | C++17 | `std::filesystem` 사용 (`body.h`) |

OpenCV 빌드 (`opencv_uwp/build_uwp_arm64.ps1`):
```powershell
cmake -S $src -B $build -G "Visual Studio 17 2022" -A ARM64 `
  -DCMAKE_SYSTEM_NAME=WindowsStore -DCMAKE_SYSTEM_VERSION="10.0" `
  -DBUILD_LIST="core,imgproc" `
  -DBUILD_SHARED_LIBS=OFF -DBUILD_WITH_STATIC_CRT=OFF `
  -DWITH_JPEG=OFF -DWITH_PNG=OFF -DWITH_TIFF=OFF ... (전부 OFF)
```
- `CMAKE_SYSTEM_NAME=WindowsStore` **필수** — UWP 금지 API를 피한다.
- `BUILD_WITH_STATIC_CRT=OFF` — Unity/IL2CPP가 동적 CRT를 쓰므로 맞춰야 한다.
- imgcodecs 계열을 전부 끈 것은 raw RGB를 받기 때문(§2.5). JPEG를 받으려면 이 전제가 깨진다.
- `features2d`가 없다 → **M3T의 texture 모달리티는 지금 빌드로는 불가**(§1.4).

### 2.5 UWP 래퍼에서 원본 대비 수정한 부분

**(a) 입력 포맷: JPEG → raw RGB.**
JNI 원본은 JPEG를 받아 `cv::imdecode`했다. UWP 포팅에서 raw RGB로 바꿨다 —
`imgcodecs` 의존성 제거가 목적. `srt3d_uwp.cpp:104-105` 주석이 근거다:
> (JNI 는 jpeg 를 받아 RGB 로 변환 후 push 했음 — 여기선 이미 RGB 라 그대로 push.)

**(b) GL 전면 제거 (`SRT3D_NO_GL`).**
UWP엔 데스크톱 OpenGL이 없다. `tracker.h` / `region_modality.h` diff에서 확인되는 패턴:
```diff
+#ifndef SRT3D_NO_GL
 #include <srt3d/occlusion_renderer.h>
+#endif
+#ifdef SRT3D_NO_GL
+class OcclusionRenderer;  // forward decls (GL-free build)
+class RendererGeometry;
+class Viewer;
+#endif
-  void AddViewer(std::shared_ptr<Viewer> viewer_ptr);
+  // void AddViewer(std::shared_ptr<Viewer> viewer_ptr);
-  bool StartTracker(bool start_tracking);
+  // bool StartTracker(bool start_tracking);
```
Viewer/Renderer 계열 API는 **주석 처리**됐다. 결과로 `.meta` 생성이 기기에서 불가능해진다 — §7.1.

**(c) 카메라 소스 교체.** upstream의 `azure_kinect_camera.h` / `loader_camera.h` / `image_viewer.h`가
빠지고, 대신 외부 버퍼를 밀어 넣는 **`virtual_camera.hpp`**가 추가됐다.
```
upstream  SRT3D/include/srt3d/        헤더 15개
vendored  srt3d_uwp/srt3d/include/srt3d/  헤더 12개  = 15 − (위 3개)
          srt3d_uwp/srt3d/virtual_camera.hpp          ← 추가분. include/ 아래가 아니라 한 단계 위
```
그래서 include 경로도 혼자만 다르다 (`srt3d_uwp.cpp:26`):
```cpp
#include <srt3d/tracker.h>              // 나머지는 꺾쇠 + include/ 경로
#include "srt3d/virtual_camera.hpp"     // 이것만 따옴표 + 상대 경로
```

**(d) 전역 단일 세션 C ABI.** `shared_ptr` 5개(`g_body/g_model/g_camera/g_tracker/g_rm`)를
파일 전역으로 두고 6개 C 함수로 감쌌다 (`srt3d_uwp.cpp:34-42`). 락 없음 — §7.4.

---

## 3. 질문 3 — 호출하는 타입 (C 익스포트 6개)

`srt3d_uwp.dll`이 노출하는 함수는 **6개**이고, 전부 아래에 있다.

`SRT3D_API` = `extern "C" __declspec(dllexport)` (`srt3d_uwp.cpp:29`).
호출 규약은 x64/ARM64에서 `__cdecl`이 기본이고, C#에서 `CallingConvention.Cdecl`로 명시했다.

| # | 함수 | C# 바인딩 | 호출 빈도 |
|---|---|---|---|
| 1 | `srt3d_init` | O | 1회 |
| 2 | `srt3d_reset_pose` | O | 1회 (+재등록 시) |
| 3 | `srt3d_track_rgb` | O | **매 프레임** |
| 4 | `srt3d_release` | O | 종료 시 |
| 5 | `srt3d_last_error` | O (`LastError()` 래핑) | 실패 시 |
| 6 | `srt3d_set_log_callback` | **X — 미사용** | — |

### 3.1 `srt3d_init`
```cpp
// srt3d_uwp.cpp:63
SRT3D_API int srt3d_init(const char* meshPath, const float* K9, int width, int height);
```
```csharp
// Srt3dNative.cs:18-19
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern int srt3d_init(string meshPath, float[] K9, int width, int height);
```
**반환**: `1`=성공, `0`=실패. 실패 시 `srt3d_last_error()`에 원인.

### 3.2 `srt3d_reset_pose`
```cpp
// srt3d_uwp.cpp:98
SRT3D_API int srt3d_reset_pose(const float* pose16);
```
```csharp
// Srt3dNative.cs:22-23
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
public static extern int srt3d_reset_pose(float[] pose16);
```
**반환**: `1`=성공, `0`=미초기화/널.

> **[2026-07-23] 회복 부스트 (시그니처 불변, 내부 동작만 추가).** `reset_pose` 호출 직후
> **12프레임 동안** 탐색을 넓힌다: `scales {12,8,5,3,2,1}` + `n_corr=30` → basin ~228px.
> 이후 평소값(`{5,2,2,1}`, `n_corr=7`, basin ~95px)으로 자동 복귀.
> **`{12,8,5,3,2,1}`는 우리 튜닝값이다 — 코어 기본값은 `{5,2,2,1}`** (upstream 대조 시 주의).
> 목적: FP 초기 자세가 크게 틀렸을 때 srt3d 가 실루엣으로 끌어오게. 호출부는 그대로 —
> C ABI/인자 불변, 동작만 달라짐. 상세·검증상태는 §9.2.

### 3.3 `srt3d_track_rgb`
```cpp
// srt3d_uwp.cpp:106
SRT3D_API int srt3d_track_rgb(const unsigned char* rgb, int width, int height, float* out17);
```
```csharp
// Srt3dNative.cs:27-28
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
public static extern int srt3d_track_rgb(byte[] rgb, int width, int height, [Out] float[] out17);
```
**반환**: `1`=성공, `0`=실패.

### 3.4 `srt3d_release`
```cpp
// srt3d_uwp.cpp:129
SRT3D_API void srt3d_release();
```
```csharp
// Srt3dNative.cs:30-31
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
public static extern void srt3d_release();
```
전역 `shared_ptr` 5개를 `reset()`. 반환 없음. 재호출 안전.

### 3.5 `srt3d_last_error`
```cpp
// srt3d_uwp.cpp:59
SRT3D_API const char* srt3d_last_error();
```
```csharp
// Srt3dNative.cs:33-41
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
static extern IntPtr srt3d_last_error();
public static string LastError() => Marshal.PtrToStringAnsi(srt3d_last_error()) ?? "";
```
DLL 소유의 `std::string` 내부 포인터. **호출자가 해제하지 말 것.** 다음 에러 발생 시 무효화.
`IntPtr`로 받아 `Marshal.PtrToStringAnsi`로 복사한다 — `string`으로 직접 받으면
마샬러가 해제를 시도해 힙이 깨진다.

### 3.6 `srt3d_set_log_callback` — **C# 바인딩 없음, 미사용**
```cpp
// srt3d_uwp.cpp:56
SRT3D_API void srt3d_set_log_callback(void (*cb)(const char*));
```
쓰려면 `[UnmanagedFunctionPointer(CallingConvention.Cdecl)]` 델리게이트를 만들고
**GC에 수거되지 않게 static 필드로 붙들어야** 한다. 지역 변수로 넘기면
다음 GC에서 콜백이 죽은 주소를 가리켜 크래시한다.

---

## 4. 질문 4 — 데이터를 어떤 타입으로 넘기는지

> **질문 취지: "캐릭터 배열로 쓰는지"**
> → **경로 문자열만 `char*`다. 이미지는 `byte[]`(부호 없는 8비트 배열), 나머지 수치는 전부 `float[]`.**
> 구조체나 클래스는 하나도 넘기지 않는다. **전부 평평한(flat) 1차원 배열**이다.
> 이건 의도된 설계다 — C ABI에 구조체를 넣으면 패딩/정렬이 컴파일러마다 달라져 깨진다.

### 4.1 전체 인자 표

| 인자 | C++ 타입 | C# 타입 | 요소 수 | 단위 | 메모리 레이아웃 |
|---|---|---|---|---|---|
| `meshPath` | `const char*` | `string` (`CharSet.Ansi`) | NUL 종료 | — | **ANSI 바이트열**. UTF-16 아님. `.obj` 절대경로 |
| `K9` | `const float*` | `float[]` | **9** | 픽셀 | row-major `[fx,0,cx, 0,fy,cy, 0,0,1]` |
| `width`/`height` | `int` | `int` | 1 | 픽셀 | 값 전달 |
| `pose16` | `const float*` | `float[]` | **16** | **미터** | 4x4 **row-major**, ob_in_cam, OpenCV 규약 |
| `rgb` | `const unsigned char*` | `byte[]` | **w*h*3** | — | **RGB24 인터리브**, stride=`w*3`, 원점 top-left |
| `out17` | `float*` | `[Out] float[]` | **≥17** | 미터 | `[0..15]`=pose row-major, `[16]`=conf |

`float`는 양쪽 다 **IEEE 754 32비트**로 동일하다. C#의 `double`을 넘기면 안 된다.

### 4.2 RGB 이미지 — 가장 헷갈리는 인자

| 항목 | 값 | 근거 |
|---|---|---|
| **C# 타입** | **`byte[]`** (`IntPtr` 아님) | `Srt3dNative.cs:28` |
| **픽셀 포맷** | **RGB24** (픽셀당 3바이트) | `CV_8UC3` (`srt3d_uwp.cpp:111`) |
| **채널 순서** | **R, G, B** (BGR 아님) | `Srt3dNative.cs:25` 주석 |
| **stride** | **`width*3` 고정, 패딩 없음** | `srt3d_uwp.cpp:111` |
| **원점** | **top-left**, 행 우선 위→아래 | `Srt3dPvCapture.cs:192` |
| **버퍼 수명** | **네이티브가 복사** → 반환 즉시 재사용 가능 | `srt3d_uwp.cpp:112` |

**`byte[]`를 쓰면 마샬링이 어떻게 되나.** P/Invoke가 호출 동안 배열을 **pin**(GC 이동 금지)하고
첫 원소의 포인터를 네이티브에 넘긴다. **복사가 아니라 고정이다.**
그래서 `IntPtr`+`Marshal.AllocHGlobal`을 직접 쓸 때와 성능이 같으면서 수명 관리가 필요 없다.

**stride가 왜 중요한가.** 네이티브는 stride 인자를 받지 않고 `width*3`을 가정한다:
```cpp
// srt3d_uwp.cpp:111
cv::Mat view(height, width, CV_8UC3, const_cast<unsigned char*>(rgb));
```
→ **행 패딩이 있으면 이미지가 한 행마다 조금씩 밀려 사선으로 찌그러진다.**
캡처 측에서 패딩을 제거해 넘겨야 한다. 지금은 `Srt3dPvCapture.cs:192`가
`d = (dy*w + dx)*3`으로 빈틈없이 채우므로 조건을 만족한다.

**버퍼 수명 — 복사가 두 번 일어난다:**
1. `srt3d_uwp.cpp:112` — `g_camera->PushImage(view.clone())`
2. `virtual_camera.hpp` `PushImage` 내부 — `image_list_.push(image.clone())`

프레임당 `w*h*3` 복사 2회. 896×504면 ≈1.35MB × 2 = 2.7MB/frame @30fps ≈ **81MB/s**.
성능이 문제되면 여기가 첫 번째 손볼 곳이다.

### 4.3 K (intrinsics) — 9개를 받지만 **4개만 읽는다**

```cpp
// srt3d_uwp.cpp:66
const float fx = K9[0], cx = K9[2], fy = K9[4], cy = K9[5];
```

| 인덱스 | 값 | 읽나 |
|---|---|---|
| `K9[0]` | fx | **O** |
| `K9[1]` | skew | **X — 무시** |
| `K9[2]` | cx | **O** |
| `K9[3]`, `K9[4]`, `K9[5]` | 0, fy, cy | `[4]`,`[5]`만 **O** |
| `K9[6..8]` | 0, 0, 1 | **X — 무시** |

호출부는 그래도 9개를 정직하게 채운다 (`Srt3dTracker.cs:245`):
```csharp
float[] K = { fx, 0, cx, 0, fy, cy, 0, 0, 1 };
```

**두 가지 함의:**
1. **skew(`K9[1]`) 무시** — 현대 디지털 센서에서 skew는 사실상 0이라 무해하다.
2. **왜곡 계수를 받는 인자가 아예 없다** → API가 **undistort된 이미지를 전제**한다.

> ✅ **그런데 HL2 PV에서는 실무상 무해하다. 실측했다(2026-07-22).**
> PV 왜곡 계수가 **전부 0**이다 — `k1=k2=k3=0, p1=p2=0`.
> 이미지 어디서든 핀홀 대비 변위가 **0.00 px**(코너 포함).
> HoloLens 2 PV 파이프라인이 이미 rectify된 프레임을 준다는 뜻이다. **할 일이 없다.**
> 상세 근거와 재측정 절차는 §6.

### 4.4 Pose — 입출력 모두 `float[16]` row-major

**(a) row-major — 코드로 확인됨.**
입력 (`srt3d_uwp.cpp:47-52`):
```cpp
Eigen::Matrix4f arr16_to_mat(const float* a) {
    Eigen::Matrix4f m;
    for (int r = 0; r < 4; ++r)
        for (int c = 0; c < 4; ++c) m(r, c) = a[r * 4 + c];   // row-major in
    return m;
}
```
출력 (`srt3d_uwp.cpp:119-120`):
```cpp
for (int r = 0; r < 4; ++r)
    for (int c = 0; c < 4; ++c) out17[r * 4 + c] = pose(r, c);   // row-major out
```
Eigen이 기본 column-major **저장**이지만 `m(r,c)` 접근자를 쓰므로 저장 순서와 무관하게 옳다.

따라서 **translation은 `[3], [7], [11]`** (row-major 4번째 열):
```
[ R00 R01 R02 tx ]     인덱스   [ 0  1  2  3 ]
[ R10 R11 R12 ty ]  →           [ 4  5  6  7 ]
[ R20 R21 R22 tz ]              [ 8  9 10 11 ]
[  0   0   0   1 ]              [12 13 14 15 ]
```

**(b) 미터 — 확인됨.** `srt3d_uwp.cpp:68-69`:
```cpp
g_body = std::make_shared<srt3d::Body>(
    name, mesh, 1.0f, true, true, srt3d::Transform3fA::Identity());
//               ^^^^ geometry_unit_in_meter = 1.0
```
`1.0`이므로 **`.obj`의 좌표값이 곧 미터**다. mesh를 mm로 만들면 여기를 `0.001f`로 바꿔야 한다.

> **[2026-07-23] pose 원점이 bbox 중심으로 바뀜.** 예전 mesh는 원점이 bbox 중심에서 10.3cm
> 치우쳐 있었다(→ `.meta`의 `max_body_diameter`가 0.43으로 2배 과대 → 템플릿에 물체가 절반
> 크기로 렌더 → 윤곽 성김 → tilt 구분 불가). **bbox 중심으로 재정렬**해 diameter 0.216 정상화.
> 그 결과 `pose16`의 translation이 이제 **물체 bbox 중심** 기준이다(예전엔 치우친 원점 기준).
> `srt3d_reset_pose`에 넣는 FP pose도 같은 재정렬 mesh 기준이라 일관됨. HUD 의 `obZ`(=`out17[11]`)
> 해석도 바뀜 — 이제 표면이 아니라 중심 거리다. 상세는 §9.1.

**(c) ob_in_cam — 확인됨(간접).** srt3d는 이 pose를 `body2world_pose`라 부르지만,
`VirtualCamera`가 `set_camera2world_pose()`를 **한 번도 호출하지 않아** `world2camera = I`다
(`camera.h:54-55` 기본값 Identity). 따라서 `body2camera == body2world`
→ 반환 pose는 **카메라 기준 object pose = ob_in_cam**이 맞다.

**(d) OpenCV 규약 (+X right, +Y down, +Z forward)** — (c)는 "world == camera"까지만 보장한다.
축 방향은 `region_modality.cpp`의 투영식이 `fu*X/Z + ppu` 형태(부호 반전 없음)인 점과,
FoundationPose의 OpenCV pose를 `srt3d_reset_pose`에 **그대로** 넣어 추적이 성립한다는 실측으로
뒷받침된다. 소스 한 줄로 못 박은 것은 아니므로 이 항목만 **"실측 기반"**으로 이해할 것.

### 4.5 출력 수령 방식 — **호출자 할당 `[Out]` 배열**

리턴 버퍼도, `out` 파라미터 구조체도, 콜백도 아니다.
**호출자가 17개를 미리 할당해 넘기고 네이티브가 그 안에 채운다.**

```csharp
float[] outv = new float[17];   // Srt3dTracker.cs:258
Srt3dNative.srt3d_track_rgb(rgb, w, h, outv);
_conf = outv[16];               // Srt3dTracker.cs:260
```

| 인덱스 | 내용 |
|---|---|
| `out17[0..15]` | pose 4x4 row-major (ob_in_cam, 미터) |
| `out17[3], [7], [11]` | tx, ty, tz |
| **`out17[16]`** | **confidence** — §2.3의 KL 비율, 범위 0~1 |

`[Out]` 특성은 "네이티브→관리" 방향만 복사하라는 지시다. 붙이지 않으면 in/out 양방향으로
마샬링해 불필요한 복사가 생긴다(동작은 한다).

conf의 출처 (`srt3d_uwp.cpp:116, 121`):
```cpp
std::vector<float> conf = g_tracker->EvaluateDistribution();
...
out17[16] = conf.empty() ? 0.0f : conf[0];   // 비면 0.0
```
`EvaluateDistribution()`은 region modality마다 하나씩 반환하는데 우리는 modality가 1개이므로 `conf[0]`.

> ⚠️ **`out17`을 17개 미만으로 할당하면 힙이 조용히 깨진다.** 네이티브는 크기를 검사하지 않는다.

### 4.6 호출 시퀀스 — 초기화 1회 vs 매 프레임

```
[1회]     파일 준비        StreamingAssets → persistentDataPath 복사
[1회]     srt3d_init(meshPath, K9, w, h)
[1회]     srt3d_reset_pose(pose16)            ← FoundationPose 초기 pose
[매프레임] srt3d_track_rgb(rgb, w, h, out17)
[종료]     srt3d_release()
```

**① 파일 준비** (`Srt3dTracker.cs:144-148`):
```csharp
_meshPath = Path.Combine(d, "joke_book_hl2c.obj");
string meta = Path.Combine(d, "joke_book_hl2c.obj.meta");
yield return Copy("srt3d/joke_book_hl2c.obj", _meshPath);
yield return Copy("srt3d/joke_book_hl2c.obj.meta.bytes", meta);
if (!File.Exists(_meshPath) || !File.Exists(meta)) { Hud("FAIL: model copy"); yield break; }
```
`srt3d_init`은 `meshPath` 하나만 받지만 **같은 위치의 `<meshPath>.meta`도 함께 읽는다**
(`srt3d_uwp.cpp:71`). 두 파일이 짝으로 있어야 한다.

> ⚠️ **Unity `.meta` 확장자 충돌.** srt3d 모델 파일이 하필 `.meta`인데 Unity가 그걸 자기
> 에셋 메타데이터로 가로챈다. 그래서 **`<mesh>.obj.meta.bytes`로 이름을 바꿔 배포**하고
> 런타임에 `<mesh>.obj.meta`로 복사한다. StreamingAssets는 UWP에서 패키지 안이라
> `File.IO`가 안 되므로 `UnityWebRequest`로 읽는다.

**② 초기화 + 초기 pose** (`Srt3dTracker.cs:245-249`) — **첫 MFR 프레임에서 1회**:
```csharp
float[] K = { fx, 0, cx, 0, fy, cy, 0, 0, 1 };
if (Srt3dNative.srt3d_init(_meshPath, K, w, h) == 0) {
    Hud("srt3d_init FAIL\n" + Srt3dNative.LastError()); return;
}
Srt3dNative.srt3d_reset_pose(_fpPose);
```
K를 첫 프레임까지 미루는 이유: PV intrinsics는 카메라가 열린 뒤에야 나온다.
**`srt3d_init`의 `w,h`와 이후 `srt3d_track_rgb`의 `w,h`는 반드시 일치해야 한다.**

**③ 매 프레임 추적** (`Srt3dTracker.cs:258-260`):
```csharp
float[] outv = new float[17];
if (Srt3dNative.srt3d_track_rgb(rgb, w, h, outv) == 0) {
    if (_frame % 30 == 0) Hud("track FAIL\n" + Srt3dNative.LastError()); return;
}
_frame++; _conf = outv[16]; _obZ = outv[11]; ComputePoseDelta(outv);
```

**④ 해제** (`Srt3dTracker.cs:1003`): `if (_inited) Srt3dNative.srt3d_release();`

**스레딩**: `srt3d_track_rgb`는 **Unity 메인 스레드에서만** 호출한다.
네이티브는 전역 `shared_ptr` 기반 **단일 세션**이고 락이 없다 (`srt3d_uwp.cpp:34-42`).
현재 구조는 캡처(MFR 콜백 = 백그라운드 스레드)가 최신 프레임만 락으로 보관하고,
`Update()`가 꺼내서 네이티브를 호출한다 — **캡처만 백그라운드, 트래킹은 메인.**

---

## 5. 좌표 규약 — 가장 많이 막히는 지점

> 이 절은 실제로 하루를 태운 곳이다. 증상이 "그럴듯하게 틀림"이라 잔차나 conf로는 안 잡힌다.

### 5.1 전체 체인

srt3d 출력은 **ob_in_cam**: OpenCV 규약(**+X right, +Y down, +Z forward**), translation **미터**.
Unity world까지:

```
world = cam2world · C · M · C          (C = diag(1, -1, 1),  C⁻¹ = C)
```

| 기호 | 의미 |
|---|---|
| `M` | srt3d가 준 ob_in_cam (row-major 4x4 → `Matrix4x4`) |
| `C` | OpenCV cam ↔ Unity cam. **Y만 뒤집는다** — 양쪽 다 +Z가 전방이므로 Z는 그대로 |
| `cam2world` | PV 카메라 → Unity world |

**양쪽에 C가 붙는 conjugation**이라는 게 중요하다. 앞쪽 C는 카메라 좌표를, 뒤쪽 C는 object 좌표를
변환한다(mesh 정점이 `.obj` = srt3d object 프레임에 있으므로).

한쪽만 붙이면 `det = -1`인 **반사 행렬**이 되어 mesh가 거울상으로 그려진다.
그걸 없애려고 `C`를 `diag(1,-1,-1)`(det +1)로 바꾸면 반사는 사라지지만 **Z가 뒤집혀
홀로그램이 카메라 뒤에 그려진다.** 증상을 가린 것이지 고친 게 아니다.

### 5.2 ⚠️ 카메라 소스에 따라 C가 달라진다

**이게 핵심 함정이다.** `cam2world`를 어디서 얻었느냐에 따라 광학축 규약이 다르다:

| 카메라 소스 | cam2world 규약 | 필요한 C |
|---|---|---|
| `PhotoCapture.TryGetCameraToWorldMatrix()` | **−Z forward** | `diag(1, -1, -1)` |
| MediaFrameReader + MS `ToUnity()` 헬퍼 | **+Z forward** (Unity 표준) | `diag(1, -1, 1)` |

**PhotoCapture가 −Z인 근거**: MS 샘플이 카메라 위치/회전을 이렇게 뽑는다.
```
position = c2w.GetColumn(3) - c2w.GetColumn(2)      // −Z 방향으로 이동
rotation = Quaternion.LookRotation(-c2w.GetColumn(2), c2w.GetColumn(1))
```
`-col2`를 forward로 쓴다는 것이 곧 −Z forward 규약이다.

**MFR + ToUnity()가 +Z인 근거**: `ToUnity()`의 `F = diag(1,1,-1)` conjugation이 Windows(RH)를
Unity(LH)로 옮기면서 광학축을 +Z forward로 정렬한다.

> **카메라 소스를 바꾸면 C의 전제가 깨진다.** PhotoCapture로 개발하다 MediaFrameReader로
> 갈아타면 이전에 맞던 `diag(1,-1,-1)`이 조용히 틀리기 시작한다. 코드는 그대로인데 결과만 틀린다.

차이는 Z 하나뿐이다:
```
diag(1,-1,-1) · M  의 translation = (tx, −ty, −tz)
C · M · C          의 translation = (tx, −ty, +tz)
```

### 5.3 ToUnity() 헬퍼는 직접 짜지 말 것

`System.Numerics.Matrix4x4`(WinRT, row-vector, RH) → `UnityEngine.Matrix4x4`(column-vector, LH)
변환은 **transpose + F·A·F conjugation**을 동시에 해야 한다. MS 공식 헬퍼를 그대로 쓸 것:

```csharp
// Srt3dPvCapture.cs:243 — MS "Converting between coordinate systems" 문서의 헬퍼
static Matrix4x4 NumericsToUnity(System.Numerics.Matrix4x4 m)
    => new Matrix4x4(
        new Vector4( m.M11,  m.M12, -m.M13,  m.M14),
        new Vector4( m.M21,  m.M22, -m.M23,  m.M24),
        new Vector4(-m.M31, -m.M32,  m.M33, -m.M34),
        new Vector4( m.M41,  m.M42, -m.M43,  m.M44));
```
직접 짜면 대개 **한쪽만 flip**하게 되고, 그러면 거울상 + 뒤쪽 렌더가 된다.

### 5.4 증상별 판별표

| 증상 | 원인 후보 | 확인 방법 |
|---|---|---|
| 회전은 맞는데 **위치만 평행이동** | principal point(cx/cy) 또는 이미지 flip과 K 불일치 | 이미지를 flip 했다면 `cx' = (w-1) - cx`를 같이 했는지 |
| 물체가 **카메라 뒤로 감** | **Z 부호** — C가 `diag(1,-1,-1)`인지 `diag(1,-1,1)`인지 | `ob_in_cam`의 tz는 양수인데 head-local Z가 음수면 확정 |
| **거울상**으로 그려짐 | conjugation 한쪽만 적용 (det = −1) | `T.determinant`를 찍어본다. +1이어야 정상 |
| 원점(0,0,0)에 붙어 있음 | `cam2world`를 못 받아 identity로 그림 | 비 locatable 프레임이면 렌더를 **스킵**해야 한다 |
| 상하만 뒤집힘 | 이미지 원점(top-left vs bottom-left) | MFR = top-left, HoloLensCameraStream raw = bottom-up |

**진단 팁**: 후보 행렬을 여러 개 만들어 각각의 object 원점을 head-local로 환산해 HUD에 병기하면
한 번의 빌드로 판별된다. 단 **렌더는 하나로 고정**할 것 — 자동 선택(auto-pick)은 어느 게
맞았는지 알 수 없게 만든다.
```csharp
Vector3 p = cam.transform.InverseTransformPoint(T.MultiplyPoint3x4(Vector3.zero));
// p.z 가 ob_in_cam 의 tz 와 부호·크기가 맞는 후보가 정답
```

### 5.5 mesh 정점 변환은 full 4x4로

`Matrix4x4.rotation` / `Transform` 분해는 반사 행렬(det −1)에서 깨진다. 후보 판별 중에는
반사가 섞일 수 있으므로 정점을 직접 변환할 것 (`Srt3dTracker.cs`의 `ApplyPose`):
```csharp
for (int i = 0; i < _objVerts.Length; i++)
    _worldVerts[i] = T.MultiplyPoint3x4(_objVerts[i]);
```
GameObject transform은 identity로 두고 mesh 정점 자체를 world로 만든다.

---

## 6. 렌즈 왜곡 — API는 undistort를 전제하고, HL2 PV는 실제로 그 조건을 만족한다

`srt3d_init`은 `[fx, cx, fy, cy]` **핀홀 4개만** 받는다(§4.3). 왜곡 계수 인자가 없다는 것은
**입력 이미지가 이미 undistort 되어 있다고 가정**한다는 뜻이다.

### 6.1 결론 먼저: PV 왜곡 계수는 전부 0 (실측, 2026-07-22)

기기(`<DEVICE_IP>`)에서 hl2ss로 PV 캘리브레이션을 받아 확인했다.
저장 위치: `hl2_pipeline/calibration/personal_video/1000_640_360/`

```
focal_length     fx=493.99  fy=494.21          (640x360)
principal_point  cx=315.90  cy=167.47
radial           k1=+0.000000  k2=+0.000000  k3=+0.000000
tangential       p1=+0.000000  p2=+0.000000
```
이미지 어디에서든 핀홀 대비 변위가 **0.00 px**다(코너 r=0.760 포함).

fx/fy/cx/cy는 정상적인 실측값이 나왔으므로(같은 페이로드를 파싱한다) 파싱 실패로 0이 나온 게
아니다. 정규화값도 기기 MFR이 보고하는 값과 일치한다:
`cx/W = 0.4936`(640x360) vs `442/896 = 0.4933`(896x504).

→ **HoloLens 2 PV 파이프라인은 이미 rectify 된 프레임을 준다.**
   srt3d가 요구하는 "undistort된 입력" 조건이 이미 충족되어 있다. 할 일이 없다.

> 다른 카메라로 옮긴다면 이 값을 **반드시 다시 재야 한다.**

### 6.2 API 규약상으로는 왜곡이 있을 수 있다

`Windows.Media.Devices.Core.CameraIntrinsics` MS 공식 문서:
> 클래스 설명: *"Represents the intrinsics that describe the camera **distortion model**."*
>
> `UndistortedProjectionTransform`: *"...transforms a 2D coordinate ... **without compensating
> for the distortion model of the camera**. The 2D point ... **will not accurately map to the
> pixel coordinate in a video frame unless the app applies its own distortion compensation**."*

즉 `FocalLength`/`PrincipalPoint`는 핀홀 부분일 뿐이고, 계수가 0이 아니라면
`RadialDistortion`(k1,k2,k3) / `TangentialDistortion`(p1,p2)를 **추가로 적용**해야 한다.
**API는 왜곡이 0이라고 보장하지 않는다** — 그래서 §6.1의 실측이 필요했다.

### 6.3 우리는 undistort를 하지 않는다 (그리고 할 필요가 없다)

`Srt3dPvCapture.cs:207-208`이 읽는 것은 `FocalLength`와 `PrincipalPoint`뿐이다:
```csharp
fx = intr.FocalLength.X;    fy = intr.FocalLength.Y;
cx = intr.PrincipalPoint.X; cy = intr.PrincipalPoint.Y;
```
`RadialDistortion` / `TangentialDistortion` / `UndistortPoint`는 **한 번도 참조하지 않는다.**
RGB는 `SoftwareBitmap` BGRA에서 채널만 바꿔 그대로 넘긴다 — remap 단계가 없다.
§6.1의 실측 때문에 이건 **문제가 아니다.**

### 6.4 PC 쪽(FoundationPose)도 똑같이 무시한다 — 확인됨

- `hl2ss_3dcv.py:207` `pv_create_intrinsics(focal_length, principal_point)` — 핀홀만 조립
- RM 센서에는 `rm_vlc_undistort` / `rm_depth_undistort`가 있는데
  (`hl2ss_3dcv.py:157,173`) **PV에는 대응하는 undistort 헬퍼도 map도 없다**
- `hl2_capture.py:42-50` `intrinsics_to_K()`도 fx/fy/cx/cy만 뽑는다

hl2ss의 PV 캘리브레이션 구조체에 `radial_distortion` / `tangential_distortion`이
**존재하지만**(`hl2ss.py:2011-2015`) 파이프라인에서 쓰이지 않는다.

### 6.5 함의

| 항목 | 결론 |
|---|---|
| FP 초기 pose ↔ srt3d 추적의 **상호 불일치** | **아니다.** 양쪽 다 핀홀만 쓰고 실제 왜곡도 0 |
| 실제 기하 대비 **계통 오차** | **없다.** 실측 \|Δ\| = 0.00 px (코너 포함) |
| conf 저하의 원인 후보 | **배제됨** |

> ⚠️ **왜곡 보정을 추가하려는 충동을 조심할 것.** 계수가 0이므로 얻을 게 없고,
> 기기 쪽(srt3d)과 PC 쪽(FoundationPose) 중 **한쪽에만** remap을 넣으면
> 지금 성립하는 두 파이프라인의 일관성이 깨져서 오히려 나빠진다. 바꿀 거면 양쪽을 같이.

### 6.6 재측정 방법 (다른 카메라/해상도로 옮길 때)

PV 캘리브레이션은 hl2ss로 받는다. **wiseui 앱이 기기에서 실행 중이어야 한다.**
```python
import sys; sys.path.insert(0, r"<HL2SS_REPO>\viewer")
import hl2ss, hl2ss_lnm, hl2ss_3dcv
hl2ss_lnm.start_subsystem_pv(HOST, hl2ss.StreamPort.PERSONAL_VIDEO)
c = hl2ss_3dcv.get_calibration_pv(OUT, HOST, hl2ss.StreamPort.PERSONAL_VIDEO,
                                  width=640, height=360, framerate=30)
print(c.radial_distortion, c.tangential_distortion)   # k1,k2,k3 / p1,p2
hl2ss_lnm.stop_subsystem_pv(HOST, hl2ss.StreamPort.PERSONAL_VIDEO)
```
- **hl2ss PV mode2는 표준 해상도만 받는다.** 896x504로 요청하면 `Exception: connection closed`.
  640x360으로 받을 것 — **왜곡 계수는 정규화 좌표계라 해상도와 무관**하다.
- 결과는 `<OUT>/personal_video/<focus>_<W>_<H>/`에 캐시된다. 다시 재려면 폴더를 지울 것.
- 환경은 `conda my_base` (base에는 `av` 모듈이 없어 hl2ss import가 실패한다).

계수가 0이 아니라면 선택지는 둘이다:
1. **트래커에 넣기 전 remap** — `cv::initUndistortRectifyMap` + `cv::remap`.
   프레임당 비용이 늘고, K도 undistort 후 값으로 바꿔야 한다.
2. **API 확장** — `srt3d_init`에 distCoeffs를 추가하고 코어의 투영식에 반영. 범위가 훨씬 크다.

어느 쪽이든 **PC 쪽(FoundationPose)도 같이** 바꿔야 한다(§6.5의 경고).

---

## 7. 온디바이스 제약 — 나중에 같은 걸 하는 사람이 알아야 할 것

### 7.1 GL 제거가 가장 큰 제약 → `.meta`를 기기에서 만들 수 없다

UWP엔 데스크톱 OpenGL이 없어서 `SRT3D_NO_GL`로 렌더러 계열을 통째로 뺐다(§2.5b).
`srt3d/src/`에는 **6개 .cpp만** 있다: `body, camera, common, model, region_modality, tracker`.
`normal_renderer.h`, `normal_viewer.h`, `occlusion_renderer.h`, `renderer.h`,
`renderer_geometry.h`, `viewer.h`는 **헤더만 있고 구현이 없다.**

**결과: `.meta`(sparse viewpoint model)를 기기에서 생성할 수 없다.**
`.meta`는 여러 시점의 윤곽 템플릿 묶음이고, 원본은 없으면 `tracker.setup()`이 `NormalRenderer`(GL)로
자동 생성한다. GL-free 빌드는 **로드만** 한다.

→ `.meta`는 GL 있는 머신에서 미리 만들어 배포해야 한다. 현재는 WSL conda 환경에서:
```
conda activate srt3d
cd /mnt/d/ProjectsTracking/pysrt3d
python gen_meta.py joke_book
```
현재 배포본은 **16MB**다 (`Assets/StreamingAssets/srt3d/joke_book_hl2c.obj.meta.bytes`).
**객체를 바꾸면 mesh와 `.meta`를 같이 다시 만들어야 한다.**

> M3T로 갈아타도 이 문제는 사라지지 않는다 — M3T도 `region_model.bin` / `depth_model.bin`을
> 렌더러로 사전 생성한다(M3T readme). 오히려 모달리티가 늘어 생성물이 늘어난다.

### 7.2 UWP 링커: vccorlib/msvcrt 순서

`vccorlib.lib`가 `msvcrt.lib`보다 **먼저** 링크돼야 한다. 아니면:
- `LNK2038: vccorlib_lib_should_be_specified_before_msvcrt_lib`
- `LNK2005: __crtWinrtInitType already defined`

MS 공식 해법대로 자동 링크를 끄고 순서를 명시한다 (`CMakeLists.txt:42-52`):
```cmake
if(CMAKE_SYSTEM_NAME STREQUAL "WindowsStore")
    target_link_options(srt3d_uwp PRIVATE
        $<$<CONFIG:Release>:/NODEFAULTLIB:vccorlib.lib>
        $<$<CONFIG:Release>:/NODEFAULTLIB:msvcrt.lib>
        $<$<CONFIG:Release>:vccorlib.lib>
        $<$<CONFIG:Release>:msvcrt.lib>
        ... Debug 도 동일 패턴(vccorlibd/msvcrtd)
endif()
```
**Debug/Release가 라이브러리 이름이 다르므로 둘 다 필요하다.**

### 7.3 비표준 C++ (MSVC 확장에 의존)

`virtual_camera.hpp`의 `PushImage`가 **non-const 참조**를 받는데:
```cpp
void PushImage(cv::Mat& image)                       // virtual_camera.hpp
g_camera->PushImage(view.clone());                   // srt3d_uwp.cpp:112 — rvalue 전달
```
`view.clone()`은 임시 객체(rvalue)라 표준 C++에선 `cv::Mat&`에 바인딩되지 않는다.
MSVC가 기본으로 허용하는 확장(경고 C4239)이다.
→ **`/permissive-`나 `/Zc:referenceBinding`을 켜면 컴파일이 깨진다.** 다른 컴파일러로
옮길 계획이면 `const&`나 값 전달로 고쳐야 한다.

### 7.4 스레딩

- 네이티브는 **단일 세션 전역 상태**, 락 없음 (`srt3d_uwp.cpp:34-42`). 동시 호출 금지.
- OpenCV를 `WITH_TBB=OFF`, `WITH_OPENMP=OFF`로 빌드했다 → 내부 병렬화 없음.
  HL2의 제한된 CPU에서 의도한 선택이지만, 트래킹이 **완전히 단일 스레드**라는 뜻이다.
- `VirtualCamera`는 `set_realtime(true)`로 큐에 쌓인 프레임을 버리고 최신 것만 쓴다
  (`srt3d_uwp.cpp:78`). 처리가 캡처보다 느려도 지연이 누적되지 않는다.

### 7.5 성능에 직접 걸리는 것

- **프레임당 이미지 복사 2회** (§4.2). 가장 명확한 최적화 지점 — 896×504@30fps에서 ≈81MB/s.
- **conf 계산의 숨은 correspondence 1회** (§2.3). conf가 불필요하면 삭감 가능.
- 트래커 반복 횟수 (`srt3d_uwp.cpp:81-82`):
  ```cpp
  g_tracker->set_n_corr_iterations(7);
  g_tracker->set_n_update_iterations(2);
  ```
- 입력 해상도가 곧 비용이다. `srt3d_init`의 `w,h`와 `srt3d_track_rgb`의 `w,h`는 같아야 하므로,
  낮추려면 **K도 같은 비율로 스케일**해야 한다 (fx, fy, cx, cy 전부).

### 7.6 그 밖의 함정

- **`.meta` 확장자 충돌** (§4.6). Unity 프로젝트라면 반드시 만난다.
- **StreamingAssets는 UWP에서 `File.IO` 불가.** `UnityWebRequest`로 읽어
  `persistentDataPath`에 복사한 뒤 그 경로를 네이티브에 넘긴다.
- **DLL은 Editor에 없다.** `Srt3dNative`의 선언 자체는 무해(지연 로드)하지만
  호출부는 `#if !UNITY_EDITOR`로 감싸야 한다 (`Srt3dNative.cs:3-4`).
- **PV 배타 점유.** hl2ss와 MediaFrameReader가 동시에 카메라를 못 잡는다(§1.2).

### 7.7 빌드 구성

```cmake
find_package(OpenCV REQUIRED COMPONENTS core imgproc)   # CMakeLists.txt:17
file(GLOB SRT3D_CORE .../srt3d/src/*.cpp)               # 코어 6개
add_library(srt3d_uwp SHARED ${SRT3D_CORE} srt3d_uwp.cpp)
target_compile_definitions(srt3d_uwp PRIVATE SRT3D_NO_GL)
```
```powershell
# build_uwp_arm64.ps1:17-24
cmake -S $root -B $build -G "Visual Studio 17 2022" -A ARM64 `
  -DCMAKE_SYSTEM_NAME=WindowsStore -DCMAKE_SYSTEM_VERSION="10.0" `
  -DCMAKE_SYSTEM_PROCESSOR=ARM64 -DCMAKE_POLICY_VERSION_MINIMUM="3.5" `
  -DOpenCV_STATIC=ON `
  -DOpenCV_DIR="<OPENCV_INSTALL>\ARM64\vc17\staticlib"
```

> ⚠️ `OpenCV_DIR`은 **`install/ARM64/vc17/staticlib`를 직접** 가리켜야 한다.
> install 루트의 dispatcher `OpenCVConfig.cmake`가 static 구성을 인식하지 못한다.

산출물을 `Assets/Plugins/WSA/ARM64/srt3d_uwp.dll`에 넣고, Unity Inspector에서
플랫폼 **WSAPlayer / ARM64**로 지정한다.

---

## 8. 다른 트래커로 교체하려면

이 6개 함수만 같은 규약으로 구현하면 `Srt3dTracker.cs`는 거의 그대로 쓸 수 있다.
최소 계약은 실질적으로 **3개**다:

| 함수 | 계약 |
|---|---|
| `init(meshPath, K9, w, h) → int` | 모델 로드 + K 설정. 1=성공 |
| `reset_pose(pose16) → int` | ob_in_cam(row-major, 미터) 주입 |
| `track_rgb(rgb, w, h, out17) → int` | RGB24 top-left, stride=w*3 → pose16 + conf |

교체 시 확인할 것:
1. **pose 규약** (row-major? ob_in_cam? 미터? +Z 전방?) — §4.4의 검증 절차를 그대로 밟을 것
2. **이미지 원점과 stride** 가정 (§4.2)
3. **K에서 실제로 읽는 항목** (skew/왜곡 지원 여부) (§4.3)
4. **스레드 안전성** — 현재 호출부는 메인 스레드 단일 호출을 전제한다 (§7.4)
5. **confidence의 의미와 범위** — 현재는 pysrt3d의 KL 비율이고 stock SRT3D에도 M3T에도 없다 (§2.3)
6. **사전 생성물** — `.meta` 같은 오프라인 산출물이 필요한지, 기기에서 만들 수 있는지 (§7.1)

---

## 9. 2026-07-23 변경 — 초기 자세 밀림 수정

> **⚠️ 상태: 2026-07-23 기준 구현 완료, 기기 실측 검증 대기.**
> 아래 수정들은 코드 분석으로 원인을 짚고 구현·배포까지 했으나, **실제로 tilt를 고쳤는지는
> 아직 관측 안 됨.** `tz/median ≈ 1.02~1.05`도 예측이지 관측이 아니다.
> **검증 기준 (등록 후 확인):**
> - (a) FP 로그 `tz/median ≈ 1.02~1.05` (원점이 앞면보다 두께 절반 뒤)
> - (b) 틀린 자세에 conf 1.00 이 **안** 나옴
> - (c) mesh 가 책 기울기를 따라가고, 여러 번 등록해도 방향이 일정
>
> 오늘 하루 "코드로 확정했는데 실측이 뒤집은" 일이 반복됐다(UV 가설·탐색선 길이 가설·depth
> 가설 모두 실측에서 무너짐, §9.4·부록). 그러니 이 절의 "수정함"은 **"구현함"**으로 읽고,
> 검증 결과가 나오면 이 상태 줄을 갱신할 것.

**증상:** FP 초기 pose로 등록하면 mesh가 책에서 밀리고 기울어진 채 추적됨. 여러 겹 진단 끝에
**인터페이스는 하나도 안 바뀌고**(타입·마샬링·호출 순서 §3~4 그대로) **내부 동작/데이터만** 바꾼 3가지.

### 9.1 mesh를 bbox 중심으로 재정렬 (`pose16` 원점 변경)

**원인 (코드 확정):** 예전 mesh는 원점이 bbox 중심에서 **10.3cm** 치우쳐 있었다.
srt3d 는 `maximum_body_diameter = 2.2 × (원점기준 max_radius)` (`body.cpp:143`)로 계산하므로
원점 오프셋이 diameter를 **0.43으로 2배 부풀렸다**(실제 bbox 대각 0.211). 그 diameter는 템플릿
생성에서 가상 카메라 focal_length를 정한다 — `focal = image_size × sphere_radius / diameter`
(`model.cpp:544`, focal ∝ 1/diameter). 그래서 **물체가 2000×2000 템플릿에 절반 크기로 렌더** →
윤곽선이 성기게 샘플링 → **기울어진 사다리꼴 vs 정면 사각형을 구분 못 함** → tilt 교정 실패 +
틀린 자세에 conf 1.00.

> ⚠️ **탐색선 길이는 diameter와 무관하다.** `line_length = 19세그먼트 × scales{5,2,2,1}` 고정
> (`region_modality.cpp:500,566`). "diameter가 탐색선을 늘린다"는 오해였고, 실제 경로는
> **템플릿 focal_length**다.

**수정:** mesh를 bbox 중심으로 평행이동(정점만, UV/faces/mtl 유지) → **재생성된 `.meta`의
`max_body_diameter = 0.2157`** (옛 0.4287의 절반)로 정상화 → 템플릿에 물체 2배 크게 렌더 →
윤곽 조밀. `.meta`는 재생성 필수(§9.3).

> **diameter 숫자 주의.** srt3d 는 `2.2 × (원점기준 max_radius)`로 계산하므로(`body.cpp:143`)
> `.meta` 헤더값은 **0.2157**이다. mesh 를 직접 재면 나오는 값과 다르다:
> AABB 대각 0.211, OBB 대각 0.209, `2×max_radius` 0.196, FP 자체 `compute_mesh_diameter` 0.1945.
> **정상 범위는 0.19~0.22.** 문서에서 "0.216"은 이 `.meta` 헤더값(0.2157) 반올림이다.
> (0.43 처럼 2배로 나오면 원점 오프셋 재발.)

**함의:**
- `pose16` translation이 이제 **물체 bbox 중심** 기준(예전엔 치우친 원점). §4.4 참조.
- HUD `obZ`(=`out17[11]`)가 표면 거리가 아니라 중심 거리. 검증: FP `tz/median ≈ 1.02~1.05`
  (원점이 앞면 표면보다 두께 절반 ~1.2cm 뒤) 이면 mesh·FP·depth·meta 전부 정합.
- **Cat.meta(정상 예제) 대조:** 생성 파라미터(sphere_radius 0.8, n_divides 4, n_points 200,
  image_size 2000) 동일, diameter만 우리가 2배였음 → 원점 오프셋이 유일한 결함이었음을 확인.

### 9.2 회복 부스트 (`reset_pose` 직후 12프레임)

**관측:** FP 가 이 납작한 책의 **tilt(viewpoint)를 현재 설정에서 못 정한다** — 등록 1회의 상위 25개
후보 viewpoint가 구 전체에 60~90° 흩어짐.

> **측정 조건 명시.** 이 60~90° 흩어짐은 **텍스처 수정 후(§9.4) + fp32 전환 후(FP_AMP=0)** 한 번의
> 등록에서 잰 값이다(sort_ids 상위 25개의 `idx//6`). fp32 전환으로 1위 후보가 통째로 바뀌었으므로
> 이 수치는 **그 실행의 관측**이지 물체의 불변 성질이 아니다. "원리적으로 불가능"이 아니라
> "현재 설정에서 관측됨"으로 읽을 것. in-plane 회전은 텍스처로 잘 정해졌다(상위 25개의 `idx%6`가
> 한 값에 16/25 쏠림) — 못 정하는 건 out-of-plane tilt 뿐이다.

srt3d 가 교정해야 하는데(§1.2: 같은 SRT3D가 Galaxy 에선 끌어왔음), 기본 회복 basin이 좁다: 가장 넓은
scale 5 → `line_length 95px`(±47px). FP 가 준 크게-틀린 tilt가 그 basin 밖이면 못 끌어오고
배경 엣지를 물어 conf 만 높을 수 있다.

**수정 (`srt3d_uwp.cpp`):** `reset_pose`가 `g_boost_frames=12` 설정. `track_rgb`가 부스트 중이면
`scales{12,8,5,3,2,1}`(basin ~228px) + `n_corr=30`, 이후 평소값 복귀. `set_scales`는 `set_up_`을
안 건드려 재-setup/모델 재생성 없이 즉시 반영(GL-free 기기에서 안전). C ABI 불변.

### 9.3 Galaxy XR 격리 — HL 전용 mesh 분리

FP 서버(`fp_server_gxr.py`, "gxr"=Galaxy XR)와 mesh(`FoundationPose/my_data/joke_book/...`)가
Galaxy XR과 **공유**된다. 재정렬을 공유본에 하면 Galaxy pose가 어긋난다. 그래서:
- 공유 `optimized_poisson_texture_mapped_mesh.obj` / `.obj.meta`는 **원상 복구**(Galaxy 그대로).
- HL 전용 **`joke_book_hl2c.obj`**(재정렬본, 원본 `.mtl`/`.png` 공유) 신규 생성.
  `.meta`는 `gen_meta.py --force joke_book_hl2` → `joke_book_hl2c.obj.meta`.
- 기기 `Assets/StreamingAssets/srt3d/model.obj`(+`.meta.bytes`)는 `hl2c` 소스(바이트 동일 짝).
- FP amp 기본은 **on 복구**(Galaxy 무영향), HL 세션만 `FP_AMP=0`(fp32 — fp16 양자화가 회전
  후보 순위를 뭉갰음. 별개 발견).

**HL2 세션 FP 실행:**
```
$env:FP_AMP=0
python fp_server_gxr.py --mesh my_data/joke_book/textured_meshes/joke_book_hl2c.obj
```
**Galaxy 세션:** `--mesh .../optimized_poisson_texture_mapped_mesh.obj` (amp 기본 on).

### 9.4 부수 발견 (인터페이스 무관, 기록만)

- **trimesh OBJ 텍스처 버그:** `trimesh.load(obj)`가 `map_Kd` PNG를 **2×2 더미**로 로드
  (실제 1814×890 무시). FP render-compare가 균일색으로 렌더 → 회전 판별 0. `fp_server_gxr.py`가
  로드 후 실제 PNG를 `material.image`에 직접 붙여 우회. (UV 자체는 정상 — 렌더에 JOKES 또렷.)
- **fp16 양자화:** scorer가 `torch.cuda.amp.autocast`로 돌면 `score_logit`이 fp16 정밀도로
  뭉개져(54 근처 간격 0.03125) 252개 회전 순위가 동점화. `FP_AMP=0`으로 fp32.

---

## 부록 A. 미확인 / 후속 확인이 필요한 항목

정직하게 남겨 둔다. 사수님이 되물을 가능성이 높은 순서로.

| # | 항목 | 상태 | 확인 방법 |
|---|---|---|---|
| 1 | Galaxy XR `m3t_jni.cpp`가 실제로 켜는 모달리티 | **미확인** — 프로젝트가 이 머신에 없음 | `m3t_jni.cpp`에서 `DepthModality`/`TextureModality` 생성 및 `Optimizer` 등록 여부 확인 |
| 2 | pysrt3d 포크 커밋 해시 | **미확인** — VCS 메타데이터 없음 | vendored 코어를 pysrt3d 커밋들과 diff해 일치점 탐색 |
| 3 | pose가 OpenCV 축 규약(+Y down/+Z fwd)인지 | **실측 기반** (소스로 단정 못 함) | §4.4(d) |
| 4 | Galaxy XR이 쓰는 M3T 리비전 | **미확인** | 해당 프로젝트의 서브모듈/커밋 확인 |
| 5 | **같은 SRT3D인데 Galaxy XR은 초기 자세를 끌어왔고 HL2는 못 끌어온 이유** | **미확인** — 알고리즘 아닌 조건 차이 | 후보: ① `.meta`/mesh(원점·diameter) ② srt3d 런타임 파라미터(scales·n_corr·kl) ③ 카메라·해상도 ④ 배경 복잡도 ⑤ FP 초기 pose가 틀린 정도. §9 수정(재정렬·부스트)이 ①②를 건드림 — 검증 대기 |
| 6 | **§9 수정(재정렬·부스트)이 실제로 tilt를 고쳤는지** | **구현 완료, 실측 검증 대기** | §9 머리 검증 기준 (a)(b)(c) |
