# WISEUI HoloLens 2 Workspace Consolidation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Consolidate the WISEUI HoloLens 2 workspace, preserve all recoverable local state, publish the client and server as separate GitHub repositories, and retain a reproducible pinned dependency setup.

**Architecture:** The workspace root is an organizational directory. The Unity/UWP client and Python initialization server are independent Git repositories; hl2ss, SAM3, and FoundationPose remain independent pinned upstream checkouts. Generated builds, device calibration, checkpoints, captures, and legacy working trees remain local under artifacts or archive.

**Tech Stack:** PowerShell 7, Git, GitHub web UI, Unity 2022.3.62f3, Visual Studio 2022/MSBuild, Python/pytest, WSL2, Docker Desktop, SAM3, FoundationPose, hl2ss.

**Spec:** `docs/superpowers/specs/2026-09-07-wiseui-hl2-workspace-design.md`

## Global Constraints

- Preserve the client Git history beginning at current commit `7c0f4af`.
- Create `youngbin0212/wiseui-hl2-server` as a private repository.
- Do not publish sensor captures, calibration, checkpoints, Docker images, datasets, signing private keys, credentials, or local agent settings.
- Pin hl2ss `fcc4e84`, SAM3 `f66a251`, and FoundationPose `e3d597b`.
- Preserve current dirty SAM3 and FoundationPose working state before normalizing or removing any file.
- Use one PowerShell process for each move and verify every resolved source and destination is under `D:/ProjectsTracking` before moving.
- Keep compatibility junctions at the original paths until end-to-end HoloLens initialization and tracking pass.
- Do not remove a source archive or original build until its manifest and critical hashes have been verified.
- Use client initialization server URL `http://192.168.0.7:8002` only as an explicit local build value, never as a committed production default.

---

### Task 1: Capture a recoverable pre-migration inventory

**Files:**
- Create: `D:/ProjectsTracking/wiseui-hl2/archive/migration-20260907/manifest.json`
- Create: `D:/ProjectsTracking/wiseui-hl2/archive/migration-20260907/git-status/*.txt`
- Create: `D:/ProjectsTracking/wiseui-hl2/archive/migration-20260907/hashes.csv`

**Interfaces:**
- Consumes: current client, server, dependency, build, and backup paths.
- Produces: immutable evidence used to verify every later move and rollback.

- [ ] **Step 1: Resolve and validate all migration roots**

Use `Resolve-Path -LiteralPath` for `hololens2_wiseui`, `hl2_pipeline`, `hl2ss`, `sam3`, `FoundationPose`, and `hololens2_wiseui_backup_20260803`. Assert every result starts with `D:\ProjectsTracking\` and no destination exists as an unrelated real directory.

- [ ] **Step 2: Record filesystem and Git inventory**

Record source path, resolved path, file count, byte count, last-write time, Git HEAD, branch, remotes, status, stashes, and tags. Record that the client starts at `7c0f4af`, hl2ss at `fcc4e84`, SAM3 at `f66a251`, and FoundationPose at `e3d597b`.

- [ ] **Step 3: Hash critical files**

Hash with SHA-256:

```text
hololens2_wiseui/Assets/Scripts/Srt3dTracker.cs
hololens2_wiseui/Assets/Plugins/WSA/ARM64/hl2ss.dll
hololens2_wiseui/Assets/Plugins/WSA/ARM64/srt3d_uwp.dll
hololens2_wiseui/Assets/StreamingAssets/srt3d/joke_book_hl2c.obj
hololens2_wiseui/Assets/StreamingAssets/srt3d/joke_book_hl2c.obj.meta.bytes
hl2_pipeline/init_server.py
hl2_pipeline/hl2_capture.py
sam3/sam3_server.py
sam3/checkpoints/sam3.1_multiplex.pt
FoundationPose/fp_server_gxr.py
FoundationPose/my_data/joke_book/textured_meshes/joke_book_hl2c.obj
FoundationPose/my_data/joke_book/textured_meshes/joke_book_hl2c.obj.meta
```

- [ ] **Step 4: Export dependency changes without modifying working trees**

Save `git diff --binary` for FoundationPose tracked changes and lists of untracked files for SAM3 and FoundationPose. Create Git bundles for the client and all dependency repositories. Verify each bundle with `git bundle verify`.

- [ ] **Step 5: Verify rollback evidence**

Parse `manifest.json`, ensure every declared source exists, every required critical hash is present, and every Git bundle verifies. Stop migration on any failure.

---

### Task 2: Create the server repository from reusable source

**Files:**
- Create: `D:/ProjectsTracking/wiseui-hl2/server/wiseui-hl2-server/.gitignore`
- Create: `D:/ProjectsTracking/wiseui-hl2/server/wiseui-hl2-server/README.md`
- Copy: `init_server.py`, `hl2_capture.py`
- Create: `diagnostics/diag_pose.py`
- Create: `diagnostics/hl2_register.py`
- Create: `diagnostics/test_first_frame.py`
- Create: `diagnostics/test_gaze_point.py`
- Create: `integrations/sam3/sam3_server.py`
- Create: `integrations/foundationpose/fp_server_gxr.py`
- Create: `tests/test_repository_contract.py`

**Interfaces:**
- Consumes: reusable Python entry points from `hl2_pipeline`, SAM3, and FoundationPose.
- Produces: a standalone private server repository that imports dependencies by explicit paths.

- [ ] **Step 1: Write the repository contract test**

The test must assert that required entry points exist, Python files parse with `ast.parse`, `.gitignore` covers each forbidden class, and no tracked candidate path starts with `calibration/`, `out/`, `probe_out/`, `diag_captures/`, `checkpoints/`, `__pycache__/`, or `.claude/`.

- [ ] **Step 2: Run the test against the empty repository skeleton**

Run:

```powershell
& "D:/Programs/anaconda3/envs/my_base/python.exe" -m pytest tests/test_repository_contract.py -q
```

Expected: failure because the entry points and ignore contract are absent.

- [ ] **Step 3: Copy only reusable source**

Copy the listed scripts without their output directories. Keep integration entry points in the server repository; do not copy upstream SAM3 or FoundationPose packages.

- [ ] **Step 4: Add a deny-by-default `.gitignore`**

Ignore `.env`, calibration, output, captures, checkpoints, model weights, archives, caches, Python environments, Docker exports, Unity packages, `*.pfx`, local settings, and files larger than the explicitly reviewed server model assets.

- [ ] **Step 5: Document process boundaries**

README must describe ports 5556, 8000, and 8002; Windows, WSL2, and Docker responsibilities; HoloLens `HL2_HOST`; the book model requirement; start order; health checks; and shutdown.

- [ ] **Step 6: Run repository contract tests**

Expected: all tests pass with no runtime data present.

- [ ] **Step 7: Initialize and commit the server repository**

```powershell
git init -b main
git add .
git commit -m "feat: preserve WISEUI HoloLens initialization server"
```

Review `git status --short` and `git ls-files` before committing.

---

### Task 3: Make dependency setup reproducible

**Files:**
- Create: `config/dependencies.lock.yaml`
- Create: `patches/foundationpose.patch`
- Create: `scripts/setup_dependencies.ps1`
- Create: `scripts/check_dependency_state.ps1`
- Create: `tests/test_dependency_contract.py`

**Interfaces:**
- Consumes: upstream URLs, pinned commits, the captured FoundationPose binary diff, and `WISEUI_HL2_ROOT`.
- Produces: deterministic local dependency setup without vendoring upstream repositories.

- [ ] **Step 1: Write failing dependency contract tests**

Tests parse the YAML and assert the three exact URLs and commits. They verify that every patch target path exists in the pinned FoundationPose tree and that setup rejects a dirty checkout before checkout/reset/apply operations.

- [ ] **Step 2: Run tests and observe missing lock/setup failure**

Run with the server repository's selected Python environment. Expected: failure due to absent contract files.

- [ ] **Step 3: Create the lock file**

Record:

```yaml
hl2ss: {url: https://github.com/jdibenes/hl2ss.git, commit: fcc4e84}
sam3: {url: https://github.com/facebookresearch/sam3.git, commit: f66a251}
foundationpose: {url: https://github.com/NVlabs/FoundationPose.git, commit: e3d597b, patch: patches/foundationpose.patch}
```

- [ ] **Step 4: Generate and review the FoundationPose patch**

Generate the patch from `estimater.py` and `learning/training/predict_score.py`. Exclude line-ending-only changes in `build_all.sh`. Confirm the patch contains no datasets, binary blobs, addresses, usernames, or credentials.

- [ ] **Step 5: Implement safe setup behavior**

The script accepts `-WorkspaceRoot`, defaults to the parent workspace when `WISEUI_HL2_ROOT` is set, clones only missing repositories, verifies remote URL and HEAD, and stops on a dirty existing checkout. It applies the FoundationPose patch with `git apply --check` before `git apply`.

- [ ] **Step 6: Validate against disposable local clones**

Clone from the existing local repositories into a temporary test root without network access. Check out each pin, apply the patch, and run the contract tests. Remove only the verified temporary root.

- [ ] **Step 7: Commit dependency reproducibility files**

Commit message: `build: pin external research dependencies`.

---

### Task 4: Replace machine-specific server and client path assumptions

**Files:**
- Modify: server `hl2_capture.py`
- Create: server `.env.example`
- Create: server `scripts/run_init_server.ps1`
- Create: server `scripts/run_sam3.ps1`
- Create: server `scripts/run_foundationpose.ps1`
- Create: server `scripts/check_services.ps1`
- Create: client `Assets/Scripts/WiseUiRuntimeConfig.cs`
- Modify: client `Assets/Scripts/Srt3dTracker.cs`
- Modify: client `Assets/Editor/BuildScript.cs`
- Create: client `Assets/Tests/Editor/WiseUiRuntimeConfigTests.cs`

**Interfaces:**
- Consumes: environment variable `WISEUI_HL2_ROOT` and build variable `WISEUI_INIT_SERVER_URL`.
- Produces: relocatable Windows/WSL2/Docker launch paths and an explicit client build-time server URL.

- [ ] **Step 1: Write failing client runtime configuration tests**

Tests assert URL normalization, rejection of non-HTTP schemes, fallback to `http://127.0.0.1:8002`, and use of an injected build value without storing `192.168.0.7` in tracked source.

- [ ] **Step 2: Implement client configuration**

`WiseUiRuntimeConfig` receives the initialization URL generated by `BuildScript`. `BuildScript.BuildUWP` reads `WISEUI_INIT_SERVER_URL`, validates it, writes a temporary untracked StreamingAssets configuration for export, and removes it in `finally`. `Srt3dTracker.Boot` loads the value before `InitFlow` and logs its source.

- [ ] **Step 3: Write failing server launch-contract tests**

Tests run launch scripts in `-CheckOnly` mode and assert resolved dependency roots, ports, book mesh pair, SAM3 checkpoint, Python executables, Docker availability, and HoloLens host without starting services.

- [ ] **Step 4: Implement relocatable launch scripts**

Resolve dependency paths from `WISEUI_HL2_ROOT`. Pass `HL2SS_VIEWER`, `SAM3_CHECKPOINT`, `SAM3_SERVER_ADDR`, `FP_AMP=0`, `HL2_HOST`, `FP_URL`, `SAM3_ADDR`, and `INIT_PORT` explicitly. Do not edit upstream dependency source at launch time.

- [ ] **Step 5: Run client and server focused tests**

Use Unity `D:/Unity/Editors/2022.3.62f3/Editor/Unity.exe` for Editor tests and `D:/Programs/anaconda3/envs/my_base/python.exe` for server tests. Expected: all configuration and syntax tests pass.

- [ ] **Step 6: Commit both repositories**

Client commit: `build: configure initialization server at export time`.

Server commit: `build: add relocatable service launchers`.

---

### Task 5: Preserve release artifacts and migrate directories

**Files:**
- Create: `artifacts/releases/20260723/`
- Create: `artifacts/models/sam3/`
- Move: client, server, dependency, and backup directories to the approved layout.
- Create: original-path directory junctions.

**Interfaces:**
- Consumes: verified manifest from Task 1 and repositories from Tasks 2–4.
- Produces: final local layout while old paths remain usable through junctions.

- [ ] **Step 1: Preserve the latest installable release**

Copy the July 23 ARM64 MSIX, public certificate, ARM64 VCLibs dependency, and installation scripts. Hash the copied files and compare them with the originals.

- [ ] **Step 2: Preserve local runtime assets**

Move the SAM3 checkpoint to `artifacts/models/sam3/sam3.1_multiplex.pt` and configure `SAM3_CHECKPOINT` accordingly. Archive HoloLens calibration and server output under dated directories without tracking them.

- [ ] **Step 3: Verify move targets**

Resolve absolute source and destination paths and assert both are under `D:/ProjectsTracking`. Ensure destinations are empty or absent and manifest hashes still match immediately before each move.

- [ ] **Step 4: Move repositories in one PowerShell process**

Move:

```text
hololens2_wiseui                 -> wiseui-hl2/client/wiseui-hl2-client
hl2_pipeline                     -> wiseui-hl2/server/wiseui-hl2-server-local-archive
hl2ss                            -> wiseui-hl2/dependencies/hl2ss
sam3                             -> wiseui-hl2/dependencies/sam3
FoundationPose                   -> wiseui-hl2/dependencies/FoundationPose
hololens2_wiseui_backup_20260803 -> wiseui-hl2/archive/hololens2_wiseui_backup_20260803
```

Keep the clean server repository at `wiseui-hl2/server/wiseui-hl2-server`. The local archive retains calibration, output, and scripts not selected for publication.

- [ ] **Step 5: Create compatibility junctions**

Create junctions from the six original paths to their new active or archived locations. Record junction targets in the migration manifest. Do not create a junction over any remaining real directory.

- [ ] **Step 6: Re-run hashes and Git checks**

Client and dependency HEADs, client history, critical source hashes, plugin hashes, mesh hashes, and release hashes must match the pre-migration manifest.

---

### Task 6: Verify build and end-to-end initialization from the new layout

**Files:**
- Create locally: `artifacts/validation/20260907/`
- Update: migration manifest validation section.

**Interfaces:**
- Consumes: final workspace layout, runtime configuration, HoloLens `192.168.0.16`.
- Produces: evidence that migration did not break build or runtime.

- [ ] **Step 1: Run server static and focused tests**

Run pytest, Python syntax compilation, dependency state checks, and launch scripts in `-CheckOnly` mode. Store logs under the local validation directory.

- [ ] **Step 2: Export the Unity client**

With the editor closed:

```powershell
$env:WISEUI_INIT_SERVER_URL='http://192.168.0.7:8002'
& 'D:/Unity/Editors/2022.3.62f3/Editor/Unity.exe' -batchmode -quit `
  -projectPath 'D:/ProjectsTracking/wiseui-hl2/client/wiseui-hl2-client' `
  -executeMethod BuildScript.BuildUWP `
  -logFile 'D:/ProjectsTracking/wiseui-hl2/artifacts/validation/20260907/unity-export.log'
```

If the first run performs only script compilation and domain reload, run the same command once more. Require a successful BuildReport.

- [ ] **Step 3: Build ARM64 MSIX**

Use Visual Studio 2022 Enterprise MSBuild from `C:/Program Files/Microsoft Visual Studio/2022/Enterprise/MSBuild/Current/Bin/MSBuild.exe` with Release, ARM64, SideloadOnly, no bundle, and signing enabled. Preserve the resulting package under a new dated release directory.

- [ ] **Step 4: Start and probe all services**

Start SAM3, FoundationPose, and init_server through the new launchers. Require port 5556 open, `GET http://127.0.0.1:8000/health` successful and initialized, and `GET http://127.0.0.1:8002/health` successful.

- [ ] **Step 5: Run HoloLens initialization**

Install the new MSIX through Device Portal, launch the app, verify PV/depth capture, submit `/init_box`, require a 16-value pose, require `srt3d_init` success, and observe ongoing RGB tracking. Save logs and non-sensitive diagnostic summaries locally.

- [ ] **Step 6: Confirm compatibility paths and clean Git state**

Verify old junction paths resolve, both repositories have clean status after runtime, and generated files remain ignored.

---

### Task 7: Remove reproducible bulk and finalize local cleanup

**Files:**
- Remove after verification: old `Build_612`, Unity `Library`, `Temp`, and `Logs` through their resolved new paths.
- Update: migration manifest cleanup section.

**Interfaces:**
- Consumes: successful Task 6 evidence and preserved releases.
- Produces: reclaimed disk space with rollback assets retained.

- [ ] **Step 1: Reconfirm deletion targets**

Resolve each target and assert it is inside `D:/ProjectsTracking/wiseui-hl2/client/wiseui-hl2-client`. Record size before deletion. Never delete through an unresolved or unexpected junction.

- [ ] **Step 2: Remove generated directories with native PowerShell**

Use `Remove-Item -LiteralPath ... -Recurse` only on the four verified targets. Do not construct a cross-shell deletion command.

- [ ] **Step 3: Verify rebuildability and preserved release**

Confirm Unity opens or recreates required cache on the next export and the preserved MSIX hashes still match. Record reclaimed bytes.

- [ ] **Step 4: Retain archives through publication verification**

Keep the legacy client backup, server local archive, migration bundles, calibration, and dependency experiments until both GitHub repositories and end-to-end validation remain stable.

---

### Task 8: Rename, create, review, and publish GitHub repositories

**Files:**
- Modify remotely: GitHub repository names and visibility.
- Modify locally: Git remotes after successful remote operations.

**Interfaces:**
- Consumes: clean verified client and server repositories.
- Produces: canonical GitHub client and private server repositories.

- [ ] **Step 1: Scan staged publication content**

Search tracked content for `192.168.0.16`, `192.168.0.7`, home-directory paths, tokens, passwords, private-key markers, `.pfx`, calibration, captures, checkpoints, and files over 50MB. Review every match. Permit documented example addresses only when explicitly intended.

- [ ] **Step 2: Rename the existing client repository in GitHub**

Rename `youngbin0212/wiseui-hl2-object-tracking` to `youngbin0212/wiseui-hl2-client` using the authenticated GitHub web UI. Verify the old URL redirects and the new repository retains branches and history.

- [ ] **Step 3: Update and verify client origin**

Set origin to `https://github.com/youngbin0212/wiseui-hl2-client.git`, fetch, verify ancestry contains `228adbb` and `7c0f4af`, then push `main`.

- [ ] **Step 4: Create the private server repository**

Create `youngbin0212/wiseui-hl2-server` with private visibility and no generated README, license, or `.gitignore`. Add it as `origin` to the prepared server repository.

- [ ] **Step 5: Push and independently inspect server content**

Push `main`, inspect the GitHub tree, confirm private visibility, and repeat the secret/large-file scan against `origin/main`. Verify dependency source is referenced by URL and pin rather than vendored.

- [ ] **Step 6: Record final publication state**

Add repository URLs, commit IDs, visibility, push timestamps, validation results, compatibility junctions, archive locations, and rollback instructions to the migration manifest.

- [ ] **Step 7: Final verification**

Require clean Git status in client and server, matching local/remote HEADs, accessible client README, accessible private server README, valid preserved artifacts, and all Task 6 runtime checks. Stop without removing archives if any condition fails.
