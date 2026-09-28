// The lobby's bottom-menu screens, after the web prototype (royal-aves/src/menus.js and menus.css): Eventos, Recordes,
// Coleção and Equipes, plus Inventário (from the coin counter) and Perfil (from the avatar). One screen covers the lobby
// above the bottom navigation; its lists are built here, from the saved progress, each time it opens. It is created by
// LobbyController when the game runs, so the lobby prefab itself is not changed.
using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts;
using SweetSugar.Scripts.Core;
using SweetSugar.Scripts.GUI.Boost;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class LobbyMenus : MonoBehaviour
    {
        public enum Route { Home, Events, Records, Teams, Collection, Inventory, Profile }

        // 1% of the lobby canvas width (1080), the prototype's cqw unit.
        const float Cq = 10.8f;

        static readonly Color Cream = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        static readonly Color Ink = new Color32(0x17, 0x3E, 0x5D, 0xFF);
        static readonly Color Muted = new Color32(0x47, 0x62, 0x77, 0xFF);
        static readonly Color ScreenColor = new Color32(0x3A, 0x50, 0x67, 0xFF);
        static readonly Color HeaderColor = new Color32(0x08, 0x6E, 0xBE, 0xFF);
        static readonly Color Gold = new Color32(0xFF, 0xD3, 0x4C, 0xFF);
        static readonly Color RankGold = new Color32(0xD7, 0x90, 0x19, 0xFF);
        static readonly Color TabBack = new Color32(0x21, 0x39, 0x4F, 0xFF);
        static readonly Color TabColor = new Color32(0x4F, 0x6F, 0x87, 0xFF);
        static readonly Color TabOn = new Color32(0x85, 0xB5, 0xD6, 0xFF);
        static readonly Color FormColor = new Color32(0x45, 0x60, 0x77, 0xFF);
        static readonly Color CaptionColor = new Color32(0xEF, 0xF6, 0xFA, 0xFF);
        static readonly Color LockedTint = new Color(0.55f, 0.6f, 0.68f, 1);
        static readonly Color PostalTop = new Color32(0xFB, 0xFC, 0xE8, 0xFF);
        static readonly Color PostalBottom = new Color32(0xEE, 0x9A, 0x10, 0xFF);

        static readonly (BoostType type, string name)[] ShopBoosters =
        {
            (BoostType.Stripes, "Foguete"),
            (BoostType.Packages, "Pacote explosivo"),
            (BoostType.MulticolorCandy, "Bomba de cartas"),
            (BoostType.Marmalade, "Avião"),
            (BoostType.Bomb, "Martelo"),
            (BoostType.ExplodeArea, "Canhão"),
            (BoostType.FreeMove, "Troca livre"),
            (BoostType.ExtraMoves, "+5 movimentos"),
        };

        LobbyFeaturesConfig art;
        Action<int> playLevel;
        Action<string, string> message;
        Action changed;
        RectTransform list;
        TextMeshProUGUI heading;
        ScrollRect scroll;
        Route route;

        string teamTab = "join";
        string query = "";
        bool byScore = true;
        string formName = "", formDescription = "", formMinLevel = "0", formFeedback = "";
        int formEmblem;
        bool formOpen = true;
        string profileName = "";
        int profileAvatar;

        public Route Current => route;
        public event Action<Route> Opened;

        public static LobbyMenus Create(RectTransform content, RectTransform nav, LobbyFeaturesConfig art,
            Action<int> playLevel, Action<string, string> message, Action changed)
        {
            var screen = Node("Menus", content);
            screen.SetSiblingIndex(nav.GetSiblingIndex()); // the navigation stays on top
            Stretch(screen);
            screen.offsetMin = new Vector2(0, nav.rect.height);
            var back = screen.gameObject.AddComponent<Image>();
            back.color = ScreenColor; // also blocks taps on the lobby behind it

            var menus = screen.gameObject.AddComponent<LobbyMenus>();
            menus.art = art;
            menus.playLevel = playLevel;
            menus.message = message;
            menus.changed = changed;

            var header = Node("Cabecalho", screen);
            header.anchorMin = new Vector2(0, 1);
            header.anchorMax = Vector2.one;
            header.pivot = new Vector2(0.5f, 1);
            header.sizeDelta = new Vector2(0, 20 * Cq);
            header.gameObject.AddComponent<Image>().color = HeaderColor;
            var line = Node("Linha", header);
            line.anchorMin = Vector2.zero;
            line.anchorMax = new Vector2(1, 0);
            line.pivot = new Vector2(0.5f, 1);
            line.sizeDelta = new Vector2(0, Cq);
            line.gameObject.AddComponent<Image>().color = Gold;
            menus.heading = menus.Text(header, "", 8 * Cq, Cream, TextAlignmentOptions.Center, false);
            Stretch(menus.heading.rectTransform);
            menus.heading.rectTransform.offsetMin = new Vector2(4 * Cq, 0);
            menus.heading.rectTransform.offsetMax = new Vector2(-4 * Cq, -4 * Cq);
            menus.Postal(menus.heading);

            var scrollRect = Node("Lista", screen);
            Stretch(scrollRect);
            scrollRect.offsetMax = new Vector2(0, -21 * Cq);
            menus.scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
            menus.scroll.horizontal = false;
            menus.scroll.scrollSensitivity = 40;
            var viewport = Node("Area", scrollRect);
            Stretch(viewport);
            viewport.gameObject.AddComponent<Image>().color = Color.clear; // lets the list be dragged anywhere
            viewport.gameObject.AddComponent<RectMask2D>();
            menus.list = Node("Itens", viewport);
            menus.list.anchorMin = new Vector2(0, 1);
            menus.list.anchorMax = Vector2.one;
            menus.list.pivot = new Vector2(0.5f, 1);
            menus.list.sizeDelta = Vector2.zero;
            var layout = menus.list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset((int)(3.5f * Cq), (int)(3.5f * Cq), (int)(4 * Cq), (int)(6 * Cq));
            layout.spacing = 2.8f * Cq;
            layout.childControlWidth = layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            menus.list.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            menus.scroll.viewport = viewport;
            menus.scroll.content = menus.list;

            screen.gameObject.SetActive(false);
            return menus;
        }

        public void Open(Route next)
        {
            var newRoute = next != route;
            route = next;
            gameObject.SetActive(next != Route.Home);
            if (next != Route.Home)
            {
                if (newRoute && next == Route.Profile)
                {
                    profileName = MetaProgress.ProfileName;
                    profileAvatar = MetaProgress.Avatar;
                }
                Render();
                if (newRoute) scroll.verticalNormalizedPosition = 1;
            }
            Opened?.Invoke(next);
        }

        public void Close() => Open(Route.Home);

        void Render()
        {
            for (var i = list.childCount - 1; i >= 0; i--)
            {
                var child = list.GetChild(i).gameObject;
                child.SetActive(false);
                Destroy(child);
            }
            switch (route)
            {
                case Route.Events: RenderEvents(); break;
                case Route.Records: RenderRecords(); break;
                case Route.Collection: RenderCollection(); break;
                case Route.Teams: RenderTeams(); break;
                case Route.Inventory: RenderInventory(); break;
                case Route.Profile: RenderProfile(); break;
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(list);
        }

        void Done()
        {
            changed?.Invoke();
            Render();
        }

        // ---------- screens ----------

        void RenderEvents()
        {
            heading.text = "Eventos";
            Caption("Complete entregas e recolha suas recompensas.");
            var won = MetaProgress.LevelsWon;
            foreach (var mission in art.missions)
            {
                var claimed = MetaProgress.IsMissionClaimed(mission.id);
                var done = Mathf.Min(mission.goal, won);
                var (row, column) = Card(art.iconEvents);
                Title(column, mission.title);
                Progress(column, done / (float)Mathf.Max(1, mission.goal));
                Line(column, $"{done}/{mission.goal} fases · {CoinFormat.Short(mission.coins)} moedas");
                ActionButton(row, claimed ? "Recebido" : "Recolher", !claimed && done >= mission.goal, () =>
                {
                    if (MetaProgress.ClaimMission(mission)) Done();
                });
            }
            Section("Entrega de moedas");
            foreach (var after in art.bonusRoutesAfter)
            {
                var (row, column) = Card(art.coin);
                Title(column, $"Rota bônus {after}");
                Line(column, $"{art.bonusRouteMoves} movimentos para coletar moedas.");
                ActionButton(row, won < after ? $"Após {after} fases" : "Em breve", false, null);
            }
        }

        void RenderRecords()
        {
            heading.text = "Recordes";
            Caption("Seus resultados neste aparelho");
            Tabs(new[] { "Pontuação", "Fases" }, byScore ? 0 : 1, i =>
            {
                byScore = i == 0;
                Render();
            });
            var records = MetaProgress.Records.Where(r => r.wins > 0);
            records = byScore ? records.OrderByDescending(r => r.score) : records.OrderBy(r => r.level);
            var rows = records.ToList();
            if (rows.Count == 0)
            {
                Empty(art.iconRecords, "Sua primeira marca está por vir", "Conclua uma fase para registrar sua pontuação.",
                    "Jogar", () => playLevel?.Invoke(MetaProgress.NextLevel));
                return;
            }
            for (var i = 0; i < rows.Count; i++)
            {
                var record = rows[i];
                var (row, column) = Card(null);
                var rank = Text(row, (i + 1).ToString(), 9 * Cq, RankGold, TextAlignmentOptions.Center, false);
                rank.transform.SetAsFirstSibling();
                Size(rank, 11 * Cq, 13 * Cq);
                Title(column, $"Nível {record.level}");
                Line(column, $"{record.score} pontos · {record.wins} vitória{(record.wins == 1 ? "" : "s")}");
                ActionButton(row, "Jogar", true, () => playLevel?.Invoke(record.level));
            }
        }

        void RenderCollection()
        {
            heading.text = "Coleção";
            var won = MetaProgress.LevelsWon;
            var total = art.album.Count;
            Caption($"Álbum postal · {Mathf.Min(won, total)}/{total}");

            var grid = Node("Album", list);
            var gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 2;
            gridLayout.spacing = new Vector2(3 * Cq, 3 * Cq);
            gridLayout.cellSize = new Vector2((100 - 7 - 3) / 2f * Cq, 38 * Cq);
            gridLayout.childAlignment = TextAnchor.UpperCenter;
            for (var i = 0; i < total; i++)
            {
                var index = i;
                var card = art.album[i];
                var owned = won > i;
                var cell = Node("Selo " + (i + 1), grid);
                var background = Sliced(cell, art.buttonBlue, owned ? Color.white : LockedTint);
                background.raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = background;
                button.onClick.AddListener(() =>
                {
                    Click();
                    message?.Invoke(card.title, owned ? "Selo conquistado!" : $"Conclua o nível {index + 1} para conquistar este selo.");
                });
                var column = cell.gameObject.AddComponent<VerticalLayoutGroup>();
                column.padding = new RectOffset((int)(3 * Cq), (int)(3 * Cq), (int)(3 * Cq), (int)(3 * Cq));
                column.spacing = Cq;
                column.childAlignment = TextAnchor.UpperCenter;
                column.childControlWidth = column.childControlHeight = true;
                column.childForceExpandWidth = true;
                column.childForceExpandHeight = false;
                var picture = Icon(cell, card.picture, 22 * Cq);
                picture.color = owned ? Color.white : new Color(0.05f, 0.1f, 0.2f, 0.45f); // a dark silhouette until won
                Text(cell, owned ? card.title : "Bloqueado", 4 * Cq, Cream, TextAlignmentOptions.Center, false);
                Text(cell, $"Nível {i + 1}", 3 * Cq, Cream, TextAlignmentOptions.Center, false);
            }

            var (row, textColumn) = Card(art.chest);
            Title(textColumn, "Álbum completo");
            Line(textColumn, $"{CoinFormat.Short(art.albumReward)} moedas");
            var claimed = MetaProgress.AlbumClaimed;
            ActionButton(row, claimed ? "Recebido" : "Recolher", !claimed && total > 0 && won >= total, () =>
            {
                if (MetaProgress.ClaimAlbum(art)) Done();
            });
        }

        void RenderTeams()
        {
            heading.text = "Equipes";
            Caption("Equipes locais · salvas neste aparelho");
            var tabs = new[] { "join", "search", "create" };
            Tabs(new[] { "Participar", "Procurar", "Criar" }, Array.IndexOf(tabs, teamTab), i =>
            {
                teamTab = tabs[i];
                query = "";
                formFeedback = "";
                Render();
            });

            var current = MetaProgress.CurrentTeam;
            if (current != null)
            {
                var (row, column) = Card(null);
                Emblem(row, current.emblem, 12 * Cq).transform.SetAsFirstSibling();
                Title(column, current.name);
                Line(column, "Você participa · 1 membro");
                ActionButton(row, "Sair", true, () =>
                {
                    MetaProgress.LeaveTeam();
                    Done();
                });
            }

            if (teamTab == "create")
            {
                CreateTeamForm(current != null);
                return;
            }
            if (teamTab == "search")
            {
                var search = Node("Busca", list);
                var searchRow = search.gameObject.AddComponent<HorizontalLayoutGroup>();
                searchRow.spacing = 2 * Cq;
                searchRow.childControlWidth = searchRow.childControlHeight = true;
                searchRow.childForceExpandWidth = false;
                var field = Input(search, query, "Nome da equipe…", 30, false, v => query = v);
                Size(field, -1, 11 * Cq, 1);
                ActionButton(search, "Procurar", true, Render, 28 * Cq);
            }
            var teams = MetaProgress.Teams.Where(t => string.IsNullOrEmpty(query) ||
                                                      t.name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            if (teams.Count == 0)
            {
                Empty(art.iconTeams, string.IsNullOrEmpty(query) ? "Crie sua primeira equipe" : "Nenhuma equipe encontrada",
                    "Este modo não conecta outros aparelhos nem simula jogadores.", "Criar equipe", () =>
                    {
                        teamTab = "create";
                        Render();
                    });
                return;
            }
            foreach (var team in teams)
            {
                var (row, column) = Card(null);
                Emblem(row, team.emblem, 12 * Cq).transform.SetAsFirstSibling();
                Title(column, team.name);
                Line(column, $"{(team.id == MetaProgress.TeamId ? 1 : 0)}/50 · {(team.open ? "Aberta" : "Fechada")} · nível mínimo {team.minLevel}");
                if (MetaProgress.CanJoin(team))
                    ActionButton(row, "Participar", true, () =>
                    {
                        if (MetaProgress.JoinTeam(team.id)) Done();
                    });
                else
                    ActionButton(row, "Ver", true, () => message?.Invoke(team.name,
                        (string.IsNullOrEmpty(team.description) ? "Equipe postal" : team.description) +
                        $"\n{(team.open ? "Aberta" : "Fechada")} · Nível mínimo {team.minLevel}" +
                        (team.id == MetaProgress.TeamId ? "\nVocê participa desta equipe." : "")));
            }
        }

        void CreateTeamForm(bool alreadyInTeam)
        {
            var form = Panel();
            Label(form, "Emblema");
            var emblems = Node("Emblemas", form);
            var emblemRow = emblems.gameObject.AddComponent<GridLayoutGroup>();
            emblemRow.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            emblemRow.constraintCount = 4;
            emblemRow.cellSize = new Vector2(14 * Cq, 14 * Cq);
            emblemRow.spacing = new Vector2(2 * Cq, 2 * Cq);
            emblemRow.childAlignment = TextAnchor.UpperCenter;
            for (var i = 0; i < art.teamEmblems.Count; i++)
            {
                var index = i;
                var emblem = Emblem(emblems, i, 14 * Cq);
                emblem.color = i == formEmblem ? Gold : Color.white;
                emblem.raycastTarget = true;
                var button = emblem.gameObject.AddComponent<Button>();
                button.targetGraphic = emblem;
                button.onClick.AddListener(() =>
                {
                    Click();
                    formEmblem = index;
                    Render();
                });
            }
            Label(form, "Nome");
            Size(Input(form, formName, "Nome da equipe", 30, false, v => formName = v), -1, 11 * Cq);
            Label(form, "Descrição");
            Size(Input(form, formDescription, "Apresente sua equipe", 120, true, v => formDescription = v), -1, 18 * Cq);
            Label(form, "Tipo");
            TabsIn(form, new[] { "Aberta", "Fechada" }, formOpen ? 0 : 1, i =>
            {
                formOpen = i == 0;
                Render();
            });
            Label(form, "Nível mínimo");
            Size(Input(form, formMinLevel, "0", 4, false, v => formMinLevel = v, true), -1, 11 * Cq);
            if (!string.IsNullOrEmpty(formFeedback)) Text(form, formFeedback, 3.4f * Cq, new Color32(0xFF, 0xF3, 0xA7, 0xFF));
            var create = ActionButton(form, $"Criar · {CoinFormat.Short(art.teamCreateCost)} moedas", !alreadyInTeam, () =>
            {
                int.TryParse(formMinLevel, out var minLevel);
                var error = MetaProgress.CreateTeam(formName, formDescription, formEmblem, minLevel, formOpen, art.teamCreateCost);
                if (error != null)
                {
                    formFeedback = error;
                    Render();
                    return;
                }
                formName = formDescription = formFeedback = "";
                formMinLevel = "0";
                teamTab = "join";
                Done();
            }, -1);
            Size(create, -1, 12 * Cq);
        }

        void RenderInventory()
        {
            heading.text = "Inventário";
            var balance = Node("Saldo", list);
            var balanceRow = balance.gameObject.AddComponent<HorizontalLayoutGroup>();
            balanceRow.childAlignment = TextAnchor.MiddleCenter;
            balanceRow.spacing = 3 * Cq;
            balanceRow.childControlWidth = balanceRow.childControlHeight = true;
            balanceRow.childForceExpandWidth = false;
            Icon(balance, art.coin, 11 * Cq);
            Postal(Text(balance, CoinFormat.Short(InitScript.Gems), 8 * Cq, Cream, TextAlignmentOptions.Center, false));
            Caption("Use as moedas das fases e das entregas.");

            Section("Reforços");
            var shop = MenuReference.THIS != null && MenuReference.THIS.BoostShop != null
                ? MenuReference.THIS.BoostShop.GetComponent<BoostShop>() : null;
            foreach (var (type, name) in ShopBoosters)
            {
                var product = shop != null ? shop.boostProducts.FirstOrDefault(p => p.boostType == type) : null;
                if (product == null) continue;
                var (row, column) = Card(product.icon);
                Title(column, name);
                Line(column, $"{PlayerPrefs.GetInt(type.ToString())} guardados · pacote com {product.count}");
                var price = product.GemPrices;
                ActionButton(row, $"{CoinFormat.Short(price)} moedas", InitScript.Gems >= price && InitScript.Instance != null, () =>
                {
                    if (InitScript.Gems < price) return;
                    InitScript.Instance.SpendGems(price);
                    InitScript.Instance.BuyBoost(type, price, product.count);
                    Done();
                });
            }
            if (shop == null) Caption("A loja de reforços do Sweet Sugar não foi encontrada nesta cena.");

            var init = InitScript.Instance;
            if (init != null)
            {
                var (row, column) = Card(art.heart);
                Title(column, "Recuperar vidas");
                Line(column, $"{InitScript.lifes}/{init.CapOfLife}");
                var cost = art.livesRefillCost;
                ActionButton(row, $"{CoinFormat.Short(cost)} moedas", InitScript.Gems >= cost && InitScript.lifes < init.CapOfLife, () =>
                {
                    if (InitScript.Gems < cost || InitScript.lifes >= init.CapOfLife) return;
                    init.SpendGems(cost);
                    init.RestoreLifes();
                    Done();
                });
            }
            Size(ActionButton(list, "Ganhar moedas", true, () => Open(Route.Events), -1), -1, 13 * Cq);
        }

        void RenderProfile()
        {
            heading.text = "Seu perfil";
            var form = Panel();
            Label(form, "Nome");
            Size(Input(form, profileName, "Mensageiro", 30, false, v => profileName = v), -1, 11 * Cq);
            Label(form, "Avatar");
            var avatars = Node("Avatares", form);
            var grid = avatars.gameObject.AddComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 5;
            grid.cellSize = new Vector2(15 * Cq, 15 * Cq);
            grid.spacing = new Vector2(2 * Cq, 2 * Cq);
            grid.childAlignment = TextAnchor.UpperCenter;
            for (var i = 0; i < art.avatars.Count; i++)
            {
                var index = i;
                var cell = Node("Avatar " + i, avatars);
                var back = Sliced(cell, art.token, i == profileAvatar ? Gold : Color.white);
                back.raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>();
                button.targetGraphic = back;
                button.onClick.AddListener(() =>
                {
                    Click();
                    profileAvatar = index;
                    Render();
                });
                var picture = Node("Imagem", cell);
                Stretch(picture);
                picture.offsetMin = Vector2.one * 1.5f * Cq;
                picture.offsetMax = Vector2.one * -1.5f * Cq;
                var image = picture.gameObject.AddComponent<Image>();
                image.sprite = art.avatars[i];
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
            Size(ActionButton(form, "Salvar", true, () =>
            {
                MetaProgress.SetProfile(profileName, profileAvatar);
                changed?.Invoke();
                message?.Invoke("Seu perfil", "Perfil salvo.");
            }, -1), -1, 12 * Cq);
        }

        // ---------- building blocks ----------

        static RectTransform Node(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static LayoutElement Size(Component target, float width = -1, float height = -1, float flexibleWidth = -1)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null) element = target.gameObject.AddComponent<LayoutElement>();
            if (width >= 0) element.preferredWidth = element.minWidth = width;
            if (height >= 0) element.preferredHeight = element.minHeight = height;
            if (flexibleWidth >= 0) element.flexibleWidth = flexibleWidth;
            return element;
        }

        static Image Sliced(RectTransform rect, Sprite sprite, Color color)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        TextMeshProUGUI Text(Transform parent, string value, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, bool wrap = true)
        {
            var text = Node("Texto", parent).gameObject.AddComponent<TextMeshProUGUI>();
            if (art.displayFont != null) text.font = art.displayFont;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            return text;
        }

        // The prototype's "postal-text": the CorreioMagico font with cream-to-orange letters and a blue outline.
        void Postal(TextMeshProUGUI text)
        {
            if (art.postalFont == null) return;
            text.font = art.postalFont;
            if (art.postalMaterial != null) text.fontSharedMaterial = art.postalMaterial;
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(PostalTop, PostalTop, PostalBottom, PostalBottom);
        }

        Image Icon(Transform parent, Sprite sprite, float size)
        {
            var image = Node("Icone", parent).gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            Size(image, size, size);
            return image;
        }

        // A cream card: optional icon, a column for texts (returned) and room for a button at the end of the row.
        (RectTransform row, RectTransform column) Card(Sprite icon)
        {
            var card = Node("Cartao", list);
            Sliced(card, art.card, Color.white);
            var row = card.gameObject.AddComponent<HorizontalLayoutGroup>();
            var pad = (int)(2.5f * Cq);
            row.padding = new RectOffset(pad, pad, pad, pad);
            row.spacing = 2 * Cq;
            row.childAlignment = TextAnchor.MiddleLeft;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = false;
            Size(card).minHeight = 15 * Cq;
            if (icon != null) Icon(card, icon, 12 * Cq);
            var column = Node("Textos", card);
            var stack = column.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 0.8f * Cq;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            Size(column, -1, -1, 1);
            return (card, column);
        }

        void Title(Transform column, string value) => Text(column, value, 4.5f * Cq, Ink);

        void Line(Transform column, string value) => Text(column, value, 3.1f * Cq, Muted);

        void Caption(string value) => Text(list, value, 3.7f * Cq, CaptionColor, TextAlignmentOptions.Center);

        void Section(string value) => Text(list, value, 5 * Cq, Cream, TextAlignmentOptions.Center);

        void Label(Transform parent, string value) => Text(parent, value, 4.2f * Cq, Cream);

        void Progress(Transform column, float value)
        {
            var bar = Node("Barra", column);
            Sliced(bar, art.barBackground, Color.white);
            Size(bar, -1, 3.6f * Cq);
            var fill = Node("Preenchimento", bar);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            Sliced(fill, art.barFill, Color.white);
            fill.gameObject.SetActive(value > 0);
        }

        Button ActionButton(Transform parent, string label, bool enabled, Action onClick, float width = 26 * Cq)
        {
            var rect = Node("Botao", parent);
            var image = Sliced(rect, art.buttonGreen, Color.white);
            image.raycastTarget = true;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.75f);
            button.colors = colors;
            button.interactable = enabled && onClick != null;
            if (onClick != null)
                button.onClick.AddListener(() =>
                {
                    Click();
                    onClick();
                });
            var text = Text(rect, label, 3.7f * Cq, Cream, TextAlignmentOptions.Center, false);
            Stretch(text.rectTransform);
            text.rectTransform.offsetMin = new Vector2(1.5f * Cq, 1.2f * Cq);
            text.rectTransform.offsetMax = new Vector2(-1.5f * Cq, -Cq);
            text.enableAutoSizing = true;
            text.fontSizeMin = 2.4f * Cq;
            text.fontSizeMax = 3.7f * Cq;
            if (width >= 0) Size(rect, width, 11 * Cq);
            else Size(rect, -1, 11 * Cq);
            return button;
        }

        void Tabs(string[] labels, int selected, Action<int> choose) => TabsIn(list, labels, selected, choose);

        void TabsIn(Transform parent, string[] labels, int selected, Action<int> choose)
        {
            var back = Node("Abas", parent);
            Sliced(back, art.pill, TabBack);
            var row = back.gameObject.AddComponent<HorizontalLayoutGroup>();
            var pad = (int)(0.8f * Cq);
            row.padding = new RectOffset(pad, pad, pad, pad);
            row.spacing = 0.8f * Cq;
            row.childControlWidth = row.childControlHeight = true;
            row.childForceExpandWidth = row.childForceExpandHeight = true;
            Size(back, -1, 11 * Cq);
            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var tab = Node(labels[i], back);
                var image = Sliced(tab, art.pill, i == selected ? TabOn : TabColor);
                image.raycastTarget = true;
                var button = tab.gameObject.AddComponent<Button>();
                button.targetGraphic = image;
                button.onClick.AddListener(() =>
                {
                    Click();
                    choose(index);
                });
                var text = Text(tab, labels[i], 4.2f * Cq, i == selected ? Ink : Cream, TextAlignmentOptions.Center, false);
                Stretch(text.rectTransform);
            }
        }

        void Empty(Sprite icon, string title, string copy, string buttonLabel, Action action)
        {
            var box = Node("Vazio", list);
            var stack = box.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.padding = new RectOffset((int)(3 * Cq), (int)(3 * Cq), (int)(8 * Cq), (int)(3 * Cq));
            stack.spacing = 2.5f * Cq;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = false;
            stack.childForceExpandHeight = false;
            Icon(box, icon, 24 * Cq);
            Text(box, title, 6 * Cq, Cream, TextAlignmentOptions.Center);
            Text(box, copy, 3.7f * Cq, CaptionColor, TextAlignmentOptions.Center);
            ActionButton(box, buttonLabel, true, action, 40 * Cq);
        }

        RectTransform Panel()
        {
            var panel = Node("Formulario", list);
            Sliced(panel, art.darkBox, FormColor);
            var stack = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            var pad = (int)(4 * Cq);
            stack.padding = new RectOffset(pad, pad, pad, pad);
            stack.spacing = 2.5f * Cq;
            stack.childControlWidth = stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            return panel;
        }

        Image Emblem(Transform parent, int index, float size)
        {
            var back = Node("Emblema", parent);
            var image = Sliced(back, art.token, Color.white);
            Size(back, size, size);
            var picture = Node("Simbolo", back);
            Stretch(picture);
            picture.offsetMin = Vector2.one * size * 0.18f;
            picture.offsetMax = Vector2.one * -size * 0.18f;
            var symbol = picture.gameObject.AddComponent<Image>();
            symbol.sprite = art.teamEmblems.Count == 0 ? null : art.teamEmblems[Mathf.Clamp(index, 0, art.teamEmblems.Count - 1)];
            symbol.preserveAspect = true;
            symbol.raycastTarget = false;
            return image;
        }

        RectTransform Input(Transform parent, string value, string placeholder, int limit, bool multiline, Action<string> onChange, bool integer = false)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources { inputField = art.card });
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            var field = go.GetComponent<TMP_InputField>();
            field.characterLimit = limit;
            field.lineType = multiline ? TMP_InputField.LineType.MultiLineNewline : TMP_InputField.LineType.SingleLine;
            field.contentType = integer ? TMP_InputField.ContentType.IntegerNumber : TMP_InputField.ContentType.Standard;
            field.pointSize = 4 * Cq;
            if (art.displayFont != null) field.fontAsset = art.displayFont;
            field.textComponent.color = Ink;
            if (field.placeholder is TMP_Text hint)
            {
                hint.text = placeholder;
                hint.color = new Color(Ink.r, Ink.g, Ink.b, 0.45f);
            }
            field.text = value;
            field.onValueChanged.AddListener(v => onChange(v));
            return rect;
        }

        static void Click()
        {
            if (SoundBase.Instance != null) SoundBase.Instance.PlayOneShot(SoundBase.Instance.click);
        }
    }
}
