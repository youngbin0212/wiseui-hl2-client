# Third-Party Notices

이 저장소는 아래 서드파티 구성요소를 포함하거나, 빌드 산출물에 정적으로 링크한다.
각 항목의 라이선스와 의무를 명시한다.

> ⚠️ **이 저장소 전체는 MIT 가 아니다.**
> 본 프로젝트가 직접 작성한 코드는 MIT([`LICENSE`](LICENSE))지만,
> **hl2ss 가 Commons Clause 조건을 부과**하므로 배포물 전체를 상업적으로 **판매할 수 없다.**
> 상세는 아래 §2 참조.

최종 확인: 2026-08-03

---

## 목차

| # | 구성요소 | 라이선스 | 배포 형태 |
|---|---|---|---|
| 1 | DLR-RM SRT3D | MIT | 소스 (편입) |
| 2 | **hl2ss** | **BSD 3-Clause + Commons Clause** | 소스 + 바이너리 |
| 3 | pysrt3d | MIT | 소스 (편입, 파생) |
| 4 | tiny_obj_loader | MIT | 소스 (편입) |
| 5 | OpenCV | Apache 2.0 | **정적 링크** (바이너리) |
| 6 | zlib | zlib License | **정적 링크** (바이너리) |
| 7 | Eigen | MPL 2.0 | 헤더 (빌드 시 취득) |
| 8 | MRTK Foundation | MIT | 바이너리 (`.tgz`) |
| 9 | MRTK Standard Assets | MIT | 바이너리 (`.tgz`) |
| 10 | MR OpenXR Plugin | MIT | 바이너리 (`.tgz`) |
| 11 | MRTK Shaders | MIT | 소스 (`Assets/MRTK/`) |
| 12 | TextMesh Pro Essential Resources | **Unity Companion License** | 에셋 |
| 13 | Liberation Sans | SIL OFL 1.1 | 폰트 |
| 14 | ~~EmojiOne / JoyPixels~~ | — | **삭제됨** (§14) |
| — | Microsoft MR SDK DLL 2개 | 독점 EULA | **미포함** (§15) |

---

## 1. DLR-RM 3DObjectTracking (SRT3D)

- **URL:** https://github.com/DLR-RM/3DObjectTracking
- **Commit:** `11ae750`
- **License:** MIT
- **위치:** `native/srt3d_uwp/srt3d/` (수정본 — [`MODIFICATIONS.md`](native/srt3d_uwp/MODIFICATIONS.md) 참조)

```
Copyright (c) 2021 Manuel Stoiber, German Aerospace Center (DLR)
```

소스 19개 중 18개가 `SPDX-License-Identifier: MIT` 헤더를 보유한다
(예외 1개는 이 프로젝트가 추가한 `virtual_camera.hpp`).

**의무:** 저작권 고지 + MIT 전문 동봉 (§MIT 전문 참조).

---

## 2. hl2ss ⚠️ 상업적 판매 금지

- **URL:** https://github.com/jdibenes/hl2ss
- **Version:** **unspecified** (§16 참조)
- **License:** **BSD 3-Clause License WITH Commons Clause**
- **위치:** `Assets/Plugins/WSA/ARM64/hl2ss.dll`, `Assets/Scripts/hl2ss/hl2ss.cs`

### 저작권 고지

```
© 2022 by Stevens Institute of Technology
Contributors: Dr. Enrique Dunn
Affiliation: Stevens Institute of Technology
https://www.stevens.edu/directory/office-innovation-and-entrepreneurship
All rights reserved.
```

### 인용 요청

저자가 다음 논문 인용을 요청한다:

> Juan C. Dibene, and Enrique Dunn.
> "HoloLens 2 Sensor Streaming."
> arXiv preprint **arXiv:2211.02648** (2022).

### 라이선스 전문

```
Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.

2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

3. Neither the name of the copyright holder nor the names of its contributors
   may be used to endorse or promote products derived from this software
   without specific prior written permission.

4. (Commons Clause License Condition) Without limiting other conditions in the
   License, the grant of rights under the License will not include, and the
   License does not grant to you, the right to Sell the Software. For purposes
   of the foregoing, "Sell" means practicing any or all of the rights granted
   to you under the License to provide to third parties, for a fee or other
   consideration (including without limitation fees for hosting or
   consulting/support services related to the Software), a product or service
   whose value derives, entirely or substantially, from the functionality of
   the Software. Any license notice or attribution required by the License must
   also include this Commons Clause License Condition notice.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE
FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY,
OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
```

### 함의

- **Commons Clause 는 OSI 승인 오픈소스 조건이 아니다.** 소프트웨어의 판매,
  그리고 그 기능에 가치가 실질적으로 의존하는 제품·서비스의 유상 제공이 금지된다.
- 조항 4가 명시하듯 **이 Commons Clause 고지 자체를 귀속 표시에 포함해야 한다.**
  이 문서가 그 요건을 이행한다.
- 저장소가 `hl2ss.dll` 과 `hl2ss.cs` 를 재배포하므로 **배포물 전체가 이 제약을 승계한다.**

---

## 3. pysrt3d

- **URL:** https://github.com/Jianxff/pysrt3d
- **Version:** **unspecified — fork point not recorded** (§16 참조)
- **License:** MIT
- **위치:** `native/srt3d_uwp/srt3d/` (§1 의 수정 계층)

```
Copyright (c) 2024 Jianxff
```

**의무:** 저작권 고지 + MIT 전문 동봉.

---

## 4. tiny_obj_loader

- **URL:** https://github.com/tinyobjloader/tinyobjloader
- **Version:** 편입본 (헤더 내 버전 표기 참조)
- **License:** MIT
- **위치:** `native/srt3d_uwp/third_party/tiny_obj_loader/tiny_obj_loader.h`

```
Copyright (c) 2012-2018 Syoyo Fujita and many contributors.
```

**의무:** 저작권 고지 + MIT 전문 동봉. (헤더 파일 자체에 전문이 포함돼 있다.)

---

## 5. OpenCV ⚠️ 바이너리에 정적 링크됨

- **URL:** https://github.com/opencv/opencv
- **Version:** **4.11.0**
- **License:** **Apache License 2.0**
  (OpenCV 는 4.5.0 부터 3-clause BSD → Apache 2.0 으로 변경됐다)
- **전문:** https://www.apache.org/licenses/LICENSE-2.0
- **위치:** `Assets/Plugins/WSA/ARM64/srt3d_uwp.dll` 에 **정적 링크**
  (모듈 `core`, `imgproc` 만 / WindowsStore ARM64 static)

**의무 (Apache 2.0 §4):**

1. 라이선스 사본을 수령자에게 제공 — 위 URL 및 본 문서로 이행
2. 수정 파일에 변경 사실 명시 — **OpenCV 를 수정하지 않았다** (해당 없음)
3. 저작권·특허·상표·귀속 고지 유지 — 본 문서로 이행
4. `NOTICE` 파일 내용 재현 — **OpenCV 4.11.0 배포본에 `NOTICE` 파일이 없다**
   (소스 트리 확인함). 따라서 §4(d) 의무는 발생하지 않는다.

---

## 6. zlib ⚠️ 바이너리에 정적 링크됨

- **URL:** https://zlib.net/
- **Version:** OpenCV 4.11.0 동봉본
- **License:** zlib License
- **위치:** `srt3d_uwp.dll` 에 **정적 링크** (OpenCV 의 3rdparty 의존성으로 유입)

> 링크 검증: 생성된 `srt3d_uwp.vcxproj` 의 링크 목록에 `zlib.lib` 가 있고,
> 산출 DLL 에서 `inflate` / `deflate` / `zlib` 심볼 문자열이 확인된다.
> (`libopenjp2` 는 링크되지 **않았다** — imgcodecs 를 끈 빌드이기 때문.)

```
Copyright notice:

 (C) 1995-2022 Jean-loup Gailly and Mark Adler

  This software is provided 'as-is', without any express or implied
  warranty.  In no event will the authors be held liable for any damages
  arising from the use of this software.

  Permission is granted to anyone to use this software for any purpose,
  including commercial applications, and to alter it and redistribute it
  freely, subject to the following restrictions:

  1. The origin of this software must not be misrepresented; you must not
     claim that you wrote the original software. If you use this software
     in a product, an acknowledgment in the product documentation would be
     appreciated but is not required.
  2. Altered source versions must be plainly marked as such, and must not be
     misrepresented as being the original software.
  3. This notice may not be removed or altered from any source distribution.

  Jean-loup Gailly        Mark Adler
  jloup@gzip.org          madler@alumni.caltech.edu
```

---

## 7. Eigen

- **URL:** https://gitlab.com/libeigen/eigen
- **Version:** **3.4.0** (CMake FetchContent `GIT_TAG 3.4.0`)
- **License:** **MPL 2.0** (Mozilla Public License 2.0)
- **전문:** https://www.mozilla.org/MPL/2.0/
- **위치:** 저장소에 포함하지 않음. 빌드 시 취득되어 `srt3d_uwp.dll` 에 헤더 온리로 반영

### LGPL 미접촉 검증

Eigen 의 `COPYING.README` 는 다음을 밝힌다:

> "Eigen is primarily MPL2 licensed. … Some files contain third-party code under
> BSD or LGPL licenses, whence the other COPYING.* files here."

Eigen 3.4.0 에서 LGPL 표기가 있는 파일은 4개다:
`Eigen/src/IterativeLinearSolvers/IncompleteLUT.h`, `unsupported/Eigen/FFT`,
`unsupported/Eigen/src/IterativeSolvers/ConstrainedConjGrad.h`,
`unsupported/Eigen/src/IterativeSolvers/IterationController.h`

**이 프로젝트는 그중 어느 것도 사용하지 않는다.** `native/srt3d_uwp/CMakeLists.txt` 가
**`EIGEN_MPL2_ONLY` 를 상시 정의**하며, 이 매크로는 LGPL 코드가 include 되는 순간
컴파일 에러를 발생시킨다. 2026-08-03 이 정의로 Release 빌드가 통과함을 확인했다
(산출 DLL 854,528 bytes — 정의 전과 동일).

→ **LGPL 의무는 발생하지 않는다.**

**MPL 2.0 의무:** 파일 단위 카피레프트다. Eigen 파일을 **수정하지 않았으므로**
소스 공개 의무는 발생하지 않으며, 저작권 고지와 라이선스 전문 접근 경로 제공으로 충분하다.
사용 헤더는 `<Eigen/Dense>`, `<Eigen/Geometry>`, `<unsupported/Eigen/MatrixFunctions>` 3개다.

---

## 8. Mixed Reality Toolkit — Foundation

- **URL:** https://github.com/microsoft/MixedRealityToolkit-Unity
- **Version:** **2.8.3**
- **License:** MIT
- **위치:** `Packages/MixedReality/com.microsoft.mixedreality.toolkit.foundation-2.8.3.tgz`

```
Copyright (c) Microsoft Corporation.
```

**동반 고지 (패키지 내 `NOTICE.md`):**

```
NOTICES AND INFORMATION
Do Not Translate or Localize

This software incorporates material from third parties. Microsoft makes certain
open source code available at http://3rdpartysource.microsoft.com, or you may
send a check or money order for US $5.00, including the product name, the open
source component name, and version number, to:

Source Code Compliance Team
Microsoft Corporation
One Microsoft Way
Redmond, WA 98052
```

---

## 9. Mixed Reality Toolkit — Standard Assets

- **URL:** https://github.com/microsoft/MixedRealityToolkit-Unity
- **Version:** **2.8.3**
- **License:** MIT — `Copyright (c) Microsoft Corporation.`
- **위치:** `Packages/MixedReality/com.microsoft.mixedreality.toolkit.standardassets-2.8.3.tgz`
- **동반 고지:** §8 과 동일한 `NOTICE.md` 포함

---

## 10. Mixed Reality OpenXR Plugin

- **URL:** https://github.com/microsoft/MixedRealityToolkit-Unity (관련), Microsoft
- **Version:** **1.11.2**
- **License:** MIT — `Copyright (c) Microsoft Corporation.`
- **위치:** `Packages/MixedReality/com.microsoft.mixedreality.openxr-1.11.2.tgz`

---

## 11. MRTK — 프로젝트에 생성된 파일

- **License:** MIT — `Copyright (c) Microsoft Corporation.`
- **위치:**
  - `Assets/MRTK/Shaders/` (23개 — `.shader`, `.cginc`)
  - `Assets/MixedRealityToolkit.Generated/` (6개 — `link.xml`, `ProjectPreferences.asset`, sentinel)

MRTK 패키지가 임포트 시 프로젝트에 생성한 파일들이다. §8 과 동일 라이선스이며,
MRTK 재임포트로 재생성할 수 있다.

---

## 12. TextMesh Pro — Essential Resources ⚠️ 범용 오픈소스 아님

- **Version:** `com.unity.textmeshpro` **3.0.7**
- **License:** **Unity Companion License**
- **전문:** https://unity.com/legal/licenses/unity-companion-license
- **위치:** `Assets/TextMesh Pro/` (72개 파일)

**의무 및 제약:**

- 유효한 Unity 저작·렌더링 엔진 라이선스 하에서 저작물을 **저작·배포하는 것과 관련해서만**
  사용할 수 있다.
- **경쟁 분석 또는 경쟁 제품·서비스 개발에 사용할 수 없다.**
- **MIT/Apache 같은 범용 오픈소스 라이선스가 아니다.** 이 저장소를 Unity 밖에서
  재사용하려는 경우 이 폴더는 제외해야 한다.

> 이 폴더는 Unity 에디터에서
> `Window > TextMeshPro > Import TMP Essential Resources` 로 재생성할 수 있다.
> 프로젝트 자체 코드는 TextMeshPro 를 직접 사용하지 않으며, MRTK UI 가 내부적으로 사용한다.

---

## 13. Liberation Sans (Liberation Fonts)

- **License:** **SIL Open Font License, Version 1.1**
- **위치:** `Assets/TextMesh Pro/Fonts/`
- **전문 동봉:** ✅ `Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt` 에 이미 포함돼 있음

```
Digitized data copyright (c) 2010 Google Corporation
        with Reserved Font Arimo, Tinos and Cousine.
Copyright (c) 2012 Red Hat, Inc.
        with Reserved Font Name Liberation.
```

---

## 14. EmojiOne / JoyPixels — ✅ **삭제됨 (2026-08-03)**

**이 저장소는 EmojiOne 에셋을 더 이상 포함하지 않는다.** 라이선스 의무가 발생하지 않는다.

### 삭제 사유

동봉돼 있던 `EmojiOne Attribution.txt` 는 다음만 적혀 있고 **실제 라이선스 조건이 없었다**:

```
This sample of beautiful emojis are provided by EmojiOne https://www.emojione.com/

Please visit their website to view the complete set of their emojis and review
their licensing terms.
```

EmojiOne 은 현재 **JoyPixels** 로 개명했고 조건을 원문으로 확인할 수 없었다.
조건 불명 상태로 재배포하는 것보다, 사용하지 않는 에셋을 제거하는 편이 낫다고 판단했다.

### 미사용 근거 (삭제 전 추적 결과)

| 대상 | 참조 상태 |
|---|---|
| `EmojiOne.png` | `EmojiOne.asset` (TMP 기본 sprite asset) 에서만 참조 |
| `EmojiOne.asset` | `TMP Settings.asset` 의 `m_defaultSpriteAsset` 에서만 참조 |
| `EmojiOne.json` | 미참조 |
| 프로젝트 씬 (`SampleScene.unity`) | TextMeshPro 참조 **0건** |
| 프로젝트 코드 | TMPro 사용 **0건** |

TMP 의 기본값으로만 연결돼 있었고 실제 콘텐츠에서 쓰이지 않았다.

### 삭제 내역 — 10개 파일

```
Assets/TextMesh Pro/Sprites/EmojiOne.png                       (+ .meta)
Assets/TextMesh Pro/Sprites/EmojiOne.json                      (+ .meta)
Assets/TextMesh Pro/Sprites/EmojiOne Attribution.txt           (+ .meta)
Assets/TextMesh Pro/Resources/Sprite Assets/EmojiOne.asset     (+ .meta)
Assets/TextMesh Pro/Sprites.meta                     ← 빈 폴더의 고아 meta
Assets/TextMesh Pro/Resources/Sprite Assets.meta     ← 빈 폴더의 고아 meta
```

선행 조치로 `TMP Settings.asset` 의 `m_defaultSpriteAsset` 을
`{fileID: 0}` 으로 비워 고아 GUID 참조를 제거했다.

> ⚠️ **히스토리에는 남아 있다.** 이 파일들은 `6688e42 Initial commit` 부터 존재했으므로
> 과거 커밋에서 여전히 취득 가능하다. 완전 제거는 향후 `git filter-repo` 수행 시
> 함께 처리할 것.

---

## 15. 미포함 — Microsoft Mixed Reality SDK DLL (독점 EULA)

아래 2개는 **재배포가 금지되어 이 저장소에 포함하지 않는다.**

| NuGet 패키지 | 버전 | 라이선스 |
|---|---|---|
| `Microsoft.MixedReality.EyeTracking` | 1.0.2 | 독점 Microsoft Software License Terms |
| `Microsoft.MixedReality.SceneUnderstanding` | 1.0.14 | 독점 Microsoft Software License Terms |

해당 EULA 는 사용 범위를 개발·테스트로 한정하고 다음을 금지한다:

> "share, publish, distribute, or lend the software (except for any distributable
> code, subject to the terms above), provide the software as a stand-alone hosted
> solution for others to use, or transfer the software or this agreement to any
> third party."

`except for any distributable code` 예외가 있으나 **해당 EULA 에 "Distributable Code"
정의 절이 존재하지 않아** 예외 범위를 특정할 수 없다. 따라서 포함하지 않는다.

현재 프로젝트 구성에서는 **이 DLL 없이 빌드된다.** 복원이 필요하면
[`README.md` 「Microsoft SDK DLL 복원」](README.md#microsoft-sdk-dll-복원-선택) 참조.

> 두 패키지는 Protocol Buffers 코드를 Google 의 BSD 계열 라이선스로 포함한다고
> 명시하나, 해당 DLL 을 배포하지 않으므로 이 저장소에는 그 의무가 발생하지 않는다.

---

## 16. 버전 미상 항목

아래 항목은 정확한 버전·커밋을 특정하지 못했다. 확인되는 대로 채울 것.

| 구성요소 | 상태 | 사유 |
|---|---|---|
| **hl2ss** | `version: unspecified` | `hl2ss.dll` / `hl2ss.cs` 에 버전 표기가 없고, 취득 시점 기록이 없다. `hl2ss.cs` 파일 타임스탬프는 2024-12-19 |
| **pysrt3d** | `version: unspecified` (fork point not recorded) | 편입 소스에 VCS 메타데이터가 없어 어느 커밋에서 갈라졌는지 특정 불가. [`MODIFICATIONS.md`](native/srt3d_uwp/MODIFICATIONS.md) §1 참조 |
| **XRHandEyeTracker `source_nogl`** | `version: unspecified` | 중간 파생 단계(Galaxy XR 프로젝트에서 GL 제거). 공개 저장소가 아니며 버전 표기가 없다 |
| **tiny_obj_loader** | 버전 표기 미확인 | 편입 헤더의 버전 매크로 확인 필요 |

---

## MIT License 전문

§1(SRT3D), §3(pysrt3d), §4(tiny_obj_loader), §8~§11(Microsoft) 에 적용된다.
각 항목의 저작권 고지는 해당 절을 참조할 것.

```
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
