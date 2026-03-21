using UnityEngine;

namespace Overworked.Audio
{
    public class SFXManager : MonoBehaviour
    {
        public static SFXManager Instance { get; private set; }

        [SerializeField] private AudioClip clickSound;
        [SerializeField] private AudioClip selectSound;
        [SerializeField] private AudioClip taskSuccessSound;
        [SerializeField] private AudioClip taskFailSound;
        [SerializeField] private AudioClip newEmailSound;
        [SerializeField] private AudioClip emailExpireSound;
        [SerializeField] private AudioClip tickingRepeatSound;
        [SerializeField] private AudioClip tickingNearSound;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.5f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.5f;

        public float SfxVolume => sfxVolume;
        public float MusicVolume => musicVolume;

        private AudioSource _source;
        private AudioSource _tickSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;

            _tickSource = gameObject.AddComponent<AudioSource>();
            _tickSource.playOnAwake = false;
            _tickSource.loop = true;

            // Load saved volume preferences
            sfxVolume = PlayerPrefs.GetFloat("sfx_volume", 0.5f);
            musicVolume = PlayerPrefs.GetFloat("music_volume", 0.5f);

            // Load from Resources if not assigned in Inspector
            if (clickSound == null)
                clickSound = Resources.Load<AudioClip>("Audio/click_sound");
            if (selectSound == null)
                selectSound = Resources.Load<AudioClip>("Audio/select");
            if (taskSuccessSound == null)
                taskSuccessSound = Resources.Load<AudioClip>("Audio/task_success");
            if (taskFailSound == null)
                taskFailSound = Resources.Load<AudioClip>("Audio/task_fail");
            if (newEmailSound == null)
                newEmailSound = Resources.Load<AudioClip>("Audio/new_email");
            if (emailExpireSound == null)
                emailExpireSound = Resources.Load<AudioClip>("Audio/email_expire");
            if (tickingRepeatSound == null)
                tickingRepeatSound = Resources.Load<AudioClip>("Audio/ticking_repeat");
            if (tickingNearSound == null)
                tickingNearSound = Resources.Load<AudioClip>("Audio/ticking_near");
        }

        public void PlayClick()
        {
            if (clickSound != null && _source != null)
                _source.PlayOneShot(clickSound, sfxVolume);
        }

        public void PlaySelect()
        {
            if (selectSound != null && _source != null)
                _source.PlayOneShot(selectSound, sfxVolume);
        }

        public void PlaySuccess()
        {
            if (taskSuccessSound != null && _source != null)
                _source.PlayOneShot(taskSuccessSound, sfxVolume);
        }

        public void PlayFail()
        {
            if (taskFailSound != null && _source != null)
                _source.PlayOneShot(taskFailSound, sfxVolume);
        }

        public void PlayNewEmail()
        {
            if (newEmailSound != null && _source != null)
                _source.PlayOneShot(newEmailSound, sfxVolume);
        }

        public void PlayEmailExpire()
        {
            if (emailExpireSound != null && _source != null)
                _source.PlayOneShot(emailExpireSound, sfxVolume);
        }

        private bool _isNearTicking;

        public void StartTicking()
        {
            if (_tickSource == null || tickingRepeatSound == null) return;
            if (_tickSource.isPlaying && !_isNearTicking) return; // already playing repeat
            _tickSource.clip = tickingRepeatSound;
            _tickSource.volume = musicVolume * 0.4f;
            _tickSource.Play();
            _isNearTicking = false;
        }

        public void StartNearTicking()
        {
            if (_tickSource == null || tickingNearSound == null) return;
            if (_isNearTicking) return; // already playing near
            _tickSource.clip = tickingNearSound;
            _tickSource.volume = musicVolume * 0.7f;
            _tickSource.Play();
            _isNearTicking = true;
        }

        public void StopTicking()
        {
            if (_tickSource != null && _tickSource.isPlaying)
                _tickSource.Stop();
            _isNearTicking = false;
        }

        public void SetSfxVolume(float v)
        {
            sfxVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("sfx_volume", sfxVolume);
        }

        public void SetMusicVolume(float v)
        {
            musicVolume = Mathf.Clamp01(v);
            PlayerPrefs.SetFloat("music_volume", musicVolume);

            // Update live ticking volume
            if (_tickSource != null && _tickSource.isPlaying)
                _tickSource.volume = musicVolume * (_isNearTicking ? 0.7f : 0.4f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
