# 볼링공 조준 시스템

> 기준일: 2026-11-14 · 대상: `Assets/Scripts/Gameplay/BallAimer.cs`, `Assets/Scripts/UI/PowerGaugeUI.cs`, `Assets/Scripts/UI/SpinGaugeUI.cs`, `Assets/Scripts/Gameplay/BowlingBall.cs`, `Assets/Scripts/Gameplay/AimGuide.cs`
>
> 이 문서는 **현재 구현된 동작**을 정리한 것이다. 설계 의도·로드맵은 `README.md`, 컴포넌트 계약은 `AI_PROMPT_REFERENCE.md` 를 참조.

---

## 1. 한눈에 보기

조준은 **버튼 하나를 타이밍 맞춰 누르는 것**이 전부다. 골프 게임의 3-click 시스템과 같은 구조로, 각 단계마다 값이 자동으로 왕복하고 플레이어는 원하는 순간에 확정한다.

```
Ready
  └─ 확정 ─▶ AimingPosition  (1단계: 좌우 위치)
                 └─ 확정 ─▶ AimingPower  (2단계: 세기)
                                ├─ 방식 A ───────────────────▶ Rolling (직구 발사)
                                └─ 방식 B ─▶ AimingSpin  (3단계: 회전)
                                                        └─ 확정 ─▶ Rolling (커브 발사)
```

**단계 구성은 설정 → 접근성 → "조작 방식"(`SaveData.controlScheme`) 이 정한다.**

| 방식 | 입력 | 구성 |
|---|---|---|
| **A** (기본) | 2번 | 위치 → 세기 |
| **B** | 3번 | 위치 → 세기 → 커브 |
| **C** | 1번 | 방향만 (§4-3) |

값·판정은 `ControlScheme` 이 단일 출처이며, **게임 시작 전에만 선택 가능**하다 — `BallAimer.Start` 가 1회만 읽는다. 투구 도중 단계 구성이 바뀌면 상태 머신이 중간 상태에 갇힌다.

각 단계의 확정 입력은 모두 동일한 **`Confirm` 액션 하나**다.

| 바인딩 | 기본값 | 인덱스 |
|---|---|---|
| 키보드 | `Space` | `InputController.KeyboardBindingIndex` = 0 |
| 게임패드 | `buttonSouth` (Xbox A / PS Cross) | `InputController.GamepadBindingIndex` = 1 |

설정 → 조작 탭에서 리바인딩 가능하며, 결과는 `SaveData.inputOverridesJson` 에 저장된다.

---

## 2. 1단계 — 좌우 위치 (`AimingPosition`)

담당: **`BallAimer`** (공과 같은 GameObject 에 부착)

공이 레인 위를 좌우로 왕복한다. 원하는 지점에서 확정하면 그 x 좌표가 발사 위치가 된다.

```csharp
pingPongTime += Time.deltaTime;
float x = Mathf.PingPong(pingPongTime * oscSpeed, laneHalfWidth * 2f) - laneHalfWidth;
transform.position = new Vector3(x, spawnY, spawnZ);
```

| 항목 | 값 | 비고 |
|---|---|---|
| `laneHalfWidth` | **0.516** | 왕복 범위 ±0.516 (레인 ×1.2 고정 반영) |
| `oscSpeed` | 1.2 | 초당 이동 거리(유닛) |
| 한 왕복 주기 | **약 1.72초** | `2 × (laneHalfWidth × 2) ÷ oscSpeed` |
| y / z | `BallSpawnPoint` 값 차용 | 미발견 시 z 는 `ballStartZ` = 0.5 폴백 |

**y·z 를 스폰 포인트에서 가져오는 이유** — 공의 현재 transform 을 쓰면 직전 투구의 interpolation 잔여값이 섞여 높이가 미세하게 흔들린다. 매 프레임 canonical 값으로 덮어써 그 경로를 차단한다.

**레인이 넓어지면 조준이 쉬워진다.** `oscSpeed` 는 "초당 유닛" 이라 범위만 넓어지므로, 폭 ×1.5 에서는 한 왕복이 약 2.15초로 느려진다. 의도된 난이도 완화다.

확정 시 `ConfirmedPosition` 에 현재 위치를 저장하고 `AimingPower` 로 전이한다.

---

## 3. 2단계 — 세기 (`AimingPower`)

담당: **`PowerGaugeUI`** (화면 하단 세로 화살표 게이지)

게이지가 0% ↔ 100% 를 왕복한다. 확정한 값이 발사 힘이 된다.

```csharp
currentNormalized = Mathf.PingPong(pingPongTime * gaugeSpeed, 1f);
```

| 항목 | 값 |
|---|---|
| `gaugeSpeed` | 0.75 |
| 한 왕복 주기 | **약 2.67초** (`2 ÷ gaugeSpeed`) |
| 화살표 길이 | 20px ↔ 160px (`minHeight` ↔ `maxHeight`) |
| 표시 | `NN%` 텍스트 + 색상 |

**색상 구간** — 값의 크기를 숫자 없이도 알 수 있게 한다.

| 구간 | 색 |
|---|---|
| 0 ~ 40% | 초록 `#00C853` |
| 41 ~ 70% | 노랑 `#FFD700` |
| 71 ~ 100% | 빨강 `#FF1744` |

확정 시 `ConfirmedNormalized` 저장 후, **조작 방식에 따라 분기**한다.

```csharp
stateManager.ChangeState(spinStageEnabled ? GameState.AimingSpin : GameState.Rolling);
```

`spinStageEnabled` 는 `AimingPower` **진입 시점에 1회만** `ControlScheme.UsesSpinStage(SaveData.controlScheme)` 를 읽어 캐싱한다. 매 프레임 파일을 읽지 않기 위한 정책이며, 설정을 바꿔도 **다음 세기 결정 단계부터** 반영된다는 뜻이다.

---

## 4. 3단계 — 회전 / 커브 (`AimingSpin`) — 선택 단계

담당: **`SpinGaugeUI`** (가로 트랙 + 마커). 세기 게이지와 같은 패턴이고 축만 가로다.

마커가 좌 ↔ 우를 왕복한다. **중앙 기준 부호가 커브 방향, 중앙에서 떨어진 거리가 커브 양**이다.

```csharp
currentSigned = Mathf.PingPong(pingPongTime * gaugeSpeed, 2f) - 1f;   // -1 ~ +1
```

| 항목 | 값 |
|---|---|
| `gaugeSpeed` | 0.75 |
| 한 왕복 주기 | **약 5.33초** (`4 ÷ gaugeSpeed`) |
| 트랙 절반 폭 | 300px (`trackHalfWidth`) |
| 직구 구간 | ±0.1 (`straightZone`) — 게이지 전체 폭의 중앙 10% |
| 커브 강조 지수 | 2.0 (`curveExponent`) |

### 4-1. 게이지 값 → 실제 회전값 변환 (`ShapeSpin`)

마커 위치를 그대로 쓰지 않고 두 번 가공한다.

```csharp
float a = Mathf.Abs(raw);
if (a <= straightZone) return 0f;                       // ① 중앙 데드존 → 완전 직구
float m = (a - straightZone) / (1f - straightZone);     // ② [0.1, 1] → [0, 1] 재매핑
m = Mathf.Pow(m, curveExponent);                        // ③ 끝으로 갈수록 급격히 강해짐
return Mathf.Sign(raw) * m;
```

**① 데드존** — 중앙 근처에서 확정하면 미세한 커브가 아니라 정확히 직구가 된다. "똑바로 던지고 싶었는데 살짝 휘는" 상황을 없앤다.

**③ 지수 강조** — 중앙 부근은 완만하고 끝으로 갈수록 급격해진다. 약한 커브를 조준하기 쉬워지고, 강한 커브에는 타이밍 정확도를 요구한다.

변환 예시:

| 마커 위치(raw) | 적용 회전값 | 체감 |
|---|---|---|
| 0.05 | 0.000 | 직구 |
| 0.10 | 0.000 | 직구 (데드존 경계) |
| 0.30 | 0.049 | 아주 약한 커브 |
| 0.50 | 0.198 | 약한 커브 |
| 0.70 | 0.444 | 중간 커브 |
| 0.90 | 0.790 | 강한 커브 |
| 1.00 | 1.000 | 최대 커브 |

> ⚠️ **표시값과 적용값이 다르다.** 라벨(`FormatSpin`)은 **마커의 raw 위치**를 퍼센트로 보여준다. 즉 "R 50%" 로 표시돼도 실제 적용되는 회전값은 0.198 이다. 현재는 의도적으로 "게이지 위치"를 보여주는 셈이지만, 플레이어 입장에서는 오해의 소지가 있다. 바꾸려면 `FormatSpin` 이 `ShapeSpin(signed)` 를 쓰도록 하면 된다.

**색상** — 중앙(직구) 흰색, 좌측으로 갈수록 `#00B0FF`, 우측으로 갈수록 `#FFC400` 로 보간. 라벨은 `직구` / `L nn%` / `R nn%`.

### 4-2. 2단계 모드에서의 직구 보장

방식 A 에서는 이 단계를 건너뛰므로, 아무 조치가 없으면 `BowlingBall` 이 **직전 투구의 `ConfirmedSpin` 을 그대로 읽어** 의도치 않은 커브가 나간다.

이를 막기 위해 `SpinGaugeUI` 가 **`AimingPosition` 진입마다 `ConfirmedSpin = 0` 으로 리셋**한다. 직구 보장의 단일 지점이며, 다른 곳에 중복으로 넣지 말 것.

---

## 4-3. 조작 방식 C — 방향만 (1번 입력)

`SaveData.controlScheme` 이 C 면 1단계가 **방향 결정**으로 바뀌고, 세기·커브 단계는 아예 없다. 담당은 그대로 `BallAimer`.

공은 레인 중앙(x=0)에 고정되고, **조준 각도만 공을 중심으로 호를 그리며 왕복**한다.

```csharp
float sweep = Mathf.PingPong(pingPongTime * arcSweepSpeed, 2f) - 1f;   // -1 ~ +1
currentAimAngle = sweep * arcHalfAngleDegrees;
transform.position = new Vector3(0f, y, z);
```

확정 시 `AimingPower` 를 건너뛰고 바로 `Rolling` 으로 전이하며, 세기는 `fixedPower` 고정값을 쓴다 (`BowlingBall.ExecuteLaunch` 가 `BallAimer.UsesFixedPower` 로 분기). 커브는 적용하지 않는다 — 발사 후에는 관성으로만 굴러간다.

| 항목 | 값 | 비고 |
|---|---|---|
| `arcHalfAngleDegrees` | **8.4°** | 레인 가장자리 약 4.4°, 거터 약 4.7° (레인 ×1.2 고정 반영) |
| `arcSweepSpeed` | 1.6 | 한 왕복 2.5초 (`4 / 값`) |
| `fixedPower` | 0.65 | 힘 14.5 (`minForce`~`maxForce` 보간) |

**예상 경로 가이드가 강제로 켜진다.** 조준선이 보이지 않으면 어느 방향으로 굴릴지 알 방법이 없어 방식 자체가 성립하지 않는다.

**조준 화살표 (`AimArrow`)** — 공 뒤쪽 호 위에 블록 화살표를 그려 조준 방향을 표시한다. 런타임 생성 메시라 아트 에셋이 필요 없다.

| 항목 | 값 |
|---|---|
| `orbitRadius` | 0.5 (공 → 화살표 꼬리) |
| `arrowY` | 0.055 (레인 윗면 0.05 바로 위) |
| 머리 폭 | 0.27 (레인 폭의 약 27%) |

⚠ 메시 오브젝트는 **부모 없이 씬 루트**에 만든다. `BowlingBall` 의 `localScale` 이 10 이라 자식으로 붙이면 10배로 커진다.

> **레인 높이 주의**: 레인 윗면은 y=0.05 다. 레인 위에 무언가를 그릴 때 이보다 낮은 y 를 쓰면 묻혀서 보이지 않는다. `AimGuide.lineY` 가 0.03 이라 2026-07-09 ~ 2026-11-14 동안 예상 경로선이 줄곧 안 보였다.

---

## 5. 발사 (`Rolling`)

담당: **`BowlingBall.ExecuteLaunch` → `Launch`**

```csharp
transform.position = startPos;
Physics.SyncTransforms();          // ① 조준 중 transform 직접 대입분을 물리에 반영
rb.isKinematic = false;            // ② 물리 인계 — 여기서만 dynamic 복귀
rb.linearVelocity  = Vector3.zero; // ③ 잔여 운동 상태 제거
rb.angularVelocity = Vector3.zero;
rb.AddForce(dir * Mathf.Lerp(minForce, maxForce, power), ForceMode.Impulse);
```

`dir` 은 `BallAimer.ConfirmedDirection` 이다. A/B 에서는 `Vector3.forward` 가 들어와 동작이 같고, C 에서만 호 각도에 따른 비스듬한 방향이 들어온다. `power` 도 C 에서는 `fixedPower` 를 쓴다.

| 항목 | 값 |
|---|---|
| `minForce` | 8 (세기 0%) |
| `maxForce` | 18 (세기 100%) |

### 5-1. 공은 발사 전까지 얼어 있다

**`Rolling` 이전의 모든 상태에서 공은 `isKinematic = true`** 다. `ResetBall` 은 리셋 후 kinematic 상태로 끝내고, dynamic 복귀는 `Launch` 한 곳에서만 일어난다.

이 규칙이 없던 시절에는 위치 확정 순간 공이 dynamic 으로 돌아가, 세기·회전 게이지를 재는 수 초 동안 중력과 접촉 해소로 **공이 스스로 움직이는** 버그가 있었다 (2026-11-08 수정, `README.md` §14-21).

`Physics.SyncTransforms()` 가 필요한 이유도 여기에 있다. 조준 중 `BallAimer` 는 `transform.position` 에 직접 대입하는데, Unity 6 의 `autoSyncTransforms = false` 환경에서는 그대로 dynamic 으로 복귀하면 **stale 한 `Rigidbody.position`** 에서 시뮬레이션이 시작된다.

---

## 6. 커브 물리

공이 굴러가는 동안 매 `FixedUpdate` 마다 측면 힘을 가한다.

```csharp
if (CurrentState != Rolling) return;
if (rb.isKinematic) return;
if (Mathf.Approximately(confirmedSpin, 0f)) return;

float forwardSpeed = rb.linearVelocity.z;
if (forwardSpeed < curveMinForwardSpeed) return;

rb.AddForce(Vector3.right * confirmedSpin * curveStrength * forwardSpeed, ForceMode.Force);
```

| 항목 | 값 | 의미 |
|---|---|---|
| `curveStrength` | 0.22 | 커브 세기 계수 |
| `curveMinForwardSpeed` | 0.2 | 이 속도 미만이면 커브 중단 |

**측면 힘이 전진 속도에 비례**하는 것이 핵심이다. 빠른 공일수록 굴절이 균일하게 유지되고, 공이 느려지면 커브가 자연히 0 으로 수렴한다. 거의 멈춘 공이 옆으로 미끄러지는 현상은 `curveMinForwardSpeed` 컷오프가 막는다.

레인이 짧기 때문에(스폰 → 핀 약 7.8 유닛) `curveStrength` 를 키우면 최대 커브가 거터로 직행한다. 조정은 Play 모드에서 확인하며 할 것.

---

## 7. 예상 경로 가이드 (`AimGuide`) — 선택 기능

설정 → 접근성 → "예상 경로 표시" 를 켜면 조준 중 공의 예상 경로가 레인 위에 선으로 표시된다. 기본값은 OFF.

| 상태 | 표시 |
|---|---|
| `AimingPosition` | 왕복하는 공을 따라 **직선** |
| `AimingPower` | 확정 위치에서 **직선** (2단계 모드는 여기까지) |
| `AimingSpin` | 스핀 마커의 **실시간 값** + 확정 세기로 **곡선** 미리보기 |

예측선이 거터 영역에 닿으면 그 지점에서 잘라 거터행을 시각화한다.

| 항목 | 값 |
|---|---|
| 샘플 수 | 24 |
| 끝 지점 z | 8.3 (`guideEndZ`) |
| 선 높이 y | 0.03 |
| 선 폭 | 0.08 → 0.03 (원근감) |

곡선 계산은 `BowlingBall.PredictLateralOffset` 이 담당한다. **실제 커브 물리와 상수를 공유하는 단일 출처**이며, 마찰을 무시한 포물선 근사다.

```
v = Lerp(minForce, maxForce, power) / mass     // Impulse → Δv
a = spin × curveStrength × v / mass            // 측면 가속도
t = dz / v                                     // 등속 전진 가정
x = ½ · a · t²
```

> 잔여 작업: `lineMaterial` 이 미배선이라 런타임 `Sprites/Default` 폴백으로 동작한다. 빌드 셰이더 스트리핑 시 선이 안 보일 수 있으므로 `Assets/Materials/` 에 URP/Unlit 머티리얼을 만들어 할당하는 것이 안전하다.

---

## 8. 접근성 옵션 3종

조준에 영향을 주는 설정은 **조작 방식**(§1, 게임 시작 전에만 변경 가능) 과 아래 둘이다. 아래 둘은 기본 OFF 이고 설정 → 접근성 탭에 있다.

| 설정 | `SaveData` 필드 | 효과 | 반영 시점 |
|---|---|---|---|
| 예상 경로 표시 | `aimGuideEnabled` | 예측선 ON/OFF | 다음 **조준 사이클** 시작 시 |
| 조준 음향 가이드 | `aimingAudioGuide` | 공이 좌/우 끝에 닿을 때 비프음 | 다음 **조준 사이클** 시작 시 |

**음향 가이드 동작** — `|x|` 가 `laneHalfWidth − 0.02` 이상이면 1회 비프를 재생하고, 좌측은 팬 −1 / 우측은 팬 +1 로 스테레오 방향을 준다. 같은 끝에 머무는 동안 중복 발화하지 않도록 latch 를 걸고, `|x|` 가 임계의 70% 아래로 내려가면 풀린다 (임계 근처 진동에서 깜빡 발화 방지).

세 설정 모두 **매 프레임 파일을 읽지 않기 위해 상태 진입 시점에 1회 캐싱**한다. 그래서 게임 도중 설정을 바꿔도 즉시가 아니라 다음 사이클부터 적용된다.

---

## 9. 레인 크기 — ×1.2 고정 (2026-11-18)

레인 폭 프리셋(A/B/C)은 **삭제**됐다. 레인·핀·공이 모두 **×1.2 로 씬에 고정**되어 있고, 설정에서 바꿀 수 없다.

| | 값 | 비고 |
|---|---|---|
| 레인 바닥 폭 (한쪽) | 0.600 | 거터 안쪽 경계 |
| 거터 임계 `gutterX` | **0.6396** | 레인 끝보다 약 0.04 바깥 |
| 조준 왕복 `laneHalfWidth` | **0.516** | |
| 공 지름 | **0.239** | 질량은 9 lb 유지 |
| 핀 두께 / 같은 줄 간격 | 0.128 / 0.367 | 빈 틈 0.240 ≈ 공 지름 |
| 투구 거리 | **7.767 고정** | 스케일 대상이 아니다 |

폭에 연동된 상수는 셋이고 **항상 함께 움직여야 한다**. 하나라도 빠지면 조준 왕복이 레인을 벗어나거나, 거터 판정이 실제 거터와 어긋나거나, 예상 경로선이 엉뚱한 곳에서 잘린다.

- `BallAimer.laneHalfWidth` (0.516) — 조준 왕복 범위
- `BowlingBall.gutterX` (0.6396) — 거터 진입 판정
- `AimGuide.gutterX` (0.6396) — 예측선 절단 위치

여기에 `BallAimer.arcHalfAngleDegrees` (8.4°) 도 같이 본다.

---

## 10. 파라미터 한눈에

| 단계 | 파라미터 | 기본값 | 위치 |
|---|---|---|---|
| 위치 | `laneHalfWidth` | **0.516** | `BallAimer` |
| 위치 | `oscSpeed` | 1.2 | `BallAimer` |
| 위치 | `ballStartZ` (폴백) | 0.5 | `BallAimer` |
| 세기 | `gaugeSpeed` | 0.75 | `PowerGaugeUI` |
| 세기 | `minHeight` / `maxHeight` | 20 / 160 px | `PowerGaugeUI` |
| 회전 | `gaugeSpeed` | 0.75 | `SpinGaugeUI` |
| 회전 | `trackHalfWidth` | 300 px | `SpinGaugeUI` |
| 회전 | `straightZone` | 0.1 | `SpinGaugeUI` |
| 회전 | `curveExponent` | 2.0 | `SpinGaugeUI` |
| 발사 | `minForce` / `maxForce` | 8 / 18 | `BowlingBall` |
| 커브 | `curveStrength` | 0.22 | `BowlingBall` |
| 커브 | `curveMinForwardSpeed` | 0.2 | `BowlingBall` |
| 거터 | `gutterX` | **0.6396** | `BowlingBall`, `AimGuide` |
| 가이드 | `samples` / `guideEndZ` | 24 / 8.3 | `AimGuide` |

---

## 11. 건드릴 때 주의할 것

**① 같은 프레임 연쇄 확정 (`enteredFrame` 가드)**

`BallAimer`, `PowerGaugeUI`, `SpinGaugeUI` 모두 상태 진입 시 `enteredFrame = Time.frameCount` 를 기록하고, 확정 핸들러에서 `if (Time.frameCount == enteredFrame) return;` 로 막는다. 하나의 `Confirm` 이벤트가 `AimingPosition → AimingPower → AimingSpin → Rolling` 까지 한 프레임에 연쇄 전이하는 것을 방지한다. **제거하면 버튼 한 번에 공이 날아간다.**

> 부작용: Unity 에디터가 백그라운드이면 플레이어 루프가 멈춰 `Time.frameCount` 가 증가하지 않는다. 이 가드가 영구히 참이 되어 입력이 전혀 먹지 않는 것처럼 보인다. 에디터 자동화 테스트 시 `Application.runInBackground = true` 를 먼저 켤 것.

**② 직구 보장의 단일 지점**

`SpinGaugeUI` 의 `AimingPosition` 진입 시 `ConfirmedSpin = 0` 리셋. 2단계 모드에서 stale 회전값이 새는 것을 막는 유일한 장치다.

**③ 물리 소유권**

`Rolling` 이전 전 상태에서 공은 kinematic, dynamic 복귀는 `BowlingBall.Launch` 한 곳에서만. `ResetBall` / `ForceResetCoroutine` / `BallAimer` 어디에도 `isKinematic = false` 를 다시 넣지 말 것.

**④ 커브 상수의 단일 출처**

`curveStrength` / `curveMinForwardSpeed` 는 실제 물리(`FixedUpdate`)와 예측선(`PredictLateralOffset`)이 공유한다. 한쪽만 바꾸면 가이드가 거짓말을 한다.

---

## 12. 관련 파일

| 파일 | 역할 |
|---|---|
| `Assets/Scripts/Gameplay/BallAimer.cs` | 1단계 위치 조준 + 음향 가이드 |
| `Assets/Scripts/UI/PowerGaugeUI.cs` | 2단계 세기 게이지 + 단계 분기 |
| `Assets/Scripts/UI/SpinGaugeUI.cs` | 3단계 회전 게이지 + `ShapeSpin` |
| `Assets/Scripts/Gameplay/BowlingBall.cs` | 발사, 커브 물리, 거터 판정, 경로 예측 |
| `Assets/Scripts/Gameplay/AimGuide.cs` | 예상 경로 LineRenderer |
| `Assets/Scripts/Gameplay/InputController.cs` | `Confirm` 액션 + 리바인딩 |
| `Assets/Scripts/Core/GameStateManager.cs` | 상태 전이 |

---

*조준 시스템 스냅샷 2026-11-14 — 변경 시 본 문서와 `README.md` §14 를 함께 갱신할 것.*
