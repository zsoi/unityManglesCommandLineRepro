using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEditor.Build.Content;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class Tester
{
    private static void Log(string line)
    {
        Debug.LogFormat(LogType.Log, LogOption.NoStacktrace, null, line);
    }
    
    private static void LogE(string line)
    {
        Debug.LogFormat(LogType.Error, LogOption.NoStacktrace, null, line);
    }

    [MenuItem("Tools/Automated Command Line Tests")]
    public static void RunTests()
    {
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.Mono2x);
        RunTestsWithCurrentBuildSettings();
        
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Debug);
        RunTestsWithCurrentBuildSettings();
        
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Release);
        RunTestsWithCurrentBuildSettings();
        
        PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Standalone, Il2CppCompilerConfiguration.Master);
        RunTestsWithCurrentBuildSettings();
    }

    private static void RunTestsWithCurrentBuildSettings()
    {
        try
        {
            var outputDir = System.IO.Path.GetFullPath(Path.Combine("out"));

            if (System.IO.Directory.Exists(outputDir))
            {
                Log("Deleting out: " + outputDir);
                System.IO.Directory.Delete(outputDir, true);
            }

            Log("Creating out: " + outputDir);
            System.IO.Directory.CreateDirectory(outputDir);

            Log("Building player");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                options = BuildOptions.EnableHeadlessMode,
                scenes = (from x in EditorBuildSettings.scenes select x.path).ToArray(),
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Unknown,
                locationPathName = Path.Combine(outputDir, PlayerSettings.productName + ".exe"),
                extraScriptingDefines = new[] { "WRITE_CMDLINE_AND_QUIT" }
            });

            if (report == null)
            {
                Log("No report, build failed?");
                return;
            }

            if (report.summary.result != BuildResult.Succeeded)
            {
                Log($"Build failed: {report.summary.result}");
                return;
            }


            RunCmdlineTest("");
            RunCmdlineTest("-developer -screen-width 1920 -screen-height 1080 -force-d3d11 -some-par -simplepar --seeThis \"For.some\"=04 --andThisparameter \"Some Test.Text\"=off -force-gfx-direct  -force-d3d12-debug");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    private static void RunCmdlineTest(string commandLine)
    {
        var name = PlayerSettings.productName;

        var workingDir = System.IO.Path.GetFullPath($"out");
        var executable = System.IO.Path.GetFullPath($"out/{name}.exe");

        var cmdlineFile = workingDir + "/commandline.txt";
        if (System.IO.File.Exists(cmdlineFile))
        {
            System.IO.File.Delete(cmdlineFile);
        }

        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDir,
            Arguments = commandLine
        });

        if (!proc.WaitForExit(10 * 1000))
        {
            Log("Process not quit after timeout, killing");
            proc.Kill();
        }

        if (executable.Contains(" "))
        {
            commandLine = "\"" + executable + "\" " + commandLine;
        }
        else
        {
            commandLine = executable + " " + commandLine;
        }

        var recordedCmdline = System.IO.File.ReadAllText(cmdlineFile);

        if (recordedCmdline != commandLine)
        {
            LogE("Discrepancy found");
            LogE(commandLine);
            LogE(recordedCmdline);
        }
    }
}