using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using RoyalAves.Effects;
namespace RoyalAves.EditorTools
{
    public static class PackageExplosionAssetTools
    {
        const string TexturePath="Assets/RoyalAves/Art/Effects/PackageExplosion/Explosion.png";
        const string MaterialPath="Assets/RoyalAves/Resources/Effects/Explosion.mat";
        const string PrefabPath="Assets/RoyalAves/Resources/Effects/PackageExplosionFlipbook.prefab";
        [InitializeOnLoadMethod]static void Init(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Logs/package-explosion-asset.request"))return;
            if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            File.Delete("Logs/package-explosion-asset.request");
            try{Build();File.WriteAllText("Logs/package-explosion-asset.result","PASS: Explosion.tif 6x6, frame blending, 3x5 bursts, dedicated material/prefab. Prior cartoon effect unused.");ExplosivePackageLaboratoryTools.Open();}
            catch(Exception ex){File.WriteAllText("Logs/package-explosion-asset.result","FAIL "+ex);Debug.LogException(ex);}
        }
        [MenuItem("Royal Aves/Efeitos/Pacote explosivo/Preparar explosão da textura 6x6")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("Pare o Play antes de preparar.");
            var importer=(TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);var shader=Shader.Find("RoyalAves/Explosion");
            if(texture==null||shader==null)throw new Exception("Textura/shader ausentes.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,MaterialPath);}
            material.name="Explosion";material.shader=shader;material.mainTexture=texture;
            material.SetColor("_Color",new Color(1,.45f,.1f,.4f));material.SetColor("_EmissionColor",new Color(3,.9f,.12f,1));
            material.SetFloat("_FlipbookBlending",1);material.SetFloat("_SoftParticlesEnabled",1);material.SetFloat("_SoftParticlesFarFadeDistance",2);
            // The 2D board has no scene-depth intersections. Do not fade against an invalid depth texture.
            material.SetFloat("_UseSceneDepth",0);EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            var root=new GameObject("PackageExplosionFlipbook");root.transform.localScale=Vector3.one*2;
            try
            {
                var child=new GameObject("FireBall");child.transform.SetParent(root.transform,false);
                var ps=child.AddComponent<ParticleSystem>();ps.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.duration=1;main.loop=false;main.prewarm=false;main.playOnAwake=false;
                main.startLifetime=new ParticleSystem.MinMaxCurve(.28f,.42f);main.startSpeed=0;main.startSize=new ParticleSystem.MinMaxCurve(2,3);
                main.startRotation=new ParticleSystem.MinMaxCurve(-Mathf.PI*2,Mathf.PI*2);main.startColor=Color.white;
                main.scalingMode=ParticleSystemScalingMode.Hierarchy;main.simulationSpace=ParticleSystemSimulationSpace.Local;main.maxParticles=30;
                var emission=ps.emission;emission.enabled=true;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(0,3,5,.035f)});
                var shape=ps.shape;shape.enabled=true;shape.shapeType=ParticleSystemShapeType.Sphere;shape.radius=.35f;
                var sheet=ps.textureSheetAnimation;sheet.enabled=true;sheet.mode=ParticleSystemAnimationMode.Grid;sheet.numTilesX=sheet.numTilesY=6;
                sheet.animation=ParticleSystemAnimationType.WholeSheet;sheet.frameOverTime=new ParticleSystem.MinMaxCurve(1,AnimationCurve.Linear(0,0,1,1));sheet.cycleCount=1;
                var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.black,0),new GradientColorKey(Color.white,.12f),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.65f,.12f),new GradientAlphaKey(.35f,.55f),new GradientAlphaKey(0,1)});color.color=gradient;
                var renderer=ps.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
                renderer.sortMode=ParticleSystemSortMode.Distance;renderer.maxParticleSize=5;renderer.flip=new Vector3(.5f,.5f,0);
                renderer.receiveShadows=true;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.sortingLayerName=PowerUpEffectOrder.SortingLayer;renderer.sortingOrder=PowerUpEffectOrder.Value+12;
                renderer.SetActiveVertexStreams(new System.Collections.Generic.List<ParticleSystemVertexStream>{ParticleSystemVertexStream.Position,ParticleSystemVertexStream.Color,ParticleSystemVertexStream.UV,ParticleSystemVertexStream.UV2,ParticleSystemVertexStream.AnimBlend});
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
        }
    }
}
