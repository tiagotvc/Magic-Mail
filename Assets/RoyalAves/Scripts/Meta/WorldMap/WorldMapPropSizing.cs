using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    // One authored height per family, shared by generated scenery, saved layouts and the editor.
    public static class WorldMapPropSizing
    {
        public static float Height(Sprite sprite, PropKind kind, WorldMapKit kit)
        {
            if(kit!=null && sprite!=null)
            {
                if(sprite==kit.postmanSprite)return 1.6f;
                if(System.Array.IndexOf(kit.groundDetails,sprite)>=0)return .72f;
                if(WorldMapScenery.IsPostalTree(sprite,kit))return 1.5f;
                if(kit.simpleNature.Length==6)
                {
                    if(sprite==kit.simpleNature[0]||sprite==kit.simpleNature[1])return 1.5f;
                    if(sprite==kit.simpleNature[2])return .58f;
                    if(sprite==kit.simpleNature[3])return .27f;
                    if(sprite==kit.simpleNature[4])return .22f;
                    if(sprite==kit.simpleNature[5])return .34f;
                }
                if(kit.simplePostal.Length==6)
                {
                    if(sprite==kit.simplePostal[0]||sprite==kit.simplePostal[1])return 1.42f;
                    if(sprite==kit.simplePostal[2])return 1.06f;
                    if(sprite==kit.simplePostal[3]||sprite==kit.simplePostal[4])return .84f;
                    if(sprite==kit.simplePostal[5])return .67f;
                }
                if(sprite==kit.tree||sprite==kit.pine)return 1.7f;
                if(sprite==kit.lamp)return 1.5f;
                if(sprite==kit.mailboxClosed||sprite==kit.mailboxOpen)return .9f;
                if(sprite==kit.postOffice||sprite==kit.hut||sprite==kit.agency)return 1.8f;
                if(sprite==kit.shrubCoral||sprite==kit.shrubGold||sprite==kit.shrubMint)return .52f;
                if(sprite==kit.detailTulips||sprite==kit.detailBells)return .45f;
                if(sprite==kit.detailRocks)return .36f;
                if(sprite==kit.shrub)return .52f;
                if(sprite==kit.flowers)return .6f;
                if(sprite==kit.parcels)return .7f;
                if(sprite==kit.sign)return .8f;
                if(sprite==kit.bench)return 1f;
                if(sprite==kit.topiary)return 1.3f;
                if(sprite==kit.fountain)return 1.2f;
                if(sprite==kit.factoryBody||sprite==kit.factory)return 2.3f;
                if(sprite==kit.lighthouse||sprite==kit.windmill)return 2.1f;
            }
            switch(kind)
            {
                case PropKind.Tree:return 1.7f;
                case PropKind.Lamp:return 1.5f;
                case PropKind.Mailbox:return .9f;
                case PropKind.Landmark:return 1.8f;
                case PropKind.Geyser:return 1.12f;
                case PropKind.Dog:return .56f;
                case PropKind.FloatingMail:return .26f;
                case PropKind.GroundDetail:return .72f;
                default:return .65f;
            }
        }
    }
}
