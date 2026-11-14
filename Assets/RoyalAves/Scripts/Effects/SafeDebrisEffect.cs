using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Same independent ballistic motion as MaterialBurstEffect, with source-textured metal
    // instead of wood shapes. Never advances an animation sheet or changes player currency.
    public sealed class SafeDebrisEffect : MonoBehaviour
    {
        sealed class Piece
        {
            public Transform node;
            public Vector3 origin, velocity, tumble;
            public float spin, phase;
            public bool coin;
        }
        readonly List<Piece> pieces = new List<Piece>();
        readonly List<Mesh> meshes = new List<Mesh>();
        Material material;
        float unit, age;
        public int PieceCount => pieces.Count;
        public float Lifetime => 2.6f;
        public bool Automatic { get; set; } = true;

        public static SafeDebrisEffect Metal(SpriteRenderer source, Rect region, int count, int seed)
        {
            var effect=Create(source,"SafeMetalFragments");
            effect.material=new Material(source.sharedMaterial) { mainTexture=source.sprite.texture };
            var random=new System.Random(seed);
            var seeds=new List<Vector2>();
            for(int i=0;i<count;i++) seeds.Add(new Vector2(
                Mathf.Lerp(region.xMin,region.xMax,((i%3)+.2f+(float)random.NextDouble()*.6f)/3),
                Mathf.Lerp(region.yMin,region.yMax,((i/3)+.2f+(float)random.NextDouble()*.6f)/Mathf.Ceil(count/3f))));
            for(int i=0;i<count;i++)
            {
                var polygon=new List<Vector2> {new Vector2(region.xMin,region.yMin),new Vector2(region.xMax,region.yMin),
                    new Vector2(region.xMax,region.yMax),new Vector2(region.xMin,region.yMax)};
                for(int j=0;j<count && polygon.Count>0;j++)
                {
                    if(i==j)continue;
                    Vector2 normal=seeds[j]-seeds[i], midpoint=(seeds[j]+seeds[i])*.5f;
                    var clipped=new List<Vector2>();
                    for(int k=0;k<polygon.Count;k++)
                    {
                        Vector2 a=polygon[k], b=polygon[(k+1)%polygon.Count];
                        float da=Vector2.Dot(a-midpoint,normal),db=Vector2.Dot(b-midpoint,normal);
                        if(da<=0)clipped.Add(a);
                        if((da<=0)!=(db<=0))clipped.Add(Vector2.Lerp(a,b,da/(da-db)));
                    }
                    polygon=clipped;
                }
                if(polygon.Count<3)continue;
                effect.AddMetal(source,polygon,random);
            }
            effect.Sample(0);return effect;
        }

        static SafeDebrisEffect Create(SpriteRenderer source,string name)
        {
            var root=new GameObject(name);root.layer=source.gameObject.layer;
            // Sibling of the visual, so disabling the source renderer does not hide debris.
            root.transform.SetParent(source.transform.parent,false);
            root.transform.localPosition=source.transform.localPosition;
            root.transform.localRotation=source.transform.localRotation;
            root.transform.localScale=source.transform.localScale;
            var effect=root.AddComponent<SafeDebrisEffect>();
            effect.unit=Mathf.Max(source.sprite.bounds.size.x,source.sprite.bounds.size.y);
            return effect;
        }
        Vector3 Local(Sprite sprite,Vector2 uv) => new Vector3(
            (uv.x*sprite.rect.width-sprite.pivot.x)/sprite.pixelsPerUnit,
            (uv.y*sprite.rect.height-sprite.pivot.y)/sprite.pixelsPerUnit);
        void AddMetal(SpriteRenderer source,List<Vector2> polygon,System.Random random)
        {
            var sprite=source.sprite;
            Vector2 center=Vector2.zero;foreach(var p in polygon)center+=p;center/=polygon.Count;
            var origin=Local(sprite,center);
            var vertices=new List<Vector3>();var uv=new List<Vector2>();
            var colors=new List<Color>();var indices=new List<int>();
            float depth=unit*(.018f+(float)random.NextDouble()*.018f);
            Vector2 UV(Vector2 p) => new Vector2((sprite.rect.x+p.x*sprite.rect.width)/sprite.texture.width,
                (sprite.rect.y+p.y*sprite.rect.height)/sprite.texture.height);
            int Add(Vector3 position,Vector2 tex,Color tint)
            {
                int index=vertices.Count;vertices.Add(position);uv.Add(tex);colors.Add(tint);return index;
            }
            // Back and extruded edge faces are real geometry, not a flat paper polygon.
            for(int face=0;face<2;face++)
            {
                float z=face==0?depth:-depth;
                Color tint=face==0?source.color*new Color(.6f,.75f,.8f,1):source.color;
                int first=Add(new Vector3(0,0,z),UV(center),tint);
                foreach(var p in polygon)Add(Local(sprite,p)-origin+Vector3.forward*z,UV(p),tint);
                for(int i=0;i<polygon.Count;i++)indices.AddRange(new[]{first,first+1+i,first+1+(i+1)%polygon.Count});
            }
            for(int i=0;i<polygon.Count;i++)
            {
                Vector2 a=polygon[i],b=polygon[(i+1)%polygon.Count];
                Vector3 pa=Local(sprite,a)-origin,pb=Local(sprite,b)-origin;
                Vector2 normal=new Vector2(b.y-a.y,a.x-b.x).normalized;
                float light=Mathf.Lerp(.38f,.85f,Vector2.Dot(normal,new Vector2(-.6f,.8f))*.5f+.5f);
                Color tint=new Color(light,light,light,1)*source.color;
                int first=Add(pa-Vector3.forward*depth,UV(center),tint);
                Add(pb-Vector3.forward*depth,UV(center),tint);Add(pb+Vector3.forward*depth,UV(center),tint);Add(pa+Vector3.forward*depth,UV(center),tint);
                indices.AddRange(new[]{first,first+1,first+2,first,first+2,first+3});
            }
            var mesh=new Mesh{name="SafeSolidMetalChunk"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();
            var node=Node(mesh,source,"MetalPiece");
            Vector2 direction=(center-Vector2.one*.5f).normalized;
            pieces.Add(new Piece { node=node,origin=origin,
                velocity=new Vector3(direction.x*(1.5f+(float)random.NextDouble()),direction.y*.8f+1.5f)*unit,
                spin=((float)random.NextDouble()*2-1)*420,
                tumble=new Vector3(100+(float)random.NextDouble()*200,120+(float)random.NextDouble()*180,0) });
        }
        Transform Node(Mesh mesh,SpriteRenderer source,string name)
        {
            meshes.Add(mesh);var go=new GameObject(name);go.layer=gameObject.layer;
            go.transform.SetParent(transform,false);go.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder+3;
            return go.transform;
        }

        public static SafeDebrisEffect Coins(SpriteRenderer source,int count=18)
        {
            var effect=Create(source,"SafeScatteredCoins");
            effect.material=new Material(Shader.Find("Sprites/Default")) {mainTexture=Texture2D.whiteTexture};
            var random=new System.Random(48271);
            for(int i=0;i<count;i++)
            {
                const int sides=20;
                var vertices=new Vector3[1+sides*2];var colors=new Color[vertices.Length];var indices=new List<int>();
                float radius=effect.unit*(.045f+(float)random.NextDouble()*.02f);
                colors[0]=new Color(1,.76f,.14f);
                for(int k=0;k<sides;k++)
                {
                    float a=k*Mathf.PI*2/sides;var p=new Vector3(Mathf.Cos(a),Mathf.Sin(a))*radius;
                    vertices[1+k]=p*.77f;vertices[1+sides+k]=p;
                    colors[1+k]=new Color(1,.76f,.14f);
                    colors[1+sides+k]=Mathf.Sin(a)>.1f?new Color(1,.96f,.48f):new Color(.85f,.41f,.025f);
                    int n=(k+1)%sides;
                    indices.AddRange(new[]{0,1+k,1+n,1+k,1+sides+k,1+sides+n,1+k,1+sides+n,1+n});
                }
                var mesh=new Mesh{name="SafeCoin",vertices=vertices,colors=colors,triangles=indices.ToArray()};
                var node=effect.Node(mesh,source,"FlyingCoin");
                var direction=new Vector2(Mathf.Cos(i*2.399963f),Mathf.Sin(i*2.399963f));
                effect.pieces.Add(new Piece {node=node,coin=true,origin=new Vector3(direction.x*.15f,-.15f)*effect.unit,
                    velocity=new Vector3(direction.x*(1.4f+(float)random.NextDouble()*1.8f),
                        1.7f+(float)random.NextDouble()*2.2f)*effect.unit,
                    spin=((float)random.NextDouble()*2-1)*600,phase=(float)random.NextDouble()*6});
            }
            effect.Sample(0);return effect;
        }
        public void Sample(float time)
        {
            foreach(var p in pieces)
            {
                p.node.gameObject.SetActive(time>=0 && time<Lifetime);
                if(time<0)continue;
                p.node.localPosition=p.origin+p.velocity*time+Vector3.down*(unit*3.8f*time*time);
                p.node.localRotation=Quaternion.Euler(p.tumble*time+Vector3.forward*(p.spin*time));
                p.node.localScale=p.coin?new Vector3(.18f+.82f*Mathf.Abs(Mathf.Cos(time*12+p.phase)),1,1):Vector3.one;
            }
        }
        void Update() { if(!Automatic)return;age+=Time.deltaTime;Sample(age);if(age>=Lifetime)Destroy(gameObject); }
        static void Release(Object value) { if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value); }
        void OnDestroy() {foreach(var mesh in meshes)Release(mesh);Release(material);}
    }
}
