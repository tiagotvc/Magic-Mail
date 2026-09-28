// Creates Resources/RoyalAvesFeatures: the content and look of the lobby's bottom-menu screens (Eventos, Recordes,
// Coleção, Equipes, Inventário, Perfil), from the web prototype's Data/events.json and the project's art. The screens
// themselves are built by LobbyMenus when the game runs, so the lobby prefab (aligned by hand) is not touched.
// Menu: Royal Aves > Criar telas do menu inferior. Running it again only fills what is missing; edited values stay.
using System.Linq;
using RoyalAves.Meta;
using UnityEditor;
using UnityEngine;

namespace RoyalAves.EditorTools
{
    public static class RoyalAvesMenusBuilder
    {
        const string AssetPath = "Assets/RoyalAves/Resources/" + LobbyFeaturesConfig.ResourcePath + ".asset";
        const string EventsPath = "Assets/RoyalAves/Data/events.json";
        const string Art = "Assets/RoyalAves/Art/";

        [MenuItem("Royal Aves/Criar telas do menu inferior")]
        static void ApplyFromMenu()
        {
            if (!RoyalAvesTools.CanRunTool()) return;
            RoyalAvesReskin.EnsureImportSettings();
            var features = EnsureFeatures();
            Selection.activeObject = features;
            EditorUtility.DisplayDialog("Menu inferior",
                "Telas prontas: Eventos, Ranking (recordes), Equipes e Coleção no menu inferior; Inventário ao tocar nas moedas; " +
                "Perfil ao tocar no avatar.\n\n" +
                $"Conteúdo e arte em {AssetPath} (metas, selos do álbum, emblemas, avatares, preço das vidas). " +
                "O prefab do lobby não foi alterado.", "OK");
        }

        internal static LobbyFeaturesConfig EnsureFeatures()
        {
            var features = AssetDatabase.LoadAssetAtPath<LobbyFeaturesConfig>(AssetPath);
            var created = features == null;
            if (created)
            {
                features = ScriptableObject.CreateInstance<LobbyFeaturesConfig>();
                AssetDatabase.CreateAsset(features, AssetPath);
            }

            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(EventsPath);
            var file = json != null ? JsonUtility.FromJson<Data.EventsFile>(json.text) : null;
            if (features.missions.Count == 0 && file?.missions != null)
                features.missions = file.missions.Select(m => new LobbyFeaturesConfig.Mission { id = m.id, title = m.name, goal = m.goal, coins = m.coins }).ToList();
            if (features.album.Count == 0 && file?.album?.cards != null)
            {
                // The prototype's last stamp was Avenaldo; the manager is now the main character.
                features.album = file.album.cards.Select(c => new LobbyFeaturesConfig.AlbumCard
                {
                    title = c.sprite == "manager" ? "A Gerente" : c.title,
                    picture = AlbumPicture(c.sprite)
                }).ToList();
                features.albumReward = file.album.reward;
            }
            if (created && file?.bonusRoutes != null)
            {
                features.bonusRoutesAfter = file.bonusRoutes.unlockAfterLevels.ToList();
                features.bonusRouteMoves = file.bonusRoutes.moves;
            }
            if (created && file?.teams != null) features.teamCreateCost = file.teams.createCost;

            if (features.teamEmblems.Count == 0)
                features.teamEmblems = new[] { "Pieces/envelope", "Pieces/gift", "Pieces/key", "Pieces/stamp", "Pieces/bow", "Pieces/quill", "Pieces/ink", "Specials/orb" }
                    .Select(Load).Where(s => s != null).ToList();
            if (features.avatars.Count == 0)
                features.avatars = new[] { "Characters/Gerente/gerente-neutra", "Pieces/envelope", "Pieces/gift", "Pieces/key", "Pieces/stamp" }
                    .Select(Load).Where(s => s != null).ToList();

            Fill(ref features.card, "UI/Generated/card");
            Fill(ref features.buttonGreen, "UI/Generated/btn-green");
            Fill(ref features.buttonBlue, "UI/Generated/btn-blue");
            Fill(ref features.pill, "UI/Generated/pill");
            Fill(ref features.darkBox, "UI/Generated/darkbox");
            Fill(ref features.barBackground, "UI/Generated/bar-bg");
            Fill(ref features.barFill, "UI/Generated/bar-fill");
            Fill(ref features.token, "UI/Generated/token");
            Fill(ref features.coin, "UI/Icons/moeda");
            Fill(ref features.star, "UI/Icons/estrela");
            Fill(ref features.heart, "UI/Icons/vida_ativa");
            Fill(ref features.chest, "UI/Chest/bau_fechado");
            Fill(ref features.iconEvents, "UI/Menu/menu_correio_pequeno");
            Fill(ref features.iconRecords, "UI/Menu/menu_trofeu");
            Fill(ref features.iconTeams, "UI/Menu/menu_amigos");
            Fill(ref features.iconCollection, "UI/Menu/menu_selos");
            if (features.displayFont == null) features.displayFont = RoyalAvesFonts.Display;
            if (features.postalFont == null) features.postalFont = RoyalAvesFonts.Postal;
            if (features.postalMaterial == null) features.postalMaterial = RoyalAvesFonts.PostalMaterialAsset;

            EditorUtility.SetDirty(features);
            AssetDatabase.SaveAssets();
            return features;
        }

        static Sprite AlbumPicture(string name)
        {
            if (name == "manager") return Load("Characters/Gerente/gerente-neutra");
            return Load("Pieces/" + name) ?? Load("Specials/" + name);
        }

        static void Fill(ref Sprite field, string path)
        {
            if (field == null) field = Load(path);
        }

        static Sprite Load(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art + path + ".png");
            return sprite != null ? sprite : null;
        }
    }
}
