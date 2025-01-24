using UnityEngine;
using System.Collections;

namespace TarodevController
{
    public class PlayerSoundManager : MonoBehaviour
    {
        #region Setup
        [Header("Audio Clips")]
        [SerializeField] private AudioClip[] _footstepsClips; 
        [SerializeField] private AudioClip _wallSlideClip; 
        [SerializeField] private AudioClip _jumpClip;
        [SerializeField] private AudioClip _landingClip; 
        [SerializeField] private AudioClip _dashClip; 
        
        [Header("Pitch Variation")]
        private float _minPitchMultiplier = 0.98f; 
        private float _maxPitchMultiplier = 1.02f; 
        
        private AudioSource _footstepSource; 
        private AudioSource _wallSlideSource; 
        private AudioSource _jumpSource; 
        private AudioSource _landingSource;
        private AudioSource _dashSource; 
        
        private float _footstepPitchBase; 
        private float _wallSlidePitchBase;
        private float _jumpPitchBase;
        private float _landingPitchBase;
        private float _dashPitchBase;
        

        private IPlayerController _player;
        private bool _isPlayingWallSlide;
        
        #endregion

        #region Initialization
        private void Awake()
        {
            var audioSources = GetComponents<AudioSource>();
            if (audioSources.Length >= 5)
            {
                _footstepSource = audioSources[0];  
                _wallSlideSource = audioSources[1];  
                _jumpSource = audioSources[2]; 
                _landingSource = audioSources[3];
                _dashSource = audioSources[4]; 

                // Stocker les pitches de base
                _footstepPitchBase = _footstepSource.pitch;
                _wallSlidePitchBase = _wallSlideSource.pitch;
                _jumpPitchBase = _jumpSource.pitch;
                _landingPitchBase = _landingSource.pitch;
                _dashPitchBase = _dashSource.pitch;
            }
            else
            {
                Debug.LogWarning("Pas assez de AudioSources attachées à ce GameObject.");
            }

            _player = GetComponentInParent<IPlayerController>();
        }
        #endregion

        #region Event Handlers
        private void OnEnable()
        {
            _player.Jumped += OnJumped;
            _player.GroundedChanged += OnGroundedChanged;
        }

        private void OnDisable()
        {
            _player.Jumped -= OnJumped;
            _player.GroundedChanged -= OnGroundedChanged;
        }

        private void OnJumped()
        {
            if (_jumpClip != null)
            {
                PlayClipWithPitchVariation(_jumpSource, _jumpClip, _jumpPitchBase);
            }
        }

        private void OnGroundedChanged(bool grounded, float impact)
        {
            if (grounded)
            {
                PlayClipWithPitchVariation(_landingSource, _landingClip, _landingPitchBase);
            }
        }
        #endregion

        #region Sound Handling
        private void Update()
        {
            HandleWallSlideSound();
        }

        private void HandleFootsteps() // Animation event
        {
            PlayRandomClipWithPitchVariation(_footstepSource, _footstepsClips, _footstepPitchBase);   
        }

        private void HandleWallSlideSound()
        {
            if (_player.IsSliding && !_isPlayingWallSlide)
            {
                _wallSlideSource.clip = _wallSlideClip;
                _wallSlideSource.loop = true;
                _wallSlideSource.pitch = Random.Range(_wallSlidePitchBase * _minPitchMultiplier, _wallSlidePitchBase * _maxPitchMultiplier);
                _wallSlideSource.Play();
                _isPlayingWallSlide = true;
            }
            else if (!_player.IsSliding && _isPlayingWallSlide)
            {
                _wallSlideSource.loop = false;
                _wallSlideSource.Stop();
                _isPlayingWallSlide = false;
            }
        }

        private float _dashCooldown = 0.08f; 
        private float _lastDashSoundTime; 
        private void HandleDashSound() // Animation event
        {
            if (Time.time - _lastDashSoundTime >= _dashCooldown)
            {
                PlayClipWithPitchVariation(_dashSource, _dashClip, _dashPitchBase);
                _lastDashSoundTime = Time.time; 
            }
        }
        #endregion

        #region Audio Playback
        private void PlayRandomClipWithPitchVariation(AudioSource source, AudioClip[] clips, float basePitch)
        {
            if (clips == null || clips.Length == 0) return;
            int index = Random.Range(0, clips.Length);
            PlayClipWithPitchVariation(source, clips[index], basePitch);
        }

        private IEnumerator ResetPitchAfterDelay(AudioSource source, float basePitch, float delay)
        {
            yield return new WaitForSeconds(delay);
            source.pitch = basePitch;
        }

        private void PlayClipWithPitchVariation(AudioSource source, AudioClip clip, float basePitch)
        {
            if (clip == null) return;

            source.pitch = Random.Range(basePitch * _minPitchMultiplier, basePitch * _maxPitchMultiplier);

            source.PlayOneShot(clip);

            // Restaurer après un délai égal à la durée du clip
            StartCoroutine(ResetPitchAfterDelay(source, basePitch, clip.length));
        }
        #endregion
    }
}
