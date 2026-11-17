// Conteúdo da tela "intermediária" que aparece quando o jogador escolhe um power-up (reforço) no jogo: ícone, nome
// (imagem por idioma) e texto de como usar (português e inglês). Uma entrada por BoostType.
//
// Edite pelo menu Royal Aves > Introdução de power-up > Editor. O asset fica em
// Resources/BoosterIntros/BoosterIntroCatalog; a tela em si é o prefab Resources/UI/BoosterIntroScreen.
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "BoosterIntroCatalog", menuName = "Royal Aves/Catálogo de introduções de power-up")]
    public sealed class BoosterIntroCatalog : ScriptableObject
    {
        public const string ResourcePath = "BoosterIntros/BoosterIntroCatalog";

        [Serializable]
        public class Entry
        {
            [Tooltip("Qual power-up da barra mostra esta tela.")] public BoostType type = BoostType.Arrow;
            [Tooltip("Desligue para este power-up não mostrar a tela.")] public bool enabled = true;

            [Header("Figura")]
            public Sprite icon;
            [Tooltip("Multiplica o tamanho da figura (1 = o do prefab).")] public float iconScale = 1f;
            [Tooltip("Desloca a figura em pixels do canvas (1080x1920).")] public Vector2 iconOffset;

            [Header("Nome (imagem por idioma)")]
            public Sprite nameSpritePt;
            public Sprite nameSpriteEn;
            [Tooltip("Mostrado como texto enquanto não houver imagem de nome.")] public string titlePt;
            public string titleEn;
            [Tooltip("Multiplica o tamanho da imagem do nome.")] public float nameScale = 1f;

            [Header("Como usar (use <hl>palavra</hl> para destacar)")]
            [TextArea(2, 4)] public string instructionPt;
            [TextArea(2, 4)] public string instructionEn;

            public string DisplayName => string.IsNullOrEmpty(titlePt) ? type.ToString() : titlePt;
        }

        public List<Entry> entries = new List<Entry>();

        static BoosterIntroCatalog instance;
        public static BoosterIntroCatalog Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<BoosterIntroCatalog>(ResourcePath);
                return instance;
            }
        }

        public static Entry Find(BoostType type)
        {
            var catalog = Instance;
            return catalog == null ? null : catalog.entries.Find(e => e != null && e.enabled && e.type == type);
        }
    }
}
