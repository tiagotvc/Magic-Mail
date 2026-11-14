using System.Linq;
using SweetSugar.Scripts.Blocks;
using SweetSugar.Scripts.Items;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.TargetScripts.TargetSystem;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Authoring choices only. Never delete/reindex serialized engine enums or target records.
    public static class MagicMailEditorCatalog
    {
        public static readonly SquareTypes[] Blocks={SquareTypes.EmptySquare,SquareTypes.SolidBlock,
            SquareTypes.SugarSquare,SquareTypes.MailCubby,SquareTypes.FrankingMachine,SquareTypes.IceBlock,
            SquareTypes.EnchantedStone,SquareTypes.Inkwell,SquareTypes.Safe,SquareTypes.PostalTape,SquareTypes.RoutePotions,SquareTypes.BagOpenings,SquareTypes.SortingSignals};
        public static readonly Spawners[] Generators={Spawners.None,Spawners.HORIZONTAL_STRIPED,
            Spawners.VERTICAL_STRIPED,Spawners.MARMALADE,Spawners.MULTICOLOR};
        static readonly string[] Targets={"CollectItems","SugarSquare","SolidBlock","Stars","MachineLetters",
            "IceBlock","MailCubby","FlyingLetters","RoutePotions","BagOpenings","EnchantedStone","Inkwell","Safe","SortingSignals"};
        public static bool IsBlock(SquareTypes type)=>Blocks.Contains(type);
        public static bool IsTarget(string name)=>Targets.Contains(name);
        public static string ItemResource(ItemsTypes type)
        {
            switch(type)
            {
                case ItemsTypes.NONE:return "Item";
                case ItemsTypes.HORIZONTAL_STRIPED:return "HORIZONTAL_STRIPED";
                case ItemsTypes.VERTICAL_STRIPED:return "VERTICAL_STRIPED";
                case ItemsTypes.PACKAGE:return "PACKAGE";
                case ItemsTypes.MARMALADE:return "MARMALADE";
                case ItemsTypes.MULTICOLOR:return "MULTICOLOR";
                case ItemsTypes.FLYING_LETTER:return "FLYING_LETTER";
                default:return null;
            }
        }
        public static bool IsItem(ItemsTypes type)=>ItemResource(type)!=null;
        public static bool IsItemPrefab(GameObject prefab,ItemsTypes type)=>ItemResource(type)!=null &&
            prefab!=null && prefab==Resources.Load<GameObject>("Items/"+ItemResource(type));
        public static string ItemLabel(ItemsTypes type)
        {
            switch(type)
            {
                case ItemsTypes.NONE:return "Peça normal";
                case ItemsTypes.HORIZONTAL_STRIPED:return "Foguete horizontal";
                case ItemsTypes.VERTICAL_STRIPED:return "Foguete vertical";
                case ItemsTypes.PACKAGE:return "Dinamite";
                case ItemsTypes.MARMALADE:return "Hélice";
                case ItemsTypes.MULTICOLOR:return "Light Ball";
                case ItemsTypes.FLYING_LETTER:return "Ovo mágico";
                default:return type+" (legado)";
            }
        }
        public static string BlockLabel(SquareTypes type)
        {
            switch(type)
            {
                case SquareTypes.EmptySquare:return "Chão normal";
                case SquareTypes.SolidBlock:return "Caixa de madeira";
                case SquareTypes.SugarSquare:return "Grama";
                case SquareTypes.MailCubby:return "Escaninho (2×2)";
                case SquareTypes.FrankingMachine:return "Franqueadora";
                case SquareTypes.RoutePotions:return "Po\u00e7\u00f5es (2\u00d72)";
                case SquareTypes.BagOpenings:return "Bolsa do carteiro";
                case SquareTypes.SortingSignals:return "Sinalizador de triagem";
                case SquareTypes.PostalTape:return "Fita postal";
                case SquareTypes.IceBlock:return "Gelo";
                case SquareTypes.EnchantedStone:return "Pedra rúnica";
                case SquareTypes.Inkwell:return "Tinteiro";
                case SquareTypes.Safe:return "Cofre (2×2)";
                default:return type+" (legado)";
            }
        }
        // Keep a loaded legacy value as its own option; opening a level must not convert it.
        public static int[] TargetChoices(TargetEditorScriptable config,int current=-1)=>
            Enumerable.Range(0,config.targets.Count).Where(i=>i==current || IsTarget(config.targets[i].name)).ToArray();
        public static Spawners DrawGenerator(Spawners current)
        {
            var choices=Generators.Contains(current)?Generators:Generators.Concat(new[]{current}).ToArray();
            string Label(Spawners value)=>value==Spawners.None?"Nenhum":value==Spawners.HORIZONTAL_STRIPED?"Foguete horizontal":
                value==Spawners.VERTICAL_STRIPED?"Foguete vertical":value==Spawners.MARMALADE?"Hélice":
                value==Spawners.MULTICOLOR?"Light Ball":value+" (legado)";
            return (Spawners)EditorGUILayout.IntPopup((int)current,choices.Select(Label).ToArray(),choices.Select(v=>(int)v).ToArray(),GUILayout.Width(250));
        }
        public static bool HasLegacyTimer(LevelData data)=>data.fields.Any(f=>f.levelSquares.Any(b=>b.item?.ItemType==ItemsTypes.TimeBomb ||
            b.spawners.Any(s=>s.SpawnersType==Spawners.TimeBomb || s.SpawnersType_2==Spawners.TimeBomb)));
        public static bool HasLegacyIngredients(LevelData data)=>data.SpawnerExits || data.generateIngredientOnlyFromSpawner ||
            data.fields.Any(f=>f.levelSquares.Any(b=>b.item?.ItemType==ItemsTypes.INGREDIENT ||
                b.spawners.Any(s=>s.SpawnersType==Spawners.Ingredient || s.SpawnersType_2==Spawners.Ingredient)));
    }
}
