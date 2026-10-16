using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    public static class WorldMapLampArt
    {
        // Normalized source-image coordinates, bottom-left origin. Keep the halo
        // and glass emission registered to the actual lantern in each silhouette.
        public static Vector4 Glass(Sprite sprite)
        {
            var name=sprite!=null?sprite.name:"";if(name.EndsWith("_0"))name=name.Substring(0,name.Length-2);
            switch(name)
            {
                case "lamp-dark-forest-v47":return new Vector4(.5f,.805f,.20f,.15f);
                case "lamp-volcano-v47":return new Vector4(.5f,.805f,.23f,.15f);
                case "lamp-straight-glossy-v46":return new Vector4(.498f,.704f,.195f,.083f);
                default:return new Vector4(.5f,.70f,.29f,.14f);
            }
        }
        public static bool Cold(Sprite sprite)=>sprite!=null&&sprite.name.StartsWith("lamp-dark-forest-v47");
        public static bool HasRegisteredHead(Sprite sprite)=>sprite!=null&&(sprite.name.StartsWith("lamp-dark-forest-v47")||sprite.name.StartsWith("lamp-volcano-v47")||sprite.name.StartsWith("lamp-straight-glossy-v46"));
        public static Color Tint(Sprite sprite)=>Cold(sprite)?new Color(.70f,.30f,1):sprite!=null&&sprite.name.StartsWith("lamp-volcano-v47")?new Color(1,.42f,.08f):new Color(1,.75f,.35f);
    }
}
