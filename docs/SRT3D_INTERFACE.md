# SRT3D 온디바이스 트래커 — 인터페이스 명세

HoloLens 2(UWP/ARM64, IL2CPP)에서 Unity C#이 네이티브 SRT3D 트래커를 호출하는 규약.
같은 구조를 재현하거나 SRT3D를 다른 트래커로 교체할 때 필요한 것 전부.

문서의 모든 내용은 아래 실제 파일에서 뽑은 것이다. 추측한 부분은 명시했다.

| 역할 | 파일 |
|---|---|
| C# 바인딩 | `Assets/Scripts/Srt3dNative.cs` (Unity 프로젝트) |
| 호출부 | `Assets/Scripts/Srt3dTracker.cs` (Unity 프로젝트) |
| C 브리지 | `D:\ProjectsTracking\srt3d_uwp\srt3d_uwp.cpp` |
| 빌드 | `D:\ProjectsTracking\srt3d_uwp\CMakeLists.txt`, `build_uwp_arm64.ps1` |
| 코어 | `D:\ProjectsTracking\srt3d_uwp\srt3d\` (헤더 12 + .cpp 6) |
| OpenCV | `D:\ProjectsTracking\opencv_uwp\build_uwp_arm64.ps1` |
| 산출물 | `Assets/Plugins/WSA/ARM64/srt3d_uwp.dll` (854,016 B, 2026-06-09) |

---

## 1. C-익스포트 시그니처 전체

`srt3d_uwp.dll`이 노출하는 함수는 **6개**다. 전부 아래에 있다.

`SRT3D_API`는 `extern "C" __declspec(dllexport)` (`srt3d_uwp.cpp:28-32`).
호출 규약은 x64/ARM64에서 `__cdecl`이 기본이고, C# 쪽에서 `CallingConvention.Cdecl`로 명시했다.

### 1.1 `srt3d_init`

```cpp
// srt3d_uwp.cpp:63
SRT3D_API int srt3d_init(const char* meshPath, const float* K9, int width, int height);
```
```csharp
// Srt3dNative.cs:18-19
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
public static extern int srt3d_init(string meshPath, float[] K9, int width, int height);
```

| 인자 | 타입 | 크기 | 단위/레이아웃 | 비고 |
|---|---|---|---|---|
| `meshPath` | `const char*` | NUL 종료 | **ANSI**(`CharSet.Ansi`) | `.obj` 절대경로. 같은 위치의 `<meshPath>.meta`도 함께 읽는다 (`srt3d_uwp.cpp:71`) |
| `K9` | `const float*` | **9개** | row-major `[fx,0,cx, 0,fy,cy, 0,0,1]` | 픽셀 단위 |
| `width`/`height` | `int` | — | 픽셀 | 이후 `srt3d_track_rgb`에 넘길 이미지 크기와 **반드시 일치** |

**반환**: `1`=성공, `0`=실패. 실패 시 `srt3d_last_error()`에 원인이 담긴다.

> ⚠️ **K9의 9개 중 4개만 읽는다.** `srt3d_uwp.cpp:66`:
> ```cpp
> const float fx = K9[0], cx = K9[2], fy = K9[4], cy = K9[5];
> ```
> skew(`K9[1]`)와 마지막 행은 무시된다. 그리고 **왜곡 계수를 받는 인자가 아예 없다** —
> 즉 API가 undistort된 이미지를 전제한다. 함의는 §2.4에 따로 정리했다.

### 1.2 `srt3d_reset_pose`

```cpp
// srt3d_uwp.cpp:98
SRT3D_API int srt3d_reset_pose(const float* pose16);
```
```csharp
// Srt3dNative.cs:22-23
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
public static extern int srt3d_reset_pose(float[] pose16);
```

| 인자 | 타입 | 크기 | 레이아웃 |
|---|---|---|---|
| `pose16` | `const float*` | **16개** | 4x4 **row-major**, ob_in_cam, translation은 **미터** |

**반환**: `1`=성공, `0`=미초기화/널.

### 1.3 `srt3d_track_rgb`

```cpp
// srt3d_uwp.cpp:106
SRT3D_API int srt3d_track_rgb(const unsigned char* rgb, int width, int height, float* out17);
```
```csharp
// Srt3dNative.cs:27-28
[DllImport(DLL, CallingConvention = CallingConvention.Cdecl)]
public static extern int srt3d_track_rgb(byte[] rgb, int width, int height, [Out] float[] out17);
```

| 인자 | 타입 | 크기 | 레이아웃 |
|---|---|---|---|
| `rgb` | `const unsigned char*` | `width*height*3` | **RGB24**, 채널 순서 R,G,B, **stride = width\*3 (패딩 없음)**, 원점 **top-left** |
| `out17` | `float*` (`[Out]`) | **≥17** | `[0..15]` = pose 4x4 row-major, `[16]` = confidence |

**반환**: `1`=성공, `0`=실패.

### 1.4 `srt3d_release`

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

### 1.5 `srt3d_last_error`

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
DLL 소유의 `std::string` 내부 포인터. **호출자가 해제하지 말 것.** 다음 에러 발생 시 무효화된다.

### 1.6 `srt3d_set_log_callback`

```cpp
// srt3d_uwp.cpp:56
SRT3D_API void srt3d_set_log_callback(void (*cb)(const char*));
```
**C# 바인딩이 없다.** 현재 Unity에서 안 쓴다. 쓰려면 `[UnmanagedFunctionPointer(CallingConvention.Cdecl)]`
델리게이트를 만들고 **GC에 수거되지 않게 필드로 붙들어야** 한다.

---

## 2. 데이터 마샬링

### 2.1 RGB 이미지

- **타입**: `byte[]` (`IntPtr` 아님). P/Invoke가 호출 동안 배열을 pin하고 첫 원소 포인터를 넘긴다.
- **포맷**: RGB24, 채널 순서 **R,G,B**. (JNI 원본은 JPEG를 받아 `cv::imdecode` 했으나 UWP 포팅에서
  raw RGB로 바꿨다 — `imgcodecs` 의존성 제거가 목적. `srt3d_uwp.cpp:4-5`)
- **stride**: `width*3` 고정. 네이티브가 stride 인자 없이 Mat을 만들기 때문이다 (`srt3d_uwp.cpp:111`):
  ```cpp
  cv::Mat view(height, width, CV_8UC3, const_cast<unsigned char*>(rgb));
  ```
  **행 패딩이 있으면 이미지가 사선으로 밀린다.** 캡처 측에서 패딩을 제거해 넘겨야 한다.
- **원점**: top-left. 생산 측(`Srt3dPvCapture.cs`)이 `d = (dy*w + dx)*3`으로 위→아래 순서로 채운다.
- **버퍼 수명**: **네이티브가 복사본을 만든다** → 반환 즉시 관리 버퍼 재사용 가능.
  다만 복사가 **두 번** 일어난다:
  1. `srt3d_uwp.cpp:112` — `g_camera->PushImage(view.clone())`
  2. `virtual_camera.hpp` `PushImage` 내부 — `image_list_.push(image.clone())`

  프레임당 `w*h*3` 복사 2회. 896×504면 ≈1.35MB × 2 = 2.7MB/frame @30fps ≈ 81MB/s.
  성능이 문제되면 여기가 첫 번째 손볼 곳이다.

### 2.2 K (intrinsics)

- **`float[9]`**, row-major `[fx,0,cx, 0,fy,cy, 0,0,1]`.
- 실제로는 인덱스 **0, 2, 4, 5** 네 개만 읽는다 (§1.1 참조).
- 단위는 픽셀. `width`/`height`와 같은 이미지 격자 기준.
- 호출부 (`Srt3dTracker.cs:231`):
  ```csharp
  float[] K = { fx, 0, cx, 0, fy, cy, 0, 0, 1 };
  ```

### 2.3 Pose — **코드로 검증한 결과**

요청대로 "row-major / 미터 / OpenCV ob_in_cam"이 실제로 맞는지 소스에서 확인했다.

**(a) row-major — 확인됨.** 입력 (`srt3d_uwp.cpp:47-52`):
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
Eigen이 기본 column-major 저장이지만 `m(r,c)` 접근자를 쓰므로 저장 순서와 무관하게 올바르다.

**(b) 미터 — 확인됨.** `srt3d_uwp.cpp:69-70`:
```cpp
g_body = std::make_shared<srt3d::Body>(
    name, mesh, 1.0f, true, true, srt3d::Transform3fA::Identity());
//               ^^^^ geometry_unit_in_meter = 1.0
```
`Body` 생성자 3번째 인자가 `geometry_unit_in_meter` (`body.h:23-25`). `1.0`이므로
**`.obj`의 좌표값이 곧 미터**다. mesh를 다른 단위로 만들면 여기를 바꿔야 한다.

**(c) ob_in_cam — 확인됨(간접).** srt3d는 이 pose를 `body2world_pose`라 부른다. 이름만 보면
world 기준 같지만, 실제 투영은 (`region_modality.cpp:553-554`):
```cpp
body2camera_pose_ =
    camera_ptr_->world2camera_pose() * body_ptr_->body2world_pose();
```
그리고 `VirtualCamera`는 `set_camera2world_pose()`를 **한 번도 호출하지 않는다**
(`virtual_camera.hpp` 전체 확인). 기본값은 Identity (`camera.h:54-55`):
```cpp
Transform3fA camera2world_pose_{Transform3fA::Identity()};
Transform3fA world2camera_pose_{Transform3fA::Identity()};
```
따라서 `world2camera = I` → **`body2camera == body2world`**. 즉 반환 pose는 카메라 기준
object pose = **ob_in_cam**이 맞다.

**(d) OpenCV 규약** — 위 검증은 "world == camera"까지만 보장한다. `+Z 전방 / +Y 아래`인지는
`region_modality.cpp`의 투영식이 `fu*X/Z + ppu` 형태(부호 반전 없음)인 점과, FoundationPose가
내놓은 OpenCV pose를 `srt3d_reset_pose`에 **그대로** 넣어 추적이 성립한다는 실측으로 뒷받침된다.
소스 한 줄로 못 박은 것은 아니므로 이 항목만 "실측 기반"으로 이해할 것.

**출력 수령 방식**: `[Out] float[] out17` — 호출자가 미리 17개를 할당하고 넘긴다.
리턴 버퍼도 out 파라미터 구조체도 아니다.
```csharp
float[] outv = new float[17];   // Srt3dTracker.cs:244
```
`out17[16]`은 confidence. `Tracker::EvaluateDistribution()`의 첫 원소이고,
비어 있으면 `0.0f`이 들어간다 (`srt3d_uwp.cpp:121`).

### 2.4 렌즈 왜곡 — API는 undistort를 전제하고, HL2 PV는 실제로 그 조건을 만족한다

`srt3d_init`은 `[fx, cx, fy, cy]` **핀홀 4개만** 받는다. 왜곡 계수 인자가 없다는 것은
**입력 이미지가 이미 undistort 되어 있다고 가정**한다는 뜻이다.

**skew 무시는 무해하다.** 현대 디지털 센서에서 skew는 사실상 0이다.

**왜곡도 — HoloLens 2 PV에 한해서는 — 무해하다. 실측했다.**

#### (0) 결론 먼저: PV 왜곡 계수는 전부 0 (실측, 2026-07-22)

기기(192.168.0.16)에서 hl2ss로 PV 캘리브레이션을 받아 확인했다.
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
`cx/W = 0.4936` (640x360) vs `442/896 = 0.4933` (896x504).

→ **HoloLens 2 PV 파이프라인은 이미 rectify 된 프레임을 준다.**
   srt3d가 요구하는 "undistort된 입력" 조건이 우연히 이미 충족되어 있다. 할 일이 없다.

> 다른 카메라로 옮긴다면 이 값을 **반드시 다시 재야 한다.** 아래 (a)~(c)는
> API 규약상 왜곡이 있을 수 있다는 것과, 있을 경우 무엇을 조심해야 하는지에 대한 설명이다.

#### (a) API 규약상으로는 왜곡이 있을 수 있다

`Windows.Media.Devices.Core.CameraIntrinsics`의 MS 공식 문서:

> 클래스 설명: *"Represents the intrinsics that describe the camera **distortion model**."*
>
> `UndistortedProjectionTransform`: *"...transforms a 2D coordinate in meters on the image plane
> to video frame pixel coordinates **without compensating for the distortion model of the camera**.
> The 2D point resulting from this transformation **will not accurately map to the pixel coordinate
> in a video frame unless the app applies its own distortion compensation**."*
>
> `UndistortPoint`: *"Transforms a point to compensate for the distortion model of the camera,
> resulting in an undistorted point."*

즉 `FocalLength`/`PrincipalPoint`는 핀홀 부분일 뿐이고, 계수가 0이 아니라면 실제 프레임
픽셀에 맞추기 위해 `RadialDistortion`(k1,k2,k3) / `TangentialDistortion`(p1,p2)를
**추가로 적용**해야 한다. OpenCV의 `cameraMatrix + distCoeffs`와 같은 구조다.

**API는 왜곡이 0이라고 보장하지 않는다** — 그래서 (0)의 실측이 필요했다.
HL2 PV는 0이었지만, 이건 이 카메라의 성질이지 API의 보장이 아니다.

#### (b) 우리는 undistort를 하지 않는다 (그리고 할 필요가 없다)

`Srt3dPvCapture.cs:187-193`이 읽는 것은 `FocalLength`와 `PrincipalPoint`뿐이다:
```csharp
var intr = vmf.CameraIntrinsics;
fx = intr.FocalLength.X;    fy = intr.FocalLength.Y;
cx = intr.PrincipalPoint.X; cy = intr.PrincipalPoint.Y;
```
`RadialDistortion` / `TangentialDistortion` / `UndistortPoint`는 **한 번도 참조하지 않는다.**
RGB는 `SoftwareBitmap` BGRA에서 채널만 바꿔 그대로 넘긴다 — remap 단계가 없다.

(0)의 실측 때문에 이건 **문제가 아니다.** 계수가 0이므로 참조해봐야 항등 변환이다.

#### (c) PC 쪽(FoundationPose)도 똑같이 무시한다 — 확인됨

hl2ss도 PV에 대해서는 왜곡을 적용하지 않는다:
- `hl2ss_3dcv.py:207` `pv_create_intrinsics(focal_length, principal_point)` — 핀홀만 조립
- RM 센서에는 `rm_vlc_undistort` / `rm_depth_undistort`와 `undistort_map`이 있는데
  (`hl2ss_3dcv.py:157,173`) **PV에는 대응하는 undistort 헬퍼도 map도 없다**
- `hl2_capture.py:42-50` `intrinsics_to_K()`도 fx/fy/cx/cy만 뽑는다

hl2ss의 PV 캘리브레이션 구조체에는 `radial_distortion` / `tangential_distortion`이
**존재하지만**(`hl2ss.py:2011-2015`) 파이프라인에서 쓰이지 않는다.

#### (d) 함의

| 항목 | 결론 |
|---|---|
| FP 초기 pose ↔ srt3d 추적의 **상호 불일치** | **아니다.** 양쪽 다 핀홀만 쓰고 실제 왜곡도 0 |
| 실제 기하 대비 **계통 오차** | **없다.** 실측 |Δ| = 0.00 px (코너 포함) |
| conf 저하의 원인 후보 | **배제됨.** 아래 참조 |

이 절은 원래 "왜곡이 conf 저하의 원인일 수 있다"는 가설을 검증하려고 쓰였다.
API 규약만 보면 근거가 있어 보였지만 **실측으로 완전히 배제됐다.**

> ⚠️ **왜곡 보정을 추가하려는 충동을 조심할 것.** 계수가 0이므로 얻을 게 없고,
> 만약 기기 쪽(srt3d)과 PC 쪽(FoundationPose) 중 **한쪽에만** remap을 넣으면
> 지금 성립하는 두 파이프라인의 일관성이 깨져서 오히려 나빠진다.
> 바꿀 거면 양쪽을 같이 바꿔야 한다.

#### (e) 재측정 방법 (다른 카메라/해상도로 옮길 때)
PV 캘리브레이션은 hl2ss로 받는다. **wiseui 앱이 기기에서 실행 중이어야 한다.**

```python
import sys; sys.path.insert(0, r"D:\ProjectsTracking\hl2ss\viewer")
import hl2ss, hl2ss_lnm, hl2ss_3dcv
hl2ss_lnm.start_subsystem_pv(HOST, hl2ss.StreamPort.PERSONAL_VIDEO)
c = hl2ss_3dcv.get_calibration_pv(OUT, HOST, hl2ss.StreamPort.PERSONAL_VIDEO,
                                  width=640, height=360, framerate=30)
print(c.radial_distortion, c.tangential_distortion)   # k1,k2,k3 / p1,p2
hl2ss_lnm.stop_subsystem_pv(HOST, hl2ss.StreamPort.PERSONAL_VIDEO)
```

주의:
- **hl2ss PV mode2는 표준 해상도만 받는다.** 기기가 MFR로 쓰는 896x504로 요청하면
  `Exception: connection closed`가 난다. 640x360으로 받을 것 —
  **왜곡 계수는 정규화 좌표계라 해상도와 무관**하므로 값은 그대로 쓸 수 있다.
- 결과는 `<OUT>/personal_video/<focus>_<W>_<H>/`에 캐시된다. 다시 재려면 폴더를 지울 것.
- 환경은 `conda my_base` (base 에는 `av` 모듈이 없어 hl2ss import 가 실패한다).

계수가 0이 아니라면 선택지는 둘이다:
1. **트래커에 넣기 전 remap** — `cv::initUndistortRectifyMap` + `cv::remap`.
   프레임당 비용이 늘고, K도 undistort 후 값으로 바꿔야 한다.
2. **API 확장** — `srt3d_init`에 distCoeffs를 추가하고 코어의 투영식에 반영. 범위가 훨씬 크다.

어느 쪽이든 **PC 쪽(FoundationPose)도 같이** 바꿔야 한다((d)의 경고 참조).

---

## 3. 좌표 규약 — 가장 많이 막히는 지점

> 이 절은 실제로 하루를 태운 곳이다. 증상이 "그럴듯하게 틀림"이라 잔차나 conf로는 안 잡힌다.

### 3.1 전체 체인

srt3d 출력은 **ob_in_cam**이다: OpenCV 규약 (**+X right, +Y down, +Z forward**), translation은 **미터**.
Unity world까지 가는 변환은:

```
world = cam2world · C · M · C          (C = diag(1, -1, 1),  C⁻¹ = C)
```

| 기호 | 의미 |
|---|---|
| `M` | srt3d가 준 ob_in_cam (row-major 4x4 → `Matrix4x4`) |
| `C` | OpenCV cam ↔ Unity cam. **Y만 뒤집는다** — 양쪽 다 +Z가 전방이므로 Z는 그대로 |
| `cam2world` | PV 카메라 → Unity world |

**양쪽에 C가 붙는 conjugation**이라는 게 중요하다. 앞쪽 C는 카메라 좌표를, 뒤쪽 C는 object 좌표를
변환한다 (mesh 정점이 `.obj` = srt3d object 프레임에 있으므로).

한쪽만 붙이면 `det = -1`인 **반사 행렬**이 되어 mesh가 거울상으로 그려진다.
그걸 없애려고 `C`를 `diag(1,-1,-1)`(det +1)로 바꾸면 반사는 사라지지만 **Z가 뒤집혀
홀로그램이 카메라 뒤에 그려진다.** 증상을 가린 것이지 고친 게 아니다.

### 3.2 ⚠️ 카메라 소스에 따라 C가 달라진다

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
> 갈아타면(4K만 나와서 fps가 안 나오는 등의 이유로) 이전에 맞던 `diag(1,-1,-1)`이
> 조용히 틀리기 시작한다. 코드는 그대로인데 결과만 틀린다.

수식으로 확인하면 차이는 Z 하나뿐이다:
```
diag(1,-1,-1) · M  의 translation = (tx, −ty, −tz)
C · M · C          의 translation = (tx, −ty, +tz)
```

### 3.3 ToUnity() 헬퍼는 직접 짜지 말 것

`System.Numerics.Matrix4x4`(WinRT, row-vector, RH) → `UnityEngine.Matrix4x4`(column-vector, LH)
변환은 **transpose + F·A·F conjugation**을 동시에 해야 한다. MS 공식 헬퍼를 그대로 쓸 것:

```csharp
// Srt3dPvCapture.cs:226-231 — MS "Converting between coordinate systems" 문서의 헬퍼
// Unity Matrix4x4 생성자는 열(column)을 받는다 → System.Numerics 행을 열로 = transpose.
// 부호 패턴 = handedness conjugation F·A·F (F = diag(1,1,-1)). 한쪽 flip 이 아니라 양쪽.
static Matrix4x4 NumericsToUnity(System.Numerics.Matrix4x4 m)
    => new Matrix4x4(
        new Vector4( m.M11,  m.M12, -m.M13,  m.M14),
        new Vector4( m.M21,  m.M22, -m.M23,  m.M24),
        new Vector4(-m.M31, -m.M32,  m.M33, -m.M34),
        new Vector4( m.M41,  m.M42, -m.M43,  m.M44));
```

직접 짜면 대개 **한쪽만 flip**하게 되고, 그러면 거울상 + 뒤쪽 렌더가 된다.

### 3.4 증상별 판별표

| 증상 | 원인 후보 | 확인 방법 |
|---|---|---|
| 회전은 맞는데 **위치만 평행이동** | principal point (cx/cy) 또는 이미지 flip과 K 불일치 | 이미지를 flip 했다면 `cx' = (w-1) - cx` 를 같이 했는지 |
| 물체가 **카메라 뒤로 감** / 시야에서 사라짐 | **Z 부호** — C가 `diag(1,-1,-1)`인지 `diag(1,-1,1)`인지 | `ob_in_cam`의 tz는 양수인데 head-local Z가 음수면 확정 |
| **거울상**으로 그려짐 | conjugation 한쪽만 적용 (det = −1) | `T.determinant` 를 찍어본다. +1이어야 정상 |
| 원점(0,0,0)에 붙어 있음 | `cam2world`를 못 받아 identity로 그림 | 비 locatable 프레임이면 렌더를 **스킵**해야 한다 |
| 상하만 뒤집힘 | 이미지 원점 (top-left vs bottom-left) | MFR = top-left, HoloLensCameraStream raw = bottom-up |

**진단 팁**: 후보 행렬을 여러 개 만들어 각각의 object 원점을 head-local로 환산해
HUD에 병기하면 한 번의 빌드로 판별된다. 단, **렌더는 하나로 고정**할 것 —
자동 선택(auto-pick)은 어느 게 맞았는지 알 수 없게 만든다.

```csharp
Vector3 p = cam.transform.InverseTransformPoint(T.MultiplyPoint3x4(Vector3.zero));
// p.z 가 ob_in_cam 의 tz 와 부호·크기가 맞는 후보가 정답
```

### 3.5 mesh 정점 변환은 full 4x4로

`Matrix4x4.rotation` / `Transform` 분해는 반사 행렬(det −1)에서 깨진다. 후보 판별 중에는
반사가 섞일 수 있으므로 정점을 직접 변환할 것 (`Srt3dTracker.cs`의 `ApplyPose`):

```csharp
for (int i = 0; i < _objVerts.Length; i++)
    _worldVerts[i] = T.MultiplyPoint3x4(_objVerts[i]);
```
GameObject transform은 identity로 두고 mesh 정점 자체를 world로 만든다.

---

## 4. 호출 시퀀스

```
[1회] 파일 준비        StreamingAssets → persistentDataPath 복사
[1회] srt3d_init(meshPath, K9, w, h)
[1회] srt3d_reset_pose(pose16)          ← FoundationPose 초기 pose
[매프레임] srt3d_track_rgb(rgb, w, h, out17)
[종료] srt3d_release()
```

**파일 준비** (`Srt3dTracker.cs:139-143`):
```csharp
_meshPath = Path.Combine(d, "model.obj");
string meta = Path.Combine(d, "model.obj.meta");
yield return Copy("srt3d/model.obj", _meshPath);
yield return Copy("srt3d/model.obj.meta.bytes", meta);
if (!File.Exists(_meshPath) || !File.Exists(meta)) { Hud("FAIL: model copy"); yield break; }
```

> ⚠️ **Unity `.meta` 확장자 충돌.** srt3d의 모델 파일이 하필 `.meta`인데 Unity는 그걸 자기
> 에셋 메타데이터로 가로챈다. 그래서 **`model.obj.meta.bytes`로 이름을 바꿔 배포**하고
> 런타임에 `model.obj.meta`로 복사한다. StreamingAssets는 UWP에서 패키지 안이라
> `File.IO`가 안 되므로 `UnityWebRequest`로 읽는다 (`Srt3dTracker.cs:895-906`).

**초기화 + 초기 pose** (`Srt3dTracker.cs:231-235`):
```csharp
float[] K = { fx, 0, cx, 0, fy, cy, 0, 0, 1 };
if (Srt3dNative.srt3d_init(_meshPath, K, w, h) == 0) {
    Hud("srt3d_init FAIL\n" + Srt3dNative.LastError()); return;
}
Srt3dNative.srt3d_reset_pose(_fpPose);
```

**매 프레임 추적** (`Srt3dTracker.cs:244-246`):
```csharp
float[] outv = new float[17];
if (Srt3dNative.srt3d_track_rgb(rgb, w, h, outv) == 0) {
    if (_frame % 30 == 0) Hud("track FAIL\n" + Srt3dNative.LastError()); return;
}
_frame++; _conf = outv[16]; _obZ = outv[11];
```
`outv[3], outv[7], outv[11]`이 translation (tx, ty, tz) — row-major 4번째 열.

**해제** (`Srt3dTracker.cs:916`): `if (_inited) Srt3dNative.srt3d_release();`

### 스레딩

`srt3d_track_rgb`는 **Unity 메인 스레드에서만** 호출한다. 네이티브는 전역 `shared_ptr` 기반
**단일 세션**이고 락이 없다 (`srt3d_uwp.cpp:34-42`). 동시 호출 시 정의되지 않은 동작.

현재 구조는 캡처(MediaFrameReader 콜백 = 백그라운드 스레드)가 최신 프레임만 락으로 보관하고,
`Update()`가 꺼내서 네이티브를 호출한다. 즉 **캡처만 백그라운드, 트래킹은 메인**이다.

---

## 5. 빌드 구성

### 5.1 출처 (⚠️ 리비전이 고정돼 있지 않음)

```
DLR-RM/3DObjectTracking · SRT3D          (원본 알고리즘)
  └→ pysrt3d (Jianxff, MIT, 2024)        (수정본 — 아래 참조)
       └→ XRHandEyeTracker source_nogl   (Galaxy XR 프로젝트, GL 제거)
            └→ srt3d_uwp                 (UWP/ARM64 포팅)
```

- 원본: <https://github.com/DLR-RM/3DObjectTracking/tree/master/SRT3D>
- `D:\ProjectsTracking\3DObjectTracking` 로컬 체크아웃은 `f021061` (2025-05-21)인데,
  **이 리비전엔 `M3T`만 있고 `SRT3D` 디렉터리가 없다.** 즉 vendored 코어의 출처가 아니다.
- `D:\ProjectsTracking\srt3d_uwp\srt3d\`에는 **VCS 메타데이터가 없다.** CMakeLists.txt:19의
  주석이 "XRHandEyeTracker에서 복사한 검증본"이라고만 밝힌다.
  → **정확한 upstream 커밋을 특정할 수 없다. 재현성 공백이므로 새로 시작한다면 서브모듈이나
  커밋 해시를 남길 것.**

**중요: 이건 stock SRT3D가 아니다.** pysrt3d의 수정이 들어가 있다:
1. body diameter 자동 계산 (원본은 수동 지정)
2. **KL divergence 기반 confidence** — `out17[16]`의 출처. `set_kl_threshold(1.0f)`
   (`srt3d_uwp.cpp:84`)와 `EvaluateDistribution()`이 여기서 온다. **원본 SRT3D엔 없다.**
3. 초기화/추적 임계값 분리
4. 독립적 다중 모델 추적

### 5.2 의존성

| 항목 | 버전 | 구성 |
|---|---|---|
| **OpenCV** | **4.11.0** | `core`, `imgproc` **2개만**. static, WindowsStore ARM64 |
| **Eigen** | **3.4.0** | vendored (`third_party/eigen3`) |
| **tiny_obj_loader** | vendored | `third_party/tiny_obj_loader/tiny_obj_loader.h` |
| C++ 표준 | C++17 | `std::filesystem` 사용 (`body.h`) |

OpenCV 빌드 (`opencv_uwp/build_uwp_arm64.ps1`) 요점:
```powershell
cmake -S $src -B $build -G "Visual Studio 17 2022" -A ARM64 `
  -DCMAKE_SYSTEM_NAME=WindowsStore -DCMAKE_SYSTEM_VERSION="10.0" `
  -DBUILD_LIST="core,imgproc" `
  -DBUILD_SHARED_LIBS=OFF -DBUILD_WITH_STATIC_CRT=OFF `
  -DWITH_JPEG=OFF -DWITH_PNG=OFF -DWITH_TIFF=OFF ... (전부 OFF)
```
- `CMAKE_SYSTEM_NAME=WindowsStore` **필수** — UWP 금지 API를 피한다.
- `BUILD_WITH_STATIC_CRT=OFF` — Unity/IL2CPP가 동적 CRT를 쓰므로 맞춰야 한다.
- imgcodecs 계열을 전부 끈 것은 raw RGB를 받기 때문. JPEG를 받으려면 이 전제가 깨진다.

### 5.3 srt3d_uwp CMake 요점

```cmake
find_package(OpenCV REQUIRED COMPONENTS core imgproc)   # CMakeLists.txt:17
file(GLOB SRT3D_CORE .../srt3d/src/*.cpp)               # 코어 6개
add_library(srt3d_uwp SHARED ${SRT3D_CORE} srt3d_uwp.cpp)
target_compile_definitions(srt3d_uwp PRIVATE SRT3D_NO_GL)
```

빌드 (`build_uwp_arm64.ps1:17-24`):
```powershell
cmake -S $root -B $build -G "Visual Studio 17 2022" -A ARM64 `
  -DCMAKE_SYSTEM_NAME=WindowsStore -DCMAKE_SYSTEM_VERSION="10.0" `
  -DCMAKE_SYSTEM_PROCESSOR=ARM64 -DCMAKE_POLICY_VERSION_MINIMUM="3.5" `
  -DOpenCV_STATIC=ON `
  -DOpenCV_DIR="D:\ProjectsTracking\opencv_uwp\install\ARM64\vc17\staticlib"
```

> ⚠️ `OpenCV_DIR`은 **`install/ARM64/vc17/staticlib`를 직접** 가리켜야 한다.
> install 루트의 dispatcher `OpenCVConfig.cmake`가 static 구성을 인식하지 못한다
> (`build_uwp_arm64.ps1:11-12`).

산출물을 `Assets/Plugins/WSA/ARM64/srt3d_uwp.dll`에 넣고, Unity Inspector에서
플랫폼 **WSAPlayer / ARM64**로 지정한다.

---

## 6. 온디바이스 제약 — 나중에 같은 걸 하는 사람이 알아야 할 것

### 6.1 GL 제거가 가장 큰 제약

UWP엔 데스크톱 OpenGL이 없어서 `SRT3D_NO_GL`로 렌더러 계열을 통째로 뺐다.
`srt3d/src/`에는 **6개 .cpp만** 있다: `body, camera, common, model, region_modality, tracker`.
`normal_renderer.h`, `normal_viewer.h`, `occlusion_renderer.h`, `renderer.h`,
`renderer_geometry.h`, `viewer.h`는 **헤더만 있고 구현이 없다.**

**결과: `.meta`(sparse viewpoint model)를 기기에서 생성할 수 없다.**
`.meta`는 여러 시점의 윤곽 템플릿 묶음이고, 원본은 없으면 `tracker.setup()`이 `NormalRenderer`(GL)로
자동 생성한다. GL-free 빌드는 **로드만** 한다.

→ `.meta`는 GL 있는 머신에서 미리 만들어 배포해야 한다.
   현재는 WSL conda 환경에서 `pysrt3d/gen_meta.py`로 생성한다:
```
conda activate srt3d
cd /mnt/d/ProjectsTracking/pysrt3d
python gen_meta.py joke_book
```
현재 배포본은 16MB다 (`Assets/StreamingAssets/srt3d/model.obj.meta.bytes`).
**객체를 바꾸면 mesh와 `.meta`를 같이 다시 만들어야 한다.**

### 6.2 UWP 링커: vccorlib/msvcrt 순서

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

### 6.3 비표준 C++ (MSVC 확장에 의존)

`virtual_camera.hpp`의 `PushImage`가 **non-const 참조**를 받는데:
```cpp
void PushImage(cv::Mat& image)                       // virtual_camera.hpp
g_camera->PushImage(view.clone());                   // srt3d_uwp.cpp:112 — rvalue 전달
```
`view.clone()`은 임시 객체(rvalue)라 표준 C++에선 `cv::Mat&`에 바인딩되지 않는다.
MSVC가 기본으로 허용하는 확장(경고 C4239)이다.
→ **`/permissive-`나 `/Zc:referenceBinding`을 켜면 컴파일이 깨진다.** 다른 컴파일러로
옮길 계획이면 `const&`나 값 전달로 고쳐야 한다.

### 6.4 스레딩

- 네이티브는 **단일 세션 전역 상태**, 락 없음 (`srt3d_uwp.cpp:34-42`). 동시 호출 금지.
- OpenCV를 `WITH_TBB=OFF`, `WITH_OPENMP=OFF`로 빌드했다 → 내부 병렬화 없음.
  HoloLens 2의 제한된 CPU에서 의도한 선택이지만, 트래킹이 **완전히 단일 스레드**라는 뜻이다.
- `VirtualCamera`는 `set_realtime(true)`로 큐에 쌓인 프레임을 버리고 최신 것만 쓴다
  (`srt3d_uwp.cpp:78`, `virtual_camera.hpp`의 `UpdateImage`). 처리가 캡처보다 느려도
  지연이 누적되지 않는다.

### 6.5 성능에 직접 걸리는 것

- **프레임당 이미지 복사 2회** (§2.1). 가장 명확한 최적화 지점.
- 트래커 반복 횟수 (`srt3d_uwp.cpp:81-82`):
  ```cpp
  g_tracker->set_n_corr_iterations(7);
  g_tracker->set_n_update_iterations(2);
  ```
  기기 성능에 맞춰 줄일 수 있는 노브다.
- 입력 해상도가 곧 비용이다. `srt3d_init`의 `w,h`와 `srt3d_track_rgb`의 `w,h`는 같아야 하므로,
  낮추려면 **K도 같은 비율로 스케일**해야 한다 (fx, fy, cx, cy 전부).

### 6.6 그 밖의 함정

- **`.meta` 확장자 충돌** (§3). Unity 프로젝트라면 반드시 만난다.
- **StreamingAssets는 UWP에서 `File.IO` 불가.** `UnityWebRequest`로 읽어
  `persistentDataPath`에 복사한 뒤 그 경로를 네이티브에 넘긴다.
- **DLL은 Editor에 없다.** `Srt3dNative`의 선언 자체는 무해(지연 로드)하지만
  호출부는 `#if !UNITY_EDITOR`로 감싸야 한다 (`Srt3dNative.cs:3-4`).
- **왜곡 보정 인자가 없다.** 렌즈 왜곡이 큰 카메라면 넘기기 전에 undistort 해야 한다.

---

## 7. 다른 트래커로 교체하려면

이 6개 함수만 같은 규약으로 구현하면 `Srt3dTracker.cs`는 거의 그대로 쓸 수 있다.
최소 계약은 실질적으로 **3개**다:

| 함수 | 계약 |
|---|---|
| `init(meshPath, K9, w, h) → int` | 모델 로드 + K 설정. 1=성공 |
| `reset_pose(pose16) → int` | ob_in_cam(row-major, 미터) 주입 |
| `track_rgb(rgb, w, h, out17) → int` | RGB24 top-left, stride=w*3 → pose16 + conf |

교체 시 확인할 것:
1. pose 규약 (row-major? ob_in_cam? 미터? +Z 전방?) — §2.3의 검증 절차를 그대로 밟을 것
2. 이미지 원점과 stride 가정
3. K에서 실제로 읽는 항목 (skew/왜곡 지원 여부)
4. 스레드 안전성 — 현재 호출부는 메인 스레드 단일 호출을 전제한다
5. confidence의 의미와 범위 — 현재는 pysrt3d의 KL divergence 기반이고 stock SRT3D와 다르다
