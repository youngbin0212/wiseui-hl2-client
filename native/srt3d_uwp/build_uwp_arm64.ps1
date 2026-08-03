# srt3d_uwp.dll (HoloLens 2, UWP/WindowsStore ARM64) 빌드.
#
# 사전 준비 (native/srt3d_uwp/README.md 참조):
#   - OpenCV 4.11.0 을 UWP ARM64 (core+imgproc, static) 로 빌드해 둘 것  ← 유일한 수동 준비
#   - Eigen 3.4.0 은 CMake FetchContent 가 자동 처리 (준비 불필요)
#   - tiny_obj_loader 는 third_party/ 에 편입돼 있음 (준비 불필요)
#
# 실행:
#   powershell -ExecutionPolicy Bypass -File .\build_uwp_arm64.ps1 -OpenCvDir <경로>
#   또는 환경변수:  $env:SRT3D_OPENCV_DIR = "<경로>" ; .\build_uwp_arm64.ps1
#
# 산출:  build_arm64_uwp\Release\srt3d_uwp.dll
#        → Unity 프로젝트의 Assets\Plugins\WSA\ARM64\ 에 복사 (-CopyTo 로 자동화 가능)

[CmdletBinding()]
param(
    # OpenCV UWP ARM64 static 빌드의 config 폴더.
    # ⚠️ install 루트가 아니라 <install>\ARM64\vc17\staticlib 를 직접 가리켜야 한다 —
    #    루트 dispatcher 인 OpenCVConfig.cmake 는 static 구성을 인식하지 못한다.
    [string]$OpenCvDir = $(if ($env:SRT3D_OPENCV_DIR) { $env:SRT3D_OPENCV_DIR }
                           else { Join-Path $PSScriptRoot "..\..\..\opencv_uwp\install\ARM64\vc17\staticlib" }),

    # 로컬 Eigen 3.4.0 루트 (Eigen/ 을 담은 폴더).
    # 비우면 CMake FetchContent 가 GIT_TAG 3.4.0 으로 내려받는다 — 오프라인 빌드 때만 지정.
    [string]$EigenDir = $(if ($env:SRT3D_EIGEN_DIR) { $env:SRT3D_EIGEN_DIR } else { "" }),

    # 빌드 디렉터리 (기본: 스크립트 옆 build_arm64_uwp)
    [string]$BuildDir = (Join-Path $PSScriptRoot "build_arm64_uwp"),

    # 지정하면 빌드된 DLL 을 이 폴더로 복사 (예: ..\..\Assets\Plugins\WSA\ARM64)
    [string]$CopyTo = ""
)

$ErrorActionPreference = "Stop"

# $PSScriptRoot 기준 — 저장소를 어디에 클론하든 동작한다.
$root   = $PSScriptRoot
$build  = $BuildDir
$opencv = [System.IO.Path]::GetFullPath($OpenCvDir)

if (-not (Test-Path "$opencv\OpenCVConfig.cmake")) {
    throw @"
OpenCV UWP install 을 찾을 수 없음: $opencv

-OpenCvDir 로 경로를 지정하거나 `$env:SRT3D_OPENCV_DIR 를 설정할 것.
<install>\ARM64\vc17\staticlib 를 직접 가리켜야 한다 (install 루트 아님).
빌드 방법은 native/srt3d_uwp/README.md 참조.
"@
}

# Eigen 3.4.0 은 CMake FetchContent 가 자동 처리한다 (준비 불필요).
# -EigenDir 를 준 경우에만 로컬 사본을 CMake 로 넘긴다.
$cmakeArgs = @(
    "-S", $root, "-B", $build,
    "-G", "Visual Studio 17 2022", "-A", "ARM64",
    "-DCMAKE_SYSTEM_NAME=WindowsStore",
    "-DCMAKE_SYSTEM_VERSION=10.0",
    "-DCMAKE_SYSTEM_PROCESSOR=ARM64",
    "-DCMAKE_POLICY_VERSION_MINIMUM=3.5",
    "-DOpenCV_STATIC=ON",
    "-DOpenCV_DIR=$opencv"
)
if ($EigenDir) {
    $eigenFull = [System.IO.Path]::GetFullPath($EigenDir)
    if (-not (Test-Path "$eigenFull\Eigen")) { throw "-EigenDir 에 Eigen\ 폴더가 없음: $eigenFull" }
    $cmakeArgs += "-DSRT3D_EIGEN_DIR=$eigenFull"
    Write-Host "Eigen: 로컬 사본 사용 — $eigenFull" -ForegroundColor Yellow
} else {
    Write-Host "Eigen: FetchContent 로 3.4.0 다운로드 (최초 1회, 네트워크 필요)" -ForegroundColor Yellow
}

Write-Host "=== configure (UWP ARM64 DLL) ===" -ForegroundColor Cyan
cmake @cmakeArgs
if ($LASTEXITCODE -ne 0) { throw "configure 실패 ($LASTEXITCODE)" }

Write-Host "=== build (Release) ===" -ForegroundColor Cyan
cmake --build $build --config Release --parallel
if ($LASTEXITCODE -ne 0) { throw "build 실패 ($LASTEXITCODE)" }

Write-Host "=== 산출 DLL ===" -ForegroundColor Green
$dll = Get-ChildItem $build -Recurse -Filter 'srt3d_uwp.dll' | Select-Object -First 1
if (-not $dll) { throw "srt3d_uwp.dll 을 찾을 수 없음: $build" }
Write-Host ("{0}  ({1} KB)" -f $dll.FullName, [math]::Round($dll.Length/1KB, 1))

if ($CopyTo) {
    if (-not (Test-Path $CopyTo)) { throw "-CopyTo 대상 폴더 없음: $CopyTo" }
    Copy-Item $dll.FullName -Destination $CopyTo -Force
    Write-Host "복사 완료 → $CopyTo" -ForegroundColor Green
    Write-Host "Unity Inspector 에서 플랫폼을 WSAPlayer / ARM64 로 지정할 것." -ForegroundColor Yellow
}
