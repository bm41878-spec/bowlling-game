using UnityEngine;

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
        }

        void Start()
        {
            RefreshFromSave();
            Debug.Log($"{LogPrefix} 초기화 완료 — DontDestroyOnLoad 활성");
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
            // 다음 세션에 추가될 카테고리들 (자리만 남김 — 실제 구현은 SaveData 필드 추가 이후):
            // ApplyDisplay(save);
            // ApplyAccessibility(save);
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
    }
}
