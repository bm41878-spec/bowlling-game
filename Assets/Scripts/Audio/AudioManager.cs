using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 사운드 재생 중앙 관리자. mainmenu.unity 에 진입점 GameObject 로 배치되어
    /// DontDestroyOnLoad 로 Game.unity / Gameover_scene.unity 까지 살아서 넘어간다.
    /// </summary>
    /// <remarks>
    /// 책임 범위:
    /// <list type="bullet">
    ///   <item>SFX 재생 (핀 충돌·스트라이크·거터) — 클립 미배선 시 LogWarning 후 무시 (fail-safe)</item>
    ///   <item>BowlingBall / FrameManager 이벤트 구독 — SceneManager.sceneLoaded 후크로 씬 전이 시 재배선</item>
    ///   <item>AudioMixer 그룹별 음량 제어 + SaveData 영속화 (Start 시점에 SaveSystem.Load 로 복원)</item>
    /// </list>
    /// 확장 가이드: BGM/UI 클릭음 추가 시 동일 패턴 — SerializeField 로 AudioClip 추가 + Play* 메서드 + 구독자 와이어링.
    /// AudioSource 가 부족하면 BGM 용 두 번째 AudioSource (loop=true) 를 별도 SerializeField 로 분리한다.
    /// </remarks>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        private const string LogPrefix = "[Audio]";
        private const float MinVolumeDb = -80f;

        // AudioMixer 의 Exposed Parameter 이름 — .mixer 자산의 노출 파라미터 이름과 정확히 일치해야 한다.
        // 이름 변경 시 자산 측 노출 설정도 동기화하지 않으면 SetFloat 가 조용히 실패.
        private const string MasterParam = "MasterVolume";
        private const string SfxParam    = "SFXVolume";
        private const string BgmParam    = "BGMVolume";

        [Header("Mixer")]
        [SerializeField] private AudioMixer audioMixer;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup bgmGroup;

        [Header("Sources")]
        [Tooltip("일회성 SFX 재생용 AudioSource (PlayOneShot). outputAudioMixerGroup = sfxGroup.")]
        [SerializeField] private AudioSource sfxSource;

        [Tooltip("굴림 같은 지속 SFX 전용 AudioSource. loop=true 권장. outputAudioMixerGroup = sfxGroup.")]
        [SerializeField] private AudioSource rollSource;

        [Tooltip("스트라이크/스페어/거터 보이스 전용 AudioSource. 다른 SFX 와 겹쳐 재생되어도 서로 끊기지 않도록 분리. 미배선 시 sfxSource 로 폴백. outputAudioMixerGroup = sfxGroup.")]
        [SerializeField] private AudioSource voiceSource;

        [Tooltip("조준 비프음 전용 AudioSource. panStereo 가 매 호출마다 바뀌므로 sfxSource 와 분리 (다른 SFX 패닝 오염 방지). outputAudioMixerGroup = sfxGroup.")]
        [SerializeField] private AudioSource aimBeepSource;

        [Header("Clips (null 허용 — 미배선 시 LogWarning 후 무시)")]
        [SerializeField] private AudioClip pinHitClip;
        [SerializeField] private AudioClip strikeClip;
        [SerializeField] private AudioClip gutterClip;
        [SerializeField] private AudioClip ballRollClip;

        [Header("Voice Clips (null 허용 — 미배선 시 LogWarning 후 무시)")]
        [Tooltip("스트라이크 발생 시 재생할 보이스 (예: 아나운서 'Strike!'). 표시 연출은 AnnouncementController 가 별도 담당.")]
        [SerializeField] private AudioClip strikeVoiceClip;
        [Tooltip("스페어 발생 시 재생할 보이스 (예: 'Spare!').")]
        [SerializeField] private AudioClip spareVoiceClip;
        [Tooltip("거터 발생 시 재생할 보이스 (예: 'Gutter…').")]
        [SerializeField] private AudioClip gutterVoiceClip;

        [Tooltip("조준 위치가 레인 좌/우 끝단에 도달했을 때 1회 재생되는 짧은 비프 ('삐'). 시각장애 접근성 — SaveData.aimingAudioGuide 가 ON 일 때만 BallAimer 가 호출.")]
        [SerializeField] private AudioClip aimEdgeBeepClip;

        // 현재 씬에서 구독 중인 참조 — 씬 전이 시 Unwire 에 사용.
        private BowlingBall subscribedBall;
        private FrameManager subscribedFrameManager;
        private ThrowTransitionController subscribedTransition;
        private GameStateManager subscribedStateManager;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.Log($"{LogPrefix} 중복 인스턴스 감지 — 자기 자신 Destroy");
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            var save = SaveSystem.Load();
            ApplyVolumes(save.masterVolume, save.sfxVolume, save.bgmVolume);
            SetMuted(save.isMuted);

            SceneManager.sceneLoaded += HandleSceneLoaded;
            // mainmenu 진입 시점 (또는 Game.unity 단독 Play 진입 시점) 1회 시도.
            TryWireSceneRefs();

            Debug.Log($"{LogPrefix} 초기화 완료 — DontDestroyOnLoad 활성");
        }

        void OnDestroy()
        {
            UnwireSceneRefs();
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 이전 씬의 BowlingBall / FrameManager 참조는 이미 Destroy 됐을 가능성 — 정리 후 새 씬에서 재시도.
            UnwireSceneRefs();
            TryWireSceneRefs();
        }

        // ---------- 이벤트 구독 라이프사이클 ----------

        private void TryWireSceneRefs()
        {
            var ball = FindFirstObjectByType<BowlingBall>(FindObjectsInactive.Include);
            if (ball != null)
            {
                ball.OnFirstPinContact += HandlePinHit;
                subscribedBall = ball;
                Debug.Log($"{LogPrefix} BowlingBall 이벤트 구독 (OnFirstPinContact)");
            }

            // GameStateManager 는 Rolling 진입/이탈을 굴림 사운드 Start/Stop 트리거로 사용.
            // 다른 싱글톤보다 먼저 Awake 에 Instance 가 세팅되므로 본 시점에 안전.
            var stateMgr = GameStateManager.Instance;
            if (stateMgr != null)
            {
                stateMgr.OnStateChanged += HandleStateChanged;
                subscribedStateManager = stateMgr;
                Debug.Log($"{LogPrefix} GameStateManager.OnStateChanged 구독 (굴림 사운드)");
            }

            // FrameManager / TransitionController 는 GameManager 가 보유.
            // GameManager 가 [DefaultExecutionOrder(1000)] 이라 본 Start 시점엔 아직 Initialize 전일 수 있다.
            // 1회 시도 후 실패 시 코루틴으로 한 프레임 뒤 재시도.
            if (!TryWireGameManagerRefs())
                StartCoroutine(WireGameManagerRefsDeferred());
        }

        private bool TryWireGameManagerRefs()
        {
            if (GameManager.Instance == null) return false;
            var fm = GameManager.Instance.FrameManager;
            var tc = GameManager.Instance.TransitionController;
            if (fm == null || tc == null) return false;

            fm.OnFrameCompleted += HandleFrameCompleted;   // 스트라이크/스페어 사운드
            subscribedFrameManager = fm;
            tc.OnGutterBall += HandleGutter;               // 거터 판정(거터 진입 + 0핀)
            subscribedTransition = tc;
            Debug.Log($"{LogPrefix} FrameManager.OnFrameCompleted / TransitionController.OnGutterBall 구독");
            return true;
        }

        // GameManager 의 DefaultExecutionOrder(1000) 보다 뒤늦은 시점에서 다시 시도.
        // 2프레임 마진을 두어 GameManager.Start 가 확실히 끝난 뒤 잡도록 한다.
        private IEnumerator WireGameManagerRefsDeferred()
        {
            yield return null;
            yield return null;
            if (subscribedFrameManager != null && subscribedTransition != null) yield break;
            if (!TryWireGameManagerRefs())
                Debug.Log($"{LogPrefix} GameManager 미발견 — 현재 씬에 GameManager 없음 (mainmenu / Gameover_scene 정상)");
        }

        private void UnwireSceneRefs()
        {
            if (subscribedBall != null)
            {
                subscribedBall.OnFirstPinContact -= HandlePinHit;
                subscribedBall = null;
            }
            if (subscribedFrameManager != null)
            {
                subscribedFrameManager.OnFrameCompleted -= HandleFrameCompleted;
                subscribedFrameManager = null;
            }
            if (subscribedTransition != null)
            {
                subscribedTransition.OnGutterBall -= HandleGutter;
                subscribedTransition = null;
            }
            if (subscribedStateManager != null)
            {
                subscribedStateManager.OnStateChanged -= HandleStateChanged;
                subscribedStateManager = null;
            }

            // 씬 전이 시 굴림 사운드가 켜져 있었다면 정리.
            StopBallRoll();
        }

        // ---------- 이벤트 핸들러 ----------
        // 본문에 표시/사운드 로직 인라인 금지 (AI_PROMPT_REFERENCE.md §7-12 패턴 답습).
        // 핸들러는 어느 Play* 를 호출할지만 결정.

        // 핀 접촉 시: 충돌음 재생 + 굴림 사운드 즉시 정지.
        // (정지 자체는 Rolling→Scoring 전이에서도 발생하지만, 그쪽은 settleDetector 대기 후라 늦음.
        //  핀 접촉 순간 굴림이 멈추는 게 청각적으로 더 자연스러움.)
        private void HandlePinHit()
        {
            PlayPinHit();
            StopBallRoll();
        }

        private void HandleGutter()
        {
            PlayGutter();
            PlayGutterVoice();
        }

        private void HandleFrameCompleted(int frameIndex, Frame frame)
        {
            if (frame == null) return;
            if (frame.IsStrike())
            {
                PlayStrike();
                PlayStrikeVoice();
            }
            else if (frame.IsSpare())
            {
                PlaySpareVoice();
            }
        }

        // Rolling 진입 시 굴림 사운드 시작, 이탈 시 정지.
        // prev 매개변수로 단일 책임 — Rolling → Scoring 이든 Rolling → 예외든 모두 일관 처리.
        private void HandleStateChanged(GameState prev, GameState next)
        {
            if (next == GameState.Rolling && prev != GameState.Rolling) StartBallRoll();
            else if (prev == GameState.Rolling && next != GameState.Rolling) StopBallRoll();
        }

        // ---------- 공개 재생 API ----------

        public void PlayPinHit() => PlayOneShotSafe(pinHitClip, "pinHit");
        public void PlayStrike() => PlayOneShotSafe(strikeClip, "strike");
        public void PlayGutter() => PlayOneShotSafe(gutterClip, "gutter");

        // 보이스 — 전용 voiceSource(미배선 시 sfxSource 폴백)로 재생해 다른 SFX 와 겹쳐도 끊기지 않는다.
        public void PlayStrikeVoice() => PlayVoiceSafe(strikeVoiceClip, "strikeVoice");
        public void PlaySpareVoice()  => PlayVoiceSafe(spareVoiceClip,  "spareVoice");
        public void PlayGutterVoice() => PlayVoiceSafe(gutterVoiceClip, "gutterVoice");

        /// <summary>
        /// 조준 끝단 비프 — pan 은 -1(완전 좌) ~ +1(완전 우). 전용 source 를 통해 패닝하므로
        /// 다른 SFX 의 stereo balance 에 영향이 없다. 클립/source 미배선 시 LogWarning 후 무시.
        /// </summary>
        public void PlayAimEdgeBeep(float pan)
        {
            if (aimEdgeBeepClip == null)
            {
                Debug.LogWarning($"{LogPrefix} aimEdgeBeepClip 미배선 — 비프 무시");
                return;
            }
            if (aimBeepSource == null)
            {
                Debug.LogWarning($"{LogPrefix} aimBeepSource 미배선 — 비프 무시");
                return;
            }
            aimBeepSource.panStereo = Mathf.Clamp(pan, -1f, 1f);
            aimBeepSource.PlayOneShot(aimEdgeBeepClip);
        }

        // 굴림 사운드 — PlayOneShot 이 아닌 별도 source 의 Play/Stop 패턴 (loop=true 전제).
        public void StartBallRoll()
        {
            if (ballRollClip == null)
            {
                Debug.LogWarning($"{LogPrefix} ballRollClip 미배선 — 굴림 사운드 무시");
                return;
            }
            if (rollSource == null)
            {
                Debug.LogWarning($"{LogPrefix} rollSource 미배선 — 굴림 사운드 무시");
                return;
            }
            if (rollSource.isPlaying && rollSource.clip == ballRollClip) return;  // 이미 같은 클립 재생 중이면 무시

            rollSource.clip = ballRollClip;
            rollSource.loop = true;
            rollSource.Play();
            Debug.Log($"{LogPrefix} 굴림 시작");
        }

        public void StopBallRoll()
        {
            if (rollSource == null) return;
            if (!rollSource.isPlaying) return;
            rollSource.Stop();
            Debug.Log($"{LogPrefix} 굴림 정지");
        }

        private void PlayOneShotSafe(AudioClip clip, string label)
        {
            if (clip == null)
            {
                Debug.LogWarning($"{LogPrefix} {label} 클립 미배선 — 재생 무시");
                return;
            }
            if (sfxSource == null)
            {
                Debug.LogWarning($"{LogPrefix} sfxSource 미배선 — {label} 재생 무시");
                return;
            }
            sfxSource.PlayOneShot(clip);
            Debug.Log($"{LogPrefix} 재생: {label}");
        }

        // 보이스 전용 — voiceSource 우선, 없으면 sfxSource 로 폴백. 클립/소스 모두 없으면 LogWarning 후 무시.
        private void PlayVoiceSafe(AudioClip clip, string label)
        {
            if (clip == null)
            {
                Debug.LogWarning($"{LogPrefix} {label} 클립 미배선 — 재생 무시");
                return;
            }
            var src = voiceSource != null ? voiceSource : sfxSource;
            if (src == null)
            {
                Debug.LogWarning($"{LogPrefix} voiceSource/sfxSource 미배선 — {label} 재생 무시");
                return;
            }
            src.PlayOneShot(clip);
            Debug.Log($"{LogPrefix} 보이스 재생: {label}");
        }

        // ---------- 음량 ----------

        // 마지막으로 외부에서 설정한 선형 음량 캐시. mute 토글 시 이 값들을 그대로 사용해 복원.
        private float lastMasterVolume = 1.0f;
        private float lastSfxVolume    = 1.0f;
        private float lastBgmVolume    = 0.7f;
        private bool  isMuted = false;

        public bool IsMuted => isMuted;

        public void SetMasterVolume(float v) { lastMasterVolume = v; ApplyEffectiveMixer(MasterParam, v); }
        public void SetSFXVolume(float v)    { lastSfxVolume    = v; ApplyEffectiveMixer(SfxParam,    v); }
        public void SetBGMVolume(float v)    { lastBgmVolume    = v; ApplyEffectiveMixer(BgmParam,    v); }

        /// <summary>음소거 설정. 음량 값은 보존되며 토글 시 즉시 복원되거나 -80dB 강제.</summary>
        public void SetMuted(bool muted)
        {
            if (isMuted == muted) return;
            isMuted = muted;
            // 모든 그룹 재적용 — 캐싱된 last* 값 기준
            ApplyEffectiveMixer(MasterParam, lastMasterVolume);
            ApplyEffectiveMixer(SfxParam,    lastSfxVolume);
            ApplyEffectiveMixer(BgmParam,    lastBgmVolume);
            Debug.Log($"{LogPrefix} 음소거 {(muted ? "ON" : "OFF")}");
        }

        private void ApplyVolumes(float master, float sfx, float bgm)
        {
            SetMasterVolume(master);
            SetSFXVolume(sfx);
            SetBGMVolume(bgm);
            Debug.Log($"{LogPrefix} 음량 적용 — master={master:F2}, sfx={sfx:F2}, bgm={bgm:F2}");
        }

        // isMuted 합성 — mute 시 선형 입력 무시하고 0 으로 강제. SetMixerVolume 은 mute 를 모름.
        private void ApplyEffectiveMixer(string param, float linear)
        {
            float effective = isMuted ? 0f : linear;
            SetMixerVolume(param, effective);
        }

        // 선형 음량 0~1 → dB(-80~0). AudioMixer 는 dB 기준이므로 변환 필수.
        // 0 입력은 Log10(0) = -infinity 이므로 -80dB 로 clamp (사실상 무음).
        private void SetMixerVolume(string param, float linear)
        {
            if (audioMixer == null) return;
            float clamped = Mathf.Clamp01(linear);
            float db = (clamped <= 0.0001f) ? MinVolumeDb : Mathf.Log10(clamped) * 20f;
            if (!audioMixer.SetFloat(param, db))
                Debug.LogWarning($"{LogPrefix} AudioMixer 파라미터 '{param}' 노출 안 됨 — .mixer 자산의 Exposed Parameters 확인 필요");
        }
    }
}
