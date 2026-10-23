using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    // Isolated visual study: no scene, prefab, gameplay or current break changes.
    public sealed class PotionMetaballsPreview : EditorWindow
    {
        const string ShaderPath = "Assets/RoyalAves/Shaders/PotionMetaballsPreview.shader";
        const string Request = "Logs/potion-metaballs.request";
        readonly Vector4[] balls = new Vector4[28];
        Material material;
        RenderTexture target;
        double previous;
        float time;
        float speed = 1;
        float viscosity = .7f;
        bool playing = true;
        bool loop = true;
        Color ink = new Color(.55f, .13f, .9f);

        [MenuItem("Royal Aves/Elementos/Poções/Protótipo de poção metaballs")]
        public static void Open()
        {
            var window = GetWindow<PotionMetaballsPreview>("Poção • Metaballs");
            window.minSize = new Vector2(400, 640);
            window.Show();
        }

        public static void CaptureBatch()
        {
            var preview = CreateInstance<PotionMetaballsPreview>();
            try { preview.Capture(); }
            finally { DestroyImmediate(preview); }
        }

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            EditorApplication.update -= Poll;
            EditorApplication.update += Poll;
        }

        static void Poll()
        {
            if (!File.Exists(Request) || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            File.Delete(Request);
            try
            {
                Open();
                GetWindow<PotionMetaballsPreview>().Capture();
            }
            catch (Exception error)
            {
                File.WriteAllText("Logs/potion-metaballs.result", error.ToString());
                Debug.LogException(error);
            }
        }

        void OnEnable()
        {
            previous = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        void OnDisable()
        {
            EditorApplication.update -= Tick;
            if (material != null) DestroyImmediate(material);
            ReleaseTarget();
        }

        void ReleaseTarget()
        {
            if (target == null) return;
            target.Release();
            DestroyImmediate(target);
            target = null;
        }

        void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (playing)
            {
                time += Mathf.Min((float)(now - previous), .05f) * speed;
                if (time > 2.8f) { if (loop) time = 0; else { time = 2.8f; playing = false; } }
                Repaint();
            }
            previous = now;
        }

        void Render(float sampleTime, int width, int height)
        {
            if (material == null)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(ShaderPath);
                if (shader == null || !shader.isSupported) throw new InvalidOperationException("Shader de metaballs indisponível.");
                material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            }
            if (target == null || target.width != width || target.height != height)
            {
                ReleaseTarget();
                target = new RenderTexture(width, height, 0) { hideFlags = HideFlags.HideAndDontSave };
                target.Create();
            }
            for (int b = 0; b < balls.Length; b++)
            {
                int row = b / 4, col = b % 4;
                float initialX = (col - 1.5f) * .19f + Mathf.Sin(row * 2.4f) * .045f;
                float initialY = .7f - row * .125f;
                // Lower drops leave first. Upper drops follow, stretching bridges
                // until the field separates. Every drop keeps its radius/strength.
                float age = Mathf.Max(0, sampleTime - (6 - row) * .065f * viscosity);
                float vx = (col - 1.5f) * (.12f + row * .022f);
                float vy = .15f + row * .13f + Mathf.Sin(b * 2.7f) * .12f;
                float x = initialX + vx * age + Mathf.Sin(age * 4 + b) * .035f * age;
                float y = initialY - vy * age - 2.8f * age * age;
                balls[b] = new Vector4(x, y, .19f + .035f * (.5f + .5f * Mathf.Sin(b * 1.7f)), 1);
            }
            material.SetVectorArray("_Balls", balls);
            material.SetColor("_Ink", ink);
            material.SetFloat("_Aspect", (float)width / height);
            material.SetFloat("_Fusion", .55f);
            var previousTarget = RenderTexture.active;
            try { Graphics.Blit(Texture2D.blackTexture, target, material); }
            finally { RenderTexture.active = previousTarget; }
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("Poção viscosa • estudo de metaballs", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Superfície contínua que estica e se divide em gotas. Sem fade: as gotas só somem ao sair do enquadramento.", MessageType.None);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(playing ? "Pausar" : "Reproduzir")) playing = !playing;
                if (GUILayout.Button("Recomeçar")) { time = 0; playing = true; }
                loop = GUILayout.Toggle(loop, "Repetir");
            }
            time = EditorGUILayout.Slider("Instante (s)", time, 0, 2.8f);
            speed = EditorGUILayout.Slider("Velocidade", speed, .15f, 1.5f);
            viscosity = EditorGUILayout.Slider("Atraso do escoamento", viscosity, .2f, 1.5f);
            ink = EditorGUILayout.ColorField("Cor da poção", ink);
            var rect = GUILayoutUtility.GetRect(100, 10000, 200, 10000, GUILayout.ExpandHeight(true));
            if (Event.current.type == EventType.Repaint && rect.width > 1 && rect.height > 1)
            {
                Render(time, Mathf.Clamp((int)rect.width, 1, 720), Mathf.Clamp((int)rect.height, 1, 1000));
                GUI.DrawTexture(rect, target, ScaleMode.StretchToFill, false);
            }
            EditorGUILayout.LabelField("Protótipo visual • sem física de colisão • não integrado à quebra", EditorStyles.miniLabel);
        }

        void Capture()
        {
            Directory.CreateDirectory("Docs");
            Directory.CreateDirectory("Logs");
            var sheet = new Texture2D(1440, 640, TextureFormat.RGB24, false);
            var previousTarget = RenderTexture.active;
            try
            {
                float[] samples = { 0, .4f, .8f, 1.2f };
                for (int i = 0; i < samples.Length; i++)
                {
                    Render(samples[i], 360, 640);
                    RenderTexture.active = target;
                    sheet.ReadPixels(new Rect(0, 0, 360, 640), i * 360, 0);
                }
                sheet.Apply();
                File.WriteAllBytes("Docs/Pocao_Metaballs_Unity.png", sheet.EncodeToPNG());
                if (ShaderUtil.ShaderHasError(material.shader)) throw new InvalidOperationException("Erro na compilação do shader metaballs.");
                File.WriteAllText("Logs/potion-metaballs.result", "PASS: native Unity shader rendered at 0 / 0.4 / 0.8 / 1.2s. 28 constant-radius metaballs; isolated editor preview.");
            }
            finally { RenderTexture.active = previousTarget; DestroyImmediate(sheet); }
        }
    }
}
