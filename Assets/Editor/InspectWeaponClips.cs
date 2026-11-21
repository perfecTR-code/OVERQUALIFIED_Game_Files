using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class InspectWeaponClips
{
    static InspectWeaponClips()
    {
        EditorApplication.delayCall += RunInspection;
    }

    [MenuItem("Tools/Inspect Weapon Clips")]
    public static void RunInspection()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== WEAPON CLIPS INSPECTION ===");

        string[] fbxs = new string[]
        {
            "Assets/Game/Animations/Double_Hand_Pistol_Idle.fbx",
            "Assets/Game/Animations/Double_Hand_Shotgun_Idle.fbx",
            "Assets/Game/Animations/Double_Hand_Rifle_Idle.fbx"
        };

        foreach (var path in fbxs)
        {
            sb.AppendLine($"\n--- Inspecting {path} ---");
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            AnimationClip clip = null;
            foreach (var a in assets)
            {
                if (a is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                {
                    clip = ac;
                    break;
                }
            }

            if (clip == null)
            {
                sb.AppendLine("  No AnimationClip found!");
                continue;
            }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            sb.AppendLine($"  Total bindings: {bindings.Length}, duration: {clip.length}s");

            int posCount = 0, rotCount = 0, scaleCount = 0, otherCount = 0;
            foreach (var b in bindings)
            {
                if (b.propertyName.Contains("Position")) posCount++;
                else if (b.propertyName.Contains("Rotation")) rotCount++;
                else if (b.propertyName.Contains("Scale")) scaleCount++;
                else otherCount++;
            }
            sb.AppendLine($"  Pos curves: {posCount}, Rot curves: {rotCount}, Scale curves: {scaleCount}, Other: {otherCount}");

            sb.AppendLine("  Sample bindings (first 15):");
            for (int i = 0; i < Math.Min(15, bindings.Length); i++)
            {
                var b = bindings[i];
                var curve = AnimationUtility.GetEditorCurve(clip, b);
                float val0 = curve != null && curve.keys.Length > 0 ? curve.keys[0].value : 0f;
                sb.AppendLine($"    [{i}] Path: '{b.path}' | Prop: '{b.propertyName}' | Type: {b.type.Name} | Key0: {val0}");
            }

            // Check specific key bones: shoulder, upperarm, lowerarm, hand
            string[] checkBones = new string[] { "Armature", "pose_controller", "body", "shoulder_L.R", "upperarm_L.R", "lowerarm_L.R", "hand_L.R", "shoulder_L.L", "upperarm_L.L", "lowerarm_L.L", "hand_L.L" };
            sb.AppendLine("  Specific bone rotations at time 0:");
            foreach (var bone in checkBones)
            {
                foreach (var b in bindings)
                {
                    if (b.path.EndsWith(bone) && b.propertyName.StartsWith("m_LocalRotation.x"))
                    {
                        var cx = AnimationUtility.GetEditorCurve(clip, b);
                        EditorCurveBinding by = b; by.propertyName = "m_LocalRotation.y";
                        EditorCurveBinding bz = b; bz.propertyName = "m_LocalRotation.z";
                        EditorCurveBinding bw = b; bw.propertyName = "m_LocalRotation.w";
                        var cy = AnimationUtility.GetEditorCurve(clip, by);
                        var cz = AnimationUtility.GetEditorCurve(clip, bz);
                        var cw = AnimationUtility.GetEditorCurve(clip, bw);

                        Quaternion q = new Quaternion(
                            cx != null ? cx.keys[0].value : 0,
                            cy != null ? cy.keys[0].value : 0,
                            cz != null ? cz.keys[0].value : 0,
                            cw != null ? cw.keys[0].value : 1
                        );
                        Vector3 euler = q.eulerAngles;
                        sb.AppendLine($"    Bone '{bone}' path '{b.path}': rot=({q.x:F3}, {q.y:F3}, {q.z:F3}, {q.w:F3}) euler=({euler.x:F1}, {euler.y:F1}, {euler.z:F1})");
                    }
                    if (b.path.EndsWith(bone) && b.propertyName.StartsWith("m_LocalPosition.x"))
                    {
                        var cx = AnimationUtility.GetEditorCurve(clip, b);
                        EditorCurveBinding by = b; by.propertyName = "m_LocalPosition.y";
                        EditorCurveBinding bz = b; bz.propertyName = "m_LocalPosition.z";
                        var cy = AnimationUtility.GetEditorCurve(clip, by);
                        var cz = AnimationUtility.GetEditorCurve(clip, bz);
                        Vector3 pos = new Vector3(
                            cx != null ? cx.keys[0].value : 0,
                            cy != null ? cy.keys[0].value : 0,
                            cz != null ? cz.keys[0].value : 0
                        );
                        sb.AppendLine($"    Bone '{bone}' POS: ({pos.x:F3}, {pos.y:F3}, {pos.z:F3})");
                    }
                }
            }
        }

        // Also check HandWithGloves.fbx hierarchy!
        sb.AppendLine("\n--- Inspecting HandWithGloves.fbx hierarchy ---");
        var gloveAssets = AssetDatabase.LoadAllAssetsAtPath("Assets/Game/Objects,Weapons/HandWithGloves.fbx");
        foreach (var a in gloveAssets)
        {
            if (a is GameObject go)
            {
                sb.AppendLine($"  Root GameObject: {go.name}");
                void PrintHierarchy(Transform t, string indent)
                {
                    sb.AppendLine($"{indent}- {t.name} (localPos: {t.localPosition}, localRot: {t.localEulerAngles})");
                    for (int i = 0; i < t.childCount; i++)
                    {
                        // Print only first 3 children if there are many to save space, but show key bones
                        var child = t.GetChild(i);
                        if (child.name.Contains("thumb") || child.name.Contains("index") || child.name.Contains("middle") || child.name.Contains("ring") || child.name.Contains("pink"))
                        {
                            if (i == 0) sb.AppendLine($"{indent}  [fingers...]");
                            continue;
                        }
                        PrintHierarchy(child, indent + "  ");
                    }
                }
                PrintHierarchy(go.transform, "  ");
                break;
            }
        }

        File.WriteAllText("inspect_clips_result.txt", sb.ToString());
        Debug.Log("[InspectWeaponClips] Done. Output written to inspect_clips_result.txt");
    }
}
