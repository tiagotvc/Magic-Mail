using System.Collections.Generic;
using UnityEngine;
using SweetSugar.Scripts.Level;
namespace RoyalAves.Effects
{
 public sealed class Soft3DElementEffect : MonoBehaviour
 {
  sealed class Part {public Transform tr;public Mesh mesh;public Vector3[] normals;public Vector3 origin,velocity,spin;public Color color;public float size;public bool liquid;}
  readonly List<Part> parts=new List<Part>();readonly List<Object> owned=new List<Object>();float age;float duration=1.15f;
  static readonly Vector3 Light=new Vector3(-.45f,.7f,-1).normalized;
  public static int ActiveBursts {get;private set;}
  T Own<T>(T o)where T:Object{owned.Add(o);return o;}
  public static Soft3DElementEffect Burst(SpriteRenderer source,Color color,int count=22,float scatter=1,bool liquid=false)
  {
   if(source==null||source.sprite==null)return null;
   var fx=Create(source);var rng=new System.Random(source.GetHashCode()+count*19);
   for(int i=0;i<count;i++)
   {
    float a=i*2.399963f+(float)rng.NextDouble()*.5f;float size=(liquid?.025f:.045f)+(float)rng.NextDouble()*(liquid?.025f:.065f);
    var p=new Part{size=size,liquid=liquid,color=Color.Lerp(color,Color.white,(float)rng.NextDouble()*.1f),origin=new Vector3(Mathf.Cos(a),Mathf.Sin(a),-.03f)*.15f,velocity=new Vector3(Mathf.Cos(a)*(.8f+(float)rng.NextDouble()*.4f),.65f+Mathf.Sin(a)*.8f,-.1f)*scatter,spin=new Vector3(100+i*13,120-i*4,80+i*17)};
    var go=new GameObject(liquid?"Rounded liquid drop":"Bevelled solid fragment");go.layer=source.gameObject.layer;go.transform.SetParent(fx.transform,false);p.tr=go.transform;p.mesh=fx.Own(liquid?Drop():Chunk());p.normals=p.mesh.normals;
    go.AddComponent<MeshFilter>().sharedMesh=p.mesh;var r=go.AddComponent<MeshRenderer>();
    r.sharedMaterial=Resources.Load<Material>(liquid?"Soft3D/Drop":"Soft3D/Solid");r.sortingLayerName=PowerUpEffectOrder.SortingLayer;r.sortingOrder=PowerUpEffectOrder.Value+2;
    fx.parts.Add(p);
   }
   fx.Sample(0);return fx;
  }
  static Soft3DElementEffect Create(SpriteRenderer source)
  {
   var go=new GameObject("Soft3DMaterialBurst");go.layer=source.gameObject.layer;var field=source.GetComponentInParent<FieldBoard>();go.transform.SetParent(field!=null?field.transform:source.transform.parent,false);go.transform.position=source.bounds.center+Vector3.back*.15f;
   var unit=Mathf.Max(source.bounds.size.x,source.bounds.size.y);var parent=go.transform.parent!=null?go.transform.parent.lossyScale:Vector3.one;go.transform.localScale=new Vector3(unit/Mathf.Abs(parent.x),unit/Mathf.Abs(parent.y),unit);
   ActiveBursts++;return go.AddComponent<Soft3DElementEffect>();
  }
  public static void Plank(SpriteRenderer source,int state)
  {
   if(source==null||source.sprite==null)return;var fx=Create(source);fx.duration=.72f;var s=source.sprite;var tr=new GameObject("Detached soft wood reinforcement").transform;tr.SetParent(fx.transform,false);tr.gameObject.layer=source.gameObject.layer;
   var vertices=new List<Vector3>{Vector3.zero};var uv=new List<Vector2>{new Vector2(s.rect.center.x/s.texture.width,s.rect.center.y/s.texture.height)};var tri=new List<int>();Vector2 axis=new Vector2(1,state==3?-1:1).normalized,normal=new Vector2(-axis.y,axis.x);const int n=32;
   for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,x=Mathf.Cos(a);Vector2 p=axis*(x*.13f+Mathf.Sign(x)*.32f)+normal*(Mathf.Sin(a)*.13f);vertices.Add(p);uv.Add(new Vector2((s.rect.x+(p.x+.5f)*s.rect.width)/s.texture.width,(s.rect.y+(p.y+.5f)*s.rect.height)/s.texture.height));}
   for(int i=0;i<n;i++)tri.AddRange(new[]{0,i+1,(i+1)%n+1});var m=fx.Own(new Mesh());m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(tri,0);m.RecalculateBounds();var normals=new Vector3[vertices.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.back;m.normals=normals;
   tr.gameObject.AddComponent<MeshFilter>().sharedMesh=m;var mat=fx.Own(new Material(Resources.Load<Material>("Soft3D/Sprite")));mat.mainTexture=s.texture;var r=tr.gameObject.AddComponent<MeshRenderer>();r.sharedMaterial=mat;r.sortingLayerName=PowerUpEffectOrder.SortingLayer;r.sortingOrder=PowerUpEffectOrder.Value+2;
   fx.parts.Add(new Part{tr=tr,mesh=m,normals=normals,size=1,color=Color.white,velocity=new Vector3(state==3?.4f:-.4f,.15f,-.1f),spin=new Vector3(25,15,state==3?65:-65)});fx.Sample(0);
  }
  static Mesh Chunk()
  {
   var v=new List<Vector3>();var ns=new List<Vector3>();var tri=new List<int>();
   System.Action<Vector3,Vector3,Vector3,Vector3> face=(a,b,c,n)=>{int k=v.Count;v.Add(a);v.Add(b);v.Add(c);ns.Add(n);ns.Add(n);ns.Add(n);tri.AddRange(new[]{k,k+1,k+2});};
   var front=new Vector3[6];var rim=new Vector3[6];var back=new Vector3[6];
   for(int i=0;i<6;i++){float a=(i*60+15)*Mathf.Deg2Rad;float r=i%2==0?1:.85f;rim[i]=new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,0);front[i]=rim[i]*.72f+Vector3.back*.28f;back[i]=rim[i]*.8f+Vector3.forward*.35f;}
   for(int i=0;i<6;i++){int j=(i+1)%6;face(Vector3.back*.28f,front[j],front[i],Vector3.back);var n=((front[i]+front[j])*.5f+Vector3.back).normalized;face(front[i],front[j],rim[j],n);face(front[i],rim[j],rim[i],n);n=((rim[i]+rim[j])*.5f).normalized;face(rim[i],rim[j],back[j],n);face(rim[i],back[j],back[i],n);face(Vector3.forward*.35f,back[i],back[j],Vector3.forward);}
   var m=new Mesh();m.SetVertices(v);m.SetNormals(ns);m.SetTriangles(tri,0);m.RecalculateBounds();return m;
  }
  static Mesh Drop()
  {
   var v=new List<Vector3>{Vector3.back*.3f};var ns=new List<Vector3>{Vector3.back};var tri=new List<int>();const int n=24;
   for(int ring=1;ring<=3;ring++)for(int i=0;i<n;i++){float a=i*Mathf.PI*2/n,r=ring/3f;v.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,-Mathf.Sqrt(1-r*r)*.3f));ns.Add(new Vector3(Mathf.Cos(a)*r,Mathf.Sin(a)*r,-Mathf.Sqrt(1-r*r)).normalized);}
   for(int i=0;i<n;i++)tri.AddRange(new[]{0,i+1,(i+1)%n+1});
   for(int ring=0;ring<2;ring++)for(int i=0;i<n;i++){int a=1+ring*n+i,b=1+ring*n+(i+1)%n,c=a+n,d=b+n;tri.AddRange(new[]{a,c,d,a,d,b});}
   var m=new Mesh();m.SetVertices(v);m.SetNormals(ns);m.SetTriangles(tri,0);m.RecalculateBounds();return m;
  }
  public void Sample(float t)
  {
   foreach(var p in parts)
   {
    p.tr.localPosition=p.origin+p.velocity*t+Vector3.down*1.4f*t*t;p.tr.localRotation=p.liquid?Quaternion.identity:Quaternion.Euler(p.spin*t);float fade=1-Mathf.Clamp01((t-duration*.65f)/(duration*.35f));p.tr.localScale=Vector3.one*p.size*fade;if(p.liquid)p.tr.localScale=new Vector3(p.size*fade,p.size*(1+.6f*t)*fade,1);
    var c=new Color[p.normals.Length];for(int k=0;k<c.Length;k++){float shade=.55f+.45f*Mathf.Max(0,Vector3.Dot((p.liquid?Quaternion.identity:p.tr.localRotation)*p.normals[k],Light));c[k]=new Color(p.color.r*shade,p.color.g*shade,p.color.b*shade,1);}p.mesh.colors=c;
   }
  }
  void Update(){age+=Time.deltaTime;Sample(age);if(age>=duration)Destroy(gameObject);}
  void OnDestroy(){ActiveBursts--;foreach(var o in owned)if(o!=null){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}}
 }
}
