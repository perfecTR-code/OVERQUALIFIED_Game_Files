using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class RebuildFireAnimation
{
    public static void Run()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== RebuildFireAnimation Started ===");

        string fireFbxPath = "Assets/Game/Animations/Double_Hand_Pistol_Fire.fbx";
        var fireAssets = AssetDatabase.LoadAllAssetsAtPath(fireFbxPath);
        AnimationClip fireClip = null;
        foreach (var a in fireAssets)
        {
            if (a is AnimationClip ac && !ac.name.StartsWith("__preview__"))
            {
                fireClip = ac;
                break;
            }
        }

        string idleFbxPath = "Assets/Game/Animations/Double_Hand_Pistol_Idle.fbx";
        var idleAssets = AssetDatabase.LoadAllAssetsAtPath(idleFbxPath);
        AnimationClip idleClip = null;
        foreach (var a in idleAssets)
        {
            if (a is AnimationClip ac && !ac.name.StartsWith("__preview__"))
            {
                idleClip = ac;
                break;
            }
        }

        if (fireClip == null)
        {
            sb.AppendLine("ERROR: fireClip is null!");
            File.WriteAllText("rebuild_fire_result.txt", sb.ToString());
            return;
        }

        AnimationClip fixedClip = new AnimationClip();
        fixedClip.name = "Pistol_Fire_Armature";
        fixedClip.frameRate = fireClip.frameRate;
        fixedClip.legacy = false;

        var fireBindings = AnimationUtility.GetCurveBindings(fireClip);
        sb.AppendLine($"Total source bindings: {fireBindings.Length}");
        int copiedCount = 0;

        foreach (var cb in fireBindings)
        {
            // Only rotation curves!
            if (cb.propertyName.Contains("Position") || cb.propertyName.Contains("Scale"))
                continue;

            // No root Armature, camera, or IK dummy nodes
            if (string.IsNullOrEmpty(cb.path) || cb.path == "Armature" || cb.path.Contains("camera") || cb.path.Contains("IK"))
                continue;

            // Make sure it has Armature/ prefix
            string newPath = cb.path.StartsWith("Armature/") ? cb.path : "Armature/" + cb.path;

            // If it's on the Right arm (which was un-animated and had a distorted static pose in Fire.fbx),
            // let's check if idleClip has this curve so we preserve the proper holding stance
            AnimationCurve curve = null;
            if (newPath.Contains(".R") && idleClip != null)
            {
                // Find matching binding in idle
                foreach (var ib in AnimationUtility.GetCurveBindings(idleClip))
                {
                    if (ib.path == newPath && ib.propertyName == cb.propertyName)
                    {
                        var idleCurve = AnimationUtility.GetEditorCurve(idleClip, ib);
                        if (idleCurve != null && idleCurve.length > 0)
                        {
                            // Create a 2-key curve matching the idle first key over fire duration
                            float firstVal = idleCurve.keys[0].value;
                            curve = new AnimationCurve(
                                new Keyframe(0f, firstVal),
                                new Keyframe(fireClip.length, firstVal)
                            );
                        }
                        break;
                    }
                }
            }

            if (curve == null)
            {
                curve = AnimationUtility.GetEditorCurve(fireClip, cb);
            }

            if (curve != null)
            {
                EditorCurveBinding newBinding = cb;
                newBinding.path = newPath;
                AnimationUtility.SetEditorCurve(fixedClip, newBinding, curve);
                copiedCount++;
            }
        }

        sb.AppendLine($"Copied {copiedCount} rotation curves to fixedClip.");

        string outPath = "Assets/Game/Animations/Pistol_Fire_Armature.anim";
        AssetDatabase.CreateAsset(fixedClip, outPath);
        AssetDatabase.SaveAssets();
        sb.AppendLine($"Asset created and saved at: {outPath}");

        File.WriteAllText("rebuild_fire_result.txt", sb.ToString());
        Debug.Log(sb.ToString());
    }
}
