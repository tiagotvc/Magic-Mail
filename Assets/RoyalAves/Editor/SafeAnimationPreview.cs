using System;
using System.IO;
using System.Linq;
using RoyalAves.Effects;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalAves.EditorTools
{
    public sealed class SafeAnimationPreview : EditorWindow
    {
        const string Folder="Assets/RoyalAves/Art/Obstacles/Safe";
        const string Source=Folder+"/SafeStatesReference.png";
        const string Prefab="Assets/RoyalAves/Laboratory/SafeAnimation.prefab";
        const string Review="Tools/royal-postman/production/review/safe-v1";
        Scene previewScene;
        GameObject root;
        Camera camera;
        RenderTexture target;
        SafeBreakAnimation safe;
        float time,speed=1;
        int scheduled;
        double previous;
        bool playing=true,loop=true,manual;
        static readonly float[] HitTimes={.8f,1.6f,2.4f,3.2f,4f};

        [InitializeOnLoadMethod]
        static void Listen()
        {
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
        }
        static void Poll()
        {
            const string request="Logs/safe-animation.request";
            if(!File.Exists(request) || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            File.Delete(request);
            try {CaptureBatch();File.WriteAllText("Logs/safe-animation.result","PASS");}
            catch(Exception e){File.WriteAllText("Logs/safe-animation.result",e.ToString());Debug.LogException(e);}
        }

        [MenuItem("Royal Aves/Elementos/Cofre/Prévia da quebra")]
        public static void Open()
        {
            if(!EditorApplication.isPlayingOrWillChangePlaymode)Prepare();
            var window=GetWindow<SafeAnimationPreview>("Cofre — quebra");
            window.minSize=new Vector2(440,540);window.Show();
        }
        [MenuItem("Royal Aves/Elementos/Cofre/Preparar prévia")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pare o Play antes de preparar a prévia.");
            AssetDatabase.ImportAsset(Source,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Source);
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;
            importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=2048;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source);
            var images=new Sprite[5];
            // These are five static states, not animation frames. Native Sprite regions preserve PNG bytes.
            for(int i=0;i<5;i++)
            {
                int x0=Mathf.RoundToInt((i%4)*texture.width/4f),x1=Mathf.RoundToInt((i%4+1)*texture.width/4f);
                int y0=i<4?Mathf.RoundToInt(texture.height/2f):0,y1=i<4?texture.height:Mathf.RoundToInt(texture.height/2f);
                int left=x1,right=x0,bottom=y1,top=y0;
                for(int y=y0;y<y1;y++)for(int x=x0;x<x1;x++)
                    if(texture.GetPixel(x,y).a>.9f){left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
                if(right<=left || top<=bottom)throw new InvalidOperationException("Estado de cofre vazio: "+i);
                var rect=new Rect(left,bottom,right-left+1,top-bottom+1);
                var sprite=Sprite.Create(texture,rect,Vector2.one*.5f,rect.width/1.8f,0,SpriteMeshType.FullRect);
                sprite.name="SafeState"+(i+1);
                string path=Folder+"/"+sprite.name+".asset";
                var existing=AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if(existing==null){AssetDatabase.CreateAsset(sprite,path);images[i]=sprite;}
                else {EditorUtility.CopySerialized(sprite,existing);DestroyImmediate(sprite);EditorUtility.SetDirty(existing);AssetDatabase.SaveAssetIfDirty(existing);images[i]=existing;}
            }
            var shader=Shader.Find("RoyalAves/SafePaint");if(shader==null)throw new InvalidOperationException("Shader SafePaint ausente.");
            string materialPath=Folder+"/SafePaint.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,materialPath);}
            var authoringScene=EditorSceneManager.NewPreviewScene();
            var temporary=new GameObject("SafeAnimation");SceneManager.MoveGameObjectToScene(temporary,authoringScene);
            try
            {
                var animation=temporary.AddComponent<SafeBreakAnimation>();animation.Configure(images,material);
                PrefabUtility.SaveAsPrefabAsset(temporary,Prefab);
            }
            finally {EditorSceneManager.ClosePreviewScene(authoringScene);}
            AssetDatabase.SaveAssetIfDirty(material);
        }
        void OnEnable() {previous=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
        void OnDisable() {EditorApplication.update-=Tick;DisposePreview();}
        void Initialize()
        {
            if(root!=null)return;
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if(prefab==null)throw new InvalidOperationException("Use Preparar prévia primeiro.");
            previewScene=EditorSceneManager.NewPreviewScene();
            root=Instantiate(prefab);SceneManager.MoveGameObjectToScene(root,previewScene);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=31;
            safe=root.GetComponent<SafeBreakAnimation>();safe.Automatic=false;
            // Awake does not run for editor preview objects.
            var stages=Enumerable.Range(1,5).Select(i=>AssetDatabase.LoadAssetAtPath<Sprite>(Folder+"/SafeState"+i+".asset")).ToArray();
            var oldVisual=root.transform.Find("SafeVisual");if(oldVisual!=null)DestroyImmediate(oldVisual.gameObject);
            safe.Configure(stages,AssetDatabase.LoadAssetAtPath<Material>(Folder+"/SafePaint.mat"));
            var cam=new GameObject("SafePreviewCamera");SceneManager.MoveGameObjectToScene(cam,previewScene);
            camera=cam.AddComponent<Camera>();camera.scene=previewScene;camera.orthographic=true;camera.orthographicSize=1.65f;
            camera.transform.position=new Vector3(0,-.2f,-10);camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.065f,.09f,.15f,1);camera.cullingMask=1<<31;camera.enabled=false;
            target=new RenderTexture(640,640,24);target.Create();camera.targetTexture=target;
        }
        void DisposePreview()
        {
            if(previewScene.IsValid())EditorSceneManager.ClosePreviewScene(previewScene);
            if(target!=null){target.Release();DestroyImmediate(target);}root=null;camera=null;target=null;safe=null;
        }
        void ResetPreview() {Initialize();safe.ResetAnimation();time=0;scheduled=0;}
        void Advance(float delta)
        {
            // Small steps preserve the same event timing at any preview/export frame rate.
            while(delta>0)
            {
                float step=Mathf.Min(delta,.01f);delta-=step;time+=step;safe.Advance(step);
                if(!manual)while(scheduled<HitTimes.Length && time>=HitTimes[scheduled]){safe.Hit(true);scheduled++;}
            }
        }
        void Tick()
        {
            double now=EditorApplication.timeSinceStartup;
            if(playing && root!=null)
            {
                Advance(Mathf.Min(.05f,(float)(now-previous))*speed);
                if(!manual && time>7){if(loop)ResetPreview();else playing=false;}Repaint();
            }
            previous=now;
        }
        void OnGUI()
        {
            try
            {
                Initialize();
                EditorGUILayout.LabelField("Cofre: estados estáticos + fragmentos independentes",EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Acertos: "+safe.Hits+" / 5"+(safe.Completed?" — despedaçado":""));
                var area=GUILayoutUtility.GetAspectRect(1);camera.Render();GUI.DrawTexture(area,target,ScaleMode.ScaleToFit);
                playing=EditorGUILayout.Toggle("Reproduzir",playing);loop=EditorGUILayout.Toggle("Repetir sequência",loop);
                speed=EditorGUILayout.Slider("Velocidade",speed,.25f,2);
                manual=EditorGUILayout.Toggle("Acertos manuais",manual);
                EditorGUILayout.BeginHorizontal();
                if(GUILayout.Button("Especial")){manual=true;playing=true;safe.Hit(true);}
                if(GUILayout.Button("Match sem dano")){manual=true;playing=true;safe.Hit(false);}
                if(GUILayout.Button("Reiniciar"))ResetPreview();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.HelpBox("Prévia visual isolada. Moedas são efeito; não alteram o saldo. Não instala o cofre nas fases.",MessageType.Info);
            }
            catch(Exception e){EditorGUILayout.HelpBox(e.Message,MessageType.Error);}
        }
        public static void CaptureBatch()
        {
            var window=CreateInstance<SafeAnimationPreview>();window.playing=false;
            try
            {
                // Existing preview assets can be rendered in their own scene even while a user
                // is playing. Never stop Play or rebuild assets underneath their running game.
                if(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab)==null)Prepare();
                window.Initialize();Directory.CreateDirectory(Review+"/frames");
                var safe=window.safe;int completions=0;safe.Finished+=()=>completions++;
                // Diagnostic API checks, not a claim of gameplay/pointer validation.
                Require(!safe.Hit(false) && safe.Hits==0,"Match causou dano");
                window.Advance(.25f);
                Require(safe.Hit(true) && safe.Hits==1,"Primeiro especial rejeitado");
                Require(!safe.Hit(true) && safe.Hits==1,"Impacto duplicado aceito");
                window.manual=true;
                for(int i=1;i<5;i++){window.Advance(.3f);Require(safe.Hit(true),"Especial rejeitado "+i);}
                Require(!safe.Hit(true),"Dano depois do quinto acerto");window.Advance(.2f);
                Require(safe.Completed && completions==1 && !safe.View.enabled,"Quebra final ou callback incorreto");
                Require(!safe.Hit(true),"Cofre completado aceitou dano");window.Advance(3);
                Require(safe.BurstCount==0,"Fragmentos não foram limpos");
                window.ResetPreview();Require(safe.Hits==0 && safe.View.enabled && !safe.Completed,"Reset incompleto");
                window.manual=false;
                var pixels=new Texture2D(640,640,TextureFormat.RGB24,false);
                var previous=RenderTexture.active;
                try
                {
                    for(int frame=0;frame<144;frame++)
                    {
                        window.camera.Render();RenderTexture.active=window.target;
                        pixels.ReadPixels(new Rect(0,0,640,640),0,0);pixels.Apply();
                        if(frame==0)
                        {
                            Color center=pixels.GetPixel(320,360),corner=pixels.GetPixel(0,0);
                            Require(((Vector3)(Vector4)(center-corner)).sqrMagnitude>.03f,"Captura sem cofre visível");
                        }
                        File.WriteAllBytes(Review+"/frames/"+frame.ToString("D3")+".png",pixels.EncodeToPNG());
                        window.Advance(.05f);
                    }
                }
                finally {RenderTexture.active=previous;DestroyImmediate(pixels);}
                Require(safe.Completed && completions==2,"Sequência automática não completou exatamente uma vez");
                Require(safe.BurstCount==0,"Exportação terminou com fragmentos vivos");
                File.WriteAllText(Review+"/verification.txt","PASS: match sem dano; debounce; cinco acertos; quebra e callback uma vez; rejeição após término; reset; limpeza; exportação nativa de 144 quadros a 20fps.\nNão testados: ponteiro real, partida, especiais reais, metas, áudio, dispositivo. Nenhuma recompensa alterada.");
                Debug.Log("SAFE_ANIMATION_CAPTURE_PASS");
            }
            finally {window.DisposePreview();DestroyImmediate(window);}
        }
        static void Require(bool result,string message) {if(!result)throw new InvalidOperationException(message);}
    }
}
