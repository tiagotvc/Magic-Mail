using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoyalAves.Gameplay;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace RoyalAves.EditorTools
{
    public sealed class PostalTapeLaboratoryTools : EditorWindow
    {
        const string Menu="Royal Aves/Elementos/Fita postal/";
        const string Request="Logs/postal-tape.request",StartKey="MagicMail.PostalTape.Start";
        static bool attached;
        [MenuItem(Menu+"Abrir laboratorio no jogo")] public static void Open(){Directory.CreateDirectory("Logs");File.WriteAllText(Request,"open");}
        [MenuItem(Menu+"Desativar laboratorio")] public static void Disable(){PostalTapeLaboratory.Disable();SessionState.SetBool(StartKey,false);if(EditorApplication.isPlaying)EditorApplication.isPlaying=false;}
        [MenuItem(Menu+"Painel de teste")] public static void Panel()=>GetWindow<PostalTapeLaboratoryTools>("Fita postal").Show();
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Stop Play before preparing the laboratory.");
            const string master="Assets/RoyalAves/Art/Obstacles/PostalTape/TapeMaster.png";
            AssetDatabase.ImportAsset(master,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(master);importer.textureType=TextureImporterType.Default;importer.isReadable=true;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=false;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(master);int left=texture.width,right=0,bottom=texture.height,top=0;
            var pixels=texture.GetPixels32();for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>100)
            {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            var rect=new Rect(left,bottom,right-left+1,top-bottom+1);var created=Sprite.Create(texture,rect,Vector2.one*.5f,rect.width/1.8f,0,SpriteMeshType.FullRect);created.name="PostalTapeStrip";
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(PostalTapeLaboratory.SpritePath);
            if(icon==null){icon=created;AssetDatabase.CreateAsset(icon,PostalTapeLaboratory.SpritePath);}else{EditorUtility.CopySerialized(created,icon);DestroyImmediate(created);EditorUtility.SetDirty(icon);AssetDatabase.SaveAssetIfDirty(icon);}
            PostalTapeEditorTools.Build();
            var template=AssetDatabase.LoadAssetAtPath<LevelContainer>(SortingSignalLaboratory.LevelPath);
            var level=AssetDatabase.LoadAssetAtPath<LevelContainer>(PostalTapeLaboratory.LevelPath);
            if(level==null){level=CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(level,PostalTapeLaboratory.LevelPath);}
            var data=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));data.fields=data.fields.Take(1).ToList();data.currentSublevelIndex=0;data.levelNum=102;data.limit=40;data.selectedTutorial=0;data.limitType=LIMIT.MOVES;
            var sprites=Resources.Load<GameObject>("Items/Item").GetComponent<IColorableComponent>().GetSprites(1);
            var pieces=new[]{"gift","stamp","envelope","bow"}.Select(n=>sprites.First(s=>s!=null && s.texture.name==n)).ToArray();var colors=pieces.Select(p=>Array.IndexOf(sprites,p)).ToArray();data.pieceColors=colors.ToList();data.colorLimit=4;
            var field=data.fields[0];field.layers=1;field.frankingMachines.Clear();
            for(int row=0;row<field.maxRows;row++)for(int col=0;col<field.maxCols;col++)
            {
                var cell=field.levelSquares[row*field.maxCols+col];cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});cell.block=SquareTypes.EmptySquare;cell.obstacle=SquareTypes.NONE;cell.blockLayer=cell.obstacleLayer=1;
                cell.startWithoutItem=false;cell.spawners.Clear();cell.position=new Vector2Int(col,row);cell.enterSquare=row==0;cell.direction=Vector2.down;cell.isEnterTeleport=false;cell.teleportCoordinatesLinked=cell.teleportCoordinatesLinkedBack=new Vector2Int(-1,-1);cell.separatorIndexes=new bool[4];
                int color=(row+2*col)%4;cell.item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/Item"),ItemType=ItemsTypes.NONE,Color=colors[color],Tex=pieces[color].texture};
            }
            void Piece(int col,int row,int color){var item=field.levelSquares[row*field.maxCols+col].item;item.Color=colors[color];item.Texture=pieces[color].texture;}
            Piece(2,1,0);Piece(2,2,1);Piece(2,3,0);Piece(1,2,0);Piece(1,1,2);Piece(1,3,3);Piece(2,0,1);Piece(2,4,1);
            foreach(int col in new[]{0,3})field.levelSquares[5*field.maxCols+col].item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/HORIZONTAL_STRIPED"),ItemType=ItemsTypes.HORIZONTAL_STRIPED,Color=colors[0]};
            foreach(var position in PostalTapeLaboratory.Positions)
                if(!PostalTapeAuthoring.TryPaint(field.levelSquares[position.y*field.maxCols+position.x],out var error))throw new InvalidOperationException(error);
            field.layers=2;
            data.LoadTargetObject();int index=data.GetTargetIndex("CollectItems");data.SetTarget(index);level.levelData=data;
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(PostalTapeLaboratory.TargetPath);if(targets==null){targets=CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(targets,PostalTapeLaboratory.TargetPath);}
            targets.targets=new TargetList{new TargetObject{targetType=new TargetType{type=index},sprites=new SpriteList{new SpriteObject{icon=pieces[3],uiSprite=true}},CountDrawer=new Count{count=12},ShowTheScoreForStar=new BoolScoreShow()}};
            EditorUtility.SetDirty(level);EditorUtility.SetDirty(targets);AssetDatabase.SaveAssetIfDirty(level);AssetDatabase.SaveAssetIfDirty(targets);
        }
        [InitializeOnLoadMethod] static void Listen(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;}
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                const string captureRequest="Logs/postal-tape-capture.request";
                if(File.Exists(captureRequest) && EditorApplication.isPlaying && PostalTapeLaboratory.IsActive && LevelManager.THIS!=null)
                {
                    var tapes=FindObjectsByType<PostalTapeLayer>(FindObjectsSortMode.None);
                    if(LevelManager.THIS.gameStatus==GameState.Playing && tapes.Length==PostalTapeLaboratory.Positions.Length && tapes.All(t=>t.Cell!=null))
                    {
                        CaptureBoard("Logs/postal-tape-main-current.png");
                        File.WriteAllText("Logs/postal-tape-main-renderers.json",JsonUtility.ToJson(new TapeCapture{renderers=tapes.SelectMany(t=>t.Cell.GetComponentsInChildren<SpriteRenderer>()).Select(r=>new TapeRendererInfo{name=r.name,sprite=r.sprite!=null?r.sprite.name:"null",enabled=r.enabled,color=r.color,scale=r.transform.lossyScale,position=r.transform.position,material=r.sharedMaterial!=null?r.sharedMaterial.name:"null"}).ToArray()},true));
                        File.Delete(captureRequest);
                    }
                }
                if(File.Exists(Request))
                {
                    if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    File.Delete(Request);if(SceneManager.GetActiveScene().path!="Assets/SweetSugar/Scenes/game.unity" && SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Save the current scene before opening this laboratory.");
                    Prepare();foreach(var key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);LightBallLaboratory.Disable();InkwellLaboratory.Disable();
                    SessionState.SetBool(PostalTapeLaboratory.EnabledKey,true);SessionState.SetBool(StartKey,true);attached=false;
                    if(SceneManager.GetActiveScene().path!="Assets/SweetSugar/Scenes/game.unity")EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");Panel();EditorApplication.isPlaying=true;return;
                }
                if(!EditorApplication.isPlaying || !SessionState.GetBool(PostalTapeLaboratory.EnabledKey,false))return;
                var manager=LevelManager.THIS;if(manager==null)return;
                if(SessionState.GetBool(StartKey,false) && manager.gameStatus==GameState.Map && AI.THIS!=null){Application.runInBackground=true;manager.BoostPackage=manager.BoostColorfullBomb=manager.BoostStriped=0;manager.gameStatus=GameState.PrepareGame;}
                if(!PostalTapeLaboratory.IsActive)return;
                if(!attached && (manager.gameStatus==GameState.Tutorial || manager.gameStatus==GameState.Playing))
                {
                    if(PostalTapeLaboratory.Positions.Any(p=>manager.field.GetSquare(p.x,p.y)?.Item==null))return;
                    if(PostalTapeLaboratory.Positions.Any(p=>PostalTapeLayer.For(manager.field.GetSquare(p.x,p.y))==null))return;
                    attached=true;
                }
                if(attached && manager.gameStatus==GameState.Tutorial)manager.gameStatus=GameState.Playing;
                if(attached && manager.gameStatus==GameState.Playing && SessionState.GetBool(StartKey,false))
                {SessionState.SetBool(StartKey,false);foreach(var boost in manager.InGameBoosts)if(boost!=null)boost.gameObject.SetActive(false);File.WriteAllText("Logs/postal-tape-ready.result","READY: five independent crossed tapes; production-authored layers; no tape goal; envelope goal only; no campaign result.");}
            }
            catch(Exception e){File.WriteAllText("Logs/postal-tape-error.result",e.ToString());Debug.LogException(e);SessionState.SetBool(StartKey,false);}
        }
        [Serializable] sealed class TapeCapture{public TapeRendererInfo[] renderers;}
        [Serializable] sealed class TapeRendererInfo{public string name,sprite,material;public bool enabled;public Color color;public Vector3 scale,position;}
        public static void CaptureBoard(string path)
        {
            var manager=LevelManager.THIS;var field=manager.levelData.fields[0];var a=manager.field.GetSquare(0,0).transform.position;var b=manager.field.GetSquare(field.maxCols-1,field.maxRows-1).transform.position;
            var root=new GameObject("TapeBoardCapture");var camera=root.AddComponent<Camera>();camera.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(Mathf.Abs(a.y-b.y),Mathf.Abs(a.x-b.x))*.5f+.7f;camera.transform.position=(a+b)*.5f+Vector3.back*20;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.06f,.06f,.10f);var rt=new RenderTexture(960,960,24);var pixels=new Texture2D(960,960,TextureFormat.RGBA32,false);var old=RenderTexture.active;
            try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,960,960),0,0);pixels.Apply();File.WriteAllBytes(path,pixels.EncodeToPNG());}
            finally{RenderTexture.active=old;DestroyImmediate(pixels);DestroyImmediate(rt);DestroyImmediate(root);}
        }
        void OnInspectorUpdate()=>Repaint();
        void OnGUI()
        {
            EditorGUILayout.LabelField("Fita postal - uma camada",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Primeira jogada: troque o trevo da linha 3, coluna 2 para a direita. O match verde remove a fita ao lado. A fita prende troca, queda e ativacao; o impacto remove apenas a fita. Sem gelo nesta bancada.",MessageType.Info);
            if(GUILayout.Button("Abrir / reiniciar laboratorio"))Open();if(GUILayout.Button("Desativar"))Disable();
            if(!EditorApplication.isPlaying || !PostalTapeLaboratory.IsActive)return;
            var tapes=FindObjectsByType<PostalTapeLayer>(FindObjectsSortMode.None);EditorGUILayout.LabelField("Fitas restantes",tapes.Count(t=>!t.IsReleased).ToString());
            foreach(var tape in tapes)using(new EditorGUI.DisabledScope(tape.IsPeeling))if(GUILayout.Button("Descolar (diagnostico): "+(tape.Cell.row+1)+", "+(tape.Cell.col+1)))tape.HandleHit(null);
            EditorGUILayout.HelpBox("Botoes de descolar sao diagnosticos. Jogue no tabuleiro para testar matches e especiais.",MessageType.None);
        }
    }
}
