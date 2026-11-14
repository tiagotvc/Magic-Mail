using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RoyalAves.EditorTools
{
    public static class SafeEditorTools
    {
        public const string PrefabPath="Assets/SweetSugar/Resources/Blocks/Safe.prefab";
        [MenuItem("Royal Aves/Elementos/Cofre/Registrar no Level Editor")]
        public static void Prepare()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Pare o Play antes de registrar o cofre.");
            var animation=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/RoyalAves/Laboratory/SafeAnimation.prefab");
            var icon=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/RoyalAves/Art/Obstacles/Safe/SafeState1.asset");
            if(animation==null || icon==null)throw new InvalidOperationException("Prévia aprovada do cofre ausente.");
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Safe");SceneManager.MoveGameObjectToScene(root,scene);
            GameObject prefab;
            try
            {
                root.layer=10;
                var renderer=root.AddComponent<SpriteRenderer>();renderer.sprite=icon;renderer.enabled=false;
                renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/RoyalAves/Art/Obstacles/Safe/SafePaint.mat");
                var square=root.AddComponent<Square>();square.type=SquareTypes.Safe;square.sizeInSquares=new Vector2Int(2,2);
                square.canMoveIn=false;square.canMoveOut=false;square.cannotBeDestroy=false;square.undestroyable=false;square.mainRenderer=renderer;
                root.AddComponent<BindLayer>().order=6;
                root.AddComponent<SafeCell>().animationPrefab=animation;
                prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
            }
            finally {EditorSceneManager.ClosePreviewScene(scene);}
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var before=config.targets.Where(t=>t.name!="Safe").Select(t=>JsonUtility.ToJson(t)).ToArray();
            var goal=config.targets.FirstOrDefault(t=>t.name=="Safe");
            if(goal==null){goal=new TargetContainer{name="Safe"};config.targets.Add(goal);}
            goal.collectAction=CollectingTypes.Destroy;goal.setCount=SetCount.FromLevel;
            goal.localization=new LocalizationIndexFolder {
                description=new SweetSugar.Scripts.Localization.LocalizationIndex{text="Quebre os cofres",index=-1},
                failed=new SweetSugar.Scripts.Localization.LocalizationIndex{text="Ainda faltam cofres",index=-1}};
            goal.defaultSprites=new SprArrays{new SprArray{sprites=new SpriteList{new SpriteObject{icon=icon,uiSprite=true}}}};
            goal.prefabs=new List<GameObject>{prefab};
            Require(before.SequenceEqual(config.targets.Where(t=>t.name!="Safe").Select(t=>JsonUtility.ToJson(t))),"Registro alterou uma meta existente.");
            EditorUtility.SetDirty(config);AssetDatabase.SaveAssetIfDirty(config);
            Debug.Log("Cofre 2×2 registrado na paleta; meta automática, sem alterar fases.");
        }
        [InitializeOnLoadMethod] static void Listen(){EditorApplication.update-=Poll;EditorApplication.update+=Poll;}
        static void Poll()
        {
            const string request="Logs/safe-editor.request";
            if(!File.Exists(request) || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating)return;
            File.Delete(request);
            try {Prepare();VerifyAuthoring();File.WriteAllText("Logs/safe-editor.result","PASS");}
            catch(Exception e){File.WriteAllText("Logs/safe-editor.result",e.ToString());Debug.LogException(e);}
        }
        public static void VerifyAuthoring()
        {
            Require((int)SquareTypes.Inkwell==16 && (int)SquareTypes.Safe==17,"Enum antigo foi deslocado.");
            var prefab=Resources.Load<GameObject>("Blocks/Safe");
            Require(prefab!=null && prefab.GetComponent<Square>().sizeInSquares==new Vector2Int(2,2),"Prefab não ocupa 2×2.");
            Require(prefab.GetComponent<BindLayer>()!=null,"BindLayer ausente.");
            var texture=SweetSugar.Scripts.Editor.SquareUtils.GetSquareTexture(SquareTypes.Safe);
            Require(texture.Texture2D!=null && texture.Texture2D.width<500,"Ícone da paleta não é o estado recortado.");
            var data=JsonUtility.FromJson<LevelData>("{}");
            var field=new FieldData{maxCols=6,maxRows=6,levelSquares=new SquareBlocks[36]};
            data.fields=new List<FieldData>{field};
            for(int y=0;y<6;y++)for(int x=0;x<6;x++)field.levelSquares[y*6+x]=new SquareBlocks {
                position=new Vector2Int(x,y),block=SquareTypes.EmptySquare,
                blocks=new List<SquareTypeLayer>{new SquareTypeLayer{squareType=SquareTypes.EmptySquare}}};
            var editor=ScriptableObject.CreateInstance<SweetSugar.Scripts.Editor.LevelMakerEditor>();
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            void Set(string name,object value)=>editor.GetType().GetField(name,flags).SetValue(editor,value);
            void Paint(int x,int y)=>editor.GetType().GetMethod("SetSquareType",flags).Invoke(editor,new object[]{x,y});
            try
            {
                Set("levelData",data);Set("subLevelNumber",1);Set("layer",0);Set("squareType",SquareTypes.Safe);
                Paint(1,1);
                Require(SafeFootprint.Count(data)==1 && field.levelSquares.Count(b=>b.blocks.Any(l=>l.squareType==SquareTypes.Safe))==4,"Pincel não colocou quatro células para um cofre.");
                Require((bool)editor.GetType().GetField("pendingSafeTarget",flags).GetValue(editor),"Meta não solicitada pelo pincel.");
                Require(SafeFootprint.Validate(field,out _),"Grupo inválido após pintar.");
                Paint(3,1);Require(SafeFootprint.Count(data)==2,"Cofres encostados foram agrupados.");
                var roundtrip=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(data));
                Require(SafeFootprint.Count(roundtrip)==2 && SafeFootprint.Validate(roundtrip.fields[0],out _),"Save/load perdeu grupo.");
                Paint(2,2);Require(SafeFootprint.Count(data)==1 && SafeFootprint.Has(field,3,1),"Apagar quadrante afetou cofre vizinho.");
                string before=JsonUtility.ToJson(data);Paint(5,5);Require(before==JsonUtility.ToJson(data),"Borda mudou tabuleiro parcialmente.");
                field.levelSquares[4*6].block=SquareTypes.NONE;
                Require(!SafeFootprint.TryPaint(field,0,4,out _),"Cofre aceitou buraco.");
                field.levelSquares[4*6].block=SquareTypes.EmptySquare;
                field.levelSquares[4*6].blocks.Add(new SquareTypeLayer{squareType=SquareTypes.SugarSquare});
                Require(SafeFootprint.TryPaint(field,0,4,out _),"Piso compatível rejeitado.");
                SafeFootprint.Erase(field,1,5);Require(field.levelSquares[4*6].blocks.Any(l=>l.squareType==SquareTypes.SugarSquare),"Borracha removeu grama.");
                field.levelSquares[1*6+3].blocks.RemoveAll(l=>l.squareType==SquareTypes.Safe);
                Require(!SafeFootprint.Validate(field,out _),"Grupo incompleto aceito.");
            }
            finally {UnityEngine.Object.DestroyImmediate(editor);}
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/safe-authoring-verification.result","PASS: enum append-only 17; prefab 2×2; BindLayer; ícone recortado; pincel real por método; quatro células/uma meta; cofres encostados; JSON save/load; apagar quadrante; borda atômica; buraco rejeitado; grama preservada; grupo incompleto rejeitado. Não é teste de ponteiro físico nem partida.");
        }
        static void Require(bool result,string message){if(!result)throw new InvalidOperationException(message);}
    }
}
