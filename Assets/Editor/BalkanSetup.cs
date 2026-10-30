using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BalkanMines.EditorTools
{
    /// <summary>Creates the playable scene and builds the Windows player.</summary>
    public static class BalkanSetup
    {
        public const string ScenePath = "Assets/Scenes/BalkanMinesweeper.unity";

        [MenuItem("Balkan/Setup Game Scene")]
        public static void Setup()
        {
            Directory.CreateDirectory("Assets/Scenes");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Balkanske Mine 3D");
            game.AddComponent<GameBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            PlayerSettings.companyName = "Balkan Vibe";
            PlayerSettings.productName = "Balkanske Mine 3D";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.usePlayerLog = true;
            PlayerSettings.colorSpace = ColorSpace.Linear;

            QualitySettings.vSyncCount = 1;
            QualitySettings.antiAliasing = 4;

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Balkan Mines: scene created at " + ScenePath);
        }

        [MenuItem("Balkan/Build Windows Player")]
        public static void BuildWindows()
        {
            Setup();
            Directory.CreateDirectory("Build/BalkanskeMine3D");
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "Build/BalkanskeMine3D/BalkanskeMine3D.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            Debug.Log("Balkan Mines build: " + report.summary.result + "  " + report.summary.totalSize + " bytes");
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                throw new Exception("Build failed: " + report.summary.result);
        }
    }
}
