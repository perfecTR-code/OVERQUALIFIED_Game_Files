using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class CheckBlendAssets
{
    static CheckBlendAssets()
    {
        EditorApplication.delayCall += Check;
    }

    [MenuItem("Tools/Check Blend Assets")]
    public static void Check()
    {
        StringBuilder sb = new StringBuilder();
        string[] blendPaths = new string[]
        {
            "Assets/Game/Animations/Shotgun/Double_Hand/Double_Hand_Shotgun_Preparation(Before).blend",
            "Assets/Game/Animations/Shotgun/Double_Hand/Double_Hand_Shotgun_Reload.blend",
            "Assets/Game/Animations/Shotgun/Double_Hand/Double_Hand_Shotgun_Preparation(After).blend",
            "Assets/Game/Animations/Shotgun/Double_Hand/Double_Hand_Shotgun_Hold.blend",
            "Assets/Game/Animations/Shotgun/Double_Hand/Double_Hand_Shotgun_Fire.blend",
            "Assets/Game/Animations/Shotgun/Double_Hand/Double_Hand_Shotgun_Equip.blend"
        };

        foreach (var path in blendPaths)
        {
            sb.AppendLine($"Checking: {path}");
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            sb.AppendLine($"  Total sub-assets: {assets.Length}");
            foreach (var a in assets)
            {
                if (a != null)
                {
                    sb.AppendLine($"    - {a.GetType().Name}: '{a.name}'");
                    if (a is AnimationClip clip)
                    {
                        sb.AppendLine($"      [CLIP] length: {clip.length}s, frameRate: {clip.frameRate}, bindings: {AnimationUtility.GetCurveBindings(clip).Length}");
                    }
                }
            }
        }

        File.WriteAllText("check_blend_result.txt", sb.ToString());
        Debug.Log(sb.ToString());
    }
}
