# hololens2_wiseui — On-device 3D Object Tracking (HoloLens 2)

HoloLens 2에서 **RGB 기반 3D 객체 추적(SRT3D)** 을 온디바이스로 수행하는 Unity(UWP/IL2CPP) 앱.
초기 pose는 PC의 **FoundationPose**(depth+mask)로 잡고, 그 이후 프레임은 디바이스에서 **SRT3D**
(region-based edge tracking)로 RGB만으로 추적한다. 등록 대상 영역은 헤드셋에서 직접 지정한다.

> 상태: 연구/프로토타입. 추적·등록은 동작하며, latency(=PhotoCapture 캡처 경로)와 텍스처 렌더는 진행 중.

---

## 아키텍처

```
┌───────────────────────── HoloLens 2 (이 저장소) ─────────────────────────┐
│  Srt3dTracker.cs                                                          │
│   1) hl2ss(PV+depth) 스트림 ON  ──(GET /init 또는 POST /init_box)────────┐ │
│   2) 등록: 화면에서 대상 영역(box) 지정 → 서버가 초기 pose 회신          │ │
│   3) hl2ss OFF → PhotoCapture(PV, RGB) ON                                │ │
│   4) 매 프레임 srt3d_track_rgb() → mesh 오버레이 렌더                     │ │
│  Srt3dNative.cs → Assets/Plugins/WSA/ARM64/srt3d_uwp.dll (SRT3D 코어)     │ │
└──────────────────────────────────────────────────────────────────────────┘
                                     │ (같은 PV 카메라 기준 pose — 좌표 변환 없음)
┌──────────────────────────────── PC ────────────────────────────────────┐ │
│  init_server.py (8002)  ◄──────────────────────────────────────────────┘ │
│    /init      : SAM3(text='book') → mask → FP /register                   │
│    /init_box  : device box → SAM3(box 프롬프트) → mask → FP /register_with_box │
│  sam3_server (ZMQ 5556)         : 텍스트/박스 프롬프트 세그멘테이션        │
│  fp_server_gxr.py (8000)        : FoundationPose 초기 pose 추정           │
│  hl2_capture.py                 : hl2ss 에서 PV(640x360)+depth+K grab      │
└───────────────────────────────────────────────────────────────────────────┘
```

- **초기 pose**: FoundationPose가 depth+mask로 `ob_in_cam`(OpenCV) 6DoF pose 산출.
- **추적**: SRT3D는 RGB-only. 매 프레임 mesh를 내부 K로 투영해 객체 윤곽과 맞춰 pose 갱신.
- **좌표**: FP와 SRT3D가 같은 PV 카메라 기준이라 재투영/변환 없음. Unity 렌더는
  `world = cam2world · S · M` (S = 180° about X, OpenCV↔Unity 카메라 규약 차이).

---

## 저장소 구조 (핵심만)

| 경로 | 설명 |
|---|---|
| `Assets/Scripts/Srt3dTracker.cs` | 메인 — 등록 UI + 추적 + 렌더 + 진단 HUD |
| `Assets/Scripts/Srt3dNative.cs` | `srt3d_uwp.dll` P/Invoke 바인딩 |
| `Assets/Plugins/WSA/ARM64/srt3d_uwp.dll` | SRT3D 네이티브 코어 (UWP ARM64) |
| `Assets/Editor/BuildScript.cs` | UWP export 자동화 (`Build > Export UWP (ARM64)`) |
| `Assets/StreamingAssets/srt3d/` | `model.obj`(렌더/추적 메시), `model.obj.meta`(SRT3D 뷰포인트 모델), `model_wire.obj` |
| `Assets/Scripts/hl2ss/` | hl2ss 스트리밍 부트스트랩 |

PC 서버(`init_server.py`, `fp_server_gxr.py`, `hl2_capture.py`, SAM3)는 **별도 폴더**
(`../hl2_pipeline`, `../FoundationPose`)에 있으며 이 저장소에 포함되지 않는다. README의 서버 절 참고.

---

## 요구 사항

- **Unity 2022.3.62f3** (UWP/IL2CPP, ARM64)
- **Visual Studio 2022** (+ UWP 워크로드, ARM64 툴셋)
- HoloLens 2 (Device Portal 활성 / 개발자 모드)
- PC: FoundationPose + SAM3 실행 환경 (conda `my_base`, Docker 등 — 서버 절 참고)

---

## 빌드 & 배포

Unity가 UWP VS 솔루션을 **export** 하고, MSBuild가 **Release/ARM64 msix**를 만든다.

### 자동 (batchmode) — 에디터를 닫은 상태에서
```bash
# 1) Unity export (스크립트 변경 시 도메인 리로드로 1회차가 비면 2회 실행 필요)
"D:/Unity/Editors/2022.3.62f3/Editor/Unity.exe" -batchmode -quit \
  -projectPath . -executeMethod BuildScript.BuildUWP -logFile Build/export.log

# 2) MSBuild Release ARM64 → appx/msix
MSBuild.exe Build/hololens2_wiseui.sln -restore \
  /p:Configuration=Release /p:Platform=ARM64 \
  /p:UapAppxPackageBuildMode=SideloadOnly /p:AppxBundle=Never /p:AppxPackageSigningEnabled=true
```
산출물: `Build/AppPackages/hololens2_wiseui/hololens2_wiseui_1.0.0.0_ARM64_Test/*.msix`

> batchmode 주의: **스크립트를 방금 고쳤다면** 첫 배치 실행은 컴파일+도메인 리로드로
> `-executeMethod`가 버려져 export가 비어 나올 수 있다. Unity 완전 종료 후 **한 번 더** 실행하면 된다.

### 에디터에서
`Build > Export UWP (ARM64)` 메뉴 → 생성된 `Build/hololens2_wiseui.sln`을 VS에서 Release/ARM64로 배포.

### 배포
- **Device Portal**: `http://<HL2-IP>` → Views ▸ Apps → 기존 앱 Uninstall → Deploy apps ▸ Local Storage
  → `.msix` + `Dependencies/ARM64/Microsoft.VCLibs.ARM64.14.00.appx` → Install
- **VS**: 솔루션 열고 Release/ARM64/Remote Machine → F5

---

## 등록 방식 (`Srt3dTracker._initMode`)

| 모드 | 동작 | 서버 |
|---|---|---|
| `TextCenter` | 객체를 중앙에 두고 SAM `text='book'` 자동검출 | `GET /init` |
| `CenterBox` *(기본)* | 화면 중앙 **정사각형 박스**에 객체 맞추고 **핀치 1회** | `POST /init_box` |
| `CropPinch` | 두 모서리 air-tap 으로 박스 지정 | `POST /init_box` |
| `DragDraw` | 두 번 핀치(탭)로 두 모서리 지정 | `POST /init_box` |

핀치는 MRTK 손관절(엄지끝↔검지끝 거리, 히스테리시스)로 직접 감지 — 전역 포인터 클릭에 의존하지 않음.

---

## HUD 진단 값

- `fps` — 추적 프레임 처리율 (latency/버퍼링 정량화)
- `PV cap {W}x{H}` / `srt {W}x{H}` — 실제 캡처 해상도 / SRT3D 입력 해상도
- `ΔT / ΔR` — 프레임 간 pose 변화량 (책 움직일 때 뛰어야 정상; ≈0이면 추적 미갱신)
- `conf` — SRT3D 신뢰도
- `obj t=(x,y,z)m` — 초기 FP pose의 객체 위치 (z가 양수·0.2~1.5m면 정상)
- `[Srt3dTracker][DIAG]` 로그 — 지원 해상도 목록, 전체 K(fx,fy,cx,cy), kScale 등

---

## 주요 튜닝 (`Srt3dTracker.cs` 상단 `[TUNE]`)

- `_procMaxW` (기본 760) — 캡처 선택 목표 + SRT3D 입력 폭 상한. 초과 시 CPU downscale + K 동일비율 스케일.
- `_initMode`, `_centerBoxW` — 등록 방식/박스 크기
- `_initUrl` / `_initBoxUrl` — PC IP (기본 `192.168.0.7:8002`)
- `_axisFlip` — OpenCV↔Unity 렌더 좌표

해상도 처리: PhotoCapture가 4K를 고르면 SRT3D region matching이 과부하되어 추적이 멈추고 fps가
바닥난다. 그래서 지원 목록 중 `_procMaxW`에 **가장 가까운** 해상도를 선택하고, BGRA→RGB 변환과
축소를 **한 번의 strided 샘플로 융합**해 프레임당 비용을 SRT3D 해상도에만 비례하게 유지한다.

---

## PC 서버 실행 (별도 트리)

```bash
# SAM3 (WSL/GPU)     : ZMQ tcp://*:5556
# FoundationPose      : http://localhost:8000   (fp_server_gxr.py, joke_book 모델)
# init_server         : conda activate my_base; cd ../hl2_pipeline; python init_server.py  (8002)
```
환경변수: `HL2_HOST`(hl2ss 기기 IP), `FP_URL`, `SAM3_ADDR`, `OBJ_TEXT`, `INIT_PORT`.
디바이스 앱 켜기 전 **standalone hl2ss 앱은 종료**(카메라 충돌 방지), 서버 3개 기동 확인.

---

## 알려진 이슈 / TODO

- **Latency** — PhotoCapture(정지사진 API)를 루프로 도는 구조라 프레임당 지연이 남을 수 있음.
  해상도로 안 풀리면 **MediaFrameReader(비디오 프레임 소스)** 로 전환 필요(설계 변경).
- **Depth 없음** — SRT3D가 RGB-only라 원근/스케일을 실루엣으로만 추정. 해상도가 낮으면 취약.
- **텍스처 렌더** — 현재 `model.obj`를 solid opaque로 렌더. 실제 표지 텍스처는 OBJ+MTL+UV 로딩 추가 필요.
- 서버 IP/모델이 하드코딩(`192.168.0.x`, `book`/`joke_book`) — 설정화 필요.

---

## 크레딧

- **SRT3D** — Stoiber et al., Sparse Region-based 3D Object Tracking
- **FoundationPose** — NVIDIA
- **hl2ss** — HoloLens 2 sensor streaming
- **MRTK** — Mixed Reality Toolkit (입력/핸드 트래킹)
