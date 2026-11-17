// Configuração visual da modal de compra de power-up (BoostShop): para cada BoostType, a figura e o nome em
// imagem (um sprite em Português e outro em Inglês). Edite pelo Inspector do asset
// Resources/BoostShop/BoostShopCatalog (menu Royal Aves > Modal de compra de power-up > Criar/selecionar catálogo).
using System;
using System.Collections.Generic;
using SweetSugar.Scripts.GUI.Boost;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "BoostShopCatalog", menuName = "Royal Aves/Catálogo da modal de compra de power-up")]
    public sealed class BoostShopCatalog : ScriptableObject
    {
        public const string ResourcePath = "BoostShop/BoostShopCatalog";

        [Serializable]
        public class Entry
        {
            public BoostType type = BoostType.Arrow;
            [Tooltip("Figura do power-up no centro da modal. Vazio = mantém a figura que já está no prefab.")]
            public Sprite icon;
            [Tooltip("Nome em imagem, Português (a faixa rosa lá em cima).")]
            public Sprite namePt;
            [Tooltip("Nome em imagem, Inglês. Outros idiomas usam este.")]
            public Sprite nameEn;
            [Tooltip("Nome em TEXTO que entra no modelo da descrição, Português (ex.: \"bola de luz\").")]
            public string displayNamePt;
            [Tooltip("Nome em TEXTO que entra no modelo da descrição, Inglês (ex.: \"Light ball\").")]
            public string displayNameEn;
            [Tooltip("OPCIONAL: descrição completa só deste power-up, Português. Se preenchida, ignora o modelo.")]
            [TextArea(2, 4)] public string descriptionPt;
            [Tooltip("OPCIONAL: descrição completa só deste power-up, Inglês. Se preenchida, ignora o modelo.")]
            [TextArea(2, 4)] public string descriptionEn;
        }

        [Header("Modelo da descrição (vale para todos os power-ups)")]
        [Tooltip("{nome} é trocado pelo Display Name Pt de cada power-up.")]
        public string descriptionTemplatePt = "Comece o nível com {nome}";
        [Tooltip("{nome} (ou {name}) é trocado pelo Display Name En de cada power-up.")]
        public string descriptionTemplateEn = "Start the level with {nome}";

        [Header("Power-ups")]
        public List<Entry> entries = new List<Entry>();

        static BoostShopCatalog instance;
        public static BoostShopCatalog Instance
        {
            get
            {
                if (instance == null) instance = Resources.Load<BoostShopCatalog>(ResourcePath);
                return instance;
            }
        }

        public static Entry Find(BoostType type)
        {
            var catalog = Instance;
            return catalog == null ? null : catalog.entries.Find(e => e != null && e.type == type);
        }
    }
}
