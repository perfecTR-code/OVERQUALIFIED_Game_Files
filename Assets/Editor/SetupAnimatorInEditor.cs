using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Eski Mecanim yardımcısı (Artık WeaponAnimationSystem Playables API kullanıldığı için otomatik çalıştırılması kapatıldı)
public static class SetupAnimatorInEditor
{
    // static SetupAnimatorInEditor()
    // {
    //     EditorApplication.delayCall += ExecuteSetup;
    // }

    [MenuItem("Tools/Fix Player Animator and Duplicate Weapons")]
    public static void ExecuteSetup()
    {
        string controllerPath = "Assets/Game/Animations/Player_Animator.controller";
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[SetupAnimatorInEditor] Could not load controller at {controllerPath}");
            return;
        }

        var players = Object.FindObjectsByType<Player1>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        bool anyChanged = false;

        foreach (var player in players)
        {
            if (player.Player_Arms_Animator == null)
            {
                player.Player_Arms_Animator = player.GetComponentInChildren<Animator>(true);
            }

            if (player.Player_Arms_Animator != null)
            {
                if (player.Player_Arms_Animator.runtimeAnimatorController != controller)
                {
                    player.Player_Arms_Animator.runtimeAnimatorController = controller;
                    EditorUtility.SetDirty(player.Player_Arms_Animator);
                    anyChanged = true;
                    Debug.Log($"[SetupAnimatorInEditor] Assigned Player_Animator.controller to {player.Player_Arms_Animator.name}");
                }
            }

            // Deactivate any rogue or extra ShotGun_F
            var transforms = player.GetComponentsInChildren<Transform>(true);
            foreach (var t in transforms)
            {
                if (t.name == "ShotGun_F" || t.name == "Shotgun_F")
                {
                    if (t.gameObject.activeSelf)
                    {
                        t.gameObject.SetActive(false);
                        EditorUtility.SetDirty(t.gameObject);
                        anyChanged = true;
                        Debug.Log("[SetupAnimatorInEditor] Deactivated rogue ShotGun_F in scene.");
                    }
                }
            }

            EditorUtility.SetDirty(player);
        }

        if (anyChanged)
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
            Debug.Log("[SetupAnimatorInEditor] Scene saved with updated Animator and cleaned weapons!");
        }
    }
}
