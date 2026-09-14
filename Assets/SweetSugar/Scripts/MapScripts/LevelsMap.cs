// // ©2015 - Present Candy Smith
// // All rights reserved
// // Redistribution of this software is strictly not allowed.
// // Copy of this software can be obtained from unity asset store only.
// // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// // FITNESS FOR A PARTICULAR PURPOSE AND NON-INFRINGEMENT. IN NO EVENT SHALL THE
// // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// // THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using SweetSugar.Scripts.GUI;
using SweetSugar.Scripts.Level;
using SweetSugar.Scripts.System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SweetSugar.Scripts.MapScripts
{
    public class LevelsMap : MonoBehaviour {
        public static LevelsMap _instance;
        public static IMapProgressManager _mapProgressManager = new PlayerPrefsMapProgressManager ();

        public bool IsGenerated;

        public MapLevel MapLevelPrefab;
        public Transform CharacterPrefab;
        public int Count = 10;

        public WaypointsMover WaypointsMover;
        public MapLevel CharacterLevel;
        public TranslationType TranslationType;

        public bool StarsEnabled;
        public StarsType StarsType;

        public bool ScrollingEnabled;
        public MapCamera MapCamera;
        public bool IsClickEnabled;
        public bool IsConfirmationEnabled;

        private readonly List<MeshRenderer> _levelNumberRenderers = new List<MeshRenderer>();
        private GameObject _levelNumbersBatchObject;
        private MeshFilter _levelNumbersBatchFilter;
        private MeshRenderer _levelNumbersBatchRenderer;
        private Mesh _levelNumbersBatchMesh;
        private bool _levelNumbersBatchDirty = true;

        public void Awake () {
            _instance = this;
            _levelNumbersBatchDirty = true;
        }

        public void OnDestroy () {
            if (_instance == this)
                _instance = null;

            DestroyBatchMesh(_levelNumbersBatchMesh);
        }

        public void OnDisable () {
            SetSourceLevelNumberRenderersEnabled(true);
            if (_levelNumbersBatchRenderer != null)
                _levelNumbersBatchRenderer.enabled = false;
        }

        public void OnEnable () {
            if (IsGenerated) {
                Reset ();
            }

            _levelNumbersBatchDirty = true;
        }

        public void LateUpdate () {
            if (!_levelNumbersBatchDirty)
                return;

            _levelNumbersBatchDirty = false;
            RebuildLevelNumbersBatch();
        }
        

        public static List<MapLevel> GetMapLevels()
        {
            List<MapLevel> MapLevels = new List<MapLevel>();
            if (MapLevels.Count == 0)//1.4.4
                MapLevels = FindObjectsOfType<MapLevel>().OrderBy(ml => ml.Number).WhereNotNull().ToList();

            return MapLevels;
        }

        public void Reset()
        {
            UpdateMapLevels();
            PlaceCharacterToLastUnlockedLevel();
            int number = GetLastestReachedLevel();
            if (number > 1 && CrosssceneData.win)
                WalkToLevelInternal(number);
            else TeleportToLevelInternal(number,true);
            SetCameraToCharacter();
        }

        private void UpdateMapLevels()
        {
            foreach (MapLevel mapLevel in GetMapLevels())
            {
                mapLevel.UpdateState(
                    _mapProgressManager.LoadLevelStarsCount(mapLevel.Number),
                    IsLevelLocked(mapLevel.Number));
            }
        }

        public static void InvalidateLevelNumbersBatch()
        {
            if (_instance != null)
                _instance._levelNumbersBatchDirty = true;
        }

        private void RebuildLevelNumbersBatch()
        {
            List<TextMeshPro> texts = GetMapLevels()
                .Select(level => level.GetComponentInChildren<MapLevelNumber>())
                .Where(number => number != null && number.isActiveAndEnabled)
                .Select(number => number.GetComponent<TextMeshPro>())
                .Where(text => text != null)
                .ToList();

            SetSourceLevelNumberRenderersEnabled(true);
            _levelNumberRenderers.Clear();

            if (texts.Count == 0)
            {
                if (_levelNumbersBatchRenderer != null)
                    _levelNumbersBatchRenderer.enabled = false;
                return;
            }

            EnsureLevelNumbersBatchObject();

            Material sharedMaterial = null;
            int sortingLayerId = 0;
            int sortingOrder = 0;
            int vertexCount = 0;
            List<CombineInstance> combineInstances = new List<CombineInstance>(texts.Count);
            Matrix4x4 worldToBatch = _levelNumbersBatchObject.transform.worldToLocalMatrix;

            foreach (TextMeshPro text in texts)
            {
                text.ForceMeshUpdate(true, true);

                MeshRenderer sourceRenderer = text.GetComponent<MeshRenderer>();
                Mesh sourceMesh = text.mesh;
                if (sourceRenderer == null || sourceMesh == null || sourceMesh.vertexCount == 0)
                    continue;

                if (sharedMaterial == null)
                {
                    sharedMaterial = sourceRenderer.sharedMaterial;
                    sortingLayerId = sourceRenderer.sortingLayerID;
                    sortingOrder = sourceRenderer.sortingOrder;
                    _levelNumbersBatchObject.layer = text.gameObject.layer;
                }
                else if (sourceRenderer.sharedMaterial != sharedMaterial ||
                         sourceRenderer.sortingLayerID != sortingLayerId ||
                         sourceRenderer.sortingOrder != sortingOrder)
                {
                    Debug.LogWarning(
                        "Level numbers cannot be combined because their material or sorting settings differ.",
                        text);
                    _levelNumbersBatchRenderer.enabled = false;
                    return;
                }

                CombineInstance combineInstance = new CombineInstance
                {
                    mesh = sourceMesh,
                    subMeshIndex = 0,
                    transform = worldToBatch * text.transform.localToWorldMatrix
                };
                combineInstances.Add(combineInstance);
                _levelNumberRenderers.Add(sourceRenderer);
                vertexCount += sourceMesh.vertexCount;
            }

            if (combineInstances.Count == 0 || sharedMaterial == null)
            {
                _levelNumbersBatchRenderer.enabled = false;
                return;
            }

            Mesh combinedMesh = new Mesh
            {
                name = "Batched Level Numbers",
                indexFormat = vertexCount > ushort.MaxValue
                    ? IndexFormat.UInt32
                    : IndexFormat.UInt16
            };
            combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true, false);
            combinedMesh.RecalculateBounds();

            Mesh previousMesh = _levelNumbersBatchMesh;
            _levelNumbersBatchMesh = combinedMesh;
            _levelNumbersBatchFilter.sharedMesh = combinedMesh;
            _levelNumbersBatchRenderer.sharedMaterial = sharedMaterial;
            _levelNumbersBatchRenderer.sortingLayerID = sortingLayerId;
            _levelNumbersBatchRenderer.sortingOrder = sortingOrder;
            _levelNumbersBatchRenderer.enabled = true;

            foreach (MeshRenderer sourceRenderer in _levelNumberRenderers)
                sourceRenderer.enabled = false;

            if (previousMesh != null)
                DestroyBatchMesh(previousMesh);
        }

        private void EnsureLevelNumbersBatchObject()
        {
            if (_levelNumbersBatchObject != null)
                return;

            _levelNumbersBatchObject = new GameObject("BatchedLevelNumbers");
            _levelNumbersBatchObject.transform.SetParent(transform, false);
            _levelNumbersBatchFilter = _levelNumbersBatchObject.AddComponent<MeshFilter>();
            _levelNumbersBatchRenderer = _levelNumbersBatchObject.AddComponent<MeshRenderer>();
            _levelNumbersBatchRenderer.shadowCastingMode = ShadowCastingMode.Off;
            _levelNumbersBatchRenderer.receiveShadows = false;
            _levelNumbersBatchRenderer.lightProbeUsage = LightProbeUsage.Off;
            _levelNumbersBatchRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _levelNumbersBatchRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private void SetSourceLevelNumberRenderersEnabled(bool isEnabled)
        {
            foreach (MeshRenderer sourceRenderer in _levelNumberRenderers)
            {
                if (sourceRenderer != null)
                    sourceRenderer.enabled = isEnabled;
            }
        }

        private static void DestroyBatchMesh(Mesh mesh)
        {
            if (mesh == null)
                return;

            if (Application.isPlaying)
                Destroy(mesh);
            else
                DestroyImmediate(mesh);
        }

        private void PlaceCharacterToLastUnlockedLevel()
        {
            int lastUnlockedNumber = GetMapLevels().Where(l => !l.IsLocked).Select(l => l.Number).Max() - 1;
            lastUnlockedNumber = Mathf.Clamp(lastUnlockedNumber, 1, lastUnlockedNumber);
            TeleportToLevelInternal(lastUnlockedNumber, true);
        }

        public static int GetLastestReachedLevel()
        {//1.3.3
            return GetMapLevels().Where(l => !l.IsLocked).Select(l => l.Number).Max();
        }

        private void SetCameraToCharacter()
        {
            MapCamera mapCamera = FindObjectOfType<MapCamera>();
            if (mapCamera != null)
                mapCamera.SetPosition(WaypointsMover.transform.position);
        }

        #region Events

        public static event EventHandler<LevelReachedEventArgs> LevelSelected;
        public static event EventHandler<LevelReachedEventArgs> LevelReached;

        #endregion

        #region Static API

        public static void CompleteLevel(int number)
        {
            CompleteLevelInternal(number, 1);
        }

        public static void CompleteLevel(int number, int starsCount)
        {
            CompleteLevelInternal(number, starsCount);
        }

        internal static void OnLevelSelected(int number)
        {
            if (LevelSelected != null && !IsLevelLocked(number))  //need to fix in the map plugin
                LevelSelected(_instance, new LevelReachedEventArgs(number));

            if (!_instance.IsConfirmationEnabled)
                GoToLevel(number);
        }

        public static void GoToLevel(int number)
        {
            switch (_instance.TranslationType)
            {
                case TranslationType.Teleportation:
                    _instance.TeleportToLevelInternal(number, false);
                    break;
                case TranslationType.Walk:
                    _instance.WalkToLevelInternal(number);
                    break;
            }
        }

        public static bool IsLevelLocked(int number)
        {
            return number > 1 && _mapProgressManager.LoadLevelStarsCount(number - 1) == 0;
        }

        public static void OverrideMapProgressManager(IMapProgressManager mapProgressManager)
        {
            _mapProgressManager = mapProgressManager;
        }

        public static void ClearAllProgress()
        {
            _instance.ClearAllProgressInternal();
        }

        public static bool IsStarsEnabled()
        {
            return _instance.StarsEnabled;
        }

        public static bool GetIsClickEnabled()
        {
            return _instance.IsClickEnabled;
        }

        public static bool GetIsConfirmationEnabled()
        {
            return _instance.IsConfirmationEnabled;
        }

        #endregion

        private static void CompleteLevelInternal(int number, int starsCount)
        {
            if (IsLevelLocked(number))
            {
                Debug.Log(string.Format("Can't complete locked level {0}.", number));
            }
            else if (starsCount < 1 || starsCount > 3)
            {
                Debug.Log(string.Format("Can't complete level {0}. Invalid stars count {1}.", number, starsCount));
            }
            else
            {
                int curStarsCount = _mapProgressManager.LoadLevelStarsCount(number);
                int maxStarsCount = Mathf.Max(curStarsCount, starsCount);
                _mapProgressManager.SaveLevelStarsCount(number, maxStarsCount);

                if (_instance != null)
                    _instance.UpdateMapLevels();
            }
        }

        private void TeleportToLevelInternal(int number, bool isQuietly)
        {
            MapLevel mapLevel = GetLevel(number);
            mapLevel.SetEffect();
            if (mapLevel.IsLocked)
            {
                Debug.Log(string.Format("Can't jump to locked level number {0}.", number));
            }
            else
            {
                WaypointsMover.transform.position = mapLevel.PathPivot.transform.position;   //need to fix in the map plugin
                CharacterLevel = mapLevel;
                if (!isQuietly)
                    RaiseLevelReached(number);
            }
        }
    
        public delegate void ReachedLevelEvent();
        public static ReachedLevelEvent OnLevelReached;

        private void WalkToLevelInternal(int number)
        {
            MapLevel mapLevel = GetLevel(number);
            mapLevel.SetEffect();
            CharacterLevel = GetLevel(number - 1);
            if (mapLevel.IsLocked)
            {
                Debug.Log(string.Format("Can't go to locked level number {0}.", number));
            }
            else
            {
                WaypointsMover.Move(CharacterLevel.PathPivot, mapLevel.PathPivot,
                    () =>
                    {
                        RaiseLevelReached(number);
                        CharacterLevel = mapLevel;
                        OnLevelReached?.Invoke();
                    });
            }
        }

        private void RaiseLevelReached(int number)
        {
            MapLevel mapLevel = GetLevel(number);
            mapLevel.SetEffect();
            if (!string.IsNullOrEmpty(mapLevel.SceneName))
                SceneManager.LoadScene(mapLevel.SceneName);

            if (LevelReached != null)
                LevelReached(this, new LevelReachedEventArgs(number));
        }

        public MapLevel GetLevel(int number)
        {
            return GetMapLevels().SingleOrDefault(ml => ml.Number == number);
        }

        private void ClearAllProgressInternal()
        {
            foreach (MapLevel mapLevel in GetMapLevels())
                _mapProgressManager.ClearLevelProgress(mapLevel.Number);
            Reset();
        }

        public void SetStarsEnabled(bool bEnabled)
        {
            StarsEnabled = bEnabled;
            int starsCount = 0;
            foreach (MapLevel mapLevel in GetMapLevels().WhereNotNull())
            {
                mapLevel.UpdateStars(starsCount);
                starsCount = (starsCount + 1) % 4;
                mapLevel.StarsHoster.gameObject.SetActive(bEnabled);
                //mapLevel.SolidStarsHoster.gameObject.SetActive(bEnabled);
            }
        }

        public void SetStarsType(StarsType starsType)
        {
            StarsType = starsType;
            foreach (MapLevel mapLevel in GetMapLevels().WhereNotNull())
                mapLevel.UpdateStarsType(starsType);
        }

    }
}
