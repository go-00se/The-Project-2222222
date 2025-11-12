using System.Linq;
using UnityEngine;
using System.Collections;

public class FirstPersonAudio : MonoBehaviour
{
    public FirstPersonMovement character;
    public GroundCheck groundCheck;

    [Header("Step")]
    public AudioSource stepAudio;
    public AudioSource runningAudio;
    [Tooltip("Minimum velocity for moving audio to play")]
    /// <summary> "Minimum velocity for moving audio to play" </summary>
    public float velocityThreshold = .01f;
    Vector2 lastCharacterPosition;
    Vector2 CurrentCharacterPosition => new Vector2(character.transform.position.x, character.transform.position.z);

    [Header("Landing")]
    public AudioSource landingAudio;
    public AudioClip[] landingSFX;

    [Header("Jump")]
    public Jump jump;
    public AudioSource jumpAudio;
    public AudioClip[] jumpSFX;

    [Header("Crouch")]
    public Crouch crouch;
    public AudioSource crouchStartAudio, crouchedAudio, crouchEndAudio;
    public AudioClip[] crouchStartSFX, crouchEndSFX;
    
    [Header("BGM & Breathing")]
    public AudioSource bgmAudio;
    public AudioClip[] bgmClips;

    public AudioSource breathingAudio;
    public AudioClip[] breathingClips;
    public float breathingDuration = 3f; // 喘息持续时间


    AudioSource[] MovingAudios => new AudioSource[] { stepAudio, runningAudio, crouchedAudio };


    void Reset()
    {
        // Setup stuff.
        character = GetComponentInParent<FirstPersonMovement>();
        groundCheck = (transform.parent ?? transform).GetComponentInChildren<GroundCheck>();
        stepAudio = GetOrCreateAudioSource("Step Audio");
        runningAudio = GetOrCreateAudioSource("Running Audio");
        landingAudio = GetOrCreateAudioSource("Landing Audio");

        // Setup jump audio.
        jump = GetComponentInParent<Jump>();
        if (jump)
        {
            jumpAudio = GetOrCreateAudioSource("Jump audio");
        }

        // Setup crouch audio.
        crouch = GetComponentInParent<Crouch>();
        if (crouch)
        {
            crouchStartAudio = GetOrCreateAudioSource("Crouch Start Audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouched Audio");
            crouchStartAudio = GetOrCreateAudioSource("Crouch End Audio");
        }
        
        // Setup BGM & Breathing
        bgmAudio = GetOrCreateAudioSource("BGM Audio");
        breathingAudio = GetOrCreateAudioSource("Breathing Audio");
    }

    void Start()
    {
        // 播放喘息声
        if (breathingClips != null && breathingClips.Length > 0)
        {
            PlayRandomClip(breathingAudio, breathingClips);
        }

        // 延迟播放背景音乐
        if (bgmClips != null && bgmClips.Length > 0)
        {
            Invoke(nameof(PlayBGM), breathingDuration);
        }
    }
    IEnumerator FadeInBGM()
    {
        breathingAudio.volume = 0;
        breathingAudio.Play();
        while (breathingAudio.volume < 1f)
        {
            breathingAudio.volume += Time.deltaTime / 2f; // 2秒淡入
            yield return null;
        }
    }

    
    void PlayBGM()
    {
        // 随机挑一首BGM
        AudioClip clip = bgmClips[Random.Range(0, bgmClips.Length)];
        bgmAudio.clip = clip;
        bgmAudio.loop = true;
        bgmAudio.Play();
    }
    void OnEnable() => SubscribeToEvents();

    void OnDisable() => UnsubscribeToEvents();

    void FixedUpdate()
    {
        // Play moving audio if the character is moving and on the ground.
        float velocity = Vector3.Distance(CurrentCharacterPosition, lastCharacterPosition);
        if (velocity >= velocityThreshold && groundCheck && groundCheck.isGrounded)
        {
            if (crouch && crouch.IsCrouched)
            {
                SetPlayingMovingAudio(crouchedAudio);
            }
            else if (character.IsRunning)
            {
                SetPlayingMovingAudio(runningAudio);
            }
            else
            {
                SetPlayingMovingAudio(stepAudio);
            }
        }
        else
        {
            SetPlayingMovingAudio(null);
        }

        // Remember lastCharacterPosition.
        lastCharacterPosition = CurrentCharacterPosition;
    }


    /// <summary>
    /// Pause all MovingAudios and enforce play on audioToPlay.
    /// </summary>
    /// <param name="audioToPlay">Audio that should be playing.</param>
    void SetPlayingMovingAudio(AudioSource audioToPlay)
    {
        // Pause all MovingAudios.
        foreach (var audio in MovingAudios.Where(audio => audio != audioToPlay && audio != null))
        {
            audio.Pause();
        }

        // Play audioToPlay if it was not playing.
        if (audioToPlay && !audioToPlay.isPlaying)
        {
            audioToPlay.Play();
        }
    }

    #region Play instant-related audios.
    void PlayLandingAudio() => PlayRandomClip(landingAudio, landingSFX);
    void PlayJumpAudio() => PlayRandomClip(jumpAudio, jumpSFX);
    void PlayCrouchStartAudio() => PlayRandomClip(crouchStartAudio, crouchStartSFX);
    void PlayCrouchEndAudio() => PlayRandomClip(crouchEndAudio, crouchEndSFX);
    #endregion

    #region Subscribe/unsubscribe to events.
    void SubscribeToEvents()
    {
        // PlayLandingAudio when Grounded.
        groundCheck.Grounded += PlayLandingAudio;

        // PlayJumpAudio when Jumped.
        if (jump)
        {
            jump.Jumped += PlayJumpAudio;
        }

        // Play crouch audio on crouch start/end.
        if (crouch)
        {
            crouch.CrouchStart += PlayCrouchStartAudio;
            crouch.CrouchEnd += PlayCrouchEndAudio;
        }
    }

    void UnsubscribeToEvents()
    {
        // Undo PlayLandingAudio when Grounded.
        groundCheck.Grounded -= PlayLandingAudio;

        // Undo PlayJumpAudio when Jumped.
        if (jump)
        {
            jump.Jumped -= PlayJumpAudio;
        }

        // Undo play crouch audio on crouch start/end.
        if (crouch)
        {
            crouch.CrouchStart -= PlayCrouchStartAudio;
            crouch.CrouchEnd -= PlayCrouchEndAudio;
        }
    }
    #endregion

    #region Utility.
    /// <summary>
    /// Get an existing AudioSource from a name or create one if it was not found.
    /// </summary>
    /// <param name="name">Name of the AudioSource to search for.</param>
    /// <returns>The created AudioSource.</returns>
    AudioSource GetOrCreateAudioSource(string name)
    {
        // Try to get the audiosource.
        AudioSource result = System.Array.Find(GetComponentsInChildren<AudioSource>(), a => a.name == name);
        if (result)
            return result;

        // Audiosource does not exist, create it.
        result = new GameObject(name).AddComponent<AudioSource>();
        result.spatialBlend = 1;
        result.playOnAwake = false;
        result.transform.SetParent(transform, false);
        return result;
    }

    static void PlayRandomClip(AudioSource audio, AudioClip[] clips)
    {
        if (!audio || clips.Length <= 0)
            return;

        // Get a random clip. If possible, make sure that it's not the same as the clip that is already on the audiosource.
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clips.Length > 1)
            while (clip == audio.clip)
                clip = clips[Random.Range(0, clips.Length)];

        // Play the clip.
        audio.clip = clip;
        audio.Play();
    }
    #endregion 
}
