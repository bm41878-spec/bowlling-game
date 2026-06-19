// Phase 8 — 모드별 최고 점수 기록·정렬·조회. SaveSystem 위에 얹는 도메인 서비스.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 게임 종료 점수를 <see cref="SaveData.highScores"/> 에 기록하고, 모드별 상위 N개만 유지하는 정적 서비스.
    /// 저장/로드는 <see cref="SaveSystem"/> 에 위임한다.
    /// </summary>
    /// <remarks>
    /// - 모드별 분리: 쇼트(5프레임)·풀(10프레임)은 만점이 달라(75/150) 같은 리스트에서 비교하면 풀이 항상 이긴다.
    ///   따라서 <c>modeName</c> 기준으로 그룹화해 각 모드당 <see cref="MaxPerMode"/>개만 보존한다.
    /// - 정렬: 점수 내림차순, 동점 시 <c>playedAt</c>(ISO 8601) 최신 우선.
    /// - 신기록 판정: 기록 직전의 해당 모드 최고점보다 이번 점수가 크면 신기록.
    /// </remarks>
    public static class HighScoreService
    {
        /// <summary>모드당 보존할 최고 기록 수.</summary>
        public const int MaxPerMode = 10;

        private const string LogPrefix = "[HighScore]";

        /// <summary><see cref="Record"/> 결과 — 신기록 여부와 갱신 후 최고점.</summary>
        public readonly struct RecordResult
        {
            public readonly bool IsNewRecord;
            public readonly int  BestScore;

            public RecordResult(bool isNewRecord, int bestScore)
            {
                IsNewRecord = isNewRecord;
                BestScore   = bestScore;
            }
        }

        /// <summary>
        /// 한 게임 결과를 기록한다. 로드 → 직전 최고점 캡처 → 추가 → 모드별 Top N 정리 → 저장.
        /// </summary>
        /// <param name="modeName">모드명 (예: "쇼트 모드"). null/빈 문자열이면 ""로 정규화.</param>
        /// <param name="frameCount">해당 모드의 프레임 수.</param>
        /// <param name="score">이번 게임의 최종 점수.</param>
        /// <returns>신기록 여부 + 갱신 후 최고점.</returns>
        public static RecordResult Record(string modeName, int frameCount, int score)
        {
            modeName = modeName ?? string.Empty;

            SaveData data = SaveSystem.Load();
            if (data.highScores == null) data.highScores = new List<GameRecord>();

            int prevBest = BestScoreOf(data.highScores, modeName);

            data.highScores.Add(new GameRecord
            {
                modeName   = modeName,
                frameCount = frameCount,
                score      = score,
                playedAt   = DateTime.UtcNow.ToString("o"),  // ISO 8601 (round-trip)
            });

            data.highScores = TrimPerMode(data.highScores);
            SaveSystem.Save(data);

            bool isNewRecord = score > prevBest;
            int  bestScore   = Mathf.Max(prevBest, score);
            Debug.Log($"{LogPrefix} 기록: {modeName} {score}점 (직전 최고 {prevBest} → 최고 {bestScore}{(isNewRecord ? ", 신기록!" : "")})");
            return new RecordResult(isNewRecord, bestScore);
        }

        /// <summary>해당 모드의 현재 최고 점수. 기록이 없으면 0.</summary>
        public static int GetBestScore(string modeName)
        {
            modeName = modeName ?? string.Empty;
            SaveData data = SaveSystem.Load();
            return BestScoreOf(data.highScores, modeName);
        }

        /// <summary>해당 모드의 상위 기록 목록 (정렬됨, 최대 <see cref="MaxPerMode"/>개).</summary>
        public static List<GameRecord> GetHighScores(string modeName)
        {
            modeName = modeName ?? string.Empty;
            SaveData data = SaveSystem.Load();
            if (data.highScores == null) return new List<GameRecord>();
            return data.highScores
                .Where(r => r != null && r.modeName == modeName)
                .OrderByDescending(r => r.score)
                .ThenByDescending(r => r.playedAt, StringComparer.Ordinal)
                .Take(MaxPerMode)
                .ToList();
        }

        // ---------- 내부 헬퍼 ----------

        private static int BestScoreOf(List<GameRecord> records, string modeName)
        {
            if (records == null) return 0;
            int best = 0;
            bool any = false;
            foreach (var r in records)
            {
                if (r == null || r.modeName != modeName) continue;
                if (!any || r.score > best) { best = r.score; any = true; }
            }
            return any ? best : 0;
        }

        /// <summary>모드별로 그룹화해 점수 내림차순(동점 시 날짜 최신) 정렬 후 모드당 Top N만 남긴다.</summary>
        private static List<GameRecord> TrimPerMode(List<GameRecord> records)
        {
            if (records == null) return new List<GameRecord>();
            return records
                .Where(r => r != null)
                .GroupBy(r => r.modeName ?? string.Empty)
                .SelectMany(g => g
                    .OrderByDescending(r => r.score)
                    .ThenByDescending(r => r.playedAt, StringComparer.Ordinal)
                    .Take(MaxPerMode))
                .ToList();
        }
    }
}
