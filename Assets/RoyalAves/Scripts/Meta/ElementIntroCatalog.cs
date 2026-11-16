// Catálogo das telas "Novo item desbloqueado!": uma entrada por elemento introduzido numa fase. Cada entrada guarda a
// fase em que aparece, a imagem com o nome do elemento (uma por idioma) e o texto do retângulo (português e inglês).
//
// Edite pelo menu Royal Aves > Introdução de elemento > Editor (ou pelo botão no editor de fases). O asset fica em
// Resources/ElementIntros/ElementIntroCatalog.asset; a tela em si é o prefab Resources/UI/ElementIntroScreen.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "ElementIntroCatalog", menuName = "Royal Aves/Catálogo de introduções de elemento")]
    public sealed class ElementIntroCatalog : ScriptableObject
    {
        public const string ResourcePath = "ElementIntros/ElementIntroCatalog";

        [Serializable]
        public class Entry
        {
            [Tooltip("Identificador legível (ex.: grama, helice).")] public string id;
            [Tooltip("Fase em que a tela aparece (0 = desligada).")] public int level = 1;
            [Tooltip("Prefab em Resources de onde sai o ícone (ex.: Blocks/SugarSquare). Vazio se usar a imagem abaixo.")]
            public string iconResource;
            [Tooltip("Imagem do elemento; se vazia, usa a do prefab acima.")] public Sprite iconOverride;

            [Header("Nome do elemento (imagem por idioma)")]
            public Sprite nameSpritePt;
            public Sprite nameSpriteEn;
            [Tooltip("Mostrado como texto enquanto não houver imagem de nome.")] public string titlePt;
            public string titleEn;

            [Header("Texto do retângulo (use <hl>palavra</hl> para destacar)")]
            [TextArea(2, 4)] public string instructionPt;
            [TextArea(2, 4)] public string instructionEn;

            public string DisplayName => string.IsNullOrEmpty(titlePt) ? id : titlePt;
        }

        public List<Entry> entries = new List<Entry>();

        static ElementIntroCatalog instance;
        public static ElementIntroCatalog Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<ElementIntroCatalog>(ResourcePath);
                return instance;
            }
        }

        public static Entry FindByLevel(int level)
        {
            var catalog = Instance;
            return catalog == null ? null : catalog.entries.Find(e => e != null && e.level == level && level > 0);
        }
    }
}
