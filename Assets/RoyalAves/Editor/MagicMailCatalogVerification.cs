using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Editor;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Native asset audit and authoring compatibility checks, not a pointer/playthrough test.
    public static class MagicMailCatalogVerification
    {
        [Serializable] public class Entry {public string name,status,location;}
        [Serializable] public class Report
        {
            public List<Entry> elements=new List<Entry>();
            public string[] hiddenBlocks,hiddenItems,hiddenTargets;
            public string validation;
        }
        const string Output="Logs/magic-mail-catalog";
        public static void Run()
        {
            if(!Application.dataPath.Replace('\\','/').Contains("/Logs/InkwellValidationProject/Assets"))
                throw new InvalidOperationException("Somente na cópia isolada InkwellValidationProject.");
            try {Audit();EditorApplication.Exit(0);}
            catch(Exception e){File.WriteAllText(Output+".result","FAIL: "+e);Debug.LogException(e);EditorApplication.Exit(1);}
        }
        static void Audit()
        {
            string originalCatalog=File.ReadAllText("Assets/SweetSugar/Resources/Levels/TargetEditorScriptable.asset");
            string originalLevel=File.ReadAllText("Assets/SweetSugar/Resources/Levels/Level_1.asset");
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var report=new Report();
            foreach(var type in MagicMailEditorCatalog.Blocks)
            {
                var prefab=Resources.Load<GameObject>("Blocks/"+type);
                Require(prefab!=null && SquareUtils.GetSquareTexture(type).Texture2D!=null,"Bloco permitido sem prefab/ícone: "+type);
                if(type!=SquareTypes.EmptySquare)report.elements.Add(new Entry{name=MagicMailEditorCatalog.BlockLabel(type),status="Disponível",location="Blocks"});
            }
            var egg=Resources.Load<GameObject>("Items/FLYING_LETTER");
            Require(egg!=null && egg.GetComponent<RoyalAves.Gameplay.ItemFlyingLetter>()!=null,"Ovo não registrado.");
            report.elements.Add(new Entry{name="Ovo mágico",status="Disponível",location="Items"});
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(RoyalAves.Gameplay.RouteCentralLaboratory.CellPath)!=null,"Laboratório de poções ausente.");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(RoyalAves.Gameplay.MailbagLaboratory.CellPath)!=null,"Laboratório de bolsa ausente.");
            Require(Resources.Load<GameObject>("Blocks/RouteCentral")==null && Resources.Load<GameObject>("Blocks/BagOpenings")==null,"Rever auditoria de prefabs independentes.");
            report.elements.Add(new Entry{name="Poções",status="Somente laboratório; sem pincel próprio",location=RoyalAves.Gameplay.RouteCentralLaboratory.CellPath});
            report.elements.Add(new Entry{name="Bolsa do carteiro (fixa)",status="Somente laboratório; sem pincel próprio",location=RoyalAves.Gameplay.MailbagLaboratory.CellPath});
            report.elements.Add(new Entry{name="Fita",status="Ainda não produzida",location=""});
            report.hiddenBlocks=Enum.GetValues(typeof(SquareTypes)).Cast<SquareTypes>()
                .Where(t=>t!=SquareTypes.NONE && !MagicMailEditorCatalog.IsBlock(t)).Select(t=>t.ToString()).ToArray();
            report.hiddenItems=Enum.GetValues(typeof(ItemsTypes)).Cast<ItemsTypes>()
                .Where(t=>!MagicMailEditorCatalog.IsItem(t)).Select(t=>t.ToString()).ToArray();
            report.hiddenTargets=config.targets.Where(t=>!MagicMailEditorCatalog.IsTarget(t.name)).Select(t=>t.name).ToArray();
            Require(!MagicMailEditorCatalog.Blocks.Contains(SquareTypes.WireBlock),"WireBlock oferecido como Fita.");
            Require(MagicMailEditorCatalog.IsBlock(SquareTypes.SugarSquare) && MagicMailEditorCatalog.IsBlock(SquareTypes.SolidBlock),"Grama/caixa próprias ocultadas.");
            foreach(var type in Enum.GetValues(typeof(ItemsTypes)).Cast<ItemsTypes>().Where(MagicMailEditorCatalog.IsItem))
                Require(MagicMailEditorCatalog.IsItemPrefab(Resources.Load<GameObject>("Items/"+MagicMailEditorCatalog.ItemResource(type)),type),"Item próprio ausente: "+type);
            Require(!MagicMailEditorCatalog.IsItemPrefab(Resources.Load<GameObject>("Items/TestBloxkItem"),ItemsTypes.NONE),"Prefab de teste confundido com peça normal.");
            var allowed=MagicMailEditorCatalog.TargetChoices(config);
            Require(allowed.All(i=>MagicMailEditorCatalog.IsTarget(config.targets[i].name)),"Meta base oferecida para fase nova.");
            int legacy=config.targets.FindIndex(t=>t.name=="JellyBlock"),safe=config.targets.FindIndex(t=>t.name=="Safe");
            Require(legacy>=0 && safe==16 && !allowed.Contains(legacy) && allowed.Contains(safe),"Índices/filtro de metas incorretos.");
            var legacyChoices=MagicMailEditorCatalog.TargetChoices(config,legacy);
            Require(legacyChoices.Contains(legacy) && legacyChoices.All(i=>i==legacy || allowed.Contains(i)),"Meta legada não pode ser preservada isoladamente.");
            var names=new Sweet_sugar.Assets.SweetSugar.Scripts.TargetScripts.TargetEditor.TargetTypeDrawer().GetTargetsNames();
            Require(!names.Contains("Ingredients") && !names.Contains("JellyBlock"),"Inspector oferece meta base.");
            var editor=ScriptableObject.CreateInstance<LevelMakerEditor>();
            try
            {
                var flags=BindingFlags.Instance|BindingFlags.NonPublic;
                void Set(string name,object value)=>editor.GetType().GetField(name,flags).SetValue(editor,value);
                var data=JsonUtility.FromJson<LevelData>(JsonUtility.ToJson(Resources.Load<LevelContainer>("Levels/Level_1").levelData));
                Set("levelData",data);Set("subLevelNumber",1);Set("squareType",SquareTypes.WireBlock);
                string before=JsonUtility.ToJson(data);
                editor.GetType().GetMethod("SetSquareType",flags).Invoke(editor,new object[]{0,0});
                Require(before==JsonUtility.ToJson(data),"Pincel legado ainda pinta blocos ocultos.");
                Set("itemBrush",new ItemForEditor{Item=Resources.Load<GameObject>("Items/TimeBomb"),ItemType=ItemsTypes.TimeBomb});
                editor.GetType().GetMethod("SetItem",flags).Invoke(editor,new object[]{0,0});
                Require(before==JsonUtility.ToJson(data),"Pincel legado de itens ainda altera fase.");
                var random=(SquareTypes[])editor.GetType().GetField("randomSquareTypes",flags).GetValue(editor);
                Require(random.All(MagicMailEditorCatalog.IsBlock),"Random oferece bloco base.");
            }
            finally {UnityEngine.Object.DestroyImmediate(editor);}
            Require(File.ReadAllText("Assets/SweetSugar/Resources/Levels/TargetEditorScriptable.asset")==originalCatalog,"Auditoria alterou catálogo salvo.");
            Require(File.ReadAllText("Assets/SweetSugar/Resources/Levels/Level_1.asset")==originalLevel,"Auditoria alterou fase existente.");
            report.validation="PASS: prefabs/ícones reais; nove elementos próprios disponíveis, dois só em laboratório, fita não produzida; escolhas filtradas; IDs originais preservados; pincéis legados recusados; Random sem blocos base; fase/catálogo byte-preservados. Sem teste físico de interface, partida ou dispositivo.";
            File.WriteAllText(Output+".json",JsonUtility.ToJson(report,true));File.WriteAllText(Output+".result",report.validation);
        }
        static void Require(bool result,string message){if(!result)throw new InvalidOperationException(message);}
    }
}
