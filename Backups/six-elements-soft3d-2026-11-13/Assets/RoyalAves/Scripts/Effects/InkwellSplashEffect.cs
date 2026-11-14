using UnityEngine;

namespace RoyalAves.Effects
{
    // Transient liquid geometry, independent of the dry damage sprites and persistent stains.
    public sealed class InkwellSplashEffect : MonoBehaviour
    {
        public const float TravelTime = .3f;
        public static int Emitted { get; private set; }
        public static int Landed { get; private set; }
        Mesh mesh;
        Material material;
        Vector3 from, to;
        float age;
        bool landed;
        public float Age => age;
        readonly System.Collections.Generic.List<Vector3> vertices = new System.Collections.Generic.List<Vector3>();
        readonly System.Collections.Generic.List<Color> colors = new System.Collections.Generic.List<Color>();
        readonly System.Collections.Generic.List<int> triangles = new System.Collections.Generic.List<int>();
        public static void Play(SpriteRenderer jar, Vector3 destination)
        {
            var root = new GameObject("InkwellLiquidSplash"); root.layer = jar.gameObject.layer;
            var effect = root.AddComponent<InkwellSplashEffect>();
            effect.from = jar.bounds.center + Vector3.up * jar.bounds.extents.y * .8f;
            var inkwell=jar.GetComponent<RoyalAves.Gameplay.InkwellCell>();
            if(inkwell!=null && inkwell.stages!=null && inkwell.stages.Length>0 && inkwell.stages[0]!=null)
                effect.from=inkwell.MouthPosition;
            effect.to = destination;
            effect.from.z = effect.to.z = jar.transform.position.z - .8f;
            effect.mesh = new Mesh { name = "AnimatedInkDroplets" }; effect.mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = effect.mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            effect.material = new Material(Shader.Find("Sprites/Default")); renderer.sharedMaterial = effect.material;
            renderer.sortingLayerID = jar.sortingLayerID; renderer.sortingOrder = Mathf.Max(30, jar.sortingOrder + 15);
            Emitted++; effect.Draw();
        }
        Vector3 Arc(float t)
        {
            return Vector3.Lerp(from, to, t) + Vector3.up * Mathf.Sin(t * Mathf.PI) * .42f;
        }
        void Update()
        {
            age += Time.deltaTime;
            if (!landed && age >= TravelTime) { landed = true; Landed++; }
            if (age > .85f) { Destroy(gameObject); return; }
            Draw();
        }
        void Blob(Vector3 center, float width, float height, float angle, Color color)
        {
            int first = vertices.Count; vertices.Add(center); colors.Add(color);
            const int sides = 14;
            var rotation = Quaternion.Euler(0, 0, angle);
            for (int n = 0; n <= sides; n++)
            {
                float a = n * Mathf.PI * 2 / sides;
                vertices.Add(center + rotation * new Vector3(Mathf.Cos(a)*width, Mathf.Sin(a)*height)); colors.Add(color);
                if (n == 0) continue;
                triangles.Add(first); triangles.Add(first+n); triangles.Add(first+n+1);
            }
        }
        void Drop(Vector3 center, float radius, float stretch, float angle, float alpha)
        {
            Blob(center, radius, radius*stretch, angle, new Color(.23f,.005f,.5f,alpha));
            Blob(center + Vector3.up*.012f, radius*.83f, radius*stretch*.83f, angle, new Color(.49f,.025f,.94f,alpha));
            Blob(center + new Vector3(-radius*.23f,radius*stretch*.35f,-.001f), radius*.21f, radius*stretch*.30f, angle, new Color(.9f,.65f,1,alpha));
        }
        void Draw()
        {
            vertices.Clear(); colors.Clear(); triangles.Clear();
            float t = Mathf.Clamp01(age / TravelTime);
            if (age < TravelTime)
            {
                // A short connected jet followed by visibly separate, glossy droplets.
                for (int n = 0; n < 11; n++)
                {
                    float q = t - n*.018f;
                    if (q < 0) continue;
                    Drop(Arc(q), .075f*(1-n*.055f), 1.15f, -35, 1);
                }
                for(int n=0;n<4;n++)
                {
                    float q=Mathf.Clamp01((age-n*.025f)/TravelTime);
                    var position=Arc(q)+new Vector3(Mathf.Sin(n*2.4f)*.13f, Mathf.Sin(q*Mathf.PI)*n*.065f);
                    Drop(position,.045f,1.5f,n*33,1);
                }
            }
            else
            {
                float q=Mathf.Clamp01((age-TravelTime)/.55f);
                float alpha=1-Mathf.SmoothStep(.5f,1,q);
                for(int n=0;n<7;n++)
                {
                    float a=n*2*Mathf.PI/7;
                    Vector3 offset=new Vector3(Mathf.Cos(a),Mathf.Sin(a)*.65f)*(.09f+q*.42f);
                    offset.y+=Mathf.Sin(q*Mathf.PI)*.2f;
                    Drop(to+offset,.055f*(1-q*.65f),1.3f,a*Mathf.Rad2Deg,alpha);
                }
            }
            mesh.Clear(); mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetTriangles(triangles,0); mesh.RecalculateBounds();
        }
        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
