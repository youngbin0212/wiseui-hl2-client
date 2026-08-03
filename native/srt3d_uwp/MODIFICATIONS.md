Upstream: DLR-RM/3DObjectTracking @ 11ae750

Intermediate: pysrt3d (fork point not recorded)

This directory contains a full modified snapshot, not a patch series.

---

# srt3d_uwp — 원본 대비 수정 내역

이 디렉터리의 `srt3d/` 는 stock SRT3D 가 **아니다.** 두 단계의 수정을 거쳤다:

```
DLR-RM/3DObjectTracking · SRT3D  @ 11ae750   원본 (2021-10-26)
  └→ pysrt3d (Jianxff, MIT, 2024)            수정 ①  ← §1
       └→ XRHandEyeTracker source_nogl       (Galaxy XR, GL 제거)
            └→ srt3d_uwp                     수정 ②  ← §2, §3
```

- 원본: <https://github.com/DLR-RM/3DObjectTracking/tree/master/SRT3D>
- pysrt3d: <https://github.com/Jianxff/pysrt3d> (MIT, 2024)

**라이선스:** `srt3d/` 의 18/19 파일이 `SPDX-License-Identifier: MIT` +
`Copyright (c) 2021 Manuel Stoiber, German Aerospace Center (DLR)` 헤더를 보유한다.
헤더가 없는 1개는 `srt3d/virtual_camera.hpp`(§2.3에서 추가된 신규 파일)다.

> ⚠️ **재현성 공백 — pysrt3d 포크 시점 미기록.**
> 원본 `srt3d_uwp/` 트리에 VCS 메타데이터가 없었고, `CMakeLists.txt:19` 주석은
> "XRHandEyeTracker 에서 복사한 검증본"이라고만 밝힌다.
> **어느 pysrt3d 커밋에서 갈라졌는지 특정할 수 없다.** 그래서 patch series 가 아니라
> 전체 스냅샷으로 편입했다.
> upstream SRT3D 쪽은 위험이 없다 — `11ae750`(initial commit) 이후 `SRT3D/include`,
> `SRT3D/src` 가 한 번도 수정되지 않았고 이후 커밋은 readme 뿐이다.

> **줄 번호 표기.** 아래 줄 번호는 **이 스냅샷 기준**이다.
> `docs/SRT3D_INTERFACE.md` 의 인용값은 2026-07-23 부스트 추가(§3.1) 이전이라
> `srt3d_uwp.cpp` 에서 5~8줄 밀려 있다. 심볼 이름을 우선 기준으로 삼을 것.

---

## §1. pysrt3d 가 upstream SRT3D 에 가한 수정

pysrt3d 수정은 소스에 `==== Additional ====` 블록으로 명시돼 있어 식별이 쉽다.

### 1.1 KL divergence 기반 confidence 추가

| | |
|---|---|
| **무엇을** | pose 신뢰도 지표 `conf` 를 새로 도입. upstream 에는 이 개념 자체가 없다. |
| **왜** | 추적 품질을 런타임에 판정할 수단이 필요. (원문에 도입 동기가 명시돼 있지 않다 — **[확인 필요]**) |
| **어느 파일·함수** | `srt3d/include/srt3d/region_modality.h:129-140` (Additional 블록)<br>`srt3d/src/region_modality.cpp` — `RegionModality::FunctionKL()` (L19), `RegionModality::EvaluateDistribution()` (L28)<br>멤버 `kl_threshold_ = 1.0` (`region_modality.h:138`), setter `set_kl_threshold()` |

```cpp
// region_modality.h
// ==================== Additional ====================
float FunctionKL(float u, float std);
float EvaluateDistribution();
void set_kl_threshold(float kl_threshold);
float kl_threshold_ = 1.0;
// ==================== Additional ====================
```

**정의:** `conf = (KL divergence < kl_threshold_ 인 correspondence line 수) / (전체 line 수)`

- 범위 0.0~1.0 (비율이라 유계). **확률이 아니고, 재투영 오차와도 무관하다.**
- `kl_threshold_ = 1.0` 은 `srt3d_uwp.cpp` 의 `srt3d_init()` 에서
  `g_rm->set_kl_threshold(1.0f)` (L92) 로 고정된다.
- ⚠️ **다른 트래커의 confidence 와 직접 비교 불가.** M3T 에는 이 지표가 없고
  FoundationPose 의 score 와도 정의가 무관하다.
- 💡 **숨은 비용:** `EvaluateDistribution()` 이 내부에서 `CalculateBeforeCameraUpdate()` 와
  `CalculateCorrespondences(0)` 를 다시 호출한다 → conf 를 읽는 대가로
  프레임당 correspondence 계산이 1회 추가된다.

자세한 성질은 `docs/SRT3D_INTERFACE.md` §2.3 참조.

### 1.2 body diameter 자동 계산

| | |
|---|---|
| **무엇을** | `maximum_body_diameter` 를 생성자 인자에서 제거하고 mesh 정점에서 자동 계산. |
| **왜** | 원본은 호출자가 수동 지정해야 했다. (자동화 동기의 상세는 원문에 없음 — **[확인 필요]**) |
| **어느 파일·함수** | `srt3d/include/srt3d/body.h` — 생성자 시그니처, `calculate_maximum_body_diameter()` 선언 (L58)<br>`srt3d/src/body.cpp` — 정의 (L111), 생성자에서 호출 (L23), 계산식 (L143)<br>기존 setter 는 주석 처리 (`body.h:33`, `body.cpp:44-45`) |

```diff
   Body(const std::string &name, const std::filesystem::path &geometry_path,
        float geometry_unit_in_meter, bool geometry_counterclockwise,
-       bool geometry_enable_culling, float maximum_body_diameter,
-       const Transform3fA &geometry2body_pose, int occlusion_id = 0);
+       bool geometry_enable_culling, const Transform3fA &geometry2body_pose = Transform3fA::Identity());
+  // Auto calculate body diameter
+  void calculate_maximum_body_diameter();
```

**계산식** (`body.cpp:143`):
```cpp
maximum_body_diameter_ = 2.2f * max_radius;   // max_radius = 원점 기준 최대 정점 거리
```

⚠️ **생성자 시그니처가 원본과 다르다.** stock SRT3D 예제 코드를 그대로 붙이면 컴파일되지 않는다.

⚠️ **원점 기준**이라 mesh 원점이 bbox 중심에서 벗어나면 diameter 가 부풀려진다.
이 성질이 실제로 문제를 일으킨 사례는 `docs/SRT3D_INTERFACE.md` §9.1 참조
(원점 10.3cm 오프셋 → diameter 2배 → 템플릿 focal 절반 → tilt 교정 실패).

### 1.3 그 외 — upstream diff 로 특정

원문(`docs/SRT3D_INTERFACE.md` §2.2 (c))이 열거만 하고 상세를 남기지 않았던 항목들을
upstream `11ae750:SRT3D/` 와 직접 diff 해서 확인했다.

> **diff 방법.** `git archive 11ae750 SRT3D` 로 원본을 추출해 `srt3d/` 와 파일별 비교.
> ⚠️ upstream 은 CRLF, 스냅샷은 LF 라 `diff --strip-trailing-cr` 없이는 전 줄이
> 달라 보인다(예: `region_modality.cpp` 2108줄 → 실제 96줄).

#### 1.3.1 `ResetOcclusionMask()` 추가 — ✅ 확인됨

| | |
|---|---|
| **무엇을** | 외부에서 occlusion mask 를 주입/초기화하는 API 추가. upstream 에는 **이 심볼 자체가 없다.** |
| **왜** | 원문에 동기 미기재 — **[확인 필요]**. 코드상 `Tracker::TrackIter()` 가 매 호출마다 mask 를 갱신하는 경로로 쓴다. |
| **어느 파일·함수** | `RegionModality::ResetOcclusionMask()` — `region_modality.h:133`, `region_modality.cpp:43`<br>`Tracker::ResetOcclusionMask()` — `tracker.h:80`, `tracker.cpp:49` (전체 modality 에 fan-out)<br>호출부: `tracker.cpp:25` (`TrackIter()` 안) |
| **부수 멤버** | `occlusion_mask_` (`region_modality.h`, Additional 블록) |

#### 1.3.2 독립적 다중 모델 추적 — ⚠️ 부분 정정

| | |
|---|---|
| **무엇을** | **다중 modality 자체는 upstream 기능이다.** pysrt3d 가 추가한 것은 **모델별 결과를 개별 수집하는 API** 다. |
| **왜** | 원문에 동기 미기재 — **[확인 필요]** |
| **어느 파일·함수** | `Tracker::EvaluateDistribution()` → **`std::vector<float>`** (`tracker.h:78`, `tracker.cpp:41`)<br>`region_modality_ptrs_` 를 순회하며 modality 별 conf 를 `push_back`<br>`Tracker::ResetOcclusionMask()` 도 같은 방식으로 fan-out (`tracker.cpp:49`) |

**정정 근거:** upstream `11ae750` 의 `tracker.h` 가 이미 보유하고 있다 —
```cpp
void AddRegionModality(std::shared_ptr<RegionModality> region_modality_ptr);   // tracker.h:39
std::vector<std::shared_ptr<RegionModality>> region_modality_ptrs_;            // tracker.h:94
```
→ 원문 §2.2(c) 의 "독립적 다중 모델 추적"을 **신규 기능으로 읽으면 안 된다.**
실제 추가분은 "모델마다 별도 conf 를 뽑을 수 있게 된 것"이다
(단일 `float` 이 아니라 `std::vector<float>` 반환).

> ⚠️ **현재 빌드에서는 미사용.** `srt3d_uwp.cpp` 는 전역 단일 세션(§2.4)이라
> modality 를 1개만 등록한다. 다중 모델 경로는 UWP 래퍼에서 노출되지 않는다.

#### 1.3.3 "초기화 / 추적 임계값 분리" — ❌ upstream diff 에서도 미확인

**[확인 필요] 유지.** 사유:

- 스냅샷 전체에서 임계값(threshold) 계열 멤버는 **`kl_threshold_` 단 하나**이고,
  이는 §1.1 의 KL confidence 용이다. **초기화용/추적용으로 나뉜 임계값 쌍이 존재하지 않는다.**
- 전 파일 diff 의 추가 줄을 `threshold|thres|init` 로 훑어도 나오는 것은
  `kl_threshold_` 관련 5줄과 `Tracker::init_` 관련 3줄뿐이다.

**대신 diff 가 실제로 보여주는 것은 임계값이 아니라 _제어 흐름_ 의 초기화/추적 분리다:**

| | |
|---|---|
| **무엇을** | upstream 의 단일 루프 `StartTracker()` 를 주석 처리하고, 1프레임 단위 `TrackIter()` 로 대체. 최초 1회만 카메라 갱신 + modality 시작을 수행하고 이후엔 추적 사이클만 돈다. |
| **왜** | 원문에 동기 미기재 — **[확인 필요]**. 외부(JNI/C ABI)가 프레임을 밀어 넣는 구조에는 자체 루프를 도는 `StartTracker()` 를 쓸 수 없다. |
| **어느 파일·함수** | `Tracker::TrackIter()` — `tracker.h:77`, `tracker.cpp:10`<br>1회성 초기화 가드 `init_` — `tracker.h:107`, 분기 `tracker.cpp:16`, 세팅 `tracker.cpp:23`<br>주석 처리된 원본: `Tracker::StartTracker()`, `Tracker::ExecuteViewingCycle()` (`tracker.cpp:88-113`) |

```cpp
// tracker.cpp — Additional 블록
bool Tracker::TrackIter(const cv::Mat &occlusion_mask) {
  if(!set_up_) { throw std::runtime_error("Set up tracker " + name_ + " first"); return false; }
  if(!init_) {                                   // ← 초기화는 최초 1회만
    if (!UpdateCameras())          { throw std::runtime_error("Could not update cameras"); }
    if (!StartRegionModalities())  { throw std::runtime_error("Could not start region modalities"); }
    init_ = true;
  }
  ResetOcclusionMask(occlusion_mask);
  return ExecuteTrackingCycle(0);                // ← 추적은 매 호출
}
```

> 원문의 "임계값 분리"라는 표현이 위 제어 흐름 분리를 가리킨 것인지, 아니면 diff 에
> 잡히지 않는 다른 변경을 가리킨 것인지는 **판단하지 않는다.**
> 확인하려면 원문 §2.2(c) 를 쓸 때 참조한 근거를 되짚어야 한다.

#### 1.3.4 diff 에서 추가로 확인된 항목 (원문 미기재)

원문 §2.2 가 언급하지 않았으나 upstream diff 에 명확히 잡히는 변경들:

| 항목 | 위치 | 내용 |
|---|---|---|
| `Model` 경로 API 단순화 | `model.h:66, 74, 88, 150` | `directory`(`std::filesystem::path`) + `filename` 2개 → `meta_file`(`std::string`) 1개로 통합. 생성자 시그니처 변경 |
| `RotationMatrix2Vector()` | `region_modality.h:31`, `region_modality.cpp:8` | 회전행렬 → axis-angle 벡터 자유 함수 추가 |
| `min_standard_deviation_` | `region_modality.h:272`, 계산 `region_modality.cpp:509` | `FunctionKL()` 이 쓰는 멤버. upstream 에 **없음**(0건) |
| `len_data_lines()` / `get_n_lines()` | `region_modality.cpp:48, 52` | correspondence line 수 조회 접근자 |
| `set_occlusion_id()` 비활성 | `body.h:35` | 주석 처리 |
| `visualize_lines_correspondence_` 기본값 | `region_modality.h:250` | `false` → **`true`** 로 변경 |
| `debug_visualize_` | `region_modality.h` Additional | 디버그 플래그 추가 |

> `Model` 경로 API 변경은 `.meta` 취급과 직결된다 — `srt3d_uwp.cpp` 가
> `Model(name, g_body, mesh + ".meta")` 로 파일 경로를 통째로 넘기는 근거다(§2.2 참조).

---

## §2. UWP 포팅에서 가한 수정

Galaxy XR 용 `srt3d_jni.cpp` 를 UWP/IL2CPP 용으로 변환하면서 생긴 차이.

### 2.1 입력 포맷: JPEG → raw RGB

| | |
|---|---|
| **무엇을** | 이미지 입력을 JPEG 바이트에서 raw RGB24 버퍼로 변경. `cv::imdecode` 를 쓰지 않는다. |
| **왜** | OpenCV `imgcodecs` 의존성 제거. UWP static 빌드에서 JPEG/PNG/TIFF 코덱을 전부 끄기 위함. |
| **어느 파일·함수** | `srt3d_uwp.cpp` — `srt3d_track_rgb()`, 근거 주석 L114<br>(JNI 원본은 JPEG 를 받아 RGB 로 변환 후 push 했음 — 여기선 이미 RGB 라 그대로 push) |

⚠️ **전제 붕괴 지점:** JPEG 를 받도록 되돌리려면 `imgcodecs` 를 다시 켜야 하고,
그러면 `CMakeLists.txt` 의 OpenCV 구성(`BUILD_LIST="core,imgproc"`)도 바뀐다.

### 2.2 GL 전면 제거 (`SRT3D_NO_GL`)

| | |
|---|---|
| **무엇을** | OpenGL 의존 코드를 전부 `#ifndef SRT3D_NO_GL` 로 감싸 제외. Viewer / Renderer 계열 API 는 주석 처리. |
| **왜** | UWP 에 데스크톱 OpenGL 이 없다. |
| **어느 파일·함수** | 헤더: `model.h`(L10, 103, 130), `region_modality.h`(L11, 27), `tracker.h`(L10, 14, 24)<br>구현: `camera.cpp`(L73), `model.cpp`(L110-165, 300-346, 361, 533-557), `region_modality.cpp`(L86, 341, 698)<br>정의 위치: `CMakeLists.txt` — `target_compile_definitions(srt3d_uwp PRIVATE SRT3D_NO_GL)` |

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

🔴 **가장 큰 실질적 제약:** 모델 템플릿(`.meta`) 생성이 기기에서 불가능해진다.
`model.cpp:110-111` 이 명시적으로 알린다:
```cpp
#ifdef SRT3D_NO_GL
  std::cerr << "[SRT3D_NO_GL] GenerateModel disabled — precomputed .meta required"
```
→ `.meta` 는 **오프라인(GL 가능한 PC)에서 미리 생성**해 `StreamingAssets` 로 배포해야 한다.
상세는 `docs/SRT3D_INTERFACE.md` §7.1.

### 2.3 카메라 소스 교체 — `virtual_camera.hpp` 추가

| | |
|---|---|
| **무엇을** | upstream 의 카메라 구현 3종을 제거하고, 외부 버퍼를 밀어 넣는 `VirtualCamera` 를 추가. |
| **왜** | Azure Kinect / 파일 로더 / GL 뷰어는 UWP 에서 쓸 수 없다. 프레임은 Unity 쪽 PhotoCapture 가 공급한다. |
| **어느 파일·함수** | 제거: `azure_kinect_camera.h`, `loader_camera.h`, `image_viewer.h`<br>추가: `srt3d/virtual_camera.hpp` — `srt3d::VirtualCamera`<br>사용: `srt3d_uwp.cpp` — `g_camera`(L37), `set_intrinsics()`/`SetUp()`/`set_realtime(true)` (L84-86) |

```
upstream  SRT3D/include/srt3d/           헤더 15개
snapshot  srt3d/include/srt3d/           헤더 12개  = 15 − (위 3개)
          srt3d/virtual_camera.hpp       ← 추가분. include/ 아래가 아니라 한 단계 위
```

⚠️ **include 경로가 이 파일만 다르다** (`srt3d_uwp.cpp`):
```cpp
#include <srt3d/tracker.h>              // 나머지는 꺾쇠 + include/ 경로
#include "srt3d/virtual_camera.hpp"     // 이것만 따옴표 + 상대 경로
```
그래서 `CMakeLists.txt` 가 include 디렉터리를 **둘 다** 등록한다
(`${CMAKE_CURRENT_SOURCE_DIR}` 와 `${CMAKE_CURRENT_SOURCE_DIR}/srt3d/include`).

### 2.4 전역 단일 세션 C ABI

| | |
|---|---|
| **무엇을** | JNI 시그니처를 `extern "C" __declspec(dllexport)` C ABI 6함수로 변환. 상태는 파일 전역 `shared_ptr` 5개로 보관. |
| **왜** | Unity `DllImport` 로 호출하기 위함. |
| **어느 파일·함수** | `srt3d_uwp.cpp` — `SRT3D_API` 매크로 정의 (L29), 전역 `g_body`/`g_model`/`g_camera`/`g_rm`/`g_tracker` (L35-39)<br>노출 함수 6개: `srt3d_init`, `srt3d_reset_pose`, `srt3d_track_rgb`, `srt3d_release`, `srt3d_last_error`, `srt3d_set_log_callback` |

⚠️ **락이 없다.** 단일 세션 · 메인 스레드 단일 호출을 전제한다. 상세는
`docs/SRT3D_INTERFACE.md` §7.4. 호출 규약과 인자 타입은 §3~4.

---

## §3. 2026-07-23 변경

### 3.1 회복 부스트 (`reset_pose` 직후 12프레임)

| | |
|---|---|
| **무엇을** | `reset_pose()` 가 `g_boost_frames = 12` 를 설정. `track_rgb()` 가 부스트 중이면 넓은 탐색 파라미터를 쓰고, 이후 평소값으로 복귀. |
| **왜** | FoundationPose 가 준 초기 tilt 가 srt3d 의 기본 회복 basin 밖이면 끌어오지 못하고, 배경 엣지를 물어 conf 만 높게 나올 수 있다. |
| **어느 파일·함수** | `srt3d_uwp.cpp` — `g_boost_frames` (L47), 설정부는 `srt3d_reset_pose()`, 적용부는 `srt3d_track_rgb()`<br>기본값은 `srt3d_init()` 의 `set_n_corr_iterations(7)` / `set_n_update_iterations(2)` (L89-90) |

| | 기본(추적) | 부스트(회복) |
|---|---|---|
| scales | `{5,2,2,1}` | `{12,8,5,3,2,1}` |
| 최대 탐색 반경 | 95 px (±47) | 228 px (12 × 19 세그먼트) |
| `n_corr` | 7 | 30 |

**구현 근거 주석** (`srt3d_uwp.cpp:45-46`):
```
// 기본 scales {5,2,2,1}(최대 95px) + n_corr=7 은 '추적' 값이라 회복 basin 이 좁다.
// 부스트: scales {12,8,5,3,2,1}(최대 12*19=228px) + n_corr=30 → basin 확장. 수렴 후 평소 값 복귀.
```

- `set_scales` 는 `set_up_` 을 건드리지 않아 **재-setup / 모델 재생성 없이 즉시 반영**된다
  (GL-free 기기에서 안전 — §2.2 참조).
- **C ABI 불변.** 타입·마샬링·호출 순서가 바뀌지 않았다.

> ⚠️ **검증 상태: 2026-07-23 기준 구현 완료, 기기 실측 검증 대기.**
> 원문 §9 가 명시한다 — "이 절의 '수정함'은 '구현함'으로 읽고, 검증 결과가 나오면 갱신할 것."
> 부스트가 실제로 tilt 를 교정했는지는 **아직 관측되지 않았다.**
> 검증 기준은 `docs/SRT3D_INTERFACE.md` §9 머리말 참조.

> **관련 변경(소스 아님).** 같은 날 mesh 를 bbox 중심으로 재정렬하고 `.meta` 를 재생성했다(§9.1).
> 이건 `Assets/StreamingAssets/srt3d/` 의 **데이터** 변경이라 이 디렉터리에는 반영되지 않는다.
> `maximum_body_diameter` 가 0.4287 → 0.2157 로 정상화됐고, 그 결과 `pose16` 의 translation
> 기준이 **물체 bbox 중심**으로 바뀌었다. §1.2 의 diameter 계산식과 직결된다.

---

## 참고

- 인터페이스 전체 명세(C ABI 6함수, 인자 타입, 좌표 규약, 온디바이스 제약):
  `docs/SRT3D_INTERFACE.md`
- 빌드 절차: `native/srt3d_uwp/README.md`
