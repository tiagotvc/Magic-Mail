using System;
using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    [Serializable] public struct WorldMapForearm
    {
        public Rect region,heldItem;
        public Vector2 elbow;
        public float degrees;
        public float speed,phase;
        public Vector2 sourceElbow,sourceScale;
    }
    [Serializable] public sealed class WorldMapMissionCharacter
    {
        public Sprite sprite;
        public Sprite bodyBacking,forearmSprite;
        public string role,wand;
        public float height;
        public bool facingLeft;
        public WorldMapForearm[] forearms=Array.Empty<WorldMapForearm>();
    }
    [Serializable] public sealed class WorldMapMissionBeat
    {
        public int level;
        public string title,description;
        public bool special;
    }
    public sealed class WorldMapMission100 : ScriptableObject
    {
        public const string ResourcePath="RoyalAvesMission100";
        public const string LayoutResource="RoyalAvesMission100Layout";
        public bool enabled=true;
        public bool polishedEntrance;
        public static bool PolishedEntrance=>Active&&Current.polishedEntrance;
        public WorldMapLayout layout;
        public WorldMapMissionCharacter[] characters=Array.Empty<WorldMapMissionCharacter>();
        public WorldMapMissionBeat[] beats=Array.Empty<WorldMapMissionBeat>();
        static WorldMapMission100 current;
        public static WorldMapMission100 Current=>current!=null?current:current=Resources.Load<WorldMapMission100>(ResourcePath);
        public static bool Active=>Current!=null&&Current.enabled&&Current.layout!=null;
        public static string LandscapeTitle(int level)=>level<=20?"Letter Gardens":level<=40?"Ink Springs":level<=60?"Messenger Grove":level<=80?"Enchanted Stampworks":"Grand Dispatch Hill";
        public static string English(string value)
        {
            if(!Active)return value;
            switch(value){case "Cheio":return "Full";case "Mapa":return "Map";case "Eventos":return "Events";case "Equipes":return "Teams";case "Coleção":return "Collection";case "Meu nível":return "My level";case "Em breve":return "Coming soon";default:return value;}
        }
        public static WorldMapMissionCharacter Character(Sprite sprite)
        {if(Current!=null)foreach(var c in Current.characters)if(c.sprite==sprite)return c;return null;}
    }
}
