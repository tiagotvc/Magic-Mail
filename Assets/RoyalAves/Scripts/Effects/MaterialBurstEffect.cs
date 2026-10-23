using System.Collections.Generic;
using SweetSugar.Scripts.Level;
using UnityEngine;

namespace RoyalAves.Effects
{
    // Independent ballistic fragments, batched in a single mesh per impact.
    // Shapes belong to the material: tapered liquid, angular glass, split wood.
    public sealed class MaterialBurstEffect : MonoBehaviour
    {
        sealed class Fragment
        {
            public Vector2 origin, velocity, size;
            public float angle, spin, lifetime;
            public Color color;
            public int kind; // 0 liquid, 1 glass, 2 wood, 3 wood dust
        }
        static readonly Vector2[] Drop = {
            new Vector2(0,.8f), new Vector2(-.14f,.15f), new Vector2(-.34f,-.2f),
            new Vector2(-.25f,-.48f), new Vector2(0,-.58f), new Vector2(.25f,-.48f),
            new Vector2(.34f,-.2f), new Vector2(.14f,.15f) };
        static readonly Vector2[] Glass = {
            new Vector2(-.5f,-.4f), new Vector2(.42f,-.2f), new Vector2(.2f,.58f), new Vector2(-.3f,.25f) };
        static readonly Vector2[] Wood = {
            new Vector2(-.5f,-.26f), new Vector2(-.36f,-.5f), new Vector2(.3f,-.42f),
            new Vector2(.5f,-.15f), new Vector2(.37f,.12f), new Vector2(.46f,.36f),
            new Vector2(-.28f,.5f), new Vector2(-.46f,.24f) };
        readonly List<Fragment> fragments = new List<Fragment>();
        readonly List<Vector3> vertices = new List<Vector3>(1200);
        readonly List<Color> colors = new List<Color>(1200);
        readonly List<Vector2> uvs = new List<Vector2>(1200);
        readonly List<int> triangles = new List<int>(2400);
        Mesh mesh;
        Material material;
        float elapsed, gravity, bottom, unit;
        bool woodBurst;

        static MaterialBurstEffect Create(SpriteRenderer source, string name)
        {
            var root = new GameObject(name);
            var field = source.GetComponentInParent<FieldBoard>();
            root.transform.SetParent(field != null ? field.transform : source.transform.parent, false);
            root.transform.SetPositionAndRotation(source.transform.position, source.transform.rotation);
            var parentScale = root.transform.parent != null ? root.transform.parent.lossyScale : Vector3.one;
            root.transform.localScale = new Vector3(source.transform.lossyScale.x / parentScale.x,
                source.transform.lossyScale.y / parentScale.y, 1);
            var effect = root.AddComponent<MaterialBurstEffect>();
            effect.unit = Mathf.Max(source.sprite.bounds.size.x, source.sprite.bounds.size.y);
            effect.material = new Material(source.sharedMaterial);
            effect.material.SetTexture("_MainTex", Texture2D.whiteTexture);
            effect.mesh = new Mesh { name = name + "Mesh" };
            effect.mesh.MarkDynamic();
            root.AddComponent<MeshFilter>().sharedMesh = effect.mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = effect.material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = PowerUpEffectOrder.Value + 3;
            effect.bottom = source.transform.position.y - 25;
            if (Camera.main != null)
            {
                float depth = Camera.main.WorldToScreenPoint(source.transform.position).z;
                effect.bottom = Camera.main.ViewportToWorldPoint(new Vector3(.5f,0,depth)).y - 1.5f;
            }
            return effect;
        }

        public static void Potion(SpriteRenderer source, Color liquid)
        {
            if (source == null || source.sprite == null) return;
            var effect = Create(source, "PotionSpill");
            effect.gravity = effect.unit * 12;
            for (int i = 0; i < 30; i++)
            {
                bool glass = i >= 22;
                float angle = i * 2.399963f + Random.Range(-.2f,.2f);
                var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                float radius = effect.unit * Random.Range(.025f,.14f);
                float width = effect.unit * (glass ? Random.Range(.035f,.065f) : Random.Range(.025f,.065f));
                if (!glass) width *= 1.15f * 1.2f;
                var fragment = new Fragment {
                    kind = glass ? 1 : 0,
                    origin = new Vector2(0,-.23f * effect.unit) + direction * radius,
                    velocity = (direction * Random.Range(1.2f,2.6f) + Vector2.up * .8f) * effect.unit,
                    size = new Vector2(width, width * Random.Range(1.25f,2.4f)),
                    color = glass ? new Color(.64f,.85f,1,.8f) : liquid,
                    angle = Random.Range(0,360), spin = Random.Range(-440,440), lifetime = 5
                };
                effect.fragments.Add(fragment);
            }
            effect.Render(0);
        }

        public static void ExplodeWood(SpriteRenderer source, int state, bool cabinet = false)
        {
            if (source == null || source.sprite == null) return;
            var effect = Create(source, cabinet ? "CabinetWoodExplosion" : "CrateWoodExplosion");
            effect.woodBurst = true;
            effect.gravity = effect.unit * 13;
            bool plank = state > 1 && !cabinet;
            int count = cabinet ? 28 : plank ? 10 : 18;
            float plankAngle = state == 3 ? 45 : -45;
            for (int i = 0; i < count + 10; i++)
            {
                bool dust = i >= count;
                bool structural = i < (cabinet ? 6 : 3);
                Vector2 center;
                if (plank)
                {
                    float a = plankAngle * Mathf.Deg2Rad;
                    center = new Vector2(Mathf.Cos(a),Mathf.Sin(a)) * Random.Range(-.35f,.35f);
                }
                else if (cabinet)
                {
                    // Frame rails, shelves and sides, rather than empty niches.
                    float edge = i % 4;
                    center = edge < 2 ? new Vector2(edge == 0 ? -.36f : .36f,Random.Range(-.4f,.4f))
                        : new Vector2(Random.Range(-.36f,.36f), edge == 2 ? -.38f : Random.Range(-.1f,.38f));
                }
                else center = Random.insideUnitCircle * .38f;
                var direction = (center + Random.insideUnitCircle * .12f).normalized;
                if (direction.sqrMagnitude < .01f) direction = Vector2.up;
                float length = dust ? Random.Range(.015f,.035f) : structural ? Random.Range(.22f,.38f) : Random.Range(.09f,.22f);
                float thickness = length * (dust ? .7f : Random.Range(.16f,.33f));
                Color warm = Color.Lerp(new Color(.59f,.25f,.07f), new Color(1,.68f,.24f), Random.Range(.15f,.95f));
                warm *= source.color;
                effect.fragments.Add(new Fragment {
                    kind = dust ? 3 : 2, origin = Vector2.Scale(center, source.sprite.bounds.size)
                        + (Vector2)source.sprite.bounds.center,
                    velocity = (direction * Random.Range(1.8f,3.5f) + Vector2.up * .85f)
                        * effect.unit * ParcelFeedbackSettings.Intensity,
                    size = new Vector2(length,thickness) * effect.unit,
                    angle = plank ? plankAngle : Random.Range(-110,110),
                    spin = Random.Range(220,720) * (i % 2 == 0 ? 1 : -1),
                    color = warm, lifetime = dust ? Random.Range(.18f,.32f) : 5
                });
            }
            effect.Render(0);
        }

        bool Render(float time)
        {
            vertices.Clear(); colors.Clear(); uvs.Clear(); triangles.Clear();
            bool visible = false;
            foreach (var fragment in fragments)
            {
                if (time >= fragment.lifetime) continue;
                var center = fragment.origin + fragment.velocity * time - Vector2.up * (.5f * gravity * time * time);
                if (transform.TransformPoint(center).y + fragment.size.magnitude * Mathf.Abs(transform.lossyScale.y) < bottom) continue;
                visible = true;
                float rotation = fragment.angle + fragment.spin * time;
                var size = fragment.size;
                if (fragment.kind == 0)
                {
                    var velocity = fragment.velocity - Vector2.up * gravity * time;
                    rotation = Mathf.Atan2(velocity.y,velocity.x) * Mathf.Rad2Deg + 90;
                    // Stretch in flight while preserving the drop's 2D area.
                    float stretch = 1 + Mathf.Min(.8f,velocity.magnitude / unit * .07f);
                    size = new Vector2(size.x / Mathf.Sqrt(stretch),size.y * Mathf.Sqrt(stretch));
                }
                Color tint = fragment.color;
                if (fragment.kind == 3) tint.a *= 1 - Mathf.SmoothStep(.06f,fragment.lifetime,time);
                Append(fragment.kind == 0 ? Drop : fragment.kind == 1 ? Glass : Wood,
                    center,size,rotation,tint,fragment.kind == 2);
            }
            mesh.Clear();
            mesh.SetVertices(vertices); mesh.SetColors(colors); mesh.SetUVs(0,uvs); mesh.SetTriangles(triangles,0);
            mesh.RecalculateBounds();
            return visible;
        }

        void Append(Vector2[] outline, Vector2 center, Vector2 size, float degrees, Color color, bool grain)
        {
            float angle = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
            Vector2 Rotate(Vector2 p) => center + new Vector2(p.x*cos-p.y*sin,p.x*sin+p.y*cos);
            int first = vertices.Count;
            vertices.Add(center); colors.Add(color); uvs.Add(Vector2.one*.5f);
            for (int i = 0; i < outline.Length; i++)
            {
                var p = outline[i];
                vertices.Add(Rotate(Vector2.Scale(p,size)));
                Color edge = p.y > 0 ? Color.Lerp(color,Color.white,.32f) : Color.Lerp(color,Color.black,.3f);
                edge.a = color.a; colors.Add(edge); uvs.Add(Vector2.one*.5f);
                triangles.Add(first); triangles.Add(first+1+i); triangles.Add(first+1+(i+1)%outline.Length);
            }
            if (!grain) return;
            // Two thin, unequal longitudinal grain marks, attached to the shard.
            for (int line = 0; line < 2; line++)
            {
                int start = vertices.Count;
                float y = (line == 0 ? -.12f : .14f) * size.y;
                float left = (line == 0 ? -.32f : -.15f) * size.x;
                float right = .3f * size.x;
                float half = size.y * .035f;
                Color dark = Color.Lerp(color,Color.black,.3f); dark.a = color.a;
                for (int corner = 0; corner < 4; corner++)
                {
                    var p = new Vector2(corner < 2 ? left : right, corner == 0 || corner == 3 ? y-half : y+half);
                    vertices.Add(Rotate(p)); colors.Add(dark); uvs.Add(Vector2.one*.5f);
                }
                triangles.Add(start);triangles.Add(start+1);triangles.Add(start+2);
                triangles.Add(start);triangles.Add(start+2);triangles.Add(start+3);
            }
        }

        void Update()
        {
            elapsed += Time.deltaTime * (woodBurst ? ParcelFeedbackSettings.Speed : 1);
            if (!Render(elapsed)) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
