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
        [Range(0f, 1f)] [SerializeField] private float volume = 0.5f;

        private AudioSource _source;

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
        }

        public void PlayClick()
        {
            if (clickSound != null && _source != null)
                _source.PlayOneShot(clickSound, volume);
        }

        public void PlaySelect()
        {
            if (selectSound != null && _source != null)
                _source.PlayOneShot(selectSound, volume);
        }

        public void PlaySuccess()
        {
            if (taskSuccessSound != null && _source != null)
                _source.PlayOneShot(taskSuccessSound, volume);
        }

        public void PlayFail()
        {
            if (taskFailSound != null && _source != null)
                _source.PlayOneShot(taskFailSound, volume);
        }

        public void PlayNewEmail()
        {
            if (newEmailSound != null && _source != null)
                _source.PlayOneShot(newEmailSound, volume);
        }

        public void PlayEmailExpire()
        {
            if (emailExpireSound != null && _source != null)
                _source.PlayOneShot(emailExpireSound, volume);
        }

        public void SetVolume(float v)
        {
            volume = Mathf.Clamp01(v);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}
