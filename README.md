# \# 볼링 게임 프로젝트 (코드네임: \*미정\*)

# 

# > 초등학생 저학년을 대상으로 한 로우폴리 카툰 스타일의 캐주얼 볼링 게임. 전통 볼링 점수 계산 규칙을 기반으로 하되, 프레임 수를 가변(3/5/10)으로 두어 난이도와 플레이 시간을 조절한다.

# 

# \---

# 

# \## 1. 프로젝트 개요

# 

# | 항목 | 내용 |

# |---|---|

# | 장르 | 캐주얼 스포츠 (볼링) |

# | 엔진 | Unity 6 |

# | 렌더 파이프라인 | URP |

# | 타겟 플랫폼 | PC (Windows Standalone) |

# | 카메라 | 3인칭 고정 카메라 (공 뒤쪽 시점) |

# | 시각 스타일 | 로우폴리 카툰 |

# | 사운드 방향 | 카툰 코믹, 과장된 효과음 중심 |

# | 플레이 인원 | 1인 |

# | 개발 기간 | 미정 |

# | 저장 방식 | 로컬 JSON 파일 |

# 

# \---

# 

# \## 2. 타겟 사용자 및 디자인 원칙

# 

# \- 주 사용자: \*\*초등학생 저학년\*\*

# \- 핵심 목표: 높은 점수를 통한 \*\*자기만족\*\*

# \- 디자인 원칙:

# &#x20; - 텍스트 최소화, 큰 아이콘과 색 대비 사용

# &#x20; - 실패 페널티 약화 (거터 시에도 부정적 표현 자제)

# &#x20; - 입력 방식을 \*\*스페이스바 두 번\*\*으로 통일하여 학습 부담 최소화

# &#x20; - 카툰 코믹 효과로 긍정적 피드백 강화

# 

# \---

# 

# \## 3. 게임 규칙

# 

# \### 3.1 점수 계산 (전통 볼링 방식)

# 

# \- \*\*스트라이크 (X)\*\*: 첫 투구로 10핀 모두 쓰러뜨림 → 다음 2회 투구 점수가 보너스로 합산

# \- \*\*스페어 (/)\*\*: 두 번째 투구로 10핀 모두 쓰러뜨림 → 다음 1회 투구 점수가 보너스로 합산

# \- \*\*오픈\*\*: 보너스 없음, 쓰러뜨린 핀 수가 그대로 점수

# \- \*\*마지막 프레임\*\*: 스트라이크 시 +2회 추가 투구, 스페어 시 +1회 추가 투구

# 

# \### 3.2 게임 모드 (프레임 수 가변)

# 

# | 모드 | 프레임 수 | 퍼펙트 스코어 | 예상 플레이 시간 |

# |---|---|---|---|

# | 캐주얼 | 3 | 90점 | 약 2\~3분 |

# | 표준 | 5 | 150점 | 약 4\~5분 |

# | 하드 | 10 | 300점 | 약 8\~10분 |

# 

# > 퍼펙트 스코어 공식: `30 × N` (N = 프레임 수)

# 

# \---

# 

# \## 4. 플레이 방법

# 

# 게임 진행은 다음 상태 머신으로 구성된다.

# 

# ```

# \[Ready] → \[AimingPosition] → \[AimingPower] → \[Rolling] → \[Scoring] →

# &#x20;  (다음 투구) → \[AimingPosition] ...  또는  (게임 종료) → \[GameOver]

# ```

# 

# 1\. \*\*위치 결정 단계 (AimingPosition)\*\*

# &#x20;  - 볼링공이 레인 좌우 끝 사이를 일정 속도로 왕복

# &#x20;  - 스페이스바 입력 시 공의 시작 위치 확정

# 2\. \*\*세기 결정 단계 (AimingPower)\*\*

# &#x20;  - 화살표 UI 길이가 작아졌다 커졌다 반복

# &#x20;  - 스페이스바 입력 시 투구 세기 확정

# 3\. \*\*투구 단계 (Rolling)\*\*

# &#x20;  - 확정된 위치와 세기로 공이 굴러감

# &#x20;  - 핀과 충돌 후 모든 Rigidbody가 정지하면 다음 단계로 전이

# 4\. \*\*점수 산정 단계 (Scoring)\*\*

# &#x20;  - 쓰러진 핀 개수 판정 후 점수판 갱신

# &#x20;  - 보너스 점수는 후속 투구 이후 지연 확정

# 5\. \*\*반복 및 종료\*\*

# &#x20;  - 모든 프레임 완료 시 최종 점수 표시 및 게임 종료

# 

# \---

# 

# \## 5. 기술 스택 및 의존성

# 

# \- \*\*엔진\*\*: Unity 6 (URP)

# \- \*\*입력 시스템\*\*: Unity Input System (신규 패키지)

# \- \*\*물리\*\*: Built-in 3D Physics (Rigidbody, PhysicsMaterial)

# \- \*\*UI\*\*: Unity UI (uGUI) + TextMeshPro

# \- \*\*저장\*\*: JsonUtility 또는 Newtonsoft.Json

# \- \*\*버전 관리\*\*: Git

# 

# \---

# 

# \## 6. 프로젝트 구조 (예정)

# 

# ```

# Assets/

# ├── Scripts/

# │   ├── Core/                  # GameManager, 상태 머신

# │   ├── Gameplay/              # Ball, Pin, Lane, InputController

# │   ├── Scoring/               # ScoreCalculator, Frame, FrameResult

# │   ├── Config/                # ScriptableObject (BowlingRuleConfig 등)

# │   ├── UI/                    # Scoreboard, MainMenu, Tutorial

# │   ├── Audio/                 # AudioManager

# │   ├── Persistence/           # SaveSystem (JSON I/O)

# │   └── Tests/                 # 점수 계산기 유닛 테스트

# ├── Prefabs/                   # Pin, Ball, Lane, UI 등

# ├── Materials/                 # 로우폴리 카툰 머티리얼

# ├── Models/                    # 3D 모델

# ├── Audio/                     # 효과음, BGM

# ├── Scenes/

# │   ├── Main.unity             # 메인 메뉴

# │   ├── Game.unity             # 게임 플레이

# │   └── Tutorial.unity         # 튜토리얼

# └── Configs/                   # ScriptableObject 에셋 (모드별 룰)

# ```

# 

# \---

# 

# \## 7. 핵심 시스템 설계

# 

# \### 7.1 룰 설정 (ScriptableObject)

# 

# 모드별 데이터를 코드 수정 없이 에디터에서 관리 가능하도록 분리한다.

# 

# ```csharp

# \[CreateAssetMenu(fileName = "BowlingRule", menuName = "Bowling/Rule Config")]

# public class BowlingRuleConfig : ScriptableObject

# {

# &#x20;   public string modeName;        // "캐주얼", "표준", "하드"

# &#x20;   public int frameCount;         // 3, 5, 10

# &#x20;   public int pinCount = 10;

# &#x20;   public float ballSpeed;        // 좌우 이동 속도

# &#x20;   public float powerGaugeSpeed;  // 게이지 변동 속도

# }

# ```

# 

# \### 7.2 점수 계산기 (N에 독립적)

# 

# 전통 점수 계산법에서 N에 의존하는 부분은 \*\*마지막 프레임 판정 한 줄\*\*뿐이다. 따라서 동일 로직으로 모든 모드를 처리한다.

# 

# ```csharp

# public class Frame

# {

# &#x20;   public int firstRoll = -1;

# &#x20;   public int secondRoll = -1;

# &#x20;   public int thirdRoll = -1;       // 마지막 프레임 전용

# &#x20;   public int? confirmedScore;      // null = 보너스 대기 중

# 

# &#x20;   public bool IsStrike() => firstRoll == 10;

# &#x20;   public bool IsSpare()  => firstRoll + secondRoll == 10 \&\& firstRoll < 10;

# }

# 

# public class ScoreCalculator

# {

# &#x20;   public int? CalculateFrameScore(List<Frame> frames, int targetIndex, int frameCount)

# &#x20;   {

# &#x20;       bool isLast = (targetIndex == frameCount - 1);

# &#x20;       // 일반 프레임: 다음 1\~2회 투구 조회 후 보너스 합산

# &#x20;       // 마지막 프레임: 최대 3구까지 단순 합산

# &#x20;       // ...

# &#x20;   }

# }

# ```

# 

# \### 7.3 게임 플로우 관리

# 

# `GameManager`는 `BowlingRuleConfig`를 주입받아 동작하며, 프레임 수에 무관하게 동일한 코드로 진행한다.

# 

# ```csharp

# void OnThrowComplete()

# {

# &#x20;   if (IsFrameComplete())

# &#x20;   {

# &#x20;       if (currentFrame >= ruleConfig.frameCount - 1)

# &#x20;           TransitionTo(State.GameOver);

# &#x20;       else

# &#x20;       {

# &#x20;           currentFrame++;

# &#x20;           TransitionTo(State.AimingPosition);

# &#x20;       }

# &#x20;   }

# }

# ```

# 

# \### 7.4 입력 시스템

# 

# \- 좌우 이동: `Mathf.PingPong(Time.time \* speed, laneWidth)`

# \- 세기 게이지: `Mathf.PingPong(Time.time \* gaugeSpeed, 1f)` → `force = minForce + (maxForce - minForce) \* value`

# \- 스페이스바 한 번으로 위치 확정, 다시 한 번으로 세기 확정 및 투구

# 

# \### 7.5 핀 쓰러짐 판정

# 

# 각 핀의 `transform.up`과 `Vector3.up`의 각도가 임계값(예: 45도) 이상이면 쓰러진 것으로 간주한다. 임계값은 튜닝 대상이다.

# 

# ```csharp

# public bool IsFallen() => Vector3.Angle(transform.up, Vector3.up) > fallThreshold;

# ```

# 

# \### 7.6 데이터 저장 (JSON)

# 

# ```csharp

# \[Serializable]

# public class GameRecord

# {

# &#x20;   public string modeName;

# &#x20;   public int frameCount;

# &#x20;   public int score;

# &#x20;   public string playedAt;     // ISO 8601

# }

# 

# \[Serializable]

# public class SaveData

# {

# &#x20;   public List<GameRecord> highScores;       // 모드별 최고 점수

# &#x20;   public string selectedBallSkin;

# &#x20;   public string selectedCharacterSkin;

# }

# ```

# 

# 저장 위치: `Application.persistentDataPath/save.json`

# 

# \---

# 

# \## 8. 개발 로드맵

# 

# \### Phase 1. 기획 및 셋업 (1주)

# \- GDD 작성, 와이어프레임 확정

# \- Unity 6 프로젝트 생성 (URP 템플릿)

# \- Git 저장소 초기화, 폴더 구조 정리

# \- Input System 패키지 설치

# 

# \### Phase 2. 씬 및 물리 베이스 (1\~2주)

# \- 레인, 거터, 핀 10개 배치 및 프리팹화

# \- 공/핀 Rigidbody 및 PhysicsMaterial 튜닝

# \- 카메라 위치 고정 및 시야각 결정

# 

# \### Phase 3. 입력 시스템 (1주)

# \- 좌우 왕복 위치 지정 로직

# \- 세기 게이지 UI 및 확정 로직

# \- 상태 머신 초기 구현

# 

# \### Phase 4. 점수 계산 시스템 (1\~2주, 핵심)

# \- `Frame`, `ScoreCalculator` 구현

# \- \*\*유닛 테스트 작성\*\* (Section 9 참조)

# \- 마지막 프레임 별도 처리 검증

# 

# \### Phase 5. 게임 플로우 관리 (1주)

# \- `GameManager` 및 상태 전이 구현

# \- `BowlingRuleConfig` 주입 구조 완성

# \- 모드 전환 동작 확인 (3/5/10 모두 동작 검증)

# 

# \### Phase 6. UI/UX (1주)

# \- 점수판 (프레임 수에 따라 동적 생성)

# \- 메인 메뉴, 모드 선택, 결과 화면

# \- 튜토리얼 화면

# 

# \### Phase 7. 폴리싱 (1주)

# \- 카툰 코믹 효과음/BGM 적용

# \- 파티클, 화면 흔들림 등 피드백

# \- 캐릭터/공 스킨 선택 시스템

# 

# \### Phase 8. 저장 시스템 (0.5주)

# \- JSON 저장/로드 구현

# \- 최고 점수 표시

# 

# \### Phase 9. 테스트 및 빌드 (1주)

# \- 초등 저학년 대상 플레이 테스트

# \- 난이도 튜닝 (공 속도, 게이지 속도)

# \- Windows Standalone 빌드

# 

# \---

# 

# \## 9. 점수 계산기 테스트 케이스

# 

# 유닛 테스트로 반드시 검증할 케이스:

# 

# | 케이스 | 입력 | 기대 결과 |

# |---|---|---|

# | 첨부 문서 예시 (10프레임) | X, X, 8/, 6/, 5미스, ... | 첫 5프레임 누적: 28+48+64+74+79 |

# | 올 거터 | 0,0 × N | 0점 |

# | 올 스트라이크 (N=3) | X, X, X+X+X | 90점 |

# | 올 스트라이크 (N=5) | X × 5 + X+X | 150점 |

# | 올 스트라이크 (N=10) | X × 10 + X+X | 300점 |

# | 올 스페어 + 마지막 5 | 5/ × (N-1) + 5/5 | 모드별 검증 |

# | 마지막 프레임 스페어 +5 | (N-1프레임 0,0) + 5/5 | 15점 |

# | 마지막 프레임 스트라이크 +5+3 | (N-1프레임 0,0) + X+5+3 | 18점 |

# 

# \---

# 

# \## 10. MVP 범위

# 

# \### 포함

# \- \[x] 3/5/10 프레임 모드 선택

# \- \[x] 위치/세기 2단계 입력

# \- \[x] 전통 점수 계산

# \- \[x] 점수판 UI

# \- \[x] 튜토리얼 화면

# \- \[x] 캐릭터/공 스킨 선택

# \- \[x] 최고 점수 저장 (JSON)

# \- \[x] 효과음 / BGM

# 

# \### 제외 (확장 후보)

# \- 멀티플레이

# \- 온라인 랭킹

# \- 모바일/WebGL 빌드

# \- 트릭샷, 특수 핀 등 비전통 모드

# 

# \---

# 

# \## 11. 확장 계획

# 

# \- \*\*플랫폼 확장\*\*: WebGL 빌드 검토 (물리 성능 사전 테스트 필요 — 추측이지만 핀 10개 정도는 무리 없을 가능성)

# \- \*\*모드 확장\*\*: 시간 제한 모드, 도전 과제 모드

# \- \*\*콘텐츠 확장\*\*: 캐릭터/공 스킨 추가, 시즌별 테마 레인

# \- \*\*접근성\*\*: 색약 모드, 키 리바인딩

# 

# \---

# 

# \## 12. 참고 사항

# 

# \- 본 프로젝트는 학습 및 포트폴리오 목적으로 진행된다.

# \- 사용 에셋의 라이선스는 별도 관리한다 (Unity Asset Store 또는 직접 제작).

# \- 점수 계산 규칙의 원문 출처는 `docs/bowling\_rules.txt` 참조.

# 

# \---

# 

# \## 13. 향후 확정 필요 항목

# 

# \- \[ ] 프로젝트 정식 명칭

# \- \[ ] 개발 기간 및 마일스톤

# \- \[ ] 캐릭터/공 스킨 종류 및 개수

# \- \[ ] 튜토리얼 형식 (정적 이미지 / 인터랙티브)

# \- \[ ] 사용 에셋 출처 및 라이선스

