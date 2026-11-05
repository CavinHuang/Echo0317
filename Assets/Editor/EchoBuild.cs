#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Echo0317.Editor
{
    [Serializable] public class EchoReleaseRequirements
    {
        public int schema;
        public string[] requiredShots;
        public string notes;
    }
    [Serializable] public class EchoImportReport
    {
        public string unityVersion, projectName, runtimeAssembly, checkedAtUtc;
        public int nodes, endings, rules, mediaTasks;
        public bool nativeEditorLoaded;
    }
    public static class EchoBuild
    {
        const string ScenePath = "Assets/Scenes/EchoEntry.unity";
        [InitializeOnLoadMethod]
        static void SetupOnFirstOpen()
        {
            // A brand new checkout may not have imported scene metadata yet.
            EditorApplication.delayCall += () => {
                if (!Application.isPlaying &&
                    (!File.Exists(Path.Combine(Application.dataPath, "Scenes", "EchoEntry.unity")) ||
                     Array.Find(EditorBuildSettings.scenes, entry => entry.path == ScenePath && entry.enabled) == null))
                    EnsureScene();
            };
        }
        [MenuItem("03:17 / 初始化入口场景")]
        public static void EnsureScene()
        {
            if (!File.Exists(Path.Combine(Application.dataPath, "Scenes", "EchoEntry.unity")))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "Scenes"));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                // The C# runtime bootstraps its own UI and video stage after scene load.
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.Refresh();
            }
            var editorScene = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = new[] { editorScene };
            PlayerSettings.companyName = "Echo0317 Studio";
            PlayerSettings.productName = "03-17 Echo";
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            Debug.Log("[Echo0317] Unity 入口场景就绪: " + ScenePath);
        }
        [MenuItem("03:17 / 原生导入及编译验证")]
        public static void VerifyImport()
        {
            if (!Application.unityVersion.StartsWith("6000.3.", StringComparison.Ordinal))
                throw new InvalidOperationException("此项目需要 Unity 6.3 LTS (6000.3.x)，实际：" + Application.unityVersion);
            EnsureScene();
#if !ENABLE_LEGACY_INPUT_MANAGER
            throw new InvalidOperationException(
                "Unity 输入配置不兼容：本版本使用 UnityEngine.Input 与 uGUI StandaloneInputModule。" +
                "请在 Project Settings > Player > Active Input Handling 设置为 Both 或 Input Manager (Old)，" +
                "重启 Unity 后重新运行原生导入验证。");
#endif
            VerifyContent(false);
            var catalog = Echo0317.Core.GameCatalog.LoadFromStreamingAssets();
            var report = new EchoImportReport {
                unityVersion = Application.unityVersion,
                projectName = PlayerSettings.productName,
                runtimeAssembly = typeof(Echo0317.Core.EchoNarrative).Assembly.GetName().Name,
                checkedAtUtc = DateTime.UtcNow.ToString("o"),
                nodes = catalog.Nodes.Count,
                endings = Array.FindAll(catalog.Story.nodes, node => node.ending).Length,
                rules = catalog.Rules.Count,
                mediaTasks = catalog.ShotTasks.Count,
                nativeEditorLoaded = true
            };
            var folder = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Builds");
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "unity-import-verification.json"), JsonUtility.ToJson(report, true));
            Debug.Log("[Echo0317] Unity Editor import verified / " + report.unityVersion +
                " / assembly=" + report.runtimeAssembly + " / 55 nodes");
        }
        [MenuItem("03:17 / 发行资源预检查")]
        public static void VerifyContent()
        {
            VerifyContent(IsReleaseProfile());
        }
        static bool IsReleaseProfile() =>
            string.Equals(Environment.GetEnvironmentVariable("ECHO_RELEASE_PROFILE"), "release", StringComparison.OrdinalIgnoreCase);
        static void VerifyContent(bool releaseProfile)
        {
            var catalog = Echo0317.Core.GameCatalog.LoadFromStreamingAssets();
            if (catalog.Nodes.Count != 55 || catalog.Rules.Count != 5 ||
                catalog.ShotTasks.Count != 43 || catalog.Story.chapters.Length != 5)
                throw new InvalidDataException("剧情导出数量异常，发行构建已阻止");
            var mediaRoot = Path.Combine(Application.streamingAssetsPath, "Media");
            var previews = Directory.GetFiles(Path.Combine(mediaRoot, "Previs"), "*.mp4");
            if (!releaseProfile && previews.Length != 8)
                throw new InvalidDataException("内部测试版预演影片数量不正确，预期 8 条");
            foreach (var shot in catalog.ShotTasks.Values)
            {
                if (shot.status != "approved_final") continue;
                if (string.IsNullOrWhiteSpace(shot.path) ||
                    Path.GetFileName(shot.path) != shot.path ||
                    !File.Exists(Path.Combine(mediaRoot, "Final", shot.path)))
                    throw new InvalidDataException("审核通过的镜头实际文件不存在或路径非法: " + shot.id);
            }
            if (releaseProfile)
            {
                var requirementsFile = Path.Combine(Application.streamingAssetsPath, "Data", "release-required-shots.json");
                var requirements = JsonUtility.FromJson<EchoReleaseRequirements>(File.ReadAllText(requirementsFile));
                if (requirements == null || requirements.schema != 1 || requirements.requiredShots == null || requirements.requiredShots.Length == 0)
                    throw new InvalidDataException("缺少有效的正式发行镜头清单");
                foreach (var shotId in requirements.requiredShots)
                {
                    if (!catalog.ShotTasks.TryGetValue(shotId, out var shot) || shot.status != "approved_final")
                        throw new InvalidDataException("正式发行禁止使用占位视频，缺少获批镜头: " + shotId);
                    if (string.IsNullOrWhiteSpace(shot.path) || Path.GetFileName(shot.path) != shot.path ||
                        !File.Exists(Path.Combine(mediaRoot, "Final", shot.path)))
                        throw new InvalidDataException("正式发行镜头文件缺失或路径非法: " + shotId);
                }
            }
            Debug.Log("[Echo0317] 资源预检查通过：" + (releaseProfile ? "RELEASE" : "INTERNAL") + ", 55 nodes, 5 deductions");
        }
        // GameCI runs inside a licensed Unity container on a GitHub-hosted runner.
        // Container environment variables from the workflow are not guaranteed to
        // reach Unity, so set an absolute output rooted in the real project.
        // The output folder is the one GameCI publishes as an Actions artifact.
        public static void BuildWindowsHosted()
        {
            string root = Directory.GetParent(Application.dataPath).FullName;
            string output = Path.Combine(root, "build", "StandaloneWindows64", "Echo0317.exe");
            Environment.SetEnvironmentVariable("ECHO_BUILD_PATH", output);
            Environment.SetEnvironmentVariable("ECHO_RELEASE_PROFILE", "internal");
            BuildWindows();
        }

        [MenuItem("03:17 / Windows x64 构建")]
        public static void BuildWindows()
        {
            EnsureScene();
            bool releaseProfile = IsReleaseProfile();
#if !ENABLE_LEGACY_INPUT_MANAGER
            throw new InvalidOperationException("Windows Player 必须启用 Legacy Input Manager 后才能构建。请检查 Active Input Handling 并重启编辑器。");
#endif
            VerifyContent(releaseProfile);
            string path = Environment.GetEnvironmentVariable("ECHO_BUILD_PATH");
            if (string.IsNullOrEmpty(path)) path = "Builds/Windows/Echo0317.exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Builds/Windows");
            var options = new BuildPlayerOptions {
                scenes = new[] { ScenePath },
                locationPathName = path,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new Exception("Windows 构建失败: " + report.summary.result + "; 错误数 " + report.summary.totalErrors);
            Debug.Log("[Echo0317] 构建成功: " + path + " / " + report.summary.totalSize + " bytes / " +
                (releaseProfile ? "RELEASE" : "INTERNAL ALPHA"));
        }
    }
}
#endif
