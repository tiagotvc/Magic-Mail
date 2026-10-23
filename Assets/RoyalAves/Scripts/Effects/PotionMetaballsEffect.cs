using UnityEngine;
using SweetSugar.Scripts.Level;

namespace RoyalAves.Effects
{
    // Experimental overlay for the in-game laboratory only.
    public sealed class PotionMetaballsEffect : MonoBehaviour
    {
        const float PlaybackSpeed = 2f;
        readonly Vector4[] balls = new Vector4[28];
        Material material;
        Mesh mesh;
        float elapsed;
        float duration;
        float breakupTime;
        float visibleDistance;

        public static void Play(Transform parent, Vector3 origin, Color color, Shader shader)
        {
            if (shader == null || !shader.isSupported)
            {
                Debug.LogError("Poção metaballs: shader indisponível.");
                return;
            }
            float height = 14;
            float distance = 11;
            if (Camera.main != null)
            {
                var world = parent.TransformPoint(origin);
                float depth = Camera.main.WorldToScreenPoint(world).z;
                var bottom = parent.InverseTransformPoint(Camera.main.ViewportToWorldPoint(new Vector3(.5f, 0, depth)));
                distance = Mathf.Max(1, origin.y - bottom.y);
                height = Mathf.Max(7, distance + 3);
            }
            float offset = height * .5f - 1.7f;
            // The isolated visual board uses two local units per cell.
            float spreadDistance = RouteCentralPrototype.CanvasSize / 2f;
            var field = parent.GetComponentInParent<FieldBoard>();
            if (field != null)
            {
                foreach (var square in field.GetSquares())
                {
                    if (square == null) continue;
                    var below = field.GetSquare(square.col, square.row + 1);
                    if (below == null) continue;
                    float cellHeight = Mathf.Abs(parent.InverseTransformPoint(square.transform.position).y
                        - parent.InverseTransformPoint(below.transform.position).y);
                    if (cellHeight <= .001f) continue;
                    spreadDistance = cellHeight;
                    break;
                }
            }
            var root = new GameObject("PotionSpill");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = origin + Vector3.down * offset;
            var effect = root.AddComponent<PotionMetaballsEffect>();
            effect.duration = Mathf.Sqrt((height + 3) / 2.8f) + .8f;
            effect.visibleDistance = distance;
            // Start dispersing after descending exactly one grid cell.
            effect.breakupTime = (-.7f + Mathf.Sqrt(.49f + 11.2f * spreadDistance)) / 5.6f;
            effect.material = new Material(shader);
            effect.material.SetFloat("_Transparent", 1);
            effect.material.SetFloat("_DomainHeight", height);
            effect.material.SetFloat("_DomainOffset", offset);
            const float domainWidth = 8f;
            effect.material.SetFloat("_Aspect", domainWidth / height);
            effect.material.SetFloat("_Fusion", .55f);
            effect.material.SetColor("_Ink", color);
            effect.mesh = new Mesh { name = "PotionMetaballsDomain" };
            effect.mesh.vertices = new[] { new Vector3(-domainWidth/2,-height/2,0), new Vector3(domainWidth/2,-height/2,0),
                new Vector3(-domainWidth/2,height/2,0), new Vector3(domainWidth/2,height/2,0) };
            effect.mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            effect.mesh.triangles = new[] { 0,2,1,2,3,1 };
            effect.mesh.RecalculateBounds();
            root.AddComponent<MeshFilter>().sharedMesh = effect.mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = effect.material;
            renderer.sortingLayerName = PowerUpEffectOrder.SortingLayer;
            renderer.sortingOrder = 15;
            effect.Sample(0);
        }

        void Sample(float time)
        {
            float fall = .7f * time + 2.8f * time * time;
            float progress = Mathf.Clamp01(fall / visibleDistance);
            float separationAge = Mathf.Max(0, time - breakupTime);
            float spread = separationAge * separationAge / (separationAge + .08f);
            for (int b = 0; b < balls.Length; b++)
            {
                int row = b / 4, col = b % 4;
                // Connected, softly tapered stream above the grid exit; a wide
                // irregular fan of individual drops below it.
                float taper = .7f + .3f * Mathf.Sin(row / 6f * Mathf.PI);
                float x = (col - 1.5f) * .14f * taper + Mathf.Sin(row * 1.1f + time * 3) * .035f
                    + ((col - 1.5f) * (.8f + row * .1f) + Mathf.Sin(b * 2.3f) * .22f) * spread;
                // The spawn origin marks the upper edge of the outflow. Keep
                // all initial centers below it (including their visible radii).
                float y = -.25f - row * .105f * (1 + .9f * progress) - fall
                    + Mathf.Sin(col * 1.8f + row) * .015f
                    - (row * .5f + Mathf.Sin(b * 2.7f) * .2f) * spread;
                balls[b] = new Vector4(x, y, .21f + .035f * (.5f + .5f * Mathf.Sin(b * 1.7f)), 1);
            }
            material.SetVectorArray("_Balls", balls);
        }

        void Update()
        {
            // Advance the whole simulation clock so stretching, separation,
            // travel and cleanup all finish in half the previous real time.
            elapsed += Time.deltaTime * PlaybackSpeed;
            Sample(elapsed);
            if (elapsed > duration) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (material != null) Destroy(material);
            // RouteCentralPrototype.RemoveSpill owns mesh cleanup on reset.
            var filter = GetComponent<MeshFilter>();
            if (mesh != null && filter != null && filter.sharedMesh != null) Destroy(mesh);
        }
    }
}
