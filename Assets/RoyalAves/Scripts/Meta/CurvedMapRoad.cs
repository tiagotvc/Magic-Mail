using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    // Perspective projection of an actual circular ground arc. The tangent is a finite horizon:
    // beyond it ground points descend and the terrain occludes the bases of approaching props.
    public static class LevelMapProjection
    {
        static float cachedPerspective = -1;
        static float radius, eyeHeight;
        static float behind, pitchSin, pitchCos, tangentAngle, nearDepth, nearRatio, ratioSpan;

        static void Prepare(LevelMapConfig config)
        {
            if (cachedPerspective == config.perspective && radius == config.worldRadius && eyeHeight == config.cameraHeight) return;
            cachedPerspective = config.perspective;
            radius = Mathf.Max(1, config.worldRadius);
            eyeHeight = Mathf.Max(0.5f, config.cameraHeight);
            behind = 0.6f / Mathf.Max(0.05f, config.perspective);
            var pitch = Mathf.Atan2(eyeHeight, behind + 8);
            pitchSin = Mathf.Sin(pitch);
            pitchCos = Mathf.Cos(pitch);
            tangentAngle = Mathf.Acos(radius / Mathf.Sqrt((radius + eyeHeight) * (radius + eyeHeight) + behind * behind))
                - Mathf.Atan2(behind, radius + eyeHeight);
            CameraPoint(0, out var nearY, out nearDepth);
            CameraPoint(tangentAngle, out var crestY, out var crestZ);
            nearRatio = nearY / nearDepth;
            ratioSpan = crestY / crestZ - nearRatio;
        }

        static void CameraPoint(float angle, out float vertical, out float depth)
        {
            var y = radius * (Mathf.Cos(angle) - 1) - eyeHeight;
            var z = radius * Mathf.Sin(angle) + behind;
            vertical = y * pitchCos + z * pitchSin;
            depth = -y * pitchSin + z * pitchCos;
        }

        static float Angle(float distance, LevelMapConfig config) =>
            Mathf.Max(-0.6f, distance) * tangentAngle / Mathf.Max(1, config.curveDepth);

        public static float Scale(float distance, LevelMapConfig config)
        {
            Prepare(config);
            CameraPoint(Angle(distance, config), out _, out var depth);
            return nearDepth / Mathf.Max(0.5f, depth);
        }

        public static float Rise(float distance, LevelMapConfig config)
        {
            Prepare(config);
            CameraPoint(Angle(distance, config), out var y, out var z);
            return (y / z - nearRatio) / ratioSpan;
        }

        public static float GroundY(float distance, float x, Vector2 size, LevelMapConfig config)
        {
            var rise = Rise(distance, config);
            var lateral = (x / size.x - 0.5f) * 2;
            return size.y * (Mathf.LerpUnclamped(0.08f, config.horizon, rise)
                - config.crestCurvature * lateral * lateral * Mathf.Clamp01(rise));
        }

        public static float HorizonY(float x, Vector2 size, LevelMapConfig config) =>
            GroundY(config.curveDepth, x, size, config);

        public static float SurfaceFacing(float distance, LevelMapConfig config)
        {
            Prepare(config);
            var angle = Angle(distance, config);
            return Mathf.Clamp01(((radius + eyeHeight) * Mathf.Cos(angle) - behind * Mathf.Sin(angle) - radius) / eyeHeight);
        }

        public static float SurfaceRoll(float distance, float x, Vector2 size, LevelMapConfig config)
        {
            var lateral = x / size.x - 0.5f;
            var slope = -8 * config.crestCurvature * size.y / size.x * lateral * Mathf.Clamp01(Rise(distance, config));
            return Mathf.Clamp(Mathf.Atan(slope) * Mathf.Rad2Deg, -22, 22);
        }

        public static Vector2 Point(float distance, float cursor, Vector2 size, LevelMapConfig config)
        {
            var scale = Scale(distance, config);
            var x = Mathf.Sin((distance + cursor - 1) * 0.72f - 0.25f);
            var screenX = size.x * (0.5f + x * config.bend * scale);
            return new Vector2(screenX, GroundY(distance, screenX, size, config));
        }
    }

    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class CurvedMapRoad : MaskableGraphic
    {
        public LevelMapConfig Config { get; set; }
        public float Cursor { get; set; }
        public int LevelCount { get; set; }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            if (Config == null || LevelCount < 1) return;
            var end = Mathf.Min(Config.curveDepth, LevelCount - Cursor + 0.35f);
            if (end < -0.4f) return;
            Ribbon(mesh, -0.4f, end, 54, Config.roadBorder);
            Ribbon(mesh, -0.4f, end, 44, Config.road);
            // Dashes stay attached to the world as it moves under the finger.
            for (var level = Mathf.FloorToInt(Cursor) - 1; level < Cursor + end; level++)
                Ribbon(mesh, Mathf.Max(-0.4f, level - Cursor),
                    Mathf.Min(end, level - Cursor + 0.44f), 19, Config.roadStripe);
        }

        void Ribbon(VertexHelper mesh, float start, float end, float width, Color tint)
        {
            if (end <= start) return;
            var size = rectTransform.rect.size;
            var origin = rectTransform.rect.min;
            var steps = Mathf.Max(2, Mathf.CeilToInt((end - start) * 12));
            var first = mesh.currentVertCount;
            for (var i = 0; i <= steps; i++)
            {
                var d = Mathf.Lerp(start, end, i / (float)steps);
                var p = LevelMapProjection.Point(d, Cursor, size, Config) + origin;
                var tangent = LevelMapProjection.Point(d + 0.015f, Cursor, size, Config)
                    - LevelMapProjection.Point(d - 0.015f, Cursor, size, Config);
                var normal = new Vector2(tangent.y, -tangent.x).normalized;
                var half = width * 0.5f * LevelMapProjection.Scale(d, Config);
                mesh.AddVert(p - normal * half, tint, Vector2.zero);
                mesh.AddVert(p + normal * half, tint, Vector2.one);
                if (i == 0) continue;
                var v = first + i * 2;
                mesh.AddTriangle(v - 2, v, v - 1);
                mesh.AddTriangle(v - 1, v, v + 1);
            }
        }
    }
}
