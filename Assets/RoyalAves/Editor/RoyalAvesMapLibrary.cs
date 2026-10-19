using System.IO;
using System.Linq;
using RoyalAves.Meta.WorldMap;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesMapLibrary
    {
        public const string Root="Assets/RoyalAves/Resources/MapLibrary";
        public const string ArtRoot="Assets/RoyalAves/Art/MapKitV3/Library";
        public static readonly string[] Folders={"Construcoes","Arvores","Props","Efeitos","Personagens"};
        public static WorldMapLibraryItem[] Items(WorldMapAssetCategory category)=>AssetDatabase.FindAssets("t:WorldMapLibraryItem",new[]{Root+"/"+Folders[(int)category]})
            .Select(g=>AssetDatabase.LoadAssetAtPath<WorldMapLibraryItem>(AssetDatabase.GUIDToAssetPath(g)))
            .Where(x=>x!=null&&x.sprite!=null&&x.category==category).OrderBy(x=>x.Label).ToArray();
        // Icone do item "Raio de sol": faixas diagonais claras. O objeto no mapa e desenhado pelo shader, nao por este sprite.
        static Sprite LightIcon()
        {
            var path = ArtRoot + "/Efeitos/light-shaft-icon.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(ArtRoot + "/Efeitos");
                var tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
                for (var y = 0; y < 128; y++)
                    for (var x = 0; x < 128; x++)
                    {
                        var u = (x + (y - 64) * .5f) / 128f;
                        var band = (u > .1f && u < .3f) || (u > .42f && u < .52f) || (u > .62f && u < .9f);
                        tex.SetPixel(x, y, new Color(1, .93f, .55f, band ? .85f * Mathf.Clamp01(y / 40f) : 0));
                    }
                File.WriteAllBytes(path, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);
                AssetDatabase.ImportAsset(path);
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        public static void Ensure(WorldMapKit kit)
        {
            bool createdFolder=false;
            foreach(var folder in Folders)foreach(var path in new[]{Root+"/"+folder,ArtRoot+"/"+folder})
                if(!Directory.Exists(path)){Directory.CreateDirectory(path);createdFolder=true;}
            if(createdFolder)AssetDatabase.Refresh();
            void Add(string id,string title,WorldMapAssetCategory category,Sprite sprite,PropKind kind,float size)
            {
                if(sprite==null)return;
                var path=Root+"/"+Folders[(int)category]+"/"+id+".asset";
                if(AssetDatabase.LoadAssetAtPath<WorldMapLibraryItem>(path)!=null)return;
                var item=ScriptableObject.CreateInstance<WorldMapLibraryItem>();item.title=title;item.category=category;
                item.sprite=sprite;item.kind=kind;item.size=size;AssetDatabase.CreateAsset(item,path);
            }
            var b=WorldMapAssetCategory.Buildings;var tr=WorldMapAssetCategory.Trees;var p=WorldMapAssetCategory.Props;
            var e=WorldMapAssetCategory.Effects;var c=WorldMapAssetCategory.Characters;
            if(kit.simplePostal.Length==6)
            {
                Add("casa-postal","Casa postal",b,kit.simplePostal[0],PropKind.Landmark,1.42f);
                Add("oficina-postal","Oficina postal",b,kit.simplePostal[1],PropKind.Landmark,1.42f);
                Add("poste","Poste de luz",p,kit.simplePostal[2],PropKind.Lamp,1.06f);
                Add("caixa-correio","Caixa de correio",p,kit.simplePostal[5],PropKind.Mailbox,.67f);
                Add("coruja","Coruja mensageira",c,kit.simplePostal[4],PropKind.Character,.90f);
            }
            Add("carteiro","Carteiro",c,kit.postmanSprite,PropKind.Character,1.6f);
            for(int i=0;i<kit.postalTrees.Length;i++)Add("arvore-postal-"+i,"Árvore postal "+(i+1),tr,kit.postalTrees[i],PropKind.Tree,1.5f);
            for(int i=0;i<kit.entranceTrees.Length;i++)Add("arvore-entrada-"+i,"Árvore "+(i==0?"violeta":"verde-água"),tr,kit.entranceTrees[i],PropKind.Tree,1.5f);
            if(kit.simpleNature.Length==6)
            {
                Add("pinheiro","Pinheiro",tr,kit.simpleNature[1],PropKind.Tree,1.5f);
                var titles=new[]{"Arbusto verde","Flores","Pedrinhas","Cerca"};
                for(int i=2;i<6;i++)Add("natureza-"+i,titles[i-2],p,kit.simpleNature[i],PropKind.Small,WorldMapPropSizing.Height(kit.simpleNature[i],PropKind.Small,kit));
            }
            for(int i=0;i<kit.groundDetails.Length;i++)Add("detalhe-chao-"+i,i==0?"Poça de água":"Abertura de lava",p,kit.groundDetails[i],PropKind.GroundDetail,.72f);
            Add("geiser","Gêiser",e,kit.geyserBase,PropKind.Geyser,1.12f);
            Add("vapor","Vapor",e,kit.geyserSteam,PropKind.Steam,.65f);
            Add("meteoro","Meteoro",e,kit.meteorSprite,PropKind.Meteor,.55f);
            Add("raio-de-sol","Raio de sol (luz)",e,LightIcon(),PropKind.LightShaft,1f);
            Add("pedras-baixas","Pedras baixas",p,kit.waterRocks,PropKind.GroundDetail,.38f);
            // New art dropped in a category folder becomes a separate library item automatically.
            foreach(WorldMapAssetCategory category in System.Enum.GetValues(typeof(WorldMapAssetCategory)))
                foreach(var guid in AssetDatabase.FindAssets("t:Sprite",new[]{ArtRoot+"/"+Folders[(int)category]}))
                    foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)).OfType<Sprite>())
                    {
                        if(Items(category).Any(i=>i.sprite==sprite))continue;
                        var name=sprite.name;var kind=category==b?PropKind.Landmark:category==tr?PropKind.Tree:category==c?PropKind.Character:category==e?PropKind.WaterRipple:name.Contains("grass")||name.Contains("puddle")||name.Contains("opening")||name.Contains("water-stones")?PropKind.GroundDetail:PropKind.Small;
                        var title=name.Contains("postal-dock")?"Cais postal":name.Contains("water-stones")?"Pedras baixas":name.Contains("letter-factory")?"Fábrica de cartas":name.Contains("parcel-depot")?"Depósito de encomendas":name.Contains("stamp-shop")?"Loja de selos":name.Contains("shrub-coral")?"Arbusto coral":name.Contains("shrub-violet")?"Arbusto violeta":name.Contains("grass-tuft")?"Graminha":name.Contains("water-ripple")?"Reflexo em V da água":name;
                        var size=category==b?(name.Contains("postal-dock")?1.45f:name.Contains("stamp-shop")?1.25f:2.1f):name.Contains("water-stones")?.38f:name.Contains("grass-tuft")?.24f:category==e?.30f:category==p?.58f:1.5f;
                        Add("arte-"+name,title,category,sprite,kind,size);
                    }
        }
    }
    public sealed class RoyalAvesMapLibraryImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(RoyalAvesMapLibrary.ArtRoot+"/"))return;
            var importer=(TextureImporter)assetImporter;
            if(importer.textureType==TextureImporterType.Sprite)return;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.filterMode=FilterMode.Trilinear;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
        }
    }
}
