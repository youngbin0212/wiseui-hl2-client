# WISEUI HoloLens 2 Workspace Consolidation Design

**Date:** 2026-09-07

## Goal

Consolidate the HoloLens 2 client, Windows initialization server, external research dependencies, release artifacts, and legacy backup under one local workspace while preserving the client Git history and making the client and server independently reproducible from GitHub.

## Resulting local layout

```text
D:/ProjectsTracking/wiseui-hl2/
├── client/
│   └── wiseui-hl2-client/
├── server/
│   └── wiseui-hl2-server/
├── dependencies/
│   ├── hl2ss/
│   ├── sam3/
│   └── FoundationPose/
├── artifacts/
│   └── releases/
└── archive/
    └── hololens2_wiseui_backup_20260803/
```

The workspace root is an organizational directory, not a Git repository. The client, server, and each upstream dependency retain independent Git boundaries.

## GitHub repositories

### Client

The existing `youngbin0212/wiseui-hl2-object-tracking` repository is renamed to `youngbin0212/wiseui-hl2-client`. Its complete commit history, branches, tags, Unity project files, native SRT3D source, prebuilt HoloLens plugins, and required book tracking assets are preserved.

Generated Unity directories and local settings remain ignored: `Library/`, `Temp/`, `Logs/`, `Build/`, `Build_*/`, `UserSettings/`, IDE files, certificates with private keys, and local agent settings.

### Server

A new private repository named `youngbin0212/wiseui-hl2-server` is created from the reusable parts of `D:/ProjectsTracking/hl2_pipeline` and the integration entry points currently stored as untracked files inside SAM3 and FoundationPose checkouts.

The server repository contains:

```text
wiseui-hl2-server/
├── init_server.py
├── hl2_capture.py
├── diagnostics/
├── integrations/
│   ├── sam3/sam3_server.py
│   └── foundationpose/fp_server_gxr.py
├── patches/foundationpose.patch
├── config/dependencies.lock.yaml
├── scripts/setup_dependencies.ps1
├── scripts/check_services.ps1
├── tests/
├── .env.example
├── .gitignore
└── README.md
```

The repository does not include captured sensor data, device calibration, model checkpoints, Docker images, local environment files, caches, generated output, or private credentials.

## Dependency policy

Dependencies are local independent checkouts and are not nested into either project repository.

`config/dependencies.lock.yaml` records the authoritative upstream URL and exact tested commit:

| Dependency | Upstream | Pinned commit | Local policy |
|---|---|---|---|
| hl2ss | `https://github.com/jdibenes/hl2ss.git` | `fcc4e84` | Clean upstream checkout |
| SAM3 | `https://github.com/facebookresearch/sam3.git` | `f66a251` | Clean upstream checkout; WISEUI server entry point lives in the server repository |
| FoundationPose | `https://github.com/NVlabs/FoundationPose.git` | `e3d597b` | Upstream checkout plus a reviewed patch stored in the server repository |

The setup script clones missing dependencies, checks out the pinned commit, and applies only the recorded FoundationPose patch. It refuses to overwrite a dirty dependency checkout.

Git submodules are not used. They would expose internal directory coupling in both repositories and complicate the mixed Windows, WSL2, and Docker workflow. Forks are not required for clean dependencies; a patch records the small tested FoundationPose divergence without publishing datasets or a full vendor copy.

## Runtime configuration

Machine-specific addresses and paths move to environment variables documented in `.env.example`:

```text
HL2_HOST=192.168.0.16
INIT_HOST=0.0.0.0
INIT_PORT=8002
FP_URL=http://127.0.0.1:8000
SAM3_ADDR=tcp://127.0.0.1:5556
SAM3_SERVER_ADDR=tcp://host.docker.internal:5556
OBJ_TEXT=book
INIT_ITER=5
FP_AMP=0
```

No live IP address is embedded in distributable client source. The client gets the initialization server address from an explicit build or runtime configuration mechanism. The tested local value is `http://192.168.0.7:8002`.

Path resolution is based on the workspace root or explicit environment variables. Existing assumptions such as `../hl2ss`, `/mnt/d/ProjectsTracking/sam3`, and `/mnt/d/ProjectsTracking/FoundationPose` are replaced or wrapped so the new layout works from Windows, WSL2, and Docker.

## Data and artifact handling

Device-specific calibration from `hl2_pipeline/calibration` is copied to a dated local archive before migration and remains ignored in the server repository. It may be restored into the server working directory after cloning.

Diagnostic output from `hl2_pipeline/out`, `sam3/probe_out`, and FoundationPose capture/debug directories is archived locally and excluded from Git.

The most recent working MSIX, certificate, and ARM64 dependency packages are copied to `artifacts/releases/<date>/`. Private signing material is kept local. `Build_612`, Unity `Library`, `Temp`, and other reproducible generated directories are removed only after the preserved release and migration backup have been verified.

SAM3 checkpoints, `foundationpose.tar`, Docker images, datasets, and other large model artifacts remain local. README instructions identify their expected paths and acquisition requirements without committing them.

## Preservation and migration

Before moving directories, record each source path, size, Git HEAD, remote, status, and file hashes for critical scripts, calibration, client plugins, tracking mesh, SRT3D model, and preserved MSIX. Create a migration manifest in the workspace archive.

Move the clean client repository without recreating `.git`, preserving its identity and history. Copy the server source into a newly initialized repository, then separate reusable integration code from outputs and device state. Move clean dependency repositories while retaining their own `.git` directories. FoundationPose's current dirty state is captured as source patch, integration files, and local archive before its working tree is normalized.

Original compatibility paths are not silently deleted during validation. Temporary directory junctions may point old paths to the new locations until client build, all three server ports, HoloLens capture, initial pose acquisition, and SRT3D tracking have passed. Junctions are documented in the migration manifest and removed after the new paths are confirmed.

## Validation

Migration is accepted when all of the following hold:

1. Client HEAD and full Git history match the pre-migration repository.
2. A clean clone of the client can export UWP ARM64 with Unity 2022.3.62f3.
3. Server syntax and focused tests pass without calibration or captured output in Git.
4. Dependency setup resolves the pinned commits and applies the FoundationPose patch cleanly.
5. SAM3 listens on port 5556, FoundationPose reports healthy on port 8000, and init_server reports healthy on port 8002.
6. The PC reads PV, long-throw depth, and intrinsics from HoloLens `192.168.0.16`.
7. `/init_box` returns a valid 16-value pose and the client initializes SRT3D with the matching book model.
8. Git status is clean in both publishable repositories after generated runtime output.
9. GitHub contains no checkpoints, captures, calibration, private keys, credentials, or local absolute paths beyond documented examples.

## Publication and rollback

The client repository rename occurs after the local move is verified; its local `origin` is updated to the canonical new URL. GitHub redirects from the old repository URL are treated as compatibility only.

The server is pushed only after a staged-content review and secret/large-file scan. It starts private because the integration scripts encode research workflow details and depend on third-party licensing review.

Rollback uses the migration manifest and preserved archive to restore original directories. No source archive or original repository is removed until both GitHub repositories and the end-to-end initialization path have been verified.
