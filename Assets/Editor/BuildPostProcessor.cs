
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.Xml;

public class BuildPostProcessor
{
    public static void AddCapability(XmlDocument xml, string name, string capability, string namespaceURI, bool append)
    {
        XmlNode capabilities = xml.DocumentElement.GetElementsByTagName("Capabilities")[0];
        foreach (XmlNode childnode in capabilities.ChildNodes) { if ((childnode.Name == name) && (childnode.Attributes["Name"].Value == capability)) { return; } }
        XmlElement element = xml.CreateElement(name, namespaceURI);
        element.SetAttribute("Name", capability);
        if (!append) { capabilities.PrependChild(element); } else { capabilities.AppendChild(element); }
    }

    public static void AddNamespace(XmlDocument xml, string name, string URI)
    {
        xml.DocumentElement.SetAttribute(name, URI);
    }

    // 패키지 버전을 빌드 시각에서 단조 증가하게 만든다.
    //   Version="1.0.<build>.<revision>"
    //     build    = 2020-01-01 이후 경과 일수      (오늘 ≈ 2400, 65535 한도는 2199년경)
    //     revision = 자정 이후 경과 초 / 2          (0~43199, 2초 해상도)
    // 두 성분 다 UWP 한도(65535) 안이고, 날짜가 바뀌면 build 가 올라가므로 revision 리셋과 무관하게
    // 전체 순서는 단조다.
    //
    // 왜 필요한가: 버전이 1.0.0.0 으로 고정이면 내용이 다른 같은 버전이라 배포가 막힌다
    //   ("same identity as an already-installed package but the contents are different").
    //   그래서 매번 uninstall+install 해야 했고, 그 과정에서 persistentDataPath 가 지워져
    //   모델(.obj + 16MB .meta)을 매번 다시 복사했다. 버전이 오르면 `WinAppDeployCmd update`
    //   한 번으로 끝나고 앱 데이터도 보존된다.
    static void BumpPackageVersion(XmlDocument xml)
    {
        var identity = xml.DocumentElement.GetElementsByTagName("Identity");
        if (identity.Count == 0) { Debug.LogWarning("[BuildPostProcessor] Identity 없음 — 버전 유지"); return; }

        var now = System.DateTime.Now;
        int build = (int)(now.Date - new System.DateTime(2020, 1, 1)).TotalDays;
        int revision = (int)(now.TimeOfDay.TotalSeconds / 2.0);
        string version = string.Format("1.0.{0}.{1}", build, revision);

        string old = identity[0].Attributes["Version"] != null
                   ? identity[0].Attributes["Version"].Value : "(none)";
        ((XmlElement)identity[0]).SetAttribute("Version", version);
        Debug.Log(string.Format("[BuildPostProcessor] package version {0} -> {1}", old, version));
    }

    [PostProcessBuildAttribute(1)]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        string project_name = System.IO.Path.GetFileNameWithoutExtension(System.IO.Directory.GetFiles(pathToBuiltProject, "*.sln")[0]);
        string appxmanifest_fname = pathToBuiltProject + "/" + project_name + "/Package.appxmanifest";
        string rescapURI = "http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities";
        string devcapURI = "http://schemas.microsoft.com/appx/manifest/foundation/windows10";
        XmlDocument xml = new XmlDocument();
        xml.Load(appxmanifest_fname);
        AddNamespace(xml, "xmlns:rescap", rescapURI);
        AddCapability(xml, "rescap:Capability", "perceptionSensorsExperimental", rescapURI, false);
        AddCapability(xml, "DeviceCapability", "backgroundSpatialPerception", devcapURI, true);
        // 아이트래킹. ProjectSettings 의 platformCapabilities.GazeInput 은 Unity 가 값은 유지하면서도
        // manifest 로는 안 내보낸다(2022.3.62f3 확인) → 여기서 직접 주입해야 함.
        // 이게 없으면 동의 팝업이 안 뜨고 EyeGazeProvider 가 조용히 unsupported 로 떨어진다.
        AddCapability(xml, "DeviceCapability", "gazeInput", devcapURI, true);
        BumpPackageVersion(xml);
        xml.Save(appxmanifest_fname);
    }
}
