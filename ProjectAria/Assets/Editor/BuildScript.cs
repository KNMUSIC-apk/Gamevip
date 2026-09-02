// ============================================================
// BuildScript.cs
// Unity Editor automation. Builds Android APK from command line.
// Usage: Unity -batchmode -quit -projectPath <path> -executeMethod ProjectAria.Editor.BuildScript.BuildAndroid
// ============================================================
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ProjectAria.Editor
{
    public static class BuildScript
    {
        public const string DefaultOutputPath = "Builds/Android/ProjectAria.apk";
        public const string AndroidKeystoreName = "user.keystore";
        public const string KeystorePass = "changeit"; // env-overridable
        public const string KeyAlias = "projectaria";
        public const string KeyPass = "changeit";

        public static void BuildAndroid()
        {
            string outputPath = Environment.GetEnvironmentVariable("ARIA_OUTPUT_PATH") ?? DefaultOutputPath;
            string keystoreName = Environment.GetEnvironmentVariable("ARIA_KEYSTORE_NAME") ?? AndroidKeystoreName;
            string keystorePass = Environment.GetEnvironmentVariable("ARIA_KEYSTORE_PASS") ?? KeystorePass;
            string keyAlias = Environment.GetEnvironmentVariable("ARIA_KEY_ALIAS") ?? KeyAlias;
            string keyPass = Environment.GetEnvironmentVariable("ARIA_KEY_PASS") ?? KeyPass;

            // Player settings
            PlayerSettings.companyName = Environment.GetEnvironmentVariable("ARIA_COMPANY") ?? "Studio Aria";
            PlayerSettings.productName = "Project Aria";
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26; // 8.0 Oreo
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.studio.projectaria");
            PlayerSettings.Android.bundleVersionCode = 1;
            PlayerSettings.bundleVersion = "0.1.0";

            // Keystore (signing)
            string keystorePath = Path.Combine(Application.dataPath, "..", keystoreName);
            if (File.Exists(keystorePath))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = keystorePass;
                PlayerSettings.Android.keyaliasName = keyAlias;
                PlayerSettings.Android.keyaliasPass = keyPass;
                Debug.Log($"[Build] Using keystore: {keystorePath}");
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                Debug.LogWarning($"[Build] No keystore found at {keystorePath}. Building unsigned debug APK.");
            }

            // Graphics
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan });
            PlayerSettings.openGLRequireES31 = true;
            PlayerSettings.openGLRequireES31AEP = false;
            PlayerSettings.openGLRequireES32 = false;

            // Stripping
            PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
            PlayerSettings.stripEngineCode = true;

            // Make sure Bootstrap is scene 0
            var scenes = new[] {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Game.unity",
                "Assets/Scenes/MainMenu.unity"
            };
            var enabledScenes = new System.Collections.Generic.List<string>();
            foreach (var s in scenes) if (File.Exists(s)) enabledScenes.Add(s);
            EditorBuildSettings.scenes = System.Linq.Enumerable.Select(enabledScenes, s => new EditorBuildSettingsScene(s, true)).ToArray();

            // Output dir
            string dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

            // Build
            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes.ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = File.Exists(keystorePath) ? BuildOptions.None : BuildOptions.Development
            };

            Debug.Log($"[Build] Starting Android build → {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] Result: {summary.result}, Size: {summary.totalSize / 1024 / 1024} MB, Time: {summary.totalTime}");
            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
            }
        }

        public static void BuildIOS()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.studio.projectaria");
            PlayerSettings.iOS.targetOSVersionString = "13.0";
            PlayerSettings.iOS.buildNumber = "1";

            var scenes = new[] {
                "Assets/Scenes/Bootstrap.unity",
                "Assets/Scenes/Game.unity",
                "Assets/Scenes/MainMenu.unity"
            };
            var enabledScenes = new System.Collections.Generic.List<string>();
            foreach (var s in scenes) if (File.Exists(s)) enabledScenes.Add(s);

            string outputPath = "Builds/iOS/ProjectAria";
            var options = new BuildPlayerOptions
            {
                scenes = enabledScenes.ToArray(),
                locationPathName = outputPath,
                target = BuildTarget.iOS,
                targetGroup = BuildTargetGroup.iOS
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        public static void GenerateKeystore()
        {
            string keystorePath = Path.Combine(Application.dataPath, "..", AndroidKeystoreName);
            if (File.Exists(keystorePath))
            {
                Debug.Log($"[Build] Keystore already exists at {keystorePath}");
                return;
            }
            // Use JDK keytool — must be on PATH
            var psi = new System.Diagnostics.ProcessStartInfo("keytool", $"-genkey -v -keystore \"{keystorePath}\" -alias {KeyAlias} -keyalg RSA -keysize 2048 -validity 10000 -storepass {KeystorePass} -keypass {KeyPass} -dname \"CN=ProjectAria, OU=Studio, O=StudioAria, L=City, S=State, C=US\"")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            try
            {
                using var p = System.Diagnostics.Process.Start(psi);
                p.WaitForExit();
                Debug.Log($"[Build] Keystore generated: {keystorePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[Build] keytool failed: {e.Message}. Make sure JDK is on PATH.");
            }
        }
    }
}
