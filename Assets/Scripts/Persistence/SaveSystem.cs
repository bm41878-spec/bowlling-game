// Phase 8 — JSON 영속화. JsonUtility 로 Application.persistentDataPath/save.json 저장/로드.
using System;
using System.IO;
using UnityEngine;

namespace BowlingGame
{
    /// <summary>
    /// 세이브 데이터(<see cref="SaveData"/>)를 JSON 파일로 저장/로드하는 정적 유틸리티.
    /// 직렬화는 Unity 내장 <see cref="JsonUtility"/> 사용 (의존성 0).
    /// </summary>
    /// <remarks>
    /// - 저장 위치: <c>Application.persistentDataPath/save.json</c>
    ///   (Windows: <c>%USERPROFILE%/AppData/LocalLow/&lt;회사&gt;/&lt;제품&gt;/save.json</c>)
    /// - fail-safe 정책: 파일 I/O / 파싱 실패 시 <see cref="Debug.LogWarning"/> 후 게임 흐름을 막지 않는다
    ///   (예외를 호출자에게 전파하지 않음). 로드 실패 시 빈 <see cref="SaveData"/> 반환.
    /// - 첫 실행(파일 없음): 빈 <see cref="SaveData"/>(version=1, 빈 highScores) 반환.
    /// </remarks>
    public static class SaveSystem
    {
        private const string FileName = "save.json";
        private const string LogPrefix = "[SaveSystem]";

        /// <summary>세이브 파일 전체 경로.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>
        /// 세이브 데이터를 로드한다. 파일이 없거나 파싱에 실패하면 빈 <see cref="SaveData"/> 를 반환한다 (예외 전파 안 함).
        /// </summary>
        public static SaveData Load()
        {
            try
            {
                if (!File.Exists(FilePath))
                {
                    Debug.Log($"{LogPrefix} 세이브 파일 없음 — 빈 데이터로 시작 ({FilePath})");
                    return NewEmpty();
                }

                string json = File.ReadAllText(FilePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data == null)
                {
                    Debug.LogWarning($"{LogPrefix} 파싱 결과가 null — 빈 데이터로 대체");
                    return NewEmpty();
                }

                // JsonUtility 는 누락 필드를 기본값으로 두므로 highScores null 가드.
                if (data.highScores == null) data.highScores = new System.Collections.Generic.List<GameRecord>();
                Debug.Log($"{LogPrefix} 로드 완료 — 기록 {data.highScores.Count}건 (version {data.version})");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogPrefix} 로드 실패 — 빈 데이터로 진행: {e.Message}");
                return NewEmpty();
            }
        }

        /// <summary>
        /// 세이브 데이터를 JSON 으로 직렬화해 파일에 기록한다. 실패해도 예외를 전파하지 않고 경고만 남긴다.
        /// </summary>
        public static void Save(SaveData data)
        {
            if (data == null)
            {
                Debug.LogWarning($"{LogPrefix} Save 호출에 data == null — 무시");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
                Debug.Log($"{LogPrefix} 저장 완료 — 기록 {data.highScores?.Count ?? 0}건 → {FilePath}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{LogPrefix} 저장 실패 — 게임은 계속 진행: {e.Message}");
            }
        }

        private static SaveData NewEmpty() => new SaveData();
    }
}
