using System;using System.IO;using UnityEditor;using UnityEngine;
public static class RoundBlueV104Check
{
    public static void Run()
    {
        const string folder="Assets/RoundBlueV104/";const string output="../blue-enamel-v103/round-blue-v104.txt";
        try
        {
            AssetDatabase.Refresh();var importer=(TextureImporter)AssetImporter.GetAtPath(folder+"center.png");importer.textureType=TextureImporterType.Default;importer.textureShape=TextureImporterShape.Texture2D;importer.SaveAndReimport();var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"center.png");var material=AssetDatabase.LoadAssetAtPath<Material>(folder+"RoundButtonBlueV104.mat");if(texture==null||material==null||!material.IsKeywordEnabled("HSV_ON"))throw new Exception("Missing texture/material/HSV keyword");
            Shader.SetGlobalFloat("unity_GUIZTestMode",8);var target=new RenderTexture(128,128,0);target.Create();var prior=RenderTexture.active;var sample=new Texture2D(1,1,TextureFormat.RGBA32,false);
            try{Graphics.Blit(texture,target,material);RenderTexture.active=target;sample.ReadPixels(new Rect(64,80,1,1),0,0);sample.Apply();var c=sample.GetPixel(0,0);if(ShaderUtil.ShaderHasError(material.shader)||c.b<c.r*1.5f||c.b<c.g*1.2f||c.a<.9f)throw new Exception("Native shader blue check failed: "+c);File.WriteAllText(output,"PASS: persisted AllIn1 UI HSV material renders the actual user red-center PNG blue; native sample "+c+". No source pixel edits.\n");}
            finally{RenderTexture.active=prior;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(sample);}
            EditorApplication.Exit(0);
        }catch(Exception e){File.WriteAllText(output,e.ToString());Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
