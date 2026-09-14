using UnityEditor;
using System.Diagnostics;
using System.Linq;

public class ExportGitStaged
{
    [MenuItem("Tools/Export Git Staged")]
    static void Export()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "diff --cached --name-only --diff-filter=AM",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = UnityEngine.Application.dataPath.Replace("/Assets", "")
            }
        };
        
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        
        var files = output.Split('\n')
            .Where(f => !string.IsNullOrWhiteSpace(f) && f.StartsWith("Assets/"))
            .ToArray();
        
        if (files.Length > 0)
        {
            string outputPath = $"{UnityEngine.Application.dataPath.Replace("/Assets", "")}/StagedChanges.unitypackage";
            AssetDatabase.ExportPackage(files, "StagedChanges.unitypackage", ExportPackageOptions.Default);
            UnityEngine.Debug.Log($"Exported {files.Length} staged files to: {outputPath}");
        }
        else
        {
            UnityEngine.Debug.LogWarning("No staged files found in Assets/ folder");
        }
    }
}
