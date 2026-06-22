using System.Collections.Generic;
using UnityEngine;
using Bowling.Scoring;

namespace BowlingGame
{
    /// <summary>
    /// 점수판을 카드 그리드로 표시 (옵션 B). 각 프레임이 독립 카드로 렌더링되며,
    /// 현재 진행 중 프레임은 배경색 / Highlight 로 강조된다.
    /// </summary>
    /// <remarks>
    /// 라이프사이클: <see cref="Initialize"/> 가 frameCount 만큼 prefab 을 인스턴스화하여
    /// <paramref name="cardContainer"/> 의 자식으로 추가. 재초기화 시 기존 카드 모두 Destroy 후 새로 생성.
    /// </remarks>
    public class CardLayoutRenderer : ScoreboardLayoutRenderer
    {
        [Header("Layout")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject cardPrefab;  // FrameCardUI 부착된 prefab

        private const string LogPrefix = "[Scoreboard:Card]";

        // 인덱스 0..frameCount-1 의 카드 인스턴스.
        private readonly List<FrameCardUI> _cards = new List<FrameCardUI>();
        private int _activeFrameIndex = -1;

        public override void Initialize(int frameCount)
        {
            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogError($"{LogPrefix} cardContainer / cardPrefab 미할당 — 초기화 중단");
                return;
            }

            // 기존 카드 제거 — 재시작 호환.
            ClearAllCards();

            for (int i = 0; i < frameCount; i++)
            {
                GameObject go = Instantiate(cardPrefab, cardContainer);
                go.name = $"FrameCard_{i + 1}";
                var card = go.GetComponent<FrameCardUI>();
                if (card == null)
                {
                    Debug.LogError($"{LogPrefix} cardPrefab 에 FrameCardUI 컴포넌트 없음 — 인덱스 {i}");
                    continue;
                }
                card.SetFrameNumber(i + 1);
                card.Clear();
                _cards.Add(card);
            }

            _activeFrameIndex = -1;
            Debug.Log($"{LogPrefix} 카드 {_cards.Count}개 생성 완료");
        }

        public override void UpdateThrow(int frameIndex, int throwNumber, Frame frame)
        {
            if (!IsValidIndex(frameIndex)) return;
            _cards[frameIndex].SetThrows(frame, throwNumber);
        }

        public override void UpdateFrameComplete(int frameIndex, Frame frame, int cumulativeScore)
        {
            if (!IsValidIndex(frameIndex)) return;
            // 1·2구 표시도 최종 상태로 한 번 더 갱신 — 스트라이크는 throwNumber=1 시점에 "X -" 가 이미 들어가지만
            // 일반 케이스에서는 2구 표시가 여기서 확정된다.
            _cards[frameIndex].SetThrows(frame, frame.IsStrike() ? 1 : 2);
            _cards[frameIndex].SetCumulativeScore(cumulativeScore);
        }

        public override void SetActiveFrame(int frameIndex)
        {
            if (_activeFrameIndex >= 0 && _activeFrameIndex < _cards.Count)
                _cards[_activeFrameIndex].SetActive(false);

            _activeFrameIndex = frameIndex;

            if (IsValidIndex(frameIndex))
                _cards[frameIndex].SetActive(true);
        }

        public override void SetGameOver(int finalScore)
        {
            // 모든 카드 강조 해제 (게임 종료 시각적 마무리).
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].SetGameOver();
            _activeFrameIndex = -1;
        }

        public override void ClearAll()
        {
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].Clear();
            _activeFrameIndex = -1;
        }

        // ---------- 내부 ----------

        private bool IsValidIndex(int idx) => idx >= 0 && idx < _cards.Count;

        private void ClearAllCards()
        {
            for (int i = 0; i < _cards.Count; i++)
                if (_cards[i] != null) Destroy(_cards[i].gameObject);
            _cards.Clear();
        }
    }
}
