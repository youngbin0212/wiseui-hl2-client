
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
        xml.Save(appxmanifest_fname);
    }
}
