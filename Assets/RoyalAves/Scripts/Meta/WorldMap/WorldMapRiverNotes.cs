using System;
using UnityEngine;
using UnityEngine.Rendering;
namespace RoyalAves.Meta.WorldMap
{
    // A fixed pool; the score notes rise above the water without rebuilding its mesh.
    public sealed class WorldMapRiverNotes : IDisposable
    {
        readonly Transform[] notes=new Transform[8];
        readonly Mesh body,shine;
        readonly GameObject root;
        public WorldMapRiverNotes(Transform parent,int layer,Material material)
        {
            root=new GameObject("RiverSymphonyNotes"){layer=layer};root.transform.SetParent(parent,false);
            body=Build(false);shine=Build(true);
            for(int i=0;i<notes.Length;i++)
            {
                var go=new GameObject("Note_"+i){layer=layer};go.transform.SetParent(root.transform,false);notes[i]=go.transform;
                Add(go.transform,body,new Color(1,.78f,.28f));Add(go.transform,shine,new Color(1,1,.88f));go.SetActive(false);
            }
            void Add(Transform parentNote,Mesh mesh,Color color)
            {
                var go=new GameObject("GlossyNote",typeof(MeshFilter),typeof(MeshRenderer)){layer=layer};go.transform.SetParent(parentNote,false);
                go.GetComponent<MeshFilter>().sharedMesh=mesh;var r=go.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.shadowCastingMode=ShadowCastingMode.Off;r.receiveShadows=false;
                var block=new MaterialPropertyBlock();block.SetColor("_Tint",QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color);block.SetFloat("_Emission",.2f);block.SetFloat("_HazeNear",1000);block.SetFloat("_HazeFar",2000);r.SetPropertyBlock(block);
            }
        }
        static Mesh Build(bool highlight)
        {
            var v=new System.Collections.Generic.List<Vector3>();var t=new System.Collections.Generic.List<int>();
            void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d){int n=v.Count;float z=highlight?-.008f:0;v.Add(new Vector3(a.x,a.y,z));v.Add(new Vector3(b.x,b.y,z));v.Add(new Vector3(c.x,c.y,z));v.Add(new Vector3(d.x,d.y,z));t.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});}
            void Ellipse(Vector2 p,Vector2 size){for(int i=0;i<20;i++){float a=i*Mathf.PI*2/20,b=(i+1)*Mathf.PI*2/20;Quad(p,p+Vector2.Scale(new Vector2(Mathf.Cos(a),Mathf.Sin(a)),size),p+Vector2.Scale(new Vector2(Mathf.Cos(b),Mathf.Sin(b)),size),p);}}
            if(highlight)Ellipse(new Vector2(-.035f,.018f),new Vector2(.055f,.018f));
            else{Ellipse(Vector2.zero,new Vector2(.13f,.085f));Quad(new Vector2(.075f,0),new Vector2(.12f,0),new Vector2(.12f,.44f),new Vector2(.075f,.44f));Quad(new Vector2(.10f,.44f),new Vector2(.10f,.36f),new Vector2(.29f,.25f),new Vector2(.24f,.37f));}
            var mesh=new Mesh{name=highlight?"NoteSpecular":"EighthNote"};mesh.SetVertices(v);mesh.SetTriangles(t,0);mesh.uv=new Vector2[v.Count];mesh.RecalculateBounds();return mesh;
        }
        public void Tick(WorldMapRoute route,WorldMapCameraModel camera,float radius,float cursor,float time)
        {
            int used=0;
            foreach(var gap in route.Gaps)
            {
                if(gap.end<cursor-5||gap.start>cursor+16||route.CrossingAt(gap)?.liquid!=WorldMapLiquid.Symphony)continue;
                var settings=route.CrossingAt(gap);
                for(int i=0;i<settings.musicNotes&&used<notes.Length;i++)
                {
                    var n=notes[used++];n.gameObject.SetActive(true);float phase=Mathf.Repeat(time*settings.noteSpeed+i*.27f,1);
                    float s=Mathf.Lerp(gap.start,gap.end,.15f+.70f*(i+.5f)/Mathf.Max(1,settings.musicNotes)),x=route.CenterX(s)+(i%2==0?-1.45f:1.55f);
                    n.localPosition=MapCurve.Point(s-cursor,x,route.WaterHeight(s)+.15f+phase*.85f,radius,route.Curvature);
                    n.localRotation=camera.Rotation*Quaternion.Euler(0,0,-12+i*9);n.localScale=Vector3.one*(.45f+.2f*Mathf.Sin(phase*Mathf.PI))*Mathf.Sin(phase*Mathf.PI);
                }
            }
            for(int i=used;i<notes.Length;i++)notes[i].gameObject.SetActive(false);
        }
        public void Dispose(){Kill(body);Kill(shine);Kill(root);}
        static void Kill(UnityEngine.Object o){if(o==null)return;if(Application.isPlaying)UnityEngine.Object.Destroy(o);else UnityEngine.Object.DestroyImmediate(o);}
    }
}
