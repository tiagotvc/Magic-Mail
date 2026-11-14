using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace RoyalAves.EditorTools
{
 // An isolated art-review scene. No production prefab, goal, campaign or player state is loaded.
 public sealed class Soft3DAnimationLaboratory : EditorWindow
 {
  Soft3DPreview preview; double start,lastFrame; bool running=true,reported; float time;
  [MenuItem("Royal Aves/Elementos/Remodelagem 3D/Previa das seis animacoes")]
  public static void Open(){var w=GetWindow<Soft3DAnimationLaboratory>();w.titleContent=new GUIContent("Elementos 3D");w.minSize=new Vector2(720,540);w.Show();}
  void OnEnable(){start=EditorApplication.timeSinceStartup;EditorApplication.update+=Tick;}
  void OnDisable(){EditorApplication.update-=Tick;preview?.Dispose();preview=null;}
  void Tick(){if(running&&EditorApplication.timeSinceStartup-lastFrame>=1d/30){lastFrame=EditorApplication.timeSinceStartup;time=(float)(lastFrame-start)%7;Repaint();}}
  void OnGUI()
  {
   GUILayout.Label("SEIS ELEMENTOS · PRÉVIA DE ACABAMENTO",EditorStyles.boldLabel);
   GUILayout.Label("Estudo visual isolado — prefabs e regras da partida preservados.");
   using(new EditorGUILayout.HorizontalScope())
   {
    if(GUILayout.Button(running?"Pausar":"Reproduzir",GUILayout.Width(100))){running=!running;start=EditorApplication.timeSinceStartup-time;}
    if(GUILayout.Button("Reiniciar",GUILayout.Width(100))){time=0;start=EditorApplication.timeSinceStartup;}
    var next=GUILayout.HorizontalSlider(time,0,6.99f);if(Mathf.Abs(next-time)>.03f){time=next;start=EditorApplication.timeSinceStartup-time;}
   }
   try{if(preview==null)preview=new Soft3DPreview();if(Event.current.type==EventType.Repaint){preview.Sample(time);preview.camera.Render();if(!reported){reported=true;File.WriteAllText("Logs/soft3d-animation-preview.result","RENDERED: isolated native six-element preview; play, pause and scrub available");}}var rect=GUILayoutUtility.GetAspectRect(1.5f);GUI.DrawTexture(rect,preview.target,ScaleMode.ScaleToFit,false);}
   catch(Exception e){EditorGUILayout.HelpBox(e.Message,MessageType.Error);}
  }
  [InitializeOnLoadMethod]static void Listen(){EditorApplication.update+=Poll;}
  static void Poll(){const string f="Logs/soft3d-animation-preview.request";if(!File.Exists(f)||EditorApplication.isCompiling||EditorApplication.isUpdating)return;File.Delete(f);Open();File.WriteAllText("Logs/soft3d-animation-preview.result","OPEN: isolated native six-element preview");}
  public static void Export()
  {
   var folder=Path.GetFullPath(Path.Combine(Application.dataPath,"../../.."));
   if(!Directory.Exists(Path.Combine(folder,"Tools/royal-postman")))folder=Path.GetFullPath(".");
   var outDir=Path.Combine(folder,"Tools/royal-postman/previews/elementos-soft3d-animacao-v1/native");Directory.CreateDirectory(outDir);
   using(var p=new Soft3DPreview(folder))
   {
    var tex=new Texture2D(1200,800,TextureFormat.RGB24,false);
    p.Sample(0);p.camera.Render(); // Upload all newly loaded sprite textures before the first recorded frame.
    for(int i=0;i<168;i++){p.Sample(i/24f);p.camera.Render();RenderTexture.active=p.target;tex.ReadPixels(new Rect(0,0,1200,800),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(outDir,"frame-"+i.ToString("0000")+".png"),tex.EncodeToPNG());}
    RenderTexture.active=null;DestroyImmediate(tex);
   }
   File.WriteAllText(Path.Combine(outDir,"result.txt"),"PASS: 168 native Unity art-preview frames, 24fps, six isolated actors; no gameplay validation implied.");
  }
 }

 internal sealed class Soft3DPreview : IDisposable
 {
  readonly Scene scene; public Camera camera; public RenderTexture target;
  readonly List<UnityEngine.Object> owned=new List<UnityEngine.Object>();
  readonly Transform[] roots=new Transform[6]; readonly SpriteRenderer[] bodies=new SpriteRenderer[6];
  readonly List<Particle> particles=new List<Particle>();
  readonly Sprite[] crate,ink,bag,scroll,bottles;
  readonly SpriteRenderer[] flasks=new SpriteRenderer[4],letters=new SpriteRenderer[3],pips=new SpriteRenderer[3];
  SpriteRenderer signalGlyph; Sprite[] signalPieces;
  readonly Material plain,solid; Material lens; readonly Color[] colors={new Color(.35f,.86f,.12f),new Color(.64f,.19f,.94f),new Color(.96f,.13f,.26f),new Color(.04f,.65f,1)};
  sealed class Particle {public Transform tr;public Mesh mesh;public Vector3[] normals;public Color color;public Vector3 origin,velocity,spin;public float birth,life,size;public int actor;public bool liquid;}
  string folder;
  public Soft3DPreview(string project=null)
  {
   folder=Path.Combine(project??Path.GetFullPath("."),"Tools/royal-postman/previews");
   scene=EditorSceneManager.NewPreviewScene();
   plain=Own(new Material(Shader.Find("RoyalAves/Soft3DPreviewLens")));
   solid=Own(new Material(Shader.Find("RoyalAves/Soft3DPreviewSolid")));
   var go=New("Preview camera");camera=go.AddComponent<Camera>();camera.scene=scene;camera.orthographic=true;camera.orthographicSize=3.65f;camera.transform.position=new Vector3(0,0,-20);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.83f,.90f,.94f);camera.enabled=false;camera.allowHDR=false;
   target=Own(new RenderTexture(1200,800,24));target.Create();camera.targetTexture=target;
   string[] labels={"Caixa de madeira","Pedra rúnica","Tinteiro","Poções","Bolsa do carteiro","Sinalizador"};
   for(int a=0;a<6;a++)
   {
    roots[a]=New(labels[a]).transform;roots[a].position=new Vector3((a%3-1)*3.5f,a<3?1.55f:-1.8f,0);
    var title=New("Label",roots[a]).AddComponent<TextMesh>();title.text=labels[a];title.fontSize=40;title.characterSize=.055f;title.anchor=TextAnchor.MiddleCenter;title.color=new Color(.18f,.25f,.31f);title.transform.localPosition=new Vector3(0,1.38f,0);
   }
   crate=Sheet("crate-states",3,1);ink=Sheet("inkwell-states",5,1);bag=Sheet("bag-opening",4,4);scroll=Sheet("scroll-opening",4,1);bottles=Sheet("potion-bottles",2,2);
   bodies[0]=Sprite(roots[0],crate[2],2.1f);bodies[1]=Sprite(roots[1],Master("02-pedra-runica"),2.1f);bodies[2]=Sprite(roots[2],ink[0],2.1f);
   bodies[3]=Sprite(roots[3],Sheet("potion-rack",1,1)[0],2.65f);
   for(int i=0;i<4;i++){flasks[i]=Sprite(roots[3],bottles[i],1.05f);flasks[i].transform.localPosition=new Vector3(i%2==0?-.58f:.58f,i<2?.50f:-.62f,-.1f);}
   bodies[4]=Sprite(roots[4],bag[0],2.35f);bodies[4].transform.localPosition=Vector3.down*.96f;
   for(int i=0;i<3;i++){letters[i]=Sprite(roots[4],scroll[0],.6f);letters[i].sortingOrder=5;}
   bodies[5]=Sprite(roots[5],Master("06-sinalizador"),2.05f);
   var signalPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SweetSugar/Resources/Blocks/SortingSignals.prefab");
   if(signalPrefab!=null)
   {
    var signal=signalPrefab.GetComponent<RoyalAves.Gameplay.SortingSignalCell>();
    if(signal!=null){signalPieces=signal.requestedPieces;signalGlyph=Sprite(roots[5],signalPieces[1],1);signalGlyph.transform.localPosition=new Vector3(0,.1f,-.2f);signalGlyph.sortingOrder=5;}
   }
   var shader=Shader.Find("RoyalAves/Soft3DPreviewLens");if(shader!=null){lens=Own(new Material(shader));bodies[5].sharedMaterial=lens;}
   for(int i=0;i<3;i++){pips[i]=Sprite(roots[5],Disc(),.16f);pips[i].color=new Color(1,.72f,.18f);pips[i].transform.localPosition=new Vector3((i-1)*.24f,-1.03f,-.2f);}
   // Timings represent existing damage counts. Effects are material studies, not rule simulations.
   Plank(1,crate[2],true);Plank(2.2f,crate[1],false);
   Burst(0,3.4f,22,new Color(.77f,.43f,.16f));Burst(1,2,24,new Color(.58f,.56f,.70f));
   for(int i=0;i<5;i++){Burst(2,1+i*.65f,i==4?25:4,new Color(.53f,.19f,.77f));Liquid(2,1+i*.65f,colors[1],12);}
   for(int i=0;i<4;i++){var pos=flasks[i].transform.localPosition;Burst(3,1+i*.65f,8,new Color(.69f,.85f,.94f),pos);Liquid(3,1+i*.65f,colors[i],15,pos);}
   Burst(2,3.6f,7,new Color(.97f,.69f,.17f));
   Burst(3,3.6f,18,new Color(.55f,.25f,.72f));Burst(5,3.6f,20,new Color(.80f,.45f,.19f));
   Sample(0);
  }
  T Own<T>(T value) where T:UnityEngine.Object {owned.Add(value);return value;}
  GameObject New(string name,Transform parent=null){var go=new GameObject(name);SceneManager.MoveGameObjectToScene(go,scene);if(parent!=null)go.transform.SetParent(parent,false);return go;}
  Sprite Master(string name){return Load(Path.Combine(folder,"elementos-soft3d-v1",name+".png"),1,1)[0];}
  Sprite[] Sheet(string name,int x,int y){return Load(Path.Combine(folder,"elementos-soft3d-animacao-v1",name+".png"),x,y);}
  Sprite[] Load(string path,int cols,int rows)
  {
   var tex=Own(new Texture2D(2,2,TextureFormat.RGBA32,false));tex.LoadImage(File.ReadAllBytes(path));tex.filterMode=FilterMode.Bilinear;tex.wrapMode=TextureWrapMode.Clamp;
   var result=new Sprite[cols*rows];int w=tex.width/cols,h=tex.height/rows;
   for(int i=0;i<result.Length;i++)
   {
    var rect=new Rect(i%cols*w,(rows-1-i/cols)*h,w,h);var pivot=new Vector2(.5f,.5f);
    if(path.EndsWith("bag-opening.png"))
    {
     // Measure the broad opaque body base, excluding tiny fringe pixels, so a lifted flap never shifts the body.
     int baseY=0;for(int y=0;y<h;y++){int count=0;for(int x=w/5;x<w*4/5;x++)if(tex.GetPixel((int)rect.x+x,(int)rect.y+y).a>.96f)count++;if(count>w/3){baseY=y;break;}}
     pivot.y=(baseY+2f)/h;
    }
    float ppu=Mathf.Max(w,h);
    if(path.EndsWith("inkwell-states.png")){rect.y+=h*.18f;rect.height=h*.72f;ppu=rect.height;}
    result[i]=Own(UnityEngine.Sprite.Create(tex,rect,pivot,ppu,0,SpriteMeshType.FullRect));
   }return result;
  }
  SpriteRenderer Sprite(Transform parent,Sprite sprite,float size)
  {
   var r=New("Art",parent).AddComponent<SpriteRenderer>();r.sprite=sprite;r.sharedMaterial=plain;r.transform.localScale=Vector3.one*size;return r;
  }
  Sprite disc;
  Sprite Disc()
  {
   if(disc!=null)return disc;var tex=Own(new Texture2D(64,64,TextureFormat.RGBA32,false));var pix=new Color[4096];
   for(int y=0;y<64;y++)for(int x=0;x<64;x++){Vector2 p=new Vector2((x-31.5f)/31,(y-31.5f)/31);float z=Mathf.Sqrt(Mathf.Max(0,1-p.sqrMagnitude));float light=Mathf.Clamp01(.48f+.45f*z+.20f*p.y-.14f*p.x);pix[y*64+x]=new Color(light,light,light,Mathf.Clamp01((1-p.magnitude)*24));}tex.SetPixels(pix);tex.Apply();disc=Own(UnityEngine.Sprite.Create(tex,new Rect(0,0,64,64),new Vector2(.5f,.5f),64));return disc;
  }
  void Liquid(int a,float birth,Color color,int count,Vector3? position=null)
  {
   for(int i=0;i<count;i++){float angle=i*2.39996f;var r=Sprite(roots[a],Disc(),.1f);r.color=color;r.sortingOrder=12;var p=new Particle{tr=r.transform,actor=a,birth=birth,life=.95f,size=.08f+(i%4)*.025f,color=color,liquid=true,origin=position??Vector3.zero,velocity=new Vector3(Mathf.Cos(angle)*(1.1f+i%3*.3f),Mathf.Sin(angle)*1.1f+1.3f,-.3f),spin=Vector3.zero};particles.Add(p);}
  }
  void Burst(int a,float birth,int count,Color color,Vector3? position=null)
  {
   var rng=new System.Random(a*101+(int)(birth*100));
   for(int i=0;i<count;i++)
   {
    var tr=New("Rounded solid fragment",roots[a]).transform;var mesh=Own(Chunk());tr.gameObject.AddComponent<MeshFilter>().sharedMesh=mesh;tr.gameObject.AddComponent<MeshRenderer>().sharedMaterial=solid;
    float angle=(float)rng.NextDouble()*Mathf.PI*2;
    particles.Add(new Particle{tr=tr,mesh=mesh,normals=mesh.normals,actor=a,birth=birth,life=1.05f,size=.10f+(float)rng.NextDouble()*.17f,color=Color.Lerp(color,Color.white,(float)rng.NextDouble()*.12f),origin=(position??Vector3.zero)+new Vector3(Mathf.Cos(angle)*.3f,Mathf.Sin(angle)*.4f,-.1f),velocity=new Vector3(Mathf.Cos(angle)*(1.2f+(float)rng.NextDouble()),1.3f+Mathf.Sin(angle)*1.3f,-.2f),spin=new Vector3(100+i*13,120-i*4,80+i*17)});
   }
  }
  void Plank(float birth,Sprite sprite,bool descending)
  {
   var tr=New("Detached reinforcement",roots[0]).transform;var m=Own(new Mesh());var vertices=new List<Vector3>();var uv=new List<Vector2>();var indices=new List<int>();
   Vector2 axis=new Vector2(1,descending?-1:1).normalized,normal=new Vector2(-axis.y,axis.x);
   vertices.Add(Vector3.zero);uv.Add(new Vector2((sprite.rect.x+sprite.rect.width*.5f)/sprite.texture.width,.5f));
   int n=32;for(int i=0;i<n;i++)
   {
    float angle=i*Mathf.PI*2/n;float x=Mathf.Cos(angle);Vector2 p=axis*(x*.13f+Mathf.Sign(x)*.32f)+normal*(Mathf.Sin(angle)*.13f);
    vertices.Add(new Vector3(p.x*2.1f,p.y*2.1f,-.2f));uv.Add(new Vector2((sprite.rect.x+(p.x+.5f)*sprite.rect.width)/sprite.texture.width,(p.y+.5f)));
   }
   for(int i=0;i<n;i++){indices.Add(0);indices.Add(i+1);indices.Add((i+1)%n+1);}m.SetVertices(vertices);m.SetUVs(0,uv);m.SetTriangles(indices,0);var normals=new Vector3[vertices.Count];for(int i=0;i<normals.Length;i++)normals[i]=Vector3.back;m.normals=normals;m.RecalculateBounds();
   tr.gameObject.AddComponent<MeshFilter>().sharedMesh=m;var mat=Own(new Material(plain));mat.mainTexture=sprite.texture;tr.gameObject.AddComponent<MeshRenderer>().sharedMaterial=mat;
   particles.Add(new Particle{tr=tr,mesh=m,normals=normals,actor=0,birth=birth,life=.65f,size=1,color=Color.white,origin=new Vector3(0,0,-.3f),velocity=new Vector3(descending?.6f:-.6f,.25f,0),spin=new Vector3(25,15,descending?65:-65)});
  }
  // Bevelled six-sided chunks: broad front, softened edge and visible thickness.
  Mesh Chunk()
  {
   var v=new List<Vector3>();var ns=new List<Vector3>();var tri=new List<int>();
   Action<Vector3,Vector3,Vector3,Vector3> face=(a,b,c,n)=>{int k=v.Count;v.Add(a);v.Add(b);v.Add(c);ns.Add(n);ns.Add(n);ns.Add(n);tri.Add(k);tri.Add(k+1);tri.Add(k+2);};
   var front=new Vector3[6];var rim=new Vector3[6];var back=new Vector3[6];
   for(int i=0;i<6;i++){float an=(i*60+15)*Mathf.Deg2Rad;float r=i%2==0?1:.85f;rim[i]=new Vector3(Mathf.Cos(an)*r,Mathf.Sin(an)*r,0);front[i]=rim[i]*.72f+Vector3.back*.28f;back[i]=rim[i]*.80f+Vector3.forward*.35f;}
   for(int i=0;i<6;i++){int j=(i+1)%6;face(Vector3.back*.28f,front[j],front[i],Vector3.back);var n=((front[i]+front[j])*.5f+Vector3.back).normalized;face(front[i],front[j],rim[j],n);face(front[i],rim[j],rim[i],n);n=((rim[i]+rim[j])*.5f).normalized;face(rim[i],rim[j],back[j],n);face(rim[i],back[j],back[i],n);face(Vector3.forward*.35f,back[i],back[j],Vector3.forward);}
   var m=new Mesh();m.SetVertices(v);m.SetNormals(ns);m.SetTriangles(tri,0);m.RecalculateBounds();return m;
  }
  public void Sample(float t)
  {
   if(t>4.85f)t=0; // Brief intact hold before replay, instead of leaving five empty review cells.
   for(int i=0;i<6;i++){roots[i].localScale=Vector3.one;bodies[i].enabled=true;}
   int cs=t<1?2:t<2.2f?1:0;bodies[0].sprite=crate[cs];bodies[0].enabled=t<3.4f;
   bodies[1].enabled=t<2;int stage=Mathf.Clamp(Mathf.FloorToInt((t-1)/.65f)+1,0,4);bodies[2].sprite=ink[stage];bodies[2].enabled=t<3.6f;
   for(int i=0;i<4;i++)flasks[i].enabled=t<1+i*.65f;bodies[3].enabled=t<3.6f;
   // Hold the open mouth while delivering, then close with the same registered lower body.
   float phase=t<.7f?0:t<1.6f?(t-.7f)/.9f*7:t<3.5f?7:t<4.4f?7+(t-3.5f)/.9f*8:15;
   bodies[4].sprite=bag[Mathf.Clamp(Mathf.RoundToInt(phase),0,15)];
   for(int i=0;i<3;i++)
   {
    float u=(t-(1.6f+i*.38f))/1.25f;var r=letters[i];r.enabled=u>=0&&u<1;r.sprite=scroll[Mathf.Clamp((int)(u*8),0,3)];
    r.transform.localPosition=new Vector3(Mathf.Sin(u*Mathf.PI)*.30f+u*.65f,.15f+u*1.7f,-.5f);r.transform.localRotation=Quaternion.Euler(0,0,-u*18);r.transform.localScale=Vector3.one*(.55f*(1-.5f*Mathf.Clamp01((u-.65f)/.35f)));r.color=new Color(1,1,1,1-Mathf.Clamp01((u-.85f)/.15f));
   }
   int cycle=t<1?0:t<2?1:t<2.8f?2:3;if(lens!=null){lens.SetFloat("_Mode",cycle==0?0:1);lens.SetColor("_LensColor",cycle==1?colors[2]:cycle==2?colors[1]:colors[3]);}
   if(signalGlyph!=null){signalGlyph.enabled=cycle>0&&t<3.6f;if(cycle>0){signalGlyph.sprite=signalPieces[cycle];signalGlyph.transform.localScale=Vector3.one*(.55f/Mathf.Max(signalGlyph.sprite.bounds.size.x,signalGlyph.sprite.bounds.size.y));}}
   bodies[5].enabled=t<3.6f;for(int i=0;i<3;i++)pips[i].enabled=t<1+i*1.3f;
   float[] hits={3.4f,2,1+Mathf.Clamp(Mathf.Floor((t-1)/.65f),0,4)*.65f,1+Mathf.Clamp(Mathf.Floor((t-1)/.65f),0,3)*.65f,0,3.6f};
   for(int i=0;i<6;i++){if(i==4)continue;float dt=t-hits[i];if(dt>=0&&dt<.18f){float q=Mathf.Sin(dt/.18f*Mathf.PI)*.07f;roots[i].localScale=new Vector3(1+q,1-q,1);}}
   foreach(var p in particles)
   {
    float dt=t-p.birth;p.tr.gameObject.SetActive(dt>=0&&dt<p.life);if(dt<0||dt>=p.life)continue;
    p.tr.localPosition=p.origin+p.velocity*dt+Vector3.down*2.4f*dt*dt;float fade=1-Mathf.Clamp01((dt-p.life*.65f)/(p.life*.35f));p.tr.localScale=Vector3.one*p.size*fade;
    if(p.liquid){p.tr.localScale=new Vector3(p.size*fade,p.size*(1+.6f*dt)*fade,1);continue;}
    p.tr.localRotation=Quaternion.Euler(p.spin*dt);var c=new Color[p.normals.Length];var rot=p.tr.localRotation;Vector3 light=new Vector3(-.45f,.7f,-1).normalized;
    for(int k=0;k<c.Length;k++){float shade=.55f+.45f*Mathf.Max(0,Vector3.Dot(rot*p.normals[k],light));c[k]=new Color(p.color.r*shade,p.color.g*shade,p.color.b*shade,1);}p.mesh.colors=c;
   }
  }
  public void Dispose(){if(target!=null){camera.targetTexture=null;target.Release();}EditorSceneManager.ClosePreviewScene(scene);foreach(var o in owned)if(o!=null)UnityEngine.Object.DestroyImmediate(o);}
 }
}
