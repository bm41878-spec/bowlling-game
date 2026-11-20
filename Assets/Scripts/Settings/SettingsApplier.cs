using UnityEngine;
using UnityEngine.SceneManagement;

namespace BowlingGame
{
    /// <summary>
    /// 게임 진입 시 <see cref="SaveData"/> 의 사용자 설정을 카테고리별로 시스템에 일괄 적용하는 단일 진입점.
    /// mainmenu.unity 의 GameObject 에 부착되어 DontDestroyOnLoad 로 전 씬에서 살아있다.
    /// </summary>
    /// <remarks>
    /// 책임 범위:
    /// <list type="bullet">
    ///   <item>Awake/Start 시 SaveData 로드 → 카테고리별 Apply* 호출 (현재 오디오만)</item>
    ///   <item><see cref="RefreshFromSave"/> 호출 시 모든 카테고리 일괄 재적용 — 설정 UI 에서 값 변경 후 즉시 반영용</item>
    ///   <item>카테고리별 적용은 해당 시스템(AudioManager / 추후 DisplaySetter / InputBinder 등) 에 위임 — 본 클래스는 라우터</item>
    /// </list>
    /// 확장 가이드: 새 카테고리 추가 시 <see cref="RefreshFromSave"/> 에 Apply{Category}(save) 줄 추가 + 해당 메서드 구현.
    /// </remarks>
    public class SettingsApplier : MonoBehaviour
    {
        public static SettingsApplier Instance { get; private set; }
        private const string LogPrefix = "[Settings]";

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

            // 씬 전이마다 UI 스케일 재적용 — 새 씬의 CanvasScaler 들에 다시 적용해야 한다 (DisplaySetter.ApplyUIScale 은 활성 씬만 스캔).
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void Start()
        {
            RefreshFromSave();
            Debug.Log($"{LogPrefix} 초기화 완료 — DontDestroyOnLoad 활성");
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 추가 씬 (additive) 은 무시 — 새 단일 씬 진입 시에만 적용.
            if (mode != LoadSceneMode.Single) return;
            var save = SaveSystem.Load();
            DisplaySetter.ApplyUIScale(save.uiScale);
        }

        /// <summary>
        /// SaveData 를 다시 로드해 모든 카테고리를 일괄 적용한다.
        /// 설정 UI 에서 값 변경 후 호출하면 모든 시스템이 즉시 동기화된다.
        /// </summary>
        public void RefreshFromSave()
        {
            var save = SaveSystem.Load();
            ApplyAudio(save);
            ApplyInput(save);
            ApplyDisplay(save);
            ApplyAccessibility(save);
        }

        // ---------- 카테고리별 적용 ----------

        private void ApplyAudio(SaveData save)
        {
            if (AudioManager.Instance == null)
            {
                // AudioManager.Start 가 자체적으로 SaveData 를 적용하므로 본 메서드는
                // 부트스트랩 경로에서는 보통 호출 시점에 Instance 가 아직 없다 — 정상.
                // RefreshFromSave 가 외부에서 호출될 때(설정 UI 변경 후)는 Instance 가 있어야 한다.
                return;
            }

            AudioManager.Instance.SetMasterVolume(save.masterVolume);
            AudioManager.Instance.SetSFXVolume(save.sfxVolume);
            AudioManager.Instance.SetBGMVolume(save.bgmVolume);
            AudioManager.Instance.SetMuted(save.isMuted);
        }

        private void ApplyInput(SaveData save)
        {
            if (InputController.Instance == null) return;
            InputController.Instance.LoadBindingOverridesJson(save.inputOverridesJson);
        }

        private void ApplyDisplay(SaveData save)
        {
            // 해상도 / 창 모드는 정적 호출 — 씬 의존 없음.
            DisplaySetter.ApplyResolution(save.screenWidth, save.screenHeight, (FullScreenMode)save.fullScreenMode);
            // UI 스케일은 활성 씬의 CanvasScaler 들 대상 — sceneLoaded 후크에서 재적용되지만 초기 진입(mainmenu) 에서도 한 번 호출.
            DisplaySetter.ApplyUIScale(save.uiScale);
        }

        // 접근성: 현재는 라우터에 자리만. aimingAudioGuide 는 BallAimer 가 AimingPosition 진입 시점에
        // SaveSystem.Load 로 직접 캐싱하므로 본 메서드에서 runtime 상태를 푸시할 대상이 없다.
        // colorblindMode 는 실제 구현 (Color Adjust / Renderer Feature) 추가 시 여기서 적용 호출.
        private void ApplyAccessibility(SaveData save)
        {
            // 자리 표시 — 향후 색맹 모드 실제 적용 시 ColorblindFilter.Apply(save.colorblindMode) 등 호출.
        }
    }
}
