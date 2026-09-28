// Goals of the level about to start, on the level start window: each goal's picture with how many to collect, like
// the prototype ("envelope 50"). Replaces Sweet Sugar's MenuTargetIcon, which showed the pictures without numbers.
// Goals counted from the board (clear all the grass, for example) have no number before the board exists; they show
// only the picture.
using System.Collections.Generic;
using SweetSugar.Scripts.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoyalAves.Meta
{
    public class LevelGoalsView : MonoBehaviour
    {
        [Tooltip("Modelo de uma meta (desligado): a figura, com um texto filho para a quantidade.")]
        [SerializeField] Image template;

        readonly List<GameObject> shown = new List<GameObject>();

        void OnEnable()
        {
            foreach (var item in shown) Destroy(item);
            shown.Clear();
            if (template == null) return;
            template.gameObject.SetActive(false);

            var levelData = LoadingManager.LoadForPlay(PlayerPrefs.GetInt("OpenLevel", 1));
            if (levelData == null) return;
            foreach (var goal in levelData.GetTargetContainersForUI())
            {
                if (goal.extraObject == null) continue;
                var icon = Instantiate(template, template.transform.parent);
                icon.name = "Meta " + goal.extraObject.name;
                icon.sprite = goal.extraObject;
                icon.gameObject.SetActive(true);
                var count = icon.GetComponentInChildren<TextMeshProUGUI>(true);
                if (count != null)
                {
                    count.text = goal.count.ToString();
                    count.gameObject.SetActive(goal.count > 0);
                }
                shown.Add(icon.gameObject);
            }
        }
    }
}
