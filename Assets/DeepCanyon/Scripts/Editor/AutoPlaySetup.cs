using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class AutoPlaySetup
{
    static AutoPlaySetup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        EditorApplication.delayCall += TryAutoBuild;
    }

    static void TryAutoBuild()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) return;
        if (GameObject.Find("Bathyscaphe") != null) return;
        BuildAndSave();
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode) return;
        BuildAndSave();
    }

    static void BuildAndSave()
    {
        Debug.Log("[DeepCanyon] Auto-building scene...");
        var method = typeof(DeepCanyonSceneSetup).GetMethod("BuildScene",
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (method != null)
            method.Invoke(null, null);
        else
            Debug.LogError("[DeepCanyon] BuildScene method not found!");
    }
}
