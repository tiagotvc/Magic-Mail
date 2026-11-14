using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Solid, flat-shaded rock fragments, not a cut-up copy of the source sprite.
    public sealed class StoneShatterEffect : MonoBehaviour
    {
        sealed class Chunk
        {
            public Vector3 origin, velocity, spin, rotation;
            public Vector3[] points;
            public int[] triangles;
            public Color tint;
            public bool porcelainCore;
            public int glazedFaces;
        }
        struct Face
        {
            public Vector3 a, b, c;
            public Color color;
            public float depth;
        }
        readonly List<Chunk> chunks = new List<Chunk>(32);
        readonly List<Face> faces = new List<Face>(1000);
        readonly List<Vector3> vertices = new List<Vector3>(3000);
        readonly List<Color> colors = new List<Color>(3000);
        readonly List<int> triangles = new List<int>(3000);
        static readonly Vector3 Light = new Vector3(-.45f,.75f,-1).normalized;
        Mesh mesh;
        Material material, dustMaterial;
        Texture2D dustTexture;
        SpriteRenderer impact;
        float elapsed, speed, unit, bottom;
        bool porcelain, chipOnly;
        public static int PorcelainChipBursts { get; private set; }
        public static int PorcelainBreakBursts { get; private set; }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPorcelainCounters() { PorcelainChipBursts=PorcelainBreakBursts=0; }
        public bool IsPorcelain => porcelain;
        public bool IsChipOnly => chipOnly;
        public float AnimationTime => elapsed;
        public float CrackDuration => .025f;
        public float Duration => 3f;
        public int ShardCount => chunks.Count;

        public static StoneShatterEffect Play(SpriteRenderer source, float speed = 1f, float scatter = 1f)
            => Create(source,speed,scatter,false,false);

        public static StoneShatterEffect PlayPorcelain(SpriteRenderer source, bool chipOnly, float scatter = 1f)
            => Create(source,1f,scatter,true,chipOnly);

        static StoneShatterEffect Create(SpriteRenderer source, float speed, float scatter, bool porcelain, bool chipOnly)
        {
            if (source == null || source.sprite == null) return null;
            var root = new GameObject(porcelain ? chipOnly ? "InkwellPorcelainChip" : "InkwellPorcelainShatter" : "StoneShatterFX");
            if(porcelain)root.layer=source.gameObject.layer;
            var field = source.GetComponentInParent<SweetSugar.Scripts.Level.FieldBoard>();
            root.transform.SetParent(field != null ? field.transform : source.transform.parent, false);
            root.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            var parent = root.transform.parent != null ? root.transform.parent.lossyScale : Vector3.one;
            root.transform.localScale = new Vector3(source.transform.lossyScale.x / parent.x,
                source.transform.lossyScale.y / parent.y, 1);
            var effect = root.AddComponent<StoneShatterEffect>();
            effect.porcelain=porcelain;effect.chipOnly=chipOnly;
            if(porcelain){if(chipOnly)PorcelainChipBursts++;else PorcelainBreakBursts++;}
            effect.speed = Mathf.Clamp(speed,.1f,3f);
            effect.unit = Mathf.Max(source.sprite.bounds.size.x,source.sprite.bounds.size.y);
            effect.bottom = Camera.main != null
                ? Camera.main.ViewportToWorldPoint(new Vector3(.5f,0,Camera.main.WorldToScreenPoint(root.transform.position).z)).y - 2
                : root.transform.position.y - 20;
            effect.Build(source,Mathf.Clamp(scatter,.2f,2));
            effect.Sample(0);
            return effect;
        }

        void Build(SpriteRenderer source, float scatter)
        {
            mesh = new Mesh { name = "SolidStoneFragments" }; mesh.MarkDynamic();
            material = new Material(Shader.Find("Sprites/Default"));
            material.mainTexture = Texture2D.whiteTexture;
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
            renderer.sortingLayerName=PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder=PowerUpEffectOrder.Value+2;
            var flash=new GameObject("StoneImpact");flash.transform.SetParent(transform,false);
            impact=flash.AddComponent<SpriteRenderer>();impact.sprite=source.sprite;impact.sharedMaterial=source.sharedMaterial;
            impact.color=source.color;impact.flipX=source.flipX;impact.flipY=source.flipY;
            impact.sortingLayerName=renderer.sortingLayerName;impact.sortingOrder=renderer.sortingOrder+1;

            // Deliberately no lattice, Voronoi UVs, rectangular tiles or surviving paw mosaic.
            var inkwell=porcelain?source.GetComponent<RoyalAves.Gameplay.InkwellCell>():null;
            Vector3 porcelainCenter=inkwell!=null?inkwell.BodyCenterLocal:source.sprite.bounds.center;
            int count=porcelain?(chipOnly?1:22):30;
            for(int i=0;i<count;i++)
            {
                bool large=i<7, grit=i>=19;
                float radius=unit*(large?Random.Range(.075f,.115f):grit?Random.Range(.012f,.027f):Random.Range(.035f,.065f));
                if(porcelain)radius=unit*(chipOnly?Random.Range(.025f,.043f):i<8?Random.Range(.06f,.095f):Random.Range(.025f,.05f));
                float direction=i*2.399963f+Random.Range(-.5f,.5f);
                var outward=new Vector3(Mathf.Cos(direction),Mathf.Sin(direction),0);
                var center=new Vector3(Random.Range(-.32f,.32f),Random.Range(-.32f,.32f),0)*unit;
                var chunk=porcelain?CreatePorcelainChunk(radius,Random.Range(4,7)):CreateChunk(radius,grit?4:Random.Range(5,8));
                if(porcelain)
                {
                    center+=porcelainCenter;
                    if(chipOnly)center=porcelainCenter+new Vector3(unit*.23f,unit*.17f,0);
                }
                chunk.origin=center;
                chunk.velocity=(outward*Random.Range(1.2f,2.7f)+Vector3.up*Random.Range(1.1f,2.1f))*unit*scatter;
                chunk.velocity.z=Random.Range(-.12f,.12f)*unit;
                if(chipOnly)chunk.velocity=new Vector3(Random.Range(.6f,1.1f),Random.Range(.9f,1.4f),-.03f)*unit;
                chunk.spin=new Vector3(Random.Range(-360,360),Random.Range(-340,340),Random.Range(-440,440));
                chunk.rotation=new Vector3(Random.Range(-35,35),Random.Range(-35,35),Random.Range(0,360));
                float shade=Random.Range(.94f,1.08f);
                chunk.tint=porcelain?new Color(.43f*shade,.035f*shade,.8f*shade,1):new Color(.66f*shade,.65f*shade,.72f*shade,1);
                if(porcelain && !chipOnly && i>=18){chunk.tint=new Color(1,.68f,.12f,1);chunk.porcelainCore=false;}
                chunks.Add(chunk);
            }
            if(!chipOnly)AddDust();
        }

        static Chunk CreatePorcelainChunk(float radius,int count)
        {
            var points=new Vector3[count*2+2];
            float thickness=radius*.20f;
            points[0]=new Vector3(0,0,-radius*.24f);
            points[points.Length-1]=new Vector3(0,0,thickness);
            for(int i=0;i<count;i++)
            {
                float angle=(i+Random.Range(-.13f,.13f))*Mathf.PI*2/count;
                float r=radius*Random.Range(.8f,1.1f);
                var edge=new Vector3(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r*.8f,0);
                points[i+1]=edge;points[i+1+count]=edge+Vector3.forward*thickness;
            }
            var indexes=new List<int>();
            // Keep glazed front faces first, then exposed white porcelain at the rim/back.
            for(int i=0;i<count;i++){indexes.Add(0);indexes.Add((i+1)%count+1);indexes.Add(i+1);}
            for(int i=0;i<count;i++)
            {
                int a=i+1,b=(i+1)%count+1,c=a+count,d=b+count;
                indexes.AddRange(new[]{a,b,d,a,d,c,points.Length-1,c,d});
            }
            return new Chunk{points=points,triangles=indexes.ToArray(),porcelainCore=true,glazedFaces=count};
        }

        // An uneven beveled rock: raised front ridge, broad broken sides and a back face.
        // The whole solid rotates in three axes so facets change lighting in flight.
        static Chunk CreateChunk(float radius, int count)
        {
            var points=new Vector3[count*2+2];
            float depth=radius*Random.Range(.65f,1.1f);
            points[0]=new Vector3(radius*Random.Range(-.15f,.15f),radius*.1f,-depth);
            points[points.Length-1]=new Vector3(0,0,depth*.65f);
            float aspect=Random.Range(.7f,1.2f);
            for(int i=0;i<count;i++)
            {
                float angle=(i+Random.Range(-.18f,.18f))*Mathf.PI*2/count;
                float r=radius*Random.Range(.78f,1.13f);
                float x=Mathf.Cos(angle)*r*aspect, y=Mathf.Sin(angle)*r;
                points[i+1]=new Vector3(x,y,-depth*Random.Range(.2f,.55f));
                points[i+1+count]=new Vector3(x*.73f+radius*.09f,y*.78f-radius*.12f,depth*.6f);
            }
            var indexes=new List<int>(count*12);
            void Triangle(int a,int b,int c) {indexes.Add(a);indexes.Add(b);indexes.Add(c);}
            for(int i=0;i<count;i++)
            {
                int front=i+1,next=(i+1)%count+1,back=front+count,backNext=next+count;
                Triangle(0,next,front);
                Triangle(front,next,backNext);Triangle(front,backNext,back);
                Triangle(points.Length-1,back,backNext);
            }
            return new Chunk{points=points,triangles=indexes.ToArray()};
        }

        public void Sample(float time)
        {
            float travel=Mathf.Max(0,time-CrackDuration);
            impact.enabled=!chipOnly && time<CrackDuration;
            faces.Clear();vertices.Clear();colors.Clear();triangles.Clear();
            if(time>=CrackDuration)
            foreach(var chunk in chunks)
            {
                var rotation=Quaternion.Euler(chunk.rotation+chunk.spin*travel);
                var center=chunk.origin+chunk.velocity*travel+Vector3.down*(unit*6.2f*travel*travel);
                if(transform.TransformPoint(center).y<bottom)continue;
                for(int i=0;i<chunk.triangles.Length;i+=3)
                {
                    Vector3 a=rotation*chunk.points[chunk.triangles[i]],b=rotation*chunk.points[chunk.triangles[i+1]],c=rotation*chunk.points[chunk.triangles[i+2]];
                    var normal=Vector3.Cross(b-a,c-a).normalized;
                    // Camera faces along +Z; only forward-facing solid surfaces are needed.
                    if(normal.z>=0)continue;
                    float light=Mathf.Clamp01(Vector3.Dot(normal,Light));
                    Color faceColor=chunk.porcelainCore && i>=chunk.glazedFaces*3 ? new Color(.94f,.88f,.81f,1) : chunk.tint;
                    Color tint=Color.Lerp(faceColor*(porcelain?.62f:.48f),Color.Lerp(faceColor,Color.white,.37f),light);
                    tint.a=1;
                    a+=center;b+=center;c+=center;
                    faces.Add(new Face{a=a,b=b,c=c,color=tint,depth=(a.z+b.z+c.z)/3});
                }
            }
            faces.Sort((a,b)=>b.depth.CompareTo(a.depth));
            foreach(var face in faces)
            {
                int first=vertices.Count;
                vertices.Add(face.a);vertices.Add(face.b);vertices.Add(face.c);
                colors.Add(face.color);colors.Add(face.color);colors.Add(face.color);
                triangles.Add(first);triangles.Add(first+1);triangles.Add(first+2);
            }
            mesh.Clear();mesh.SetVertices(vertices);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateBounds();
        }

        void AddDust()
        {
            dustTexture=new Texture2D(32,32,TextureFormat.RGBA32,false);
            var pixels=new Color[1024];
            for(int y=0;y<32;y++)for(int x=0;x<32;x++)
            {
                float distance=Vector2.Distance(new Vector2(x+.5f,y+.5f),new Vector2(16,16))/16;
                pixels[y*32+x]=new Color(1,1,1,Mathf.Pow(Mathf.Clamp01(1-distance),2));
            }
            dustTexture.SetPixels(pixels);dustTexture.Apply();
            dustMaterial=new Material(Shader.Find("Sprites/Default"));dustMaterial.mainTexture=dustTexture;
            var root=new GameObject("StoneDust");root.transform.SetParent(transform,false);
            var system=root.AddComponent<ParticleSystem>();system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            var main=system.main;main.loop=false;main.duration=.12f;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.15f,.3f);
            main.startSpeed=new ParticleSystem.MinMaxCurve(unit*.7f,unit*1.7f);
            main.startSize=new ParticleSystem.MinMaxCurve(unit*.14f,unit*.3f);
            main.startColor=porcelain?new Color(.9f,.83f,.77f,.35f):new Color(.76f,.74f,.8f,.5f);main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.simulationSpeed=speed;main.maxParticles=20;
            var emission=system.emission;emission.rateOverTime=0;emission.SetBursts(new[]{new ParticleSystem.Burst(.02f,14)});
            var shape=system.shape;shape.shapeType=ParticleSystemShapeType.Circle;shape.radius=unit*.24f;
            var overLife=system.colorOverLifetime;overLife.enabled=true;
            var gradient=new Gradient();gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(0,1)});overLife.color=gradient;
            var renderer=root.GetComponent<ParticleSystemRenderer>();renderer.sharedMaterial=dustMaterial;
            renderer.sortingLayerName=PowerUpEffectOrder.SortingLayer;renderer.sortingOrder=PowerUpEffectOrder.Value+3;
            system.Play();
        }

        void Update()
        {
            elapsed+=Time.deltaTime*speed;Sample(elapsed);
            if(elapsed>=Duration || elapsed>.4f && faces.Count==0)Destroy(gameObject);
        }
        void OnDestroy()
        {
            if(Application.isPlaying){Destroy(mesh);Destroy(material);Destroy(dustMaterial);Destroy(dustTexture);}
            else{DestroyImmediate(mesh);DestroyImmediate(material);DestroyImmediate(dustMaterial);DestroyImmediate(dustTexture);}
        }
    }
}
