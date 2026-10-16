using UnityEngine;

namespace RoyalAves.Meta.WorldMap
{
    // Unico servico matematico da curva: chao, medalhoes, pontes, props, sombras e cliques usam isto.
    // A spherical surface: longitudinal roll and the rounded planetary horizon share one radius.
    public static class MapCurve
    {
        public static Vector3 Point(float s, float x, float h, float radius,float curvature=1)
        {
            var t = s / radius;
            var c = Mathf.Cos(t);
            var sn = Mathf.Sin(t);
            var lateralRadius=radius/Mathf.Max(.1f,curvature);
            var lateral = x / lateralRadius;
            var ring = radius-lateralRadius+(lateralRadius+h)*Mathf.Cos(lateral);
            return new Vector3((lateralRadius+h)*Mathf.Sin(lateral), ring*c-radius, ring*sn);
        }

        public static Vector3 Normal(float s, float radius, float x = 0,float curvature=1)
        {
            var t = s / radius;
            var lateral = x*curvature/radius;
            return new Vector3(Mathf.Sin(lateral), Mathf.Cos(lateral) * Mathf.Cos(t), Mathf.Cos(lateral) * Mathf.Sin(t));
        }

        public static Vector3 Tangent(float s, float radius)
        {
            var t = s / radius;
            return new Vector3(0, -Mathf.Sin(t), Mathf.Cos(t));
        }

        public static Quaternion Orientation(float s, float radius, float x = 0,float curvature=1) =>
            Quaternion.LookRotation(Tangent(s, radius), Normal(s, radius, x,curvature));
    }
}
