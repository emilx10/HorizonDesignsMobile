using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;

public static class AndroidApkBuilder
{
    private const string DefaultOutputPath = @"C:\Users\Daddy\Desktop\HorizonDesignsMobile.apk";

    public static void BuildApk()
    {
        var outputPath = GetOutputPath();
        var outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorUserBuildSettings.buildAppBundle = false;

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/GameScene.unity" },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException($"Android APK build failed: {report.summary.result}");
        }

        UnityEngine.Debug.Log($"Android APK build succeeded: {outputPath}");
    }

    private static string GetOutputPath()
    {
        var args = Environment.GetCommandLineArgs();
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], "-apkOutput", StringComparison.OrdinalIgnoreCase))
            {
                return args[i + 1];
            }
        }

        return DefaultOutputPath;
    }
}
