using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Independent Sprite, conformed to the same floor as the lane instead of standing like a sign.
    public sealed class WorldMapGroundDetail : IDisposable
    {
        const int Columns=16,Rows=12;
        readonly GameObject root;
        readonly Mesh mesh;
        readonly MeshRenderer renderer;
        readonly Vector3[] vertices=new Vector3[(Columns+1)*(Rows+1)];
        readonly Func<Texture,Material> material;
        readonly MaterialPropertyBlock block=new MaterialPropertyBlock();
        Sprite shown;
        bool shownMirrored;
        float ratio;
        static Sprite glowSprite;
        public void Glow(PropPlacement p,WorldMapRoute route,Transform world,float cursor,float radius,Material glowMaterial,Vector2 footprint,Color color)
        {
            if(glowSprite==null)glowSprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),new Vector2(.5f,.5f),1);
            Tick(glowSprite,p,route,world,cursor,radius,false,glowMaterial,1,false,false,footprint,color);root.name="HaloNoTerreno";
        }
        public bool Active=>root!=null&&root.activeInHierarchy;
        public Bounds LocalBounds=>mesh.bounds;
        public WorldMapGroundDetail(Transform parent,int layer,Func<Texture,Material> material)
        {
            this.material=material;
            root=new GameObject("DetalheDoChao",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};
            root.transform.SetParent(parent,false);
            mesh=new Mesh{name="GroundDetailConformed",hideFlags=HideFlags.HideAndDontSave};mesh.MarkDynamic();
            var triangles=new int[Columns*Rows*6];
            for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++)
            {
                int i=y*(Columns+1)+x,k=(y*Columns+x)*6;
                triangles[k]=i;triangles[k+1]=i+Columns+1;triangles[k+2]=i+1;
                triangles[k+3]=i+1;triangles[k+4]=i+Columns+1;triangles[k+5]=i+Columns+2;
            }
            mesh.vertices=vertices;mesh.triangles=triangles;root.GetComponent<MeshFilter>().sharedMesh=mesh;
            renderer=root.GetComponent<MeshRenderer>();renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;
            block.SetFloat("_GroundedFoot",0);block.SetFloat("_Emission",.025f);renderer.SetPropertyBlock(block);
        }
        public void SetActive(bool active)=>root.SetActive(active);
        public void Tick(Sprite sprite,PropPlacement p,WorldMapRoute route,Transform world,float cursor,float radius,bool water=false,Material effectMaterial=null,float opacity=1,bool mirror=false,bool frozen=false,Vector2? footprint=null,Color? glow=null,float magicTime=0)
        {
            root.SetActive(true);
            if(shown!=sprite||shownMirrored!=mirror)
            {
                shown=sprite;shownMirrored=mirror;var bounds=WorldMapCards.VisibleBounds(sprite);ratio=bounds.width/Mathf.Max(.001f,bounds.height);
                var uv=new Vector2[vertices.Length];
                for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)
                {
                    float sx=Mathf.Lerp(bounds.xMin,bounds.xMax,mirror?1-x/(float)Columns:x/(float)Columns),sy=Mathf.Lerp(bounds.yMin,bounds.yMax,y/(float)Rows);
                    uv[y*(Columns+1)+x]=new Vector2((sx*sprite.pixelsPerUnit+sprite.pivot.x+sprite.rect.x)/sprite.texture.width,
                        (sy*sprite.pixelsPerUnit+sprite.pivot.y+sprite.rect.y)/sprite.texture.height);
                }
                mesh.uv=uv;
            }
            renderer.sharedMaterial=effectMaterial??material(sprite.texture);
            block.SetFloat("_GroundedFoot",0);block.SetFloat("_Emission",.025f);
            block.SetFloat("_FrozenPuddle",frozen?1:0);
            block.SetFloat("_MagicFlow",p.workMotion==WorldMapWorkMotion.InkStream?2:0);block.SetFloat("_MagicClock",magicTime);
            var region=sprite.rect;var texture=sprite.texture;
            block.SetVector("_IceRect",new Vector4(region.x/texture.width,region.y/texture.height,region.width/texture.width,region.height/texture.height));
            block.SetVector("_IceWorld",new Vector4(p.offset,p.s,p.height*ratio,p.height));
            block.SetColor("_Tint",new Color(1,1,1,opacity));renderer.SetPropertyBlock(block);
            if(glow.HasValue){block.SetColor("_Color",glow.Value);renderer.SetPropertyBlock(block);}
            for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)
            {
                float s=p.s+(y/(float)Rows-.5f)*(footprint?.y??p.height);
                float side=p.offset+(x/(float)Columns-.5f)*(footprint?.x??p.height*ratio);
                var point=MapCurve.Point(s-cursor,route.CenterX(s)+WorldMapTerrain.Lateral(side,s-cursor),
                    (water||route.InGap(s)?route.WaterHeight(s):WorldMapTerrain.SurfaceHeight(route,s,side))+.012f,radius,route.Curvature);
                vertices[y*(Columns+1)+x]=root.transform.InverseTransformPoint(world.TransformPoint(point));
            }
            mesh.vertices=vertices;mesh.RecalculateBounds();
        }
        public void Dispose(){Kill(mesh);Kill(root);}
        static void Kill(UnityEngine.Object value)
        {if(value==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(value);else UnityEngine.Object.DestroyImmediate(value);}
    }
}
