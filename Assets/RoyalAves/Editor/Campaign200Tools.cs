using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using RoyalAves.Gameplay;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Items._Interfaces;
using SweetSugar.Scripts.TargetScripts.TargetEditor;
using SweetSugar.Scripts.TargetScripts.TargetSystem;

namespace RoyalAves.EditorTools
{
    // Recipes are authored outside Unity; all production serialization uses the native asset API.
    public static class Campaign200Tools
    {
        [Serializable] public class Book { public Recipe[] levels; }
        [Serializable] public class Recipe {
            public int level, moves, seed; public string role, intro; public int[] palette;
            public string[] layout; public Placement[] placements; public Goal[] goals;
            public Piece[] opening; public Special[] specials; public Swap openingSwap; public float workload;
        }
        [Serializable] public class Placement { public string kind; public int x,y,layers; }
        [Serializable] public class Goal { public string name; public int count; }
        [Serializable] public class Piece { public int x,y,color; }
        [Serializable] public class Special { public int x,y; public string type; }
        [Serializable] public class Swap { public int x,y,toX,toY; }
        const string Root="Assets/SweetSugar/Resources/Levels/";
        public static string RecipePath => Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Tools/level-map/campaign-200/recipes.json");
        public static Book Read() => JsonUtility.FromJson<Book>(File.ReadAllText(RecipePath));
        static SquareTypes Type(string kind) => kind switch {
            "C"=>SquareTypes.SolidBlock,"g"=>SquareTypes.SugarSquare,"A"=>SquareTypes.MailCubby,
            "M"=>SquareTypes.FrankingMachine,"I"=>SquareTypes.IceBlock,"P"=>SquareTypes.RoutePotions,
            "B"=>SquareTypes.BagOpenings,"R"=>SquareTypes.EnchantedStone,"K"=>SquareTypes.Inkwell,
            "S"=>SquareTypes.Safe,"N"=>SquareTypes.SortingSignals,"T"=>SquareTypes.PostalTape,
            _=>SquareTypes.NONE };
        static bool Solid(char c) => "CAMPRKSBN".Contains(c);
        static void Require(bool okay,string message) {if(!okay)throw new Exception(message);}

        [MenuItem("Royal Aves/Campanha/Aplicar campanha 1–200")]
        public static void Build()
        {
            Require(!EditorApplication.isPlaying,"Pare o jogo antes de aplicar a campanha.");
            var book=Read(); Require(book.levels.Length==200,"Expected 200 recipes");
            var config=Resources.Load<TargetEditorScriptable>("Levels/TargetEditorScriptable");
            var normal=Resources.Load<GameObject>("Items/Item");
            var sprites=normal.GetComponent<IColorableComponent>().GetSprites(1).ToArray();
            AssetDatabase.StartAssetEditing();
            try { foreach(var r in book.levels)
            {
                var data=new LevelData(false,r.level) {limit=r.moves,colorLimit=r.palette.Length,
                    pieceColors=r.palette.ToList(),enableMarmalade=false,selectedTutorial=0};
                int score=Mathf.RoundToInt((400+r.workload*45)/100)*100;
                data.star1=Mathf.Max(400,score);data.star2=data.star1*2;data.star3=data.star1*3;
                var f=data.AddNewField();f.maxCols=r.layout[0].Length;f.maxRows=r.layout.Length;
                f.levelSquares=new SquareBlocks[f.maxCols*f.maxRows];
                for(int y=0;y<f.maxRows;y++)for(int x=0;x<f.maxCols;x++)
                {
                    var t=r.layout[y][x]=='#'?SquareTypes.NONE:SquareTypes.EmptySquare;
                    f.levelSquares[y*f.maxCols+x]=new SquareBlocks {block=t,position=new Vector2Int(x,y),
                        direction=Vector2.down,item=new ItemForEditor(),
                        blocks=new List<SquareTypeLayer>{new SquareTypeLayer{squareType=t,size=Vector2Int.one}}};
                }
                foreach(var p in r.placements)
                {
                    var cell=f.levelSquares[p.y*f.maxCols+p.x];var type=Type(p.kind);
                    if(p.kind=="E") {SetSpecial(cell,"FLYING_LETTER");continue;}
                    string error;
                    if(p.kind=="S") {Require(SafeFootprint.TryPaint(f,p.x,p.y,out error),r.level+": "+error);continue;}
                    if(p.kind=="P") {Require(PotionFootprint.TryPaint(f,p.x,p.y,out error),r.level+": "+error);continue;}
                    int size=p.kind=="A"?2:1;
                    for(int y=p.y;y<p.y+size;y++)for(int x=p.x;x<p.x+size;x++)
                    {
                        cell=f.levelSquares[y*f.maxCols+x];
                        for(int layer=0;layer<p.layers;layer++)cell.blocks.Add(new SquareTypeLayer{squareType=type,size=Vector2Int.one,originalPos=new Vector2Int(x,y)});
                        if(p.kind=="g")cell.block=type;
                        else {cell.obstacle=type;cell.obstacleLayer=p.layers;}
                    }
                }
                foreach(var p in r.opening)
                {
                    var cell=f.levelSquares[p.y*f.maxCols+p.x];
                    cell.item=new ItemForEditor{Color=p.color,Item=normal,Texture=sprites[p.color].texture};
                }
                foreach(var p in r.specials)SetSpecial(f.levelSquares[p.y*f.maxCols+p.x],p.type);
                for(int y=0;y<f.maxRows;y++)for(int x=0;x<f.maxCols;x++)
                {
                    var c=f.levelSquares[y*f.maxCols+x];char symbol=r.layout[y][x];
                    // Every gravity segment has an inlet, including the space under a fixed station.
                    c.enterSquare=symbol!='#'&&!Solid(symbol)&&(y==0||r.layout[y-1][x]=='#'||Solid(r.layout[y-1][x]));
                }
                f.layers=f.levelSquares.Max(c=>c.blocks.Count);
                data.SetTarget(data.GetTargetIndex(r.goals[0].name));
                var level=AssetDatabase.LoadAssetAtPath<LevelContainer>(Root+"Level_"+r.level+".asset");
                if(level==null){level=ScriptableObject.CreateInstance<LevelContainer>();AssetDatabase.CreateAsset(level,Root+"Level_"+r.level+".asset");}
                level.levelData=data;EditorUtility.SetDirty(level);
                string path=Root+"Targets/TargetLevel"+r.level+".asset";
                var target=AssetDatabase.LoadAssetAtPath<TargetLevel>(path);
                if(target==null){target=ScriptableObject.CreateInstance<TargetLevel>();AssetDatabase.CreateAsset(target,path);}
                target.targets=new TargetList();
                foreach(var g in r.goals)
                {
                    int index=config.targets.FindIndex(t=>t.name==g.name);Require(index>=0,"Missing goal "+g.name);
                    var def=config.targets[index];Require(def.defaultSprites.Count>0,"Missing icons "+g.name);
                    target.targets.Add(new TargetObject {targetType=new TargetType{type=index},
                        sprites=def.defaultSprites[0].sprites.Copy(),CountDrawer=new Count{count=g.count},
                        ShowTheScoreForStar=new BoolScoreShow()});
                }
                EditorUtility.SetDirty(target);
            }
            } finally {AssetDatabase.StopAssetEditing();}
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();Validate();
        }
        static void SetSpecial(SquareBlocks cell,string name)
        {
            var prefab=Resources.Load<GameObject>("Items/"+name);Require(prefab!=null,"Missing item "+name);
            cell.item=new ItemForEditor{ItemType=(ItemsTypes)Enum.Parse(typeof(ItemsTypes),name),Item=prefab};
        }
        [MenuItem("Royal Aves/Campanha/Validar campanha 1–200")]
        public static void Validate()
        {
            int openings=0,goals=0;
            foreach(var r in Read().levels)
            {
                var d=Resources.Load<LevelContainer>("Levels/Level_"+r.level).levelData;
                var f=d.fields.Single();string error;
                Require(d.levelNum==r.level&&d.limit==r.moves,"Wrong level/moves "+r.level);
                Require(SafeFootprint.Validate(f,out error),r.level+": "+error);
                Require(PotionFootprint.Validate(f,out error),r.level+": "+error);
                var t=Resources.Load<TargetLevel>("Levels/Targets/TargetLevel"+r.level);
                Require(t.targets.Count==r.goals.Length&&t.targets.Count<=2,"Too many goal families "+r.level);
                foreach(var g in r.goals)Require(t.targets.Any(x=>x.targetType.GetTarget().name==g.name&&x.CountDrawer.count==g.count),"Goal mismatch "+r.level);
                Require(!t.targets.Any(x=>x.targetType.GetTarget().name=="PostalTapes"),"Tape goal "+r.level);
                var colors=r.opening.ToDictionary(p=>new Vector2Int(p.x,p.y),p=>p.color);
                foreach(var special in r.specials)colors[new Vector2Int(special.x,special.y)]=0;
                bool Triple(Vector2Int p,int value) =>
                    (colors.TryGetValue(p+Vector2Int.left,out int l)&&l==value&&colors.TryGetValue(p+Vector2Int.left*2,out int ll)&&ll==value)||
                    (colors.TryGetValue(p+Vector2Int.up,out int u)&&u==value&&colors.TryGetValue(p+Vector2Int.up*2,out int uu)&&uu==value);
                Require(!colors.Any(c=>Triple(c.Key,c.Value)),"Initial match on authored items "+r.level);
                Require(!colors.Any(c=>colors.TryGetValue(c.Key+Vector2Int.left,out int l)&&l==c.Value&&
                    colors.TryGetValue(c.Key+Vector2Int.up,out int u)&&u==c.Value&&
                    colors.TryGetValue(c.Key+Vector2Int.left+Vector2Int.up,out int lu)&&lu==c.Value),"Initial propeller square "+r.level);
                var a=new Vector2Int(r.openingSwap.x,r.openingSwap.y);var b=new Vector2Int(r.openingSwap.toX,r.openingSwap.toY);
                (colors[a],colors[b])=(colors[b],colors[a]);
                Require(colors.Any(c=>Triple(c.Key,c.Value)),"No opening swap "+r.level);
                foreach(var p in r.opening)Require(f.levelSquares[p.y*f.maxCols+p.x].item.Texture!=null&&d.pieceColors.Contains(p.color),"Missing piece "+r.level);
                openings++;goals+=t.targets.Count;
                if(!string.IsNullOrEmpty(r.intro))Require(RoyalAves.Meta.CampaignElementIntroductions.Has(r.level),"Missing intro "+r.level);
            }
            RoyalAves.Meta.WorldMap.WorldMapLevelCatalog.Refresh();
            Require(RoyalAves.Meta.WorldMap.WorldMapLevelCatalog.Count==200&&RoyalAves.Meta.WorldMap.WorldMapLevelCatalog.LastLevel==200,"Map catalog must contain exactly 200 levels");
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/campaign-200.result","PASS: 200 native assets; "+openings+" legal opening swaps; "+goals+" goal groups; safe/potion footprints; 18 introduction schedules; catalog 1–200. Not a victory-rate or full-playthrough test.\n"+DateTime.UtcNow.ToString("O"));
            Debug.Log("CAMPAIGN_200_PASS");
        }
    }
}
