# SRT3D 온디바이스 네이티브 플러그인 — HoloLens 2 (UWP/WindowsStore, ARM64).
# XRHandEyeTracker(Android NDK)의 CMake 를 UWP 로 변환:
#   - find_package(OpenCV): Android SDK → 우리가 빌드한 UWP ARM64 install
#   - SHARED 라이브러리(.dll) — Unity 네이티브 플러그인으로 로드
#   - android/log 링크 제거
# GL-free: SRT3D_NO_GL 정의, renderer/viewer/occlusion .cpp 없음(코어 6개만).
cmake_minimum_required(VERSION 3.18)
project(srt3d_uwp LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# UWP ARM64 OpenCV 4.11.0 (core+imgproc, static).
# build_uwp_arm64.ps1 이 -DOpenCV_DIR 로 전달한다. 직접 configure 한다면 환경변수도 가능:
#   cmake -DOpenCV_DIR=<경로> ...   또는   set SRT3D_OPENCV_DIR=<경로>
# ⚠️ install 루트가 아니라 <install>/ARM64/vc17/staticlib 를 가리켜야 한다 —
#    루트 dispatcher 인 OpenCVConfig.cmake 는 static 구성을 인식하지 못한다.
if(NOT DEFINED OpenCV_DIR AND DEFINED ENV{SRT3D_OPENCV_DIR})
    set(OpenCV_DIR "$ENV{SRT3D_OPENCV_DIR}")
endif()
if(NOT DEFINED OpenCV_DIR)
    message(FATAL_ERROR
        "OpenCV_DIR 이 지정되지 않았습니다.\n"
        "  -DOpenCV_DIR=<install>/ARM64/vc17/staticlib 로 전달하거나\n"
        "  환경변수 SRT3D_OPENCV_DIR 을 설정하거나\n"
        "  build_uwp_arm64.ps1 을 사용하세요. (native/srt3d_uwp/README.md 참조)")
endif()
find_package(OpenCV REQUIRED COMPONENTS core imgproc)

# ── Eigen 3.4.0 (헤더 온리) ──────────────────────────────────────────────
# 저장소에 포함하지 않고 FetchContent 로 버전 고정해 받는다.
#
# add_subdirectory 를 하지 않는 이유:
#   이 프로젝트가 쓰는 Eigen 헤더는 <Eigen/Dense>, <Eigen/Geometry>,
#   <unsupported/Eigen/MatrixFunctions> 3개뿐이고 링크할 라이브러리가 없다.
#   반면 Eigen 의 CMakeLists 는 test/doc/pkgconfig/install 타깃과 CTest 를 끌어들이고,
#   UWP ARM64 크로스 빌드에서 의미 없는 try_compile 검사를 돈다.
#   SOURCE_SUBDIR 를 존재하지 않는 경로로 두면 MakeAvailable 이 다운로드만 하고
#   add_subdirectory 를 건너뛴다 (CMake 문서화된 동작).
#   FetchContent_Populate 단일 인자 형태는 CMake 3.30+ 에서 deprecated 라 쓰지 않는다.
#
# 오프라인/로컬 우선순위:
#   -DSRT3D_EIGEN_DIR=<경로>  →  $ENV{SRT3D_EIGEN_DIR}  →  third_party/eigen3  →  FetchContent
set(SRT3D_EIGEN_DIR "" CACHE PATH
    "로컬 Eigen 3.4.0 루트 (Eigen/ 을 담은 폴더). 비우면 FetchContent 로 내려받는다.")

if(NOT SRT3D_EIGEN_DIR AND DEFINED ENV{SRT3D_EIGEN_DIR})
    set(SRT3D_EIGEN_DIR "$ENV{SRT3D_EIGEN_DIR}")
endif()
if(NOT SRT3D_EIGEN_DIR AND EXISTS "${CMAKE_CURRENT_SOURCE_DIR}/third_party/eigen3/Eigen")
    set(SRT3D_EIGEN_DIR "${CMAKE_CURRENT_SOURCE_DIR}/third_party/eigen3")
endif()

if(SRT3D_EIGEN_DIR)
    if(NOT EXISTS "${SRT3D_EIGEN_DIR}/Eigen")
        message(FATAL_ERROR
            "SRT3D_EIGEN_DIR 에 Eigen/ 하위 폴더가 없습니다: ${SRT3D_EIGEN_DIR}\n"
            "Eigen 루트(= Eigen/ 과 unsupported/ 를 담은 폴더)를 지정하세요.")
    endif()
    set(SRT3D_EIGEN_INCLUDE_DIR "${SRT3D_EIGEN_DIR}")
    message(STATUS "Eigen: 로컬 사본 사용 — ${SRT3D_EIGEN_INCLUDE_DIR}")
else()
    include(FetchContent)
    FetchContent_Declare(eigen
        GIT_REPOSITORY https://gitlab.com/libeigen/eigen.git
        GIT_TAG        3.4.0
        GIT_SHALLOW    TRUE
        # ↓ 존재하지 않는 하위 경로 → add_subdirectory 생략 (헤더만 필요)
        SOURCE_SUBDIR  cmake-skip-add-subdirectory
    )
    FetchContent_MakeAvailable(eigen)
    set(SRT3D_EIGEN_INCLUDE_DIR "${eigen_SOURCE_DIR}")
    message(STATUS "Eigen: FetchContent 3.4.0 — ${SRT3D_EIGEN_INCLUDE_DIR}")
endif()

# SRT3D 코어 6개 .cpp (source_nogl, XRHandEyeTracker 에서 복사한 검증본)
file(GLOB SRT3D_CORE ${CMAKE_CURRENT_SOURCE_DIR}/srt3d/src/*.cpp)

add_library(srt3d_uwp SHARED
    ${SRT3D_CORE}
    ${CMAKE_CURRENT_SOURCE_DIR}/srt3d_uwp.cpp
)

target_compile_definitions(srt3d_uwp PRIVATE
    SRT3D_NO_GL
    # Eigen 은 주로 MPL2 지만 일부 파일이 LGPL 이다 (COPYING.README).
    # EIGEN_MPL2_ONLY 를 정의하면 LGPL 코드를 include 하는 순간 컴파일 에러가 난다
    # → LGPL 의무가 발생할 경로를 구조적으로 차단한다.
    # 2026-08-03 검증: 이 정의로 빌드 통과 확인 (LGPL 코드 미접촉).
    EIGEN_MPL2_ONLY
)

target_include_directories(srt3d_uwp PRIVATE
    ${CMAKE_CURRENT_SOURCE_DIR}                    # "srt3d/virtual_camera.hpp"
    ${CMAKE_CURRENT_SOURCE_DIR}/srt3d/include       # <srt3d/*.h>
    ${CMAKE_CURRENT_SOURCE_DIR}/third_party         # <tiny_obj_loader/*.h> (편입됨)
    ${SRT3D_EIGEN_INCLUDE_DIR}                      # <Eigen/*>, <unsupported/Eigen/*>
    ${OpenCV_INCLUDE_DIRS}
)

target_link_libraries(srt3d_uwp ${OpenCV_LIBS})

# UWP(WindowsStore) 링커 이슈 해결: vccorlib.lib 가 msvcrt.lib 보다 먼저 링크돼야 함.
# (LNK2038 vccorlib_lib_should_be_specified_before_msvcrt_lib / LNK2005 __crtWinrtInitType)
# MS 공식 해법: 기본 자동링크를 끄고(NODEFAULTLIB) 올바른 순서로 명시.
if(CMAKE_SYSTEM_NAME STREQUAL "WindowsStore")
    target_link_options(srt3d_uwp PRIVATE
        $<$<CONFIG:Release>:/NODEFAULTLIB:vccorlib.lib>
        $<$<CONFIG:Release>:/NODEFAULTLIB:msvcrt.lib>
        $<$<CONFIG:Release>:vccorlib.lib>
        $<$<CONFIG:Release>:msvcrt.lib>
        $<$<CONFIG:Debug>:/NODEFAULTLIB:vccorlibd.lib>
        $<$<CONFIG:Debug>:/NODEFAULTLIB:msvcrtd.lib>
        $<$<CONFIG:Debug>:vccorlibd.lib>
        $<$<CONFIG:Debug>:msvcrtd.lib>)
endif()

set_target_properties(srt3d_uwp PROPERTIES OUTPUT_NAME "srt3d_uwp" PREFIX "")
