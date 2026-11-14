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
    public sealed class SortingSignalLaboratoryTools : EditorWindow
    {
        const string Menu="Royal Aves/Elementos/Sinalizador de triagem/";
        const string Art="Assets/RoyalAves/Art/Obstacles/SortingSignal/";
        const string Request="Logs/sorting-signal.request";
        const string StartKey="MagicMail.SortingSignalLab.Start";
        const string IntroKey="MagicMail.SortingSignalLab.Intro";
        static int width,height;
        static string error;
        [MenuItem(Menu+"Abrir laboratório no jogo")]
        public static void Open(){Directory.CreateDirectory("Logs");File.WriteAllText(Request,"open");}
        [MenuItem(Menu+"Painel de teste")]
        public static void Panel()=>GetWindow<SortingSignalLaboratoryTools>("Sinalizador — teste").Show();
        [MenuItem(Menu+"Preparar laboratório")]
        public static void Prepare()
        {
            bool keep=Soft3DProductionTools.IsInstalled;PrepareLegacy();if(keep)Soft3DProductionTools.Apply();
        }
        static void PrepareLegacy()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pare o Play para preparar os assets do laboratório.");
            AssetDatabase.ImportAsset(Art+"SignalMaster.png",ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(Art+"SignalMaster.png");
            importer.textureType=TextureImporterType.Default;importer.isReadable=true;importer.mipmapEnabled=false;
            importer.alphaIsTransparency=true;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=2048;importer.filterMode=FilterMode.Bilinear;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"SignalMaster.png");
            int left=texture.width,right=0,bottom=texture.height,top=0;
            for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(texture.GetPixel(x,y).a>.8f)
            {left=Math.Min(left,x);right=Math.Max(right,x);bottom=Math.Min(bottom,y);top=Math.Max(top,y);}
            var rect=new Rect(left,bottom,right-left+1,top-bottom+1);
            var created=Sprite.Create(texture,rect,Vector2.one*.5f,Mathf.Max(rect.width,rect.height)/1.8f,0,SpriteMeshType.FullRect);
            created.name="SortingSignalGreenSelected";
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>(Art+"SignalIcon.asset");
            if(icon==null){icon=created;AssetDatabase.CreateAsset(icon,Art+"SignalIcon.asset");}
            else{EditorUtility.CopySerialized(created,icon);DestroyImmediate(created);EditorUtility.SetDirty(icon);AssetDatabase.SaveAssetIfDirty(icon);}
            var material=AssetDatabase.LoadAssetAtPath<Material>(Art+"SignalLens.mat");
            AssetDatabase.ImportAsset("Assets/RoyalAves/Shaders/SortingSignalLens.shader",ImportAssetOptions.ForceSynchronousImport|ImportAssetOptions.ForceUpdate);
            var lensShader=AssetDatabase.LoadAssetAtPath<Shader>("Assets/RoyalAves/Shaders/SortingSignalLens.shader");
            if(lensShader==null)throw new InvalidOperationException("Shader do sinalizador não foi importado.");
            if(material==null){material=new Material(lensShader);AssetDatabase.CreateAsset(material,Art+"SignalLens.mat");}
            else {material.shader=lensShader;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);}
            var pieceSprites=Resources.Load<GameObject>("Items/Item").GetComponent<IColorableComponent>().GetSprites(1);
            string[] identities={"gift","stamp","envelope","bow"};
            var pieces=identities.Select(name=>pieceSprites.FirstOrDefault(s=>s!=null && s.texture.name==name)).ToArray();
            if(pieces.Any(s=>s==null))throw new InvalidOperationException("Paleta postal verde/vermelho/lilás/azul incompleta.");
            int[] colors=pieces.Select(piece=>Array.IndexOf(pieceSprites,piece)).ToArray();
            var preview=EditorSceneManager.NewPreviewScene();var root=new GameObject("SortingSignalCell");SceneManager.MoveGameObjectToScene(root,preview);
            GameObject prefab;
            try
            {
                var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=icon;renderer.sharedMaterial=material;
                var square=root.AddComponent<Square>();square.type=SquareTypes.SolidBlock;square.canMoveIn=square.canMoveOut=false;
                square.mainRenderer=renderer;square.sizeInSquares=Vector2Int.one;
                root.AddComponent<BindLayer>().order=6;
                var signal=root.AddComponent<SortingSignalCell>();signal.lensTemplate=material;signal.requestedPieces=pieces;signal.requestedColors=colors;
                prefab=PrefabUtility.SaveAsPrefabAsset(root,SortingSignalLaboratory.PrefabPath);
            }
            finally {EditorSceneManager.ClosePreviewScene(preview);}
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var goal=config.targets.FirstOrDefault(t=>t.name=="SortingSignals");
            if(goal==null){goal=new TargetContainer{name="SortingSignals"};config.targets.Add(goal);}
            goal.collectAction=CollectingTypes.Destroy;goal.setCount=SetCount.Manually;
            goal.defaultSprites=new SprArrays{new SprArray{sprites=new SpriteList{new SpriteObject{icon=icon,uiSprite=true}}}};
            goal.prefabs=new List<GameObject>{prefab};
            goal.localization=new LocalizationIndexFolder {
                description=new SweetSugar.Scripts.Localization.LocalizationIndex{text="Quebre os sinalizadores",index=-1},
                failed=new SweetSugar.Scripts.Localization.LocalizationIndex{text="Ainda faltam sinalizadores",index=-1}};
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
            var template=AssetDatabase.LoadAssetAtPath<LevelContainer>(InkwellLaboratory.LevelPath);
            var level=AssetDatabase.LoadAssetAtPath<LevelContainer>(SortingSignalLaboratory.LevelPath);
            if(level==null){level=CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(level,SortingSignalLaboratory.LevelPath);}
            var data=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(template.levelData));
            data.fields=data.fields.Take(1).ToList();data.currentSublevelIndex=0;data.levelNum=102;data.limit=40;data.selectedTutorial=0;
            data.limitType=LIMIT.MOVES;data.pieceColors=colors.ToList();data.colorLimit=4;
            var field=data.fields[0];field.layers=2;field.frankingMachines.Clear();
            for(int row=0;row<field.maxRows;row++)for(int col=0;col<field.maxCols;col++)
            {
                var cell=field.levelSquares[row*field.maxCols+col];bool signal=(col==3 && row==2) || (col==1 && row==5) || (col==5 && row==5);
                cell.blocks.Clear();cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.EmptySquare});
                if(signal)cell.blocks.Add(new SquareTypeLayer{squareType=SquareTypes.SolidBlock});
                cell.block=signal?SquareTypes.SolidBlock:SquareTypes.EmptySquare;cell.obstacle=SquareTypes.NONE;cell.blockLayer=cell.obstacleLayer=1;
                cell.spawners.Clear();cell.position=new Vector2Int(col,row);cell.startWithoutItem=signal;cell.enterSquare=row==0;cell.direction=Vector2.down;
                cell.isEnterTeleport=false;cell.teleportCoordinatesLinked=cell.teleportCoordinatesLinkedBack=new Vector2Int(-1,-1);cell.separatorIndexes=new bool[4];
                cell.item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/Item"),ItemType=ItemsTypes.NONE,
                    Color=colors[(row+2*col)%4],Tex=pieces[(row+2*col)%4].texture};
            }
            void Piece(int col,int row,int color)
            {
                var item=field.levelSquares[row*field.maxCols+col].item;item.Color=colors[color];item.Texture=pieces[color].texture;
            }
            // Swap (1,2) to the right: green vertical match at column 2 adjacent to signal (3,2).
            Piece(2,1,0);Piece(2,2,1);Piece(2,3,0);Piece(1,2,0);Piece(1,1,2);Piece(1,3,3);
            Piece(2,0,1);Piece(2,4,1);
            field.levelSquares[5*field.maxCols].item=new ItemForEditor{Item=Resources.Load<GameObject>("Items/HORIZONTAL_STRIPED"),ItemType=ItemsTypes.HORIZONTAL_STRIPED,Color=colors[0]};
            data.LoadTargetObject();int index=data.GetTargetIndex("SortingSignals");data.SetTarget(index);level.levelData=data;
            var targets=AssetDatabase.LoadAssetAtPath<TargetLevel>(SortingSignalLaboratory.TargetPath);
            if(targets==null){targets=CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(targets,SortingSignalLaboratory.TargetPath);}
            targets.targets=new TargetList{new TargetObject {targetType=new TargetType{type=index},sprites=new SpriteList{new SpriteObject{icon=icon,uiSprite=true}},
                CountDrawer=new Count{count=3},ShowTheScoreForStar=new BoolScoreShow()}};
            EditorUtility.SetDirty(level);EditorUtility.SetDirty(targets);AssetDatabase.SaveAssetIfDirty(level);AssetDatabase.SaveAssetIfDirty(targets);
            File.WriteAllText("Logs/sorting-signal-prepare.result","READY: primeiro ícone selecionado; três sinalizadores; quatro cores; três acertos; meta própria de laboratório; nenhuma fase da campanha alterada.");
        }
        [InitializeOnLoadMethod] static void Listen()
        {
            EditorApplication.update-=Poll;EditorApplication.update+=Poll;
            EditorApplication.playModeStateChanged-=Changed;EditorApplication.playModeStateChanged+=Changed;
        }
        static void Changed(PlayModeStateChange state)
        {
            if(state!=PlayModeStateChange.ExitingPlayMode)return;
            if(SessionState.GetBool(SortingSignalLaboratory.EnabledKey,false))
            {SortingSignalLaboratory.Disable();SessionState.SetBool(StartKey,false);LightBallLaboratoryTools.RestoreGameView();Time.timeScale=1;}
        }
        static void Poll()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            try
            {
                if(File.Exists(Request))
                {
                    if(EditorApplication.isPlaying){EditorApplication.isPlaying=false;return;}
                    if(EditorApplication.isPlayingOrWillChangePlaymode)return;
                    File.Delete(Request);
                    if(SceneManager.GetActiveScene().path!="Assets/SweetSugar/Scenes/game.unity" && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
                    Prepare();
                    foreach(var key in LightBallLaboratory.OtherKeys)SessionState.SetBool(key,false);
                    LightBallLaboratory.Disable();InkwellLaboratory.Disable();
                    SessionState.SetBool(SortingSignalLaboratory.EnabledKey,true);SessionState.SetBool(StartKey,true);SessionState.SetBool(IntroKey,false);
                    width=height=0;error=null;
                    if(SceneManager.GetActiveScene().path!="Assets/SweetSugar/Scenes/game.unity")EditorSceneManager.OpenScene("Assets/SweetSugar/Scenes/game.unity");
                    LightBallLaboratoryTools.ConfigureGameView();Panel();EditorApplication.isPlaying=true;return;
                }
                if(!EditorApplication.isPlaying || !SessionState.GetBool(SortingSignalLaboratory.EnabledKey,false))return;
                var manager=LevelManager.THIS;if(manager==null)return;
                if(SessionState.GetBool(StartKey,false) && manager.gameStatus==GameState.Map && AI.THIS!=null)
                {
                    Application.runInBackground=true;manager.BoostPackage=manager.BoostColorfullBomb=manager.BoostStriped=0;manager.gameStatus=GameState.PrepareGame;
                }
                if(!SortingSignalLaboratory.IsActive)return;
                if(manager.gameStatus==GameState.Tutorial && !SessionState.GetBool(IntroKey,false))
                {
                    SessionState.SetBool(IntroKey,true);
                    RoyalAves.Meta.ElementIntroPrototype.Show(AssetDatabase.LoadAssetAtPath<Sprite>(Art+"SignalIcon.asset"),"Sinalizadores!",
                        "Combine peças da <color=#AE193F>cor indicada</color> ao lado ou use especiais.\nSão <color=#AE193F>3 acertos</color>. A cor muda ao terminar cada jogada!");
                }
                if(manager.gameStatus==GameState.Playing)
                {
                    bool firstReady=SessionState.GetBool(StartKey,false);
                    SessionState.SetBool(StartKey,false);
                    if(width!=Screen.width || height!=Screen.height)
                    {LightBallLaboratoryTools.FitViewport(manager);width=Screen.width;height=Screen.height;}
                    foreach(var boost in manager.InGameBoosts)if(boost!=null)boost.gameObject.SetActive(false);
                    if(firstReady)File.WriteAllText("Logs/sorting-signal-ready.result","READY: laboratório real em Playing; sem recompensas. Primeira troca: coluna 2, linha 3, para a direita.");
                }
            }
            catch(Exception e){error=e.ToString();File.WriteAllText("Logs/sorting-signal-error.result",error);Debug.LogException(e);}
        }
        void OnInspectorUpdate()=>Repaint();
        void OnGUI()
        {
            EditorGUILayout.LabelField("Sinalizador de triagem — laboratório",EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("1. Faça uma combinação da cor indicada ao lado.\n2. Cor errada não causa dano; especiais sempre causam.\n3. Depois da jogada e das cascatas, todos passam para a próxima cor.\n4. Três acertos quebram cada sinalizador; a meta conta só na quebra.",MessageType.Info);
            EditorGUILayout.HelpBox("Primeiro teste: na linha 3, troque o trevo da coluna 2 com o selo à direita. Isso forma uma coluna de trevos ao lado do sinalizador superior. Depois, veja a lente mudar para vermelho.",MessageType.None);
            if(GUILayout.Button("Abrir / reiniciar laboratório"))Open();
            var signals=EditorApplication.isPlaying?FindObjectsByType<SortingSignalCell>(FindObjectsSortMode.None).OrderBy(s=>s.transform.position.y).ToArray():Array.Empty<SortingSignalCell>();
            foreach(var signal in signals)
            {
                EditorGUILayout.LabelField(signal.RequestedName,$"Acertos {signal.Hits}/3");EditorGUILayout.LabelField(signal.LastFeedback,EditorStyles.wordWrappedMiniLabel);
            }
            using(new EditorGUI.DisabledScope(signals.Length==0 || LevelManager.GetGameStatus()!=GameState.Playing))
            {
                if(GUILayout.Button("Especial no primeiro sinalizador — diagnóstico"))signals[0].HandleHit(null);
                if(GUILayout.Button("Próxima cor — diagnóstico, sem gastar jogada"))foreach(var signal in signals)signal.NextColorForReview();
                if(GUILayout.Button("Capturar tabuleiro"))InkwellLaboratoryTools.Capture("Logs/sorting-signal-board.png");
            }
            if(EditorApplication.isPlaying && signals.Length==0 && SortingSignalLaboratory.IsActive)
                EditorGUILayout.HelpBox("Todos quebrados! Reinicie para repetir. Nenhuma recompensa ou avanço de campanha foi concedido.",MessageType.Info);
            if(GUILayout.Button("Desativar e sair do Play"))
            {SortingSignalLaboratory.Disable();LightBallLaboratoryTools.RestoreGameView();EditorApplication.isPlaying=false;}
            if(error!=null)EditorGUILayout.HelpBox(error,MessageType.Error);
            EditorGUILayout.HelpBox("Os botões de diagnóstico não substituem jogar com os gestos reais. Este elemento ainda não foi instalado na campanha nem na paleta de produção.",MessageType.None);
        }
    }
}
