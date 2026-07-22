using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FilmInspiredGames.Burning.Editor
{
    public static class BurningWebBuild
    {
        private const string ScenePath = "Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity";
        private const string C06ScenePath = "Assets/Game/Burning/C06/Scenes/Burning_C06_C07_Playable.unity";
        private const string C08ScenePath = "Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity";
        private const string C13ScenePath = "Assets/Game/Burning/C13/Scenes/Burning_C13_Playable.unity";
        private const string C14ScenePath = "Assets/Game/Burning/C14/Scenes/Burning_C14_Playable.unity";
        private const string C15ScenePath = "Assets/Game/Burning/C15/Scenes/Burning_C15_Playable.unity";
        private const string C16ScenePath = "Assets/Game/Burning/C16/Scenes/Burning_C16_C18_Playable.unity";
        private const string C19ScenePath = "Assets/Game/Burning/C19/Scenes/Burning_C19_Playable.unity";
        private const string OutputPath = "Builds/Web";

        [MenuItem("Tools/Burning/Build Web for GitHub Pages")]
        public static void Build()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogWarning("재생을 멈춘 뒤 웹 빌드를 생성하세요.");
                return;
            }

            string[] scenes =
            {
                ScenePath, C06ScenePath, C08ScenePath, C13ScenePath,
                C14ScenePath, C15ScenePath, C16ScenePath, C19ScenePath
            };

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            foreach (string scene in scenes)
            {
                AssetDatabase.ImportAsset(
                    scene,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }

            if (Directory.Exists(OutputPath))
            {
                Directory.Delete(OutputPath, true);
            }

            Directory.CreateDirectory(OutputPath);

            BuildPlayerOptions options = new()
            {
                scenes = scenes,
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.CleanBuildCache
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException($"웹 빌드 실패: {report.summary.result}");
            }

            string indexPath = Path.Combine(OutputPath, "index.html");
            string buildVersion = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            File.WriteAllText(indexPath, File.ReadAllText(indexPath).Replace("BUILD_VERSION_TOKEN", buildVersion));
            File.WriteAllText(Path.Combine(OutputPath, ".nojekyll"), string.Empty);
            Debug.Log($"웹 빌드 완료: {OutputPath} ({report.summary.totalSize / 1024f / 1024f:F1} MB)");
        }
    }
}
