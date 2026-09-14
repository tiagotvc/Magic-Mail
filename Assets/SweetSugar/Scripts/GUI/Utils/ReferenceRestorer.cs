using System.Collections.Generic;
using SweetSugar.Scripts.GUI.Boost;
using SweetSugar.Scripts.GUI.BonusSpin;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SweetSugar.Scripts.GUI.Utils
{
    /// <summary>
    /// Utility class for restoring missing UI references using Find operations
    /// </summary>
    public static class ReferenceRestorer
    {
        // Cache for text components to avoid repeated Find operations
        private static Dictionary<string, TextMeshProUGUI> _textComponentCache = new Dictionary<string, TextMeshProUGUI>();
        private static Dictionary<string, Object> _objectCache = new Dictionary<string, Object>();
        
        // Clear cache when needed (e.g., scene changes)
        public static void ClearCache()
        {
            _textComponentCache.Clear();
            _objectCache.Clear();
        }
        
        #region Global references
        
        public static GameObject FindCanvasGlobal()
        {
            return GameObject.Find("CanvasGlobal");
        }
        
        public static GameObject FindCanvas()
        {
            return GameObject.Find("Canvas");
        }
        
        public static Canvas FindCanvasBack()
        {
            string cacheKey = "CanvasBack";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = GameObject.Find("CanvasBack")?.GetComponent<Canvas>();
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as Canvas;
        }
        
        public static Transform FindCanvasScoreTransform()
        {
            string cacheKey = "CanvasScoreTransform";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = GameObject.Find("CanvasScore")?.transform;
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as Transform;
        }
        
        public static GameObject FindFailedWord()
        {
            string cacheKey = "FailedWord";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = GameObject.Find("FailedWord");
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as GameObject;
        }
        
        public static GameObject FindNoMoreMatches()
        {
            string cacheKey = "NoMoreMatches";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = GameObject.Find("NoMoreMatches");
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as GameObject;
        }
        
        public static GameObject FindCompleteWord()
        {
            string cacheKey = "CompleteWord";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = GameObject.Find("CompleteWord");
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as GameObject;
        }
        
        public static GameObject FindBoostInfo(GameObject canvasGlobal)
        {
            return canvasGlobal?.transform.Find("BoostInfo")?.gameObject;
        }
        
        public static GameObject FindMenuGameOver(GameObject canvas)
        {
            return canvas?.transform.Find("MenuGameOver")?.gameObject;
        }
        
        #endregion
        
        #region Level specific references
        
        public static GameObject FindLevelCanvasObject(GameObject level)
        {
            if (level == null) return null;
            
            string cacheKey = "LevelCanvas";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = level.transform.Find("Canvas")?.gameObject;
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as GameObject;
        }
        
        public static GraphicRaycaster FindLevelCanvasRaycaster(GameObject levelCanvasObject)
        {
            if (levelCanvasObject == null) return null;
            
            string cacheKey = "LevelCanvasRaycaster";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = levelCanvasObject.GetComponent<GraphicRaycaster>();
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            return cachedComponent as GraphicRaycaster;
        }
        
        /// <summary>
        /// Toggle the GraphicRaycaster on the Level's Canvas (disable and re-enable)
        /// </summary>
        /// <param name="level">The Level GameObject</param>
        public static void ToggleLevelCanvasRaycaster(GameObject level)
        {
            if (level == null) return;
            
            var levelCanvas = FindLevelCanvasObject(level);
            var raycaster = FindLevelCanvasRaycaster(levelCanvas);
            
            if (raycaster != null)
            {
                raycaster.enabled = false;
                raycaster.enabled = true;
            }
        }
        
        #endregion
        
        #region Menu-specific references
        
        public static GameObject FindBannerButtonsVideo(Transform parent)
        {
            return parent.Find("Banner/Buttons/Video")?.gameObject;
        }
        
        public static Button FindBannerButtonsBuy(Transform parent)
        {
            return parent.Find("Banner/Buttons/Buy")?.GetComponent<Button>();
        }
        
        public static GameObject FindSoundSoundOff(Transform parent)
        {
            return parent.Find("Sound/Sound/SoundOff")?.gameObject;
        }
        
        public static Image FindSoundSound(Transform parent)
        {
            return parent.Find("Sound/Sound")?.GetComponent<Image>();
        }
        
        public static GameObject FindMusicMusicOff(Transform parent)
        {
            return parent.Find("Music/Music/MusicOff")?.gameObject;
        }
        
        public static Image FindMusicMusic(Transform parent)
        {
            return parent.Find("Music/Music")?.GetComponent<Image>();
        }
        
        public static GameObject FindImageVideo(Transform parent)
        {
            return parent.Find("Image/Video")?.gameObject;
        }
        
        #endregion
        
        #region Boost icons
        
        public static BoostIcon FindBoostIcon(Transform parent, int index)
        {
            return parent.Find($"Image/Boosters/Boost{index}")?.GetComponent<BoostIcon>();
        }
        
        #endregion
        
        #region Menu Complete
        
        public static GameObject[] FindStars(Transform parent)
        {
            GameObject[] stars = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                stars[i] = parent.Find($"Image/Star{i+1}")?.gameObject;
            }
            return stars;
        }
        
        public static TextMeshProUGUI FindScoreText(Transform parent)
        {
            return parent.Find("Image/Score")?.GetComponent<TextMeshProUGUI>();
        }
        
        #endregion
        
        #region Menu Failed
        
        public static GameObject[] FindTargetChecks(Transform parent)
        {
            GameObject[] checks = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                checks[i] = parent.Find($"Image/TargetCheck{i+1}")?.gameObject;
            }
            return checks;
        }
        
        public static GameObject[] FindTargetUnchecks(Transform parent)
        {
            GameObject[] unchecks = new GameObject[3];
            for (int i = 0; i < 3; i++)
            {
                unchecks[i] = parent.Find($"Image/TargetUnCheck{i+1}")?.gameObject;
            }
            return unchecks;
        }
        
        #endregion
        
        #region InitScript references
        
        public static GameObject FindMenuPlay()
        {
            return MenuReference.THIS?.MenuPlay;
        }
        
        public static GameObject FindGemsShop()
        {
            return MenuReference.THIS?.GemsShop;
        }
        
        public static GameObject FindLiveShop()
        {
            return MenuReference.THIS?.LiveShop;
        }
        
        public static GameObject FindPreFailed()
        {
            return MenuReference.THIS?.PreFailed;
        }
        
        public static GameObject FindPreCompleteBanner()
        {
            return MenuReference.THIS?.PreCompleteBanner;
        }
        
        public static LIFESAddCounter FindLifeAddCounter()
        {
            string cacheKey = "LIFESAddCounter";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                cachedComponent = Object.FindObjectOfType<LIFESAddCounter>();
                if (cachedComponent != null)
                    _objectCache[cacheKey] = cachedComponent;
            }
            
            return cachedComponent as LIFESAddCounter;
        }
        
        public static GameObject FindRewardObject()
        {
            return MenuReference.THIS?.Reward?.gameObject;
        }
        
        public static RewardIcon FindRewardIcon()
        {
            return MenuReference.THIS?.Reward?.GetComponent<RewardIcon>();
        }
        
        public static GameObject FindBonusSpin()
        {
            return MenuReference.THIS?.BonusSpin;
        }
        
        public static BonusSpin.BonusSpin GetBonusSpin()
        {
            string cacheKey = "BonusSpin";
            if (!_objectCache.TryGetValue(cacheKey, out var cachedComponent))
            {
                var bonusSpinObject = FindBonusSpin();
                if (bonusSpinObject != null)
                {
                    cachedComponent = bonusSpinObject.GetComponent<BonusSpin.BonusSpin>();
                    if (cachedComponent != null)
                        _objectCache[cacheKey] = cachedComponent;
                }
            }
            
            return cachedComponent as BonusSpin.BonusSpin;
        }
        
        public static GameObject FindTutorials()
        {
            return MenuReference.THIS?.Tutorials;
        }
        
        public static GameObject CreateRateObject(Transform parent)
        {
            GameObject rate = Object.Instantiate(Resources.Load("Prefabs/Rate")) as GameObject;
            if (rate != null)
            {
                rate.SetActive(false);
                rate.transform.SetParent(parent);
                rate.transform.localPosition = Vector3.zero;
                rate.GetComponent<RectTransform>().offsetMin = new Vector2(-5, -5);
                rate.GetComponent<RectTransform>().offsetMax = new Vector2(5, 5);
                rate.transform.localScale = Vector3.one;
            }
            return rate;
        }
        
        #endregion
        
        #region Generic components
        
        public static TextMeshProUGUI GetTextComponent(Transform parent, string childPath)
        {
            string key = parent.name + "_" + childPath;
            
            if (!_textComponentCache.TryGetValue(key, out var textComponent))
            {
                textComponent = parent.Find(childPath)?.GetComponent<TextMeshProUGUI>();
                if (textComponent != null)
                    _textComponentCache[key] = textComponent;
            }
            
            return textComponent;
        }
        
        public static GameObject FindVideoButton(Transform parent, GameObject imageVideo, GameObject bannerButtonsVideo)
        {
            if (imageVideo != null)
                return imageVideo;
            
            GameObject videoButton = parent.Find("Image/Video")?.gameObject;
            
            if (videoButton == null) 
            {
                if (bannerButtonsVideo != null)
                    return bannerButtonsVideo;
                
                return parent.Find("Banner/Buttons/Video")?.gameObject;
            }
            
            return videoButton;
        }
        
        #endregion
    }
}
