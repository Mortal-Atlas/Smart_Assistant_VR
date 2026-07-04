using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Xml;

public class NamespacePatcher : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        // This targets the specific OVRPlugin and InteractionSdk files that conflict
        string[] filesToPatch = Directory.GetFiles("Library/PackageCache", "AndroidManifest.xml", SearchOption.AllDirectories);

        foreach (string file in filesToPatch)
        {
            if (file.Contains("OVRPlugin") || file.Contains("InteractionSdk"))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                XmlAttribute packageAttr = doc.DocumentElement.Attributes["package"];
                if (packageAttr != null && packageAttr.Value == "com.oculus.Integration")
                {
                    // Rename the namespace to be unique so Gradle stops crying
                    packageAttr.Value = file.Contains("OVRPlugin") ? "com.oculus.Integration.ovr" : "com.oculus.Integration.interaction";
                    doc.Save(file);
                }
            }
        }
    }
}