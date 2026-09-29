// Content and look of the lobby's bottom-menu screens, after the web prototype (royal-aves/src/features.js, menus.js and
// menus.css): Eventos (delivery goals), Recordes, Coleção (postal album), Equipes (local teams), Inventário and Perfil.
// Created by Royal Aves > Criar telas do menu inferior from Data/events.json; edit it freely in the Inspector.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoyalAves.Meta
{
    [CreateAssetMenu(fileName = "RoyalAvesFeatures", menuName = "Royal Aves/Menus do lobby")]
    public class LobbyFeaturesConfig : ScriptableObject
    {
        public const string ResourcePath = "RoyalAvesFeatures";

        [Serializable]
        public class Mission
        {
            public string id;
            public string title;
            [Tooltip("Fases vencidas necessárias.")]
            [Min(1)] public int goal = 1;
            [Min(0)] public int coins;
        }

        [Serializable]
        public class AlbumCard
        {
            public string title;
            public Sprite picture;
        }

        [Header("Eventos: metas de fases vencidas (resgate único)")]
        public List<Mission> missions = new List<Mission>();
        [Tooltip("Rotas bônus: liberadas depois destas quantidades de fases vencidas. O modo de jogo delas ainda não existe no Unity.")]
        public List<int> bonusRoutesAfter = new List<int> { 5, 10 };
        [Min(1)] public int bonusRouteMoves = 20;

        [Header("Coleção: um selo por fase vencida, na ordem das fases")]
        public List<AlbumCard> album = new List<AlbumCard>();
        [Min(0)] public int albumReward = 500;

        [Header("Equipes (salvas só neste aparelho)")]
        [Min(0)] public int teamCreateCost = 100;
        public List<Sprite> teamEmblems = new List<Sprite>();

        [Header("Inventário")]
        [Tooltip("Recuperar todas as vidas.")]
        [Min(0)] public int livesRefillCost = 500;

        [Header("Perfil: avatares (o primeiro é a gerente, animada)")]
        public List<Sprite> avatars = new List<Sprite>();

        [Header("Arte das telas")]
        public Sprite card;
        public Sprite buttonGreen;
        public Sprite buttonBlue;
        public Sprite pill;
        public Sprite darkBox;
        public Sprite barBackground;
        public Sprite barFill;
        public Sprite token;
        public Sprite coin;
        public Sprite star;
        public Sprite heart;
        public Sprite chest;
        public Sprite iconEvents;
        public Sprite iconRecords;
        public Sprite iconTeams;
        public Sprite iconCollection;
        [Header("Janela das estrelas (toque no contador de estrelas)")]
        public Sprite starsFrame;
        public Sprite starsArrow;
        public Sprite closeCircle;
        public Sprite closeX;
        [Tooltip("Peças do tabuleirinho 3x3, repetidas em ordem.")]
        public List<Sprite> starsBoardPieces = new List<Sprite>();

        [Header("Menu da tela de jogo (sobe da engrenagem)")]
        public Sprite gameMenuGearOpen;
        public Sprite gameMenuExit;
        public Sprite gameMenuMusicOn;
        public Sprite gameMenuMusicOff;
        public Sprite gameMenuSoundOn;
        public Sprite gameMenuSoundOff;
        public Sprite gameMenuVibrationOn;
        public Sprite gameMenuVibrationOff;
        [Tooltip("Caixa do aviso \"Sair da fase?\" e o botão vermelho de sair.")]
        public Sprite confirmBox;
        public Sprite buttonRed;

        public TMP_FontAsset displayFont;
        public TMP_FontAsset postalFont;
        public Material postalMaterial;

        static LobbyFeaturesConfig instance;

        public static LobbyFeaturesConfig Instance =>
            instance != null ? instance : instance = Resources.Load<LobbyFeaturesConfig>(ResourcePath);
    }
}
