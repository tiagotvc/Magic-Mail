using UnityEngine;
using UnityEngine.Rendering;

namespace RoyalAves.Meta.WorldMap
{
    // Luz dourada: (a) brilhos que piscam sobre um prop (caixa, luminaria) e (b) raios de sol cartoon editaveis
    // (WorldMapLightSet). Malhas em escala unitaria; o chamador posiciona o conjunto.
    public sealed class WorldMapLightFx
    {
        static Mesh beamMesh;
        readonly GameObject root, beamGo;
        readonly MeshRenderer beamRenderer, sparkleRenderer;
        readonly MaterialPropertyBlock beamBlock = new MaterialPropertyBlock(), sparkleBlock = new MaterialPropertyBlock();
        readonly Mesh sparkleMesh;
        int builtKey = int.MinValue;

        public Transform Root => root.transform;

        public WorldMapLightFx(Transform parent, int layer, Material beam, Material sparkle)
        {
            root = new GameObject("LuzDourada") { layer = layer };
            root.transform.SetParent(parent, false);
            beamGo = Child("Feixe", layer, out beamRenderer, beam, BeamMesh());
            sparkleMesh = new Mesh { name = "WorldMapSparkles", hideFlags = HideFlags.HideAndDontSave };
            Child("Brilhos", layer, out sparkleRenderer, sparkle, sparkleMesh);
            root.SetActive(false);
        }

        GameObject Child(string name, int layer, out MeshRenderer renderer, Material material, Mesh mesh)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer)) { layer = layer };
            go.transform.SetParent(root.transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return go;
        }

        // Quadrado unitario com base em y=0 e uv 0..1.
        static Mesh BeamMesh()
        {
            if (beamMesh != null) return beamMesh;
            beamMesh = new Mesh { name = "WorldMapBeam", hideFlags = HideFlags.HideAndDontSave };
            beamMesh.SetVertices(new[] { new Vector3(-.5f, 0, 0), new Vector3(-.5f, 1, 0), new Vector3(.5f, 1, 0), new Vector3(.5f, 0, 0) });
            beamMesh.SetUVs(0, new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });
            beamMesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            beamMesh.RecalculateBounds();
            return beamMesh;
        }

        // Brilhos espalhados ao longo de um segmento (origem + dir * t * comprimento, afastados de lado ate 'spread').
        void BuildSparkles(int seed, int count, float size, Vector2 origin, Vector2 dir, float length, float spread)
        {
            var rng = new System.Random(seed * 7919 + 13);
            float R() => (float)rng.NextDouble();
            var perp = new Vector2(-dir.y, dir.x);
            var v = new Vector3[count * 4];
            var uv = new Vector2[count * 4];
            var col = new Color[count * 4];
            var tri = new int[count * 6];
            for (var i = 0; i < count; i++)
            {
                var t = Mathf.Lerp(.08f, .9f, R());
                var p = origin + dir * (t * length) + perp * ((R() - .5f) * spread * (1 - t * .7f));
                var s = size * Mathf.Lerp(.6f, 1.4f, R());
                var c = new Color(R(), R(), 0, 1);
                var b = i * 4;
                v[b] = new Vector3(p.x - s, p.y - s, -.03f); v[b + 1] = new Vector3(p.x - s, p.y + s, -.03f);
                v[b + 2] = new Vector3(p.x + s, p.y + s, -.03f); v[b + 3] = new Vector3(p.x + s, p.y - s, -.03f);
                uv[b] = new Vector2(0, 0); uv[b + 1] = new Vector2(0, 1); uv[b + 2] = new Vector2(1, 1); uv[b + 3] = new Vector2(1, 0);
                col[b] = col[b + 1] = col[b + 2] = col[b + 3] = c;
                tri[i * 6] = b; tri[i * 6 + 1] = b + 1; tri[i * 6 + 2] = b + 2; tri[i * 6 + 3] = b; tri[i * 6 + 4] = b + 2; tri[i * 6 + 5] = b + 3;
            }
            sparkleMesh.Clear();
            sparkleMesh.vertices = v;
            sparkleMesh.uv = uv;
            sparkleMesh.colors = col;
            sparkleMesh.triangles = tri;
            sparkleMesh.RecalculateBounds();
        }

        public void SetActive(bool active)
        {
            if (root != null && root.activeSelf != active) root.SetActive(active);
        }

        // (a) Brilhos sobre um prop (filho do cartao). height = altura do prop; strength 0..1.
        public void Tick(PropKind kind, float height, float time, int seed, float strength)
        {
            if (strength < .03f) { SetActive(false); return; }
            SetActive(true);
            var key = seed * 31 + (int)kind;
            if (builtKey != key)
            {
                BuildSparkles(seed, 5, .08f, new Vector2(0, .3f), Vector2.up, kind == PropKind.Mailbox ? 1.4f : 1.2f, .8f);
                builtKey = key;
            }
            root.transform.localScale = Vector3.one * height;
            beamGo.SetActive(false);
            sparkleBlock.SetFloat("_FxTime", time);
            sparkleBlock.SetFloat("_Intensity", strength * .6f);
            sparkleRenderer.SetPropertyBlock(sparkleBlock);
        }

        // (b) Raio de sol editavel. O root fica no ponto de contato (superficie); medidas em metros.
        public void TickShaft(WorldMapLightSet.Shaft s, float time, float strength)
        {
            if (strength < .02f || !s.enabled) { SetActive(false); return; }
            SetActive(true);
            root.transform.localScale = Vector3.one;
            beamGo.SetActive(true);
            beamGo.transform.localPosition = new Vector3(0, s.height, .05f);
            beamGo.transform.localRotation = Quaternion.Euler(0, 0, s.angle);
            beamGo.transform.localScale = new Vector3(s.width * s.scale, s.length * s.scale, 1);
            var c = s.Color;
            beamBlock.SetColor("_LightColor", new Color(c.r, c.g, c.b, 1));
            beamBlock.SetFloat("_Alpha", s.intensity * strength);
            beamBlock.SetFloat("_Bands", s.bands);
            beamBlock.SetFloat("_RaySpacing", s.raySpacing);
            beamBlock.SetFloat("_FxTime", time);
            beamBlock.SetFloat("_Seed", s.seed * .37f);
            beamRenderer.SetPropertyBlock(beamBlock);
            var key = Mathf.RoundToInt(s.scale * 100) * 977 + s.seed * 131 + s.sparkles * 17 + Mathf.RoundToInt(s.angle * 10) + Mathf.RoundToInt(s.length * 10) * 7
                + Mathf.RoundToInt(s.width * 10) * 3 + Mathf.RoundToInt(s.sparkleSize * 100) * 11 + Mathf.RoundToInt(s.height * 10);
            if (builtKey != key)
            {
                var a = s.angle * Mathf.Deg2Rad;
                // angulo positivo inclina o topo para a esquerda: direcao do eixo do feixe no plano do cartao
                var dir = new Vector2(-Mathf.Sin(a), Mathf.Cos(a));
                BuildSparkles(s.seed, s.sparkles, s.sparkleSize * s.scale, new Vector2(0, s.height), dir, s.length * s.scale * .85f, s.width * s.scale * .8f);
                builtKey = key;
            }
            sparkleBlock.SetFloat("_FxTime", time);
            sparkleBlock.SetFloat("_Intensity", strength * .8f);
            sparkleRenderer.SetPropertyBlock(sparkleBlock);
        }

        public void Dispose()
        {
            if (sparkleMesh == null) return;
            if (Application.isPlaying) Object.Destroy(sparkleMesh);
            else Object.DestroyImmediate(sparkleMesh);
        }
    }
}
