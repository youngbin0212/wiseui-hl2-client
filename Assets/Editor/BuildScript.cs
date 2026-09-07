// UWP(HoloLens 2) 빌드 export 자동화.
// - 메뉴: Build > Export UWP (ARM64)  — 에디터에서 클릭
// - 배치: Unity.exe -batchmode -quit -projectPath <proj> -executeMethod BuildScript.BuildUWP -logFile <log>
//   (배치는 에디터를 닫은 상태에서만 — Library 잠금 때문)
// Unity 는 VS 솔루션(Build/)만 export 한다. Debug/Release/Master 구성은 이후 MSBuild 단계에서 선택.

using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

public class BuildScript
{
    const string OutDir = "Build";
    const string RuntimeConfigPath = "Assets/StreamingAssets/wiseui.runtime.json";

    [MenuItem("Build/Export UWP (ARM64)")]
    public static void BuildUWP()
    {
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        string configPath = Path.GetFullPath(RuntimeConfigPath);
        string metaPath = configPath + ".meta";
        bool hadConfig = File.Exists(configPath);
        bool hadMeta = File.Exists(metaPath);
        byte[] previousConfig = hadConfig ? File.ReadAllBytes(configPath) : null;
        byte[] previousMeta = hadMeta ? File.ReadAllBytes(metaPath) : null;
        string initServerUrl = WiseUiRuntimeConfig.ResolveBuildUrl(
            Environment.GetEnvironmentVariable("WISEUI_INIT_SERVER_URL"),
            hadConfig ? Encoding.UTF8.GetString(previousConfig) : null);

        BuildReport report;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath));
            File.WriteAllText(configPath, WiseUiRuntimeConfig.BuildJson(initServerUrl), new UTF8Encoding(false));
            AssetDatabase.Refresh();

            // WSA/HoloLens 설정 명시 (배치 빌드에서 구성 어긋남 방지).
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WSA, BuildTarget.WSAPlayer);
            EditorUserBuildSettings.wsaArchitecture = "ARM64";
            EditorUserBuildSettings.wsaSubtarget = WSASubtarget.HoloLens;
            EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;

            var opts = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = OutDir,
                target = BuildTarget.WSAPlayer,
                targetGroup = BuildTargetGroup.WSA,
                options = BuildOptions.None,   // export 만 (appx 구성은 MSBuild 에서)
            };

            Debug.Log($"[BuildScript] export UWP → {OutDir}  scenes={scenes.Length} init={initServerUrl}");
            report = BuildPipeline.BuildPlayer(opts);
        }
        finally
        {
            if (hadConfig) File.WriteAllBytes(configPath, previousConfig);
            else if (File.Exists(configPath)) File.Delete(configPath);

            if (hadMeta) File.WriteAllBytes(metaPath, previousMeta);
            else if (File.Exists(metaPath)) File.Delete(metaPath);
            AssetDatabase.Refresh();
        }

        BuildSummary s = report.summary;
        Debug.Log($"[BuildScript] result={s.result} errors={s.totalErrors} time={s.totalTime} out={s.outputPath}");

        if (Application.isBatchMode)
            EditorApplication.Exit(s.result == BuildResult.Succeeded ? 0 : 1);
    }
}
