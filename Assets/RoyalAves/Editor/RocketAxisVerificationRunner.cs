using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class RocketAxisVerificationRunner
    {
        const string Key="MagicMail.RocketAxisBatch";
        [InitializeOnLoadMethod] static void Init() { EditorApplication.update-=Poll;EditorApplication.update+=Poll; }
        public static void Run()
        {
            if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))
                throw new InvalidOperationException("Use somente a cópia isolada do projeto.");
            Directory.CreateDirectory("Logs");
            if(File.Exists("Logs/rocket-pass-native.result"))File.Delete("Logs/rocket-pass-native.result");
            SessionState.SetBool(Key,true);
            SessionState.SetString(Key+".Deadline",DateTime.UtcNow.AddMinutes(6).ToString("O"));
            File.WriteAllText("Logs/rocket-pass.request","run");
        }
        static void Poll()
        {
            if(!SessionState.GetBool(Key,false) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            if(File.Exists("Logs/rocket-pass-native.result"))
            {
                string result=File.ReadAllText("Logs/rocket-pass-native.result");
                if(File.Exists("Logs/stone-101.request"))File.Delete("Logs/stone-101.request");
                SessionState.SetBool(Key,false);EditorApplication.Exit(result.StartsWith("PASS")?0:1);
            }
            else if(DateTime.UtcNow>DateTime.Parse(SessionState.GetString(Key+".Deadline",""),null,System.Globalization.DateTimeStyles.RoundtripKind))
            {
                File.WriteAllText("Logs/rocket-pass-native.result","FAIL: batch timeout.");
                SessionState.SetBool(Key,false);EditorApplication.Exit(1);
            }
        }
    }
}
