// Leva o visual da modal de compra de power-up (BoostShop) para as outras modais do jogo, usando as MESMAS artes
// (Assets/Royal Postman Art/Screens/Modal): moldura roxa e dourada, faixa rosa do título, botão de fechar, painel
// interno, borda laranja dos botões com miolo verde (principal) ou azul (secundário), moeda e ícone de vídeo.
//
// Modais alteradas:
//   - Loja de vidas ......... CanvasGlobal.prefab > LiveShop
//   - Nível falhou .......... CanvasGlobal.prefab > MenuFailed
//   - Nível concluído ....... RoyalAvesLobby.prefab > Victory (a que o jogo mostra de verdade, com o envelope)
//                             e CanvasGlobal.prefab > MenuComplete (a do Sweet Sugar, usada só sem o lobby)
//
// A modal BoostShop NÃO é alterada: ela só é lida (tamanho da moldura, lugar da faixa, do X e dos botões) para as
// outras ficarem iguais. No fim o script confere se algum RectTransform do BoostShop mudou e, se mudou, restaura.
// Preços, recompensas e os cliques dos botões continuam como estavam: só imagens, posições e estilo de texto mudam.
//
// Menu: Royal Aves > Modais > Aplicar visual do BoostShop (vidas, vitória e derrota).
// Também roda sozinho UMA vez depois de compilar (chave EditorPrefs abaixo); rodar de novo só reaplica os mesmos
// valores. Tudo o que dá para ajustar está nas constantes logo abaixo; depois de aplicado, cada objeto também pode
// ser ajustado à mão no prefab (uma nova execução do menu volta aos valores daqui).
using System.Collections.Generic;
using RoyalAves.Meta;
using SweetSugar.Scripts.GUI.Boost;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesModalKitBuilder
    {
        // ---------------------------------------------------------------------------------------------------------
        // AJUSTES
        // ---------------------------------------------------------------------------------------------------------
        const string MenuPath = "Royal Aves/Modais/Aplicar visual do BoostShop (vidas, vitória e derrota)";
        // Troque o final (v1 -> v2) para o script se aplicar sozinho de novo na próxima compilação.
        const string AutoKey = "RoyalAves.ModalKit.aplicado.v1";

        const string CanvasGlobalPath = "Assets/SweetSugar/Prefabs/CanvasGlobal.prefab";
        const string LobbyPath = "Assets/RoyalAves/Prefabs/RoyalAvesLobby.prefab";

        // Artes da modal BoostShop.
        const string Art = "Assets/Royal Postman Art/Screens/Modal/";
        const string FramePath = Art + "modal_base.png";                 // moldura roxa com borda dourada
        const string RibbonPath = Art + "faixa_title.png";               // faixa rosa do título
        const string CloseRingPath = Art + "border_global_rounded.png";  // aro do botão de fechar
        const string CloseCorePath = Art + "red_center.png";             // miolo vermelho do botão de fechar
        const string CloseXPath = "Assets/RoyalAves/Art/UI/UI assets/x_fechar_creme_contorno.png";
        const string ButtonBorderPath = Art + "button_border.png";       // borda laranja dos botões
        const string GreenPath = Art + "button_center.png";              // miolo verde (botão principal)
        const string BluePath = Art + "btn_blue.png";                    // miolo azul (botão secundário)
        const string PanelOuterPath = Art + "pt1.png";                   // painel interno (borda)
        const string PanelInnerPath = Art + "pt2.png";                   // painel interno (miolo creme)
        const string RaysPath = Art + "Explosão Radial Branca Transparente.png";
        const string CoinPath = Art + "coin.png";
        const string VideoIconPath = Art + "Ícone dourado de filme com play vermelho.png";

        // Título em texto sobre a faixa (no BoostShop o título é uma imagem; aqui é texto com o mesmo desenho:
        // letras creme com contorno vinho).
        const string TitleMaterialPath = "Assets/RoyalAves/Fonts/LilitaOne Modal Titulo.mat";
        static readonly Color TitleTop = new Color32(0xFF, 0xF8, 0xDC, 0xFF);
        static readonly Color TitleBottom = new Color32(0xFF, 0xD9, 0x8A, 0xFF);
        static readonly Color TitleOutline = new Color32(0x6B, 0x0F, 0x1A, 0xFF);
        const float TitleOutlineWidth = 0.25f;
        const float TitleWidth = 820, TitleHeight = 170, TitleFontSize = 120;

        // Medidas do BoostShop usadas quando ele não é encontrado (o normal é ler do próprio BoostShop).
        // Tudo em unidades do CanvasGlobal, a partir do centro da moldura: (x, y, largura, altura).
        static readonly Box FrameDefault = new Box(0, 0, 1298, 1574);
        static readonly Box RibbonDefault = new Box(13.9f, 876.6f, 1315.2f, 318.5f);
        static readonly Box TitleDefault = new Box(-16, 843, 625, 150);
        static readonly Box CloseDefault = new Box(606, 920, 239.6f, 216.4f);
        static readonly Box CloseCoreDefault = new Box(0.8f, 2.4f, 195.6f, 180.7f);      // a partir do centro do botão
        static readonly Box CloseXDefault = new Box(2.4f, 1.2f, 117.6f, 112.6f);         // idem
        static readonly Box PanelDefault = new Box(-8.7f, 348.9f, 1023.5f, 649.2f);
        static readonly Box PanelInnerDefault = new Box(1.9f, 3.7f, 969.3f, 589.5f);     // a partir do centro do painel
        static readonly Box PrimaryDefault = new Box(3.7f, -365.3f, 765.9f, 350.2f);
        static readonly Box PrimaryCoreDefault = new Box(-0.7f, -0.6f, 689.7f, 273.5f);  // a partir do centro do botão
        static readonly Box SecondaryDefault = new Box(12, -700.5f, 494, 231);
        static readonly Box SecondaryCoreDefault = new Box(-0.4f, 0.5f, 446.5f, 178.1f); // idem
        static readonly Box VideoIconDefault = new Box(-121.1f, -4.7f, 188.9f, 129.7f);  // idem

        // Loja de vidas: conteúdo dentro do painel interno (deslocamento a partir do centro do painel) e dos botões
        // (a partir do centro de cada botão).
        static readonly Box LifeHeart = new Box(0, 110, 260, 260);
        static readonly Box LifeRefillText = new Box(0, -95, 820, 100);
        static readonly Box LifeTimerText = new Box(0, -215, 820, 80);
        static readonly Box LifeCoin = new Box(-215, 8, 150, 150);
        static readonly Box LifePrice = new Box(95, 6, 400, 210);
        const float LifePriceFontMin = 70, LifePriceFontMax = 170;
        static readonly Box LifeVideoText = new Box(95, 2, 230, 120);
        const float LifeVideoFontMin = 30, LifeVideoFontMax = 62;

        // Nível falhou e Nível concluído (Sweet Sugar): o painel do meio fica onde está, com este tamanho.
        static readonly Vector2 ResultPanelSize = new Vector2(1000, 560);

        // Vitória do lobby (unidades do canvas do lobby, 1080 de largura). A moldura é a mesma arte, esticada para o
        // cartão; a faixa entra no lugar da etiqueta "NÍVEL N" e o texto dela vai para a faixa.
        static readonly Vector2 VictoryCardSize = new Vector2(950.4f, 1500);
        const float VictoryCardCenterY = 20;
        const float VictoryTagFontSize = 64;
        static readonly Vector2 VictoryContinueSize = new Vector2(560, 254);
        // Altura (a partir da borda de baixo do cartão) de cada linha do rodapé.
        const float VictoryReplayY = 135, VictoryContinueY = 313, VictoryTotalY = 478, VictoryRewardY = 554;

        // ---------------------------------------------------------------------------------------------------------

        const string UndoName = "Visual do BoostShop nas modais";
        const string SessionKey = "RoyalAves.ModalKit.tentou";
        static readonly Vector2 Half = new Vector2(0.5f, 0.5f);

        // Na passada da cena não se cria nem move nada na hierarquia (são instâncias de prefab): só valores.
        static bool scenePass;

        struct Box
        {
            public Vector2 center, size;
            public Box(float x, float y, float width, float height) { center = new Vector2(x, y); size = new Vector2(width, height); }
            public Box(Vector2 center, Vector2 size) { this.center = center; this.size = size; }
            public bool Valid => size.x > 1f && size.y > 1f && size.x < 10000f && size.y < 10000f &&
                                 !float.IsNaN(center.x) && !float.IsNaN(center.y);
        }

        class Kit
        {
            public Sprite Frame, Ribbon, CloseRing, CloseCore, CloseX, ButtonBorder, Green, Blue, PanelOuter, PanelInner, Rays, Coin, VideoIcon;
            public TMP_FontAsset Font;
            public Material TitleMaterial;
            public Box frame = FrameDefault, ribbon = RibbonDefault, title = TitleDefault, close = CloseDefault,
                closeCore = CloseCoreDefault, closeX = CloseXDefault, panel = PanelDefault, panelInner = PanelInnerDefault,
                primary = PrimaryDefault, primaryCore = PrimaryCoreDefault, secondary = SecondaryDefault,
                secondaryCore = SecondaryCoreDefault, videoIcon = VideoIconDefault;
            public bool measured;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Menu e execução automática
        // ---------------------------------------------------------------------------------------------------------

        static string ProjectAutoKey => AutoKey + "." + PlayerSettings.productGUID;

        [MenuItem(MenuPath)]
        static void ApplyFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Modais", "Pare o modo Play antes de aplicar o visual das modais.", "OK");
                return;
            }
            if (Apply()) EditorPrefs.SetBool(ProjectAutoKey, true);
        }

        [InitializeOnLoadMethod]
        static void ScheduleAutoApply()
        {
            if (Application.isBatchMode || EditorPrefs.GetBool(ProjectAutoKey, false)) return;
            EditorApplication.delayCall += AutoApply;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall += AutoApply;
            };
        }

        static void AutoApply()
        {
            if (EditorPrefs.GetBool(ProjectAutoKey, false)) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return; // tenta de novo ao sair do Play
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoApply;
                return;
            }
            // Uma tentativa automática por sessão do editor: se algo falhar, o erro não se repete a cada compilação.
            if (SessionState.GetBool(SessionKey, false)) return;
            SessionState.SetBool(SessionKey, true);
            if (Apply()) EditorPrefs.SetBool(ProjectAutoKey, true);
            else Debug.LogWarning("Royal Aves: o visual do BoostShop NÃO foi aplicado às outras modais (veja a mensagem acima). " +
                                  "Para tentar de novo: menu " + MenuPath + ".");
        }

        static bool Apply()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogWarning("Royal Aves: há um prefab aberto para edição. Feche o modo de prefab (seta no topo da Hierarchy) " +
                                 "e rode o menu " + MenuPath + ".");
                return false;
            }

            var kit = LoadKit(out var missing);
            if (missing.Count > 0)
            {
                Debug.LogError("Royal Aves: faltam artes da modal BoostShop, nada foi alterado:\n- " + string.Join("\n- ", missing));
                return false;
            }

            var log = new List<string>();

            // O BoostShop da cena é o que o usuário vê: é dele que saem as medidas. Também é fotografado para conferir
            // no fim que nada nele mudou.
            var sceneShop = FindSceneBoostShop();
            var before = Snapshot(sceneShop);
            if (sceneShop != null) Measure(kit, sceneShop);

            try
            {
                scenePass = false;
                if (!ApplyToCanvasGlobal(kit, log)) return false;
                ApplyToLobby(kit, log);
                ApplyToOpenScenes(kit, log);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Royal Aves: erro ao aplicar o visual do BoostShop nas modais: " + e);
                return false;
            }
            finally
            {
                scenePass = false;
            }

            var restored = RestoreIfChanged(before);
            log.Add(restored == 0
                ? "BoostShop conferido: nenhum tamanho, posição ou escala dele mudou"
                : "ATENÇÃO: " + restored + " objeto(s) do BoostShop tinham mudado e foram restaurados");

            Debug.Log("Royal Aves: visual da modal BoostShop aplicado às outras modais" +
                      (kit.measured ? " (medidas lidas do próprio BoostShop)" : " (BoostShop não encontrado: medidas padrão do script)") +
                      ":\n- " + string.Join("\n- ", log) +
                      "\n\nSe a cena ficou marcada como alterada, salve (Ctrl+S). Para reaplicar: menu " + MenuPath + ".");
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Onde aplicar
        // ---------------------------------------------------------------------------------------------------------

        static bool ApplyToCanvasGlobal(Kit kit, List<string> log)
        {
            var root = PrefabUtility.LoadPrefabContents(CanvasGlobalPath);
            try
            {
                if (!kit.measured)
                {
                    var shop = root.GetComponentInChildren<BoostShop>(true);
                    if (shop != null) Measure(kit, shop);
                }

                var changed = false;
                changed |= Run("Loja de vidas (CanvasGlobal > LiveShop)", WindowOf(root.transform, "LiveShop"), w => BuildLifeShop(w, kit, log), log);
                changed |= Run("Nível falhou (CanvasGlobal > MenuFailed)", WindowOf(root.transform, "MenuFailed"), w => BuildFailed(w, kit, log), log);
                changed |= Run("Nível concluído do Sweet Sugar (CanvasGlobal > MenuComplete)", WindowOf(root.transform, "MenuComplete"), w => BuildComplete(w, kit, log), log);
                if (!changed) return true;

                PrefabUtility.SaveAsPrefabAsset(root, CanvasGlobalPath, out var saved);
                if (!saved)
                {
                    Debug.LogError("Royal Aves: não consegui salvar " + CanvasGlobalPath + ".");
                    return false;
                }
                log.Add("prefab salvo: " + CanvasGlobalPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void ApplyToLobby(Kit kit, List<string> log)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPath) == null)
            {
                log.Add("NÃO ENCONTRADO: " + LobbyPath + " (vitória do lobby não alterada)");
                return;
            }
            var root = PrefabUtility.LoadPrefabContents(LobbyPath);
            try
            {
                var victory = root.GetComponentInChildren<VictoryScreen>(true);
                if (victory == null)
                {
                    log.Add("NÃO ENCONTRADO: VictoryScreen em " + LobbyPath);
                    return;
                }
                BuildVictory(victory.transform, kit, log);
                PrefabUtility.SaveAsPrefabAsset(root, LobbyPath, out var saved);
                log.Add(saved ? "prefab salvo: " + LobbyPath : "ERRO: não consegui salvar " + LobbyPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // As instâncias dos prefabs nas cenas abertas guardam valores próprios (overrides) que ganham do prefab, por
        // exemplo o botão "Continuar" da vitória. Os mesmos valores são escritos nelas; onde já são iguais aos do
        // prefab nada muda e a cena não fica suja.
        static void ApplyToOpenScenes(Kit kit, List<string> log)
        {
            scenePass = true;
            var quiet = new List<string>();
            var count = 0;
            foreach (var t in SceneTransforms())
            {
                if (t == null) continue;
                if (t.GetComponent<VictoryScreen>() != null) { BuildVictory(t, kit, quiet); count++; continue; }
                if (t.name != "LiveShop" && t.name != "MenuFailed" && t.name != "MenuComplete") continue;
                if (!PrefabUtility.IsPartOfPrefabInstance(t)) continue;
                var window = t.Find("Image") as RectTransform;
                if (window == null || window.Find("Moldura") == null) continue;
                if (t.name == "LiveShop") BuildLifeShop(window, kit, quiet);
                else if (t.name == "MenuFailed") BuildFailed(window, kit, quiet);
                else BuildComplete(window, kit, quiet);
                count++;
            }
            scenePass = false;
            if (count > 0) log.Add("cenas abertas: " + count + " modal(is) conferida(s); valores próprios da cena que conflitavam foram atualizados");
        }

        static bool Run(string label, RectTransform window, System.Action<RectTransform> build, List<string> log)
        {
            if (window == null)
            {
                log.Add("NÃO ENCONTRADA: " + label);
                return false;
            }
            log.Add(label + ":");
            build(window);
            return true;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Loja de vidas
        // ---------------------------------------------------------------------------------------------------------

        static void BuildLifeShop(RectTransform window, Kit kit, List<string> log)
        {
            Chrome(window, kit);
            log.Add("   moldura (modal_base) e faixa do título (faixa_title) no tamanho e lugar do BoostShop");

            var title = window.Find("TextMeshPro Text (1)") as RectTransform;
            if (title != null)
            {
                Place(title, kit.title.center, new Vector2(TitleWidth, TitleHeight));
                TitleStyle(title.GetComponent<TextMeshProUGUI>(), kit, TitleFontSize);
                log.Add("   título sobre a faixa, creme com contorno vinho");
            }

            // Cartão do coração -> painel interno do BoostShop (pt1 + pt2), no mesmo lugar.
            var card = window.Find("Image") as RectTransform;
            if (card != null)
            {
                Place(card, kit.panel.center, kit.panel.size);
                Panel(card, kit);
                log.Add("   cartão do coração -> painel interno (pt1 + pt2)");
            }
            var heart = window.Find("Image (2)") as RectTransform;
            if (heart != null) Place(heart, kit.panel.center + LifeHeart.center, LifeHeart.size);
            var refill = window.Find("TextMeshPro Text") as RectTransform;
            if (refill != null) Place(refill, kit.panel.center + LifeRefillText.center, LifeRefillText.size);
            var timer = window.Find("Timer") as RectTransform;
            if (timer != null) Place(timer, kit.panel.center + LifeTimerText.center, LifeTimerText.size);

            // Botões: comprar com moedas = principal (verde), vídeo = secundário (azul), empilhados como no BoostShop.
            var buttons = window.Find("Buttons") as RectTransform;
            if (buttons != null)
            {
                foreach (var layout in buttons.GetComponents<LayoutGroup>())
                {
                    Rec(layout);
                    layout.enabled = false;
                }
                Place(buttons, Vector2.zero, new Vector2(100, 100));

                var buy = buttons.Find("BuyLife") as RectTransform;
                if (buy != null)
                {
                    Place(buy, kit.primary.center, kit.primary.size);
                    StyleButton(buy, kit.ButtonBorder, kit.Green, kit.primary, kit.primaryCore);
                    var coin = buy.Find("Image") as RectTransform;
                    if (coin != null)
                    {
                        Place(coin, LifeCoin.center, LifeCoin.size);
                        var coinImage = coin.GetComponent<Image>();
                        SetImage(coinImage, kit.Coin);
                        if (coinImage != null) coinImage.preserveAspect = true;
                    }
                    var price = buy.Find("Price") as RectTransform;
                    if (price != null)
                    {
                        Place(price, LifePrice.center, LifePrice.size);
                        AutoSize(price.GetComponent<TextMeshProUGUI>(), LifePriceFontMin, LifePriceFontMax);
                    }
                    log.Add("   botão de moedas -> borda laranja + miolo verde, moeda do BoostShop (coin)");
                }

                var video = buttons.Find("Video") as RectTransform;
                if (video != null)
                {
                    Place(video, kit.secondary.center, kit.secondary.size);
                    StyleButton(video, kit.ButtonBorder, kit.Blue, kit.secondary, kit.secondaryCore);
                    var icon = video.Find("watch_video_icon") as RectTransform;
                    if (icon != null)
                    {
                        Place(icon, kit.videoIcon.center, kit.videoIcon.size);
                        SetImage(icon.GetComponent<Image>(), kit.VideoIcon);
                    }
                    var text = video.Find("TextMeshPro Text") as RectTransform;
                    if (text != null)
                    {
                        Place(text, LifeVideoText.center, LifeVideoText.size);
                        AutoSize(text.GetComponent<TextMeshProUGUI>(), LifeVideoFontMin, LifeVideoFontMax);
                    }
                    log.Add("   botão de vídeo -> borda laranja + miolo azul, ícone de filme do BoostShop");
                }
            }

            var close = window.Find("Close") as RectTransform;
            if (close != null)
            {
                StyleClose(close, kit);
                if (!scenePass) close.SetAsLastSibling();
                log.Add("   botão de fechar -> aro dourado + miolo vermelho + X creme, no canto do BoostShop");
            }
        }

        // ---------------------------------------------------------------------------------------------------------
        // Nível falhou (MenuFailed)
        // ---------------------------------------------------------------------------------------------------------

        static void BuildFailed(RectTransform window, Kit kit, List<string> log)
        {
            Chrome(window, kit);
            log.Add("   fundo de couro (popup-pre-nivel2) -> moldura (modal_base) + faixa do título (faixa_title)");

            // Painel do meio: o "Image (2)" que tem a faixa vermelha dentro (o outro "Image (2)" é o selo do nível).
            var panel = ChildWithChild(window, "Image (2)", "Image");
            ResultPanel(window, panel, kit, log);

            StyleResultButton(window.Find("Again") as RectTransform, kit, true);
            StyleResultButton(window.Find("Map") as RectTransform, kit, false);
            log.Add("   botões: \"de novo\" com miolo verde e \"mapa\" com miolo azul, ambos com a borda laranja");
        }

        // ---------------------------------------------------------------------------------------------------------
        // Nível concluído do Sweet Sugar (MenuComplete)
        // ---------------------------------------------------------------------------------------------------------

        static void BuildComplete(RectTransform window, Kit kit, List<string> log)
        {
            Chrome(window, kit);
            log.Add("   fundo rosa do Sweet Sugar -> moldura (modal_base) + faixa do título (faixa_title)");

            ResultPanel(window, window.Find("Image (1)") as RectTransform, kit, log);

            var buttons = window.Find("Buttons");
            if (buttons != null)
            {
                StyleResultButton(buttons.Find("Next") as RectTransform, kit, true);
                StyleResultButton(buttons.Find("Share") as RectTransform, kit, false);
                log.Add("   botões: \"próximo\" com miolo verde e \"compartilhar\" com miolo azul, ambos com a borda laranja");
            }

            var close = window.Find("Close") as RectTransform;
            if (close != null)
            {
                StyleClose(close, kit);
                log.Add("   botão de fechar -> aro dourado + miolo vermelho + X creme, no canto do BoostShop");
            }
        }

        // Painel do meio das telas de resultado: vira o painel interno do BoostShop; a faixa colorida dele some e o
        // título que estava nela sobe para a faixa da moldura (o texto e a tradução continuam os mesmos).
        static void ResultPanel(RectTransform window, RectTransform panel, Kit kit, List<string> log)
        {
            if (panel != null)
            {
                Place(panel, panel.anchoredPosition, ResultPanelSize);
                Panel(panel, kit);
                var banner = panel.Find("Image");
                var bannerImage = banner != null ? banner.GetComponent<Image>() : null;
                if (bannerImage != null)
                {
                    Rec(bannerImage);
                    bannerImage.enabled = false;
                }
                log.Add("   painel do meio -> painel interno (pt1 + pt2); faixa colorida antiga escondida");
            }

            var title = window.Find("Titulo") as RectTransform;
            if (title == null && !scenePass && panel != null)
            {
                title = panel.Find("Image/TextMeshPro Text") as RectTransform;
                if (title != null)
                {
                    title.SetParent(window, false);
                    title.name = "Titulo";
                }
            }
            if (title != null)
            {
                if (!scenePass) title.SetSiblingIndex(2);
                Place(title, kit.title.center, new Vector2(TitleWidth, TitleHeight));
                TitleStyle(title.GetComponent<TextMeshProUGUI>(), kit, TitleFontSize);
                log.Add("   título movido para a faixa (objeto \"Titulo\"), creme com contorno vinho");
            }
        }

        static void StyleResultButton(RectTransform button, Kit kit, bool primary)
        {
            if (button == null) return;
            if (primary) StyleButton(button, kit.ButtonBorder, kit.Green, kit.primary, kit.primaryCore);
            else StyleButton(button, kit.ButtonBorder, kit.Blue, kit.secondary, kit.secondaryCore);
        }

        // ---------------------------------------------------------------------------------------------------------
        // Vitória do lobby (VictoryScreen)
        // ---------------------------------------------------------------------------------------------------------

        static void BuildVictory(Transform victory, Kit kit, List<string> log)
        {
            var card = victory.Find("Card") as RectTransform;
            if (card == null)
            {
                log.Add("NÃO ENCONTRADO: Victory/Card");
                return;
            }
            log.Add("Nível concluído (RoyalAvesLobby > Victory):");

            SetImage(card.GetComponent<Image>(), kit.Frame);
            Rec(card);
            card.sizeDelta = VictoryCardSize;
            card.anchoredPosition = new Vector2(card.anchoredPosition.x, VictoryCardCenterY);
            log.Add("   cartão (victory-card) -> moldura (modal_base), " + VictoryCardSize.x + " x " + VictoryCardSize.y);

            // A etiqueta "NÍVEL N" vira a faixa do título, na mesma proporção em que ela aparece no BoostShop.
            var scale = VictoryCardSize.x / kit.frame.size.x;
            var tag = card.Find("Tag") as RectTransform;
            if (tag != null)
            {
                SetImage(tag.GetComponent<Image>(), kit.Ribbon);
                Rec(tag);
                tag.anchorMin = tag.anchorMax = new Vector2(0.5f, 1f);
                tag.pivot = Half;
                tag.localScale = Vector3.one;
                tag.anchoredPosition = new Vector2(kit.ribbon.center.x, kit.ribbon.center.y - kit.frame.size.y * 0.5f) * scale;
                tag.sizeDelta = kit.ribbon.size * scale;
                var text = tag.Find("Text") as RectTransform;
                if (text != null)
                {
                    Place(text, (kit.title.center - kit.ribbon.center) * scale, new Vector2(TitleWidth, TitleHeight) * scale);
                    TitleStyle(text.GetComponent<TextMeshProUGUI>(), kit, VictoryTagFontSize);
                }
                log.Add("   etiqueta do nível (banner) -> faixa do título (faixa_title), texto creme com contorno vinho");
            }

            var rays = card.Find("Stage/Rays");
            var raysImage = rays != null ? rays.GetComponent<Image>() : null;
            if (raysImage != null)
            {
                Rec(raysImage);
                raysImage.sprite = kit.Rays;
                log.Add("   raios atrás do envelope -> raios do BoostShop");
            }

            var cont = card.Find("Continue") as RectTransform;
            if (cont != null)
            {
                Rec(cont);
                cont.sizeDelta = VictoryContinueSize;
                cont.anchoredPosition = new Vector2(0, VictoryContinueY);
                StyleButton(cont, kit.ButtonBorder, kit.Green, kit.primary, kit.primaryCore);
                log.Add("   botão Continuar -> borda laranja + miolo verde");
            }
            SetY(card.Find("Replay") as RectTransform, VictoryReplayY);
            SetY(card.Find("Total") as RectTransform, VictoryTotalY);
            SetY(card.Find("Reward") as RectTransform, VictoryRewardY);
            log.Add("   rodapé (recompensa, total, Continuar, jogar de novo) reposicionado para caber dentro da moldura");
        }

        static void SetY(RectTransform rect, float y)
        {
            if (rect == null) return;
            Rec(rect);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
        }

        // ---------------------------------------------------------------------------------------------------------
        // Peças do visual
        // ---------------------------------------------------------------------------------------------------------

        // Moldura e faixa, como filhos da janela (a janela em si não muda de tamanho: a animação de abrir mexe nela).
        static void Chrome(RectTransform window, Kit kit)
        {
            var old = window.GetComponent<Image>();
            if (old != null)
            {
                Rec(old);
                old.enabled = false;
            }

            var frame = Child(window, "Moldura", 0);
            if (frame != null)
            {
                Place(frame, Vector2.zero, kit.frame.size);
                var image = ImageOn(frame);
                SetImage(image, kit.Frame);
                if (image != null) image.raycastTarget = true; // toques na modal não passam para o fundo escuro
            }

            var ribbon = Child(window, "Faixa", 1);
            if (ribbon != null)
            {
                Place(ribbon, kit.ribbon.center, kit.ribbon.size);
                var image = ImageOn(ribbon);
                SetImage(image, kit.Ribbon);
                if (image != null) image.raycastTarget = false;
            }
        }

        // Painel interno: a imagem do próprio objeto vira a borda (pt1) e um filho "Miolo" recebe o creme (pt2).
        static void Panel(RectTransform panel, Kit kit)
        {
            SetImage(panel.GetComponent<Image>(), kit.PanelOuter);
            var inner = Child(panel, "Miolo", 0);
            if (inner == null) return;
            Fit(inner, kit.panel, kit.panelInner);
            var image = ImageOn(inner);
            SetImage(image, kit.PanelInner);
            if (image != null) image.raycastTarget = false;
        }

        // Botão: a imagem do próprio botão vira a borda laranja e um filho "Miolo" (atrás do texto) recebe a cor.
        // O miolo acompanha o tamanho do botão, na mesma proporção borda/miolo do BoostShop.
        static void StyleButton(RectTransform button, Sprite border, Sprite core, Box kitButton, Box kitCore)
        {
            SetImage(button.GetComponent<Image>(), border);
            var inner = Child(button, "Miolo", 0);
            if (inner != null)
            {
                Fit(inner, kitButton, kitCore);
                var image = ImageOn(inner);
                SetImage(image, core);
                if (image != null) image.raycastTarget = false;
            }
            // Mesmo efeito de clique do BoostShop (encolhe 15%).
            if (!scenePass && button.GetComponent<ButtonPopPress>() == null) button.gameObject.AddComponent<ButtonPopPress>();
        }

        static void StyleClose(RectTransform close, Kit kit)
        {
            Place(close, kit.close.center, kit.close.size);
            SetImage(close.GetComponent<Image>(), kit.CloseRing);
            var core = Child(close, "Miolo", 0);
            if (core != null)
            {
                Fit(core, kit.close, kit.closeCore);
                var image = ImageOn(core);
                SetImage(image, kit.CloseCore);
                if (image != null) image.raycastTarget = false;
            }
            var x = Child(close, "X", 1);
            if (x != null)
            {
                Fit(x, kit.close, kit.closeX);
                var image = ImageOn(x);
                SetImage(image, kit.CloseX);
                if (image != null) image.raycastTarget = false;
            }
            if (!scenePass && close.GetComponent<ButtonPopPress>() == null) close.gameObject.AddComponent<ButtonPopPress>();
        }

        static void TitleStyle(TextMeshProUGUI text, Kit kit, float size)
        {
            if (text == null) return;
            Rec(text);
            if (kit.Font != null)
            {
                text.font = kit.Font;
                if (kit.TitleMaterial != null) text.fontSharedMaterial = kit.TitleMaterial;
            }
            text.color = Color.white;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(TitleTop, TitleTop, TitleBottom, TitleBottom);
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.raycastTarget = false;
            AutoSize(text, size * 0.5f, size);
        }

        static void AutoSize(TextMeshProUGUI text, float min, float max)
        {
            if (text == null) return;
            Rec(text);
            text.enableAutoSizing = true;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.fontSize = max;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Artes e medidas do BoostShop
        // ---------------------------------------------------------------------------------------------------------

        static Kit LoadKit(out List<string> missing)
        {
            var lost = new List<string>();
            Sprite Load(string path)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null) lost.Add(path);
                return sprite;
            }

            var kit = new Kit
            {
                Frame = Load(FramePath), Ribbon = Load(RibbonPath),
                CloseRing = Load(CloseRingPath), CloseCore = Load(CloseCorePath), CloseX = Load(CloseXPath),
                ButtonBorder = Load(ButtonBorderPath), Green = Load(GreenPath), Blue = Load(BluePath),
                PanelOuter = Load(PanelOuterPath), PanelInner = Load(PanelInnerPath),
                Rays = Load(RaysPath), Coin = Load(CoinPath), VideoIcon = Load(VideoIconPath),
            };
            kit.Font = RoyalAvesFonts.Display;
            kit.TitleMaterial = TitleMaterialFor(kit.Font);
            missing = lost;
            return kit;
        }

        // Criado uma vez; depois pode ser ajustado à mão no próprio .mat (contorno, sombra).
        static Material TitleMaterialFor(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
            if (material != null || font == null || font.material == null) return material;
            material = new Material(font.material) { name = "LilitaOne Modal Titulo" };
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, TitleOutlineWidth);
            material.SetColor(ShaderUtilities.ID_OutlineColor, TitleOutline);
            material.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            material.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.25f, 0.03f, 0.06f, 0.85f));
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0f);
            material.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.9f);
            material.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.1f);
            AssetDatabase.CreateAsset(material, TitleMaterialPath);
            return material;
        }

        // Lê do BoostShop onde ficam e que tamanho têm a moldura, a faixa, o título, o X, o painel e os botões.
        // Só leitura: nada no BoostShop é escrito. O que não for encontrado fica com a medida padrão do script.
        static void Measure(Kit kit, BoostShop shop)
        {
            var root = shop.transform;
            var frameImage = FindImage(root, kit.Frame);
            if (frameImage == null) return;
            var window = frameImage.rectTransform;
            var frame = In(window, window);
            if (!frame.Valid) return;
            kit.frame = new Box(Vector2.zero, frame.size);
            kit.measured = true;

            Take(ref kit.ribbon, window, FindImage(root, kit.Ribbon));

            var names = FindDeep(root, "Boosters_names");
            if (names != null)
                foreach (Transform child in names)
                    if (child.gameObject.activeSelf && child.GetComponent<Image>() != null)
                    {
                        Take(ref kit.title, window, child);
                        break;
                    }

            var ring = FindImage(root, kit.CloseRing);
            if (ring != null)
            {
                Take(ref kit.close, window, ring);
                TakeInside(ref kit.closeCore, window, ring, FindImage(ring.transform, kit.CloseCore));
                TakeInside(ref kit.closeX, window, ring, FindImage(ring.transform, kit.CloseX));
            }

            var outer = FindImage(root, kit.PanelOuter);
            if (outer != null)
            {
                Take(ref kit.panel, window, outer);
                TakeInside(ref kit.panelInner, window, outer, FindImage(outer.transform, kit.PanelInner));
            }

            var so = new SerializedObject(shop);
            var buy = so.FindProperty("buyBoostButton")?.objectReferenceValue as Button;
            if (buy != null)
            {
                Take(ref kit.primary, window, buy);
                TakeInside(ref kit.primaryCore, window, buy, FindImage(buy.transform, kit.Green));
            }
            var watch = so.FindProperty("watchAdButton")?.objectReferenceValue as Button;
            if (watch != null)
            {
                Take(ref kit.secondary, window, watch);
                TakeInside(ref kit.secondaryCore, window, watch, FindImage(watch.transform, kit.Blue));
                TakeInside(ref kit.videoIcon, window, watch, FindImage(watch.transform, kit.VideoIcon));
            }
        }

        // Caixa de "target" medida no espaço de "space" (centro e tamanho), já contando escalas no caminho.
        static Box In(Transform space, Component target)
        {
            var rect = target != null ? target.transform as RectTransform : null;
            if (rect == null) return default;
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 a = space.InverseTransformPoint(corners[0]);
            Vector2 b = space.InverseTransformPoint(corners[2]);
            return new Box((a + b) * 0.5f, new Vector2(Mathf.Abs(b.x - a.x), Mathf.Abs(b.y - a.y)));
        }

        static void Take(ref Box field, Transform window, Component target)
        {
            var box = In(window, target);
            if (box.Valid) field = box;
        }

        // Caixa de "inner" a partir do centro de "outer", nas unidades da janela.
        static void TakeInside(ref Box field, Transform window, Component outer, Component inner)
        {
            var o = In(window, outer);
            var i = In(window, inner);
            if (o.Valid && i.Valid) field = new Box(i.center - o.center, i.size);
        }

        static Image FindImage(Transform root, Sprite sprite)
        {
            if (sprite == null) return null;
            foreach (var image in root.GetComponentsInChildren<Image>(true))
                if (image.sprite == sprite && image.transform != root) return image;
            return null;
        }

        // ---------------------------------------------------------------------------------------------------------
        // Proteção do BoostShop
        // ---------------------------------------------------------------------------------------------------------

        struct Layout
        {
            public Vector2 anchoredPosition, sizeDelta, anchorMin, anchorMax, pivot;
            public Vector3 localScale;
        }

        static BoostShop FindSceneBoostShop()
        {
            foreach (var t in SceneTransforms())
            {
                var shop = t.GetComponent<BoostShop>();
                if (shop != null) return shop;
            }
            return null;
        }

        static Dictionary<RectTransform, Layout> Snapshot(BoostShop shop)
        {
            var result = new Dictionary<RectTransform, Layout>();
            if (shop == null) return result;
            foreach (var r in shop.GetComponentsInChildren<RectTransform>(true))
                result[r] = new Layout
                {
                    anchoredPosition = r.anchoredPosition, sizeDelta = r.sizeDelta, anchorMin = r.anchorMin,
                    anchorMax = r.anchorMax, pivot = r.pivot, localScale = r.localScale,
                };
            return result;
        }

        static int RestoreIfChanged(Dictionary<RectTransform, Layout> before)
        {
            var restored = 0;
            foreach (var pair in before)
            {
                var r = pair.Key;
                var was = pair.Value;
                if (r == null) continue;
                if (Same(r.anchoredPosition, was.anchoredPosition) && Same(r.sizeDelta, was.sizeDelta) &&
                    Same(r.anchorMin, was.anchorMin) && Same(r.anchorMax, was.anchorMax) &&
                    Same(r.pivot, was.pivot) && Same(r.localScale, was.localScale)) continue;
                Debug.LogWarning("Royal Aves: " + r.name + " (BoostShop) mudou durante a aplicação e foi restaurado: tamanho " +
                                 r.sizeDelta + " -> " + was.sizeDelta + ", posição " + r.anchoredPosition + " -> " + was.anchoredPosition +
                                 ", escala " + r.localScale + " -> " + was.localScale, r);
                Undo.RecordObject(r, "Restaurar layout do BoostShop");
                r.anchorMin = was.anchorMin;
                r.anchorMax = was.anchorMax;
                r.pivot = was.pivot;
                r.sizeDelta = was.sizeDelta;
                r.anchoredPosition = was.anchoredPosition;
                r.localScale = was.localScale;
                restored++;
            }
            return restored;
        }

        static bool Same(Vector2 a, Vector2 b) => (a - b).sqrMagnitude < 0.0001f;
        static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < 0.0001f;

        // ---------------------------------------------------------------------------------------------------------
        // Utilitários
        // ---------------------------------------------------------------------------------------------------------

        static List<Transform> SceneTransforms()
        {
            var all = new List<Transform>();
            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid() || !scene.isLoaded) continue;
                foreach (var go in scene.GetRootGameObjects())
                    all.AddRange(go.GetComponentsInChildren<Transform>(true));
            }
            return all;
        }

        // A janela ("Image") da modal com este nome, em qualquer nível do prefab.
        static RectTransform WindowOf(Transform root, string modal)
        {
            var found = FindDeep(root, modal);
            return found != null ? found.Find("Image") as RectTransform : null;
        }

        static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name) return child;
                var deeper = FindDeep(child, name);
                if (deeper != null) return deeper;
            }
            return null;
        }

        // Filho direto com este nome que tem, ele mesmo, um filho direto "childName".
        static RectTransform ChildWithChild(Transform parent, string name, string childName)
        {
            foreach (Transform child in parent)
                if (child.name == name && child.Find(childName) != null) return child as RectTransform;
            return null;
        }

        // Só registra para desfazer (e para virar override da instância) na passada da cena.
        static void Rec(Object target)
        {
            if (scenePass && target != null) Undo.RecordObject(target, UndoName);
        }

        static void Place(RectTransform rect, Vector2 center, Vector2 size)
        {
            Rec(rect);
            rect.anchorMin = rect.anchorMax = Half;
            rect.pivot = Half;
            rect.localScale = Vector3.one;
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
        }

        // Prende "rect" dentro do pai na mesma proporção em que "inner" fica dentro de "outer" no BoostShop
        // ("inner" medido a partir do centro de "outer"); assim acompanha o pai se ele mudar de tamanho.
        static void Fit(RectTransform rect, Box outer, Box inner)
        {
            Rec(rect);
            var half = new Vector2(inner.size.x / outer.size.x, inner.size.y / outer.size.y) * 0.5f;
            var middle = new Vector2(0.5f + inner.center.x / outer.size.x, 0.5f + inner.center.y / outer.size.y);
            rect.anchorMin = middle - half;
            rect.anchorMax = middle + half;
            rect.pivot = Half;
            rect.localScale = Vector3.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        // Acha o filho ou, fora da passada da cena, cria na posição "index" entre os irmãos.
        static RectTransform Child(Transform parent, string name, int index)
        {
            var existing = parent.Find(name) as RectTransform;
            if (scenePass) return existing;
            if (existing == null)
            {
                var go = new GameObject(name, typeof(RectTransform)) { layer = parent.gameObject.layer };
                existing = (RectTransform)go.transform;
                existing.SetParent(parent, false);
            }
            existing.SetSiblingIndex(index);
            return existing;
        }

        static Image ImageOn(RectTransform rect)
        {
            var image = rect.GetComponent<Image>();
            if (image == null && !scenePass) image = rect.gameObject.AddComponent<Image>();
            return image;
        }

        static void SetImage(Image image, Sprite sprite)
        {
            if (image == null) return;
            Rec(image);
            image.sprite = sprite;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.pixelsPerUnitMultiplier = 1f;
            image.color = Color.white;
            image.enabled = true;
        }
    }
}
