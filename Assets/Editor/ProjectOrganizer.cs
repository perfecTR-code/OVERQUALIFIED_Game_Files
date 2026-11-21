using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class ProjectOrganizer
{
    static ProjectOrganizer()
    {
        if (!SessionState.GetBool("ProjectOrganizer_Ran", false))
        {
            SessionState.SetBool("ProjectOrganizer_Ran", true);
            Run();
        }
    }

    [MenuItem("Tools/Organize Project Assets")]
    public static void Run()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Project Asset Organization Started ===");

        EnsureFolder("Assets", "_AssetPacks");
        EnsureFolder("Assets/Game", "Terrains");
        EnsureFolder("Assets/Game", "Prefabs");

        string[] thirdPartyPacks = new string[]
        {
            "Assets/Brick Project Studio",
            "Assets/Customized Weapons - Low poly",
            "Assets/Flashlight",
            "Assets/Low Poly Pistol Weapon Pack 1",
            "Assets/Low Poly Pistol Weapon Pack 2",
            "Assets/Low Poly Pistol Weapon Pack 3",
            "Assets/Low Poly ShotGun Weapon Pack 1",
            "Assets/Low Poly ShotGun Weapon Pack 2",
            "Assets/Low Poly Weapons VOL.1",
            "Assets/Polytope Studio",
            "Assets/StarterAssets"
        };

        foreach (var pack in thirdPartyPacks)
        {
            if (AssetDatabase.IsValidFolder(pack))
            {
                string packName = Path.GetFileName(pack);
                string newPath = "Assets/_AssetPacks/" + packName;
                string err = AssetDatabase.MoveAsset(pack, newPath);
                if (string.IsNullOrEmpty(err))
                    sb.AppendLine($"Moved folder {pack} -> {newPath}");
                else
                    sb.AppendLine($"[WARN] Could not move {pack}: {err}");
            }
        }

        string[] rootAssets = Directory.GetFiles("Assets");
        foreach (var file in rootAssets)
        {
            if (file.EndsWith(".meta")) continue;
            string unityPath = file.Replace("\\", "/");
            string fileName = Path.GetFileName(unityPath);

            if (fileName.StartsWith("New Terrain") || fileName.StartsWith("TerrainData_"))
            {
                string target = "Assets/Game/Terrains/" + fileName;
                string err = AssetDatabase.MoveAsset(unityPath, target);
                if (string.IsNullOrEmpty(err)) sb.AppendLine($"Moved terrain {fileName} -> {target}");
            }
            else if (fileName.StartsWith("ShotGun_C") || fileName.StartsWith("Minigun"))
            {
                string target = "Assets/Game/Prefabs/" + fileName;
                string err = AssetDatabase.MoveAsset(unityPath, target);
                if (string.IsNullOrEmpty(err)) sb.AppendLine($"Moved prefab {fileName} -> {target}");
            }
        }

        if (AssetDatabase.IsValidFolder("Assets/Assets"))
        {
            AssetDatabase.DeleteAsset("Assets/Assets");
            sb.AppendLine("Removed empty Assets/Assets folder");
        }

        AssetDatabase.SaveAssets();
        sb.AppendLine("=== Organization Complete! ===");

        File.WriteAllText("organize_result.txt", sb.ToString());
        Debug.Log(sb.ToString());
    }

    private static void EnsureFolder(string parent, string newFolder)
    {
        string full = parent + "/" + newFolder;
        if (!AssetDatabase.IsValidFolder(full))
        {
            AssetDatabase.CreateFolder(parent, newFolder);
        }
    }
}
