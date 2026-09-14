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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace SweetSugar.Scripts
{
    /// <summary>
    /// Sound manager
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class SoundBase : MonoBehaviour
    {
        public static SoundBase Instance;
        public AudioClip click;
        public AudioClip[] swish;
        public AudioClip[] drop;
        public AudioClip alert;
        public AudioClip timeOut;
        public AudioClip[] star;
        public AudioClip[] gameOver;
        public AudioClip cash;

        public AudioClip[] destroy;
        public AudioClip boostBomb;
        public AudioClip boostColorReplace;
        public AudioClip explosion;
        public AudioClip explosion2;
        public AudioClip getStarIngr;
        public AudioClip strippedExplosion;
        public AudioClip[] complete;
        public AudioClip block_destroy;
        public AudioClip wrongMatch;
        public AudioClip noMatch;
        public AudioClip appearStipedColorBomb;
        public AudioClip appearPackage;
        public AudioClip destroyPackage;
        public AudioClip colorBombExpl;
        private AudioSource _audioSource;
        public AudioMixer audioMixer;
        List<AudioClip> clipsPlaying = new List<AudioClip>();
        
        // Maximum number of sounds that can be played simultaneously

        ///SoundBase.Instance.audio.PlayOneShot( SoundBase.Instance.kreakWheel );

        
        void Awake()
        {

            _audioSource = GetComponent<AudioSource>();
            audioMixer = _audioSource.outputAudioMixerGroup?.audioMixer;
            if (transform.parent == null)
            {
                transform.parent = Camera.main?.transform;
                transform.localPosition = Vector3.zero;
            }
            // DontDestroyOnLoad(gameObject);
            if (Instance == null)
                Instance = this;
            else if (Instance != this)
                Destroy(gameObject);

        }

        private void Start()
        {
            audioMixer?.SetFloat("SoundVolume", PlayerPrefs.GetInt("Sound"));
        }

        public void PlayOneShot(AudioClip audioClip)
        {
            if (audioClip != null)
            {
                _audioSource.PlayOneShot(audioClip);
            }
        }
    
        public void PlaySoundsRandom(AudioClip[] clip)
        {
            if (clip.Length > 0)
                PlayOneShot(clip[Random.Range(0, clip.Length)]);
        }

        public void PlayLimitSound(AudioClip clip)
        {
            if (clipsPlaying.IndexOf(clip) < 0)
            {
                clipsPlaying.Add(clip);
                PlayOneShot(clip);
                StartCoroutine(WaitForCompleteSound(clip));
            }
        }

        IEnumerator WaitForCompleteSound(AudioClip clip)
        {
            yield return new WaitForSeconds(0.2f);
            clipsPlaying.Remove(clipsPlaying.Find(x => clip));
        }

        private const int MaxSimultaneousSounds = 5;

        /// <summary>
        /// Plays sound only if there are less than 5 sounds playing simultaneously
        /// </summary>
        /// <param name="clip">Audio clip to play</param>
        public void PlayLimitedSound(AudioClip clip)
        {
            if (clip == null) return;
            
            // Only play the sound if we have less than MaxSimultaneousSounds currently playing
            if (clipsPlaying.Count < MaxSimultaneousSounds)
            {
                clipsPlaying.Add(clip);
                PlayOneShot(clip);
                StartCoroutine(RemoveSoundAfterDelay(clip));
            }
        }
        
        /// <summary>
        /// Plays a random sound from an array, only if there are less than 5 sounds playing simultaneously
        /// </summary>
        /// <param name="clips">Array of audio clips to choose from</param>
        public void PlayLimitedRandomSound(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return;
            
            // Select a random clip from the array
            AudioClip randomClip = clips[Random.Range(0, clips.Length)];
            
            // Play it with the limitation
            PlayLimitedSound(randomClip);
        }
        
        /// <summary>
        /// Removes the sound from the playing list after a delay
        /// </summary>
        /// <param name="clip">Audio clip to remove</param>
        /// <returns></returns>
        IEnumerator RemoveSoundAfterDelay(AudioClip clip)
        {
            // Wait for the sound to complete (or at least 0.5 seconds)
            yield return new WaitForSeconds(0.5f);
            clipsPlaying.Remove(clip);
        }
    }
}
