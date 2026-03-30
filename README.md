# D.U.B.B.I.O
<img width="32%" height="auto" alt="Image" src="https://github.com/user-attachments/assets/aac0fab7-6bc1-4632-a464-35356a0ac075" />
<img width="32%" height="auto" alt="Image" src="https://github.com/user-attachments/assets/ae8abbbf-c109-456d-91ce-2045c62854d8" />
<img width="32%" height="auto" alt="Image" src="https://github.com/user-attachments/assets/d355f712-5b9d-4b5f-90d6-d6b487dc85c7" />
<div align="center">
  <b><현재 개발중인 인게임 이미지></b>
</div>

- RIMU GAMES 인디게임 프로젝트 입니다

- 제가 구현한 소스코드 일부분만 제공함을 밝힙니다.

- 개발중인 프로젝트로 소스코드에 지속적인 수정이 있을 예정입니다

## 1. 게임소개
- 소개
  - 플레이어는 D.U.B.B.I.O 가 되어 테러범을 방해, 추적, 최종적으로 테러에 대한 디테일을 파악해내 테러범을 막아내야합니다.
  - 미니게임을 통해 단서를 얻고 단서를 사용하여 범인을 추적하여 최종적으로 테러가 발생할 "장소" 와 "무기"를 찾아내는 것이 목표인 게임입니다.

- 개발 팀: RIMU GAMES
  
- 장르: 포인트앤클릭, 퍼즐
  
- 개발 일자: 2025.08 ~ (진행중)
  
- 개발 환경: C#, Unity

- 데모버전 스토어 링크: https://store.onstove.com/games/103003

## 2. 게임 소개 영상
BEAVER ROCKS 2025 온라인 전시작 소개영상
- https://youtu.be/UBgwWb-_UdQ?si=3gFIWzf-sCateH1Y

## 3. 구현 기능

### 1. 세이브 / 로드 기능
<details open>
  <summary>세이브 기능 구현 코드 일부</summary>

```csharp
string finalJson = JsonUtility.ToJson(saveDataJson, false);

await Task.Run(() =>
{
    string tempPath = saveFilePath + ".tmp";
    string bakPath = saveFilePath + ".bak";

    File.WriteAllText(tempPath, finalJson);

    if (File.Exists(saveFilePath))
    {
        if (File.Exists(bakPath)) File.Delete(bakPath);
        File.Move(saveFilePath, bakPath);
    }

    File.Move(tempPath, saveFilePath);

    if (File.Exists(bakPath)) File.Delete(bakPath);
});
```
</details>

- **구현 방식**
  - Task 기반 비동기 파일 입출력을 사용해 저장/불러오기 중 메인 스레드 정지를 최소화
  - .tmp 임시 파일 저장 후 기존 파일 백업, 최종 파일 교체 방식으로 저장 안정성 확보
  - 저장 직전 이벤트를 호출해 외부 시스템의 최신 데이터를 세이브 데이터에 반영
  - 플레이 세션 시간을 계산해 누적 플레이 타임도 함께 저장

- **구현 이유**
  - 단순히 파일만 저장하는 구조가 아니라, 게임 진행 데이터 관리와 실제 파일 저장 책임을 한 곳에 모으기 위해 구성
  - 저장 과정에서 프레임이 멈추거나 파일이 손상되는 문제를 줄이기 위해 비동기 IO와 임시 파일 교체 방식을 사용
  - 각 시스템이 직접 세이브 파일을 건드리지 않으면서 최신 상태를 안전하게 기록하기 위해 이벤트 호출 방식 선택.

### 2. 인벤토리 기능
<details open>
  <summary>인벤토리 기능 구현 코드 일부</summary>

```csharp
public bool AcquireItem(ItemType type)
{
    int mask = 1 << (int)type;

    if ((itemFlag & mask) != 0) return false;
    itemFlag |= mask;
    return true;
}

public bool RemoveItem(ItemType type)
{
    int mask = 1 << (int)type;

     if ((itemFlag & mask) == 0) return false;
    itemFlag &= ~mask;
    return true;
}
```
</details>

- **구현 방식**
  - 아이템 보유 상태를 int 하나의 비트 플래그로 관리
  - 획득, 제거, 보유 등 기능함수에서 비트 연산을 사용하여 처리
  - 전체 아이템 목록(ItemSO)을 기준으로 현재 보유 중인 아이템만 추려서 반환
  - 특정 플래그값을 기준으로 아이템 이름 배열을 만들어 저장 데이터 미리보기나 UI 출력에 활용 가능하도록 구성

- **구현 이유**
  - 아이템 종류가 많지 않고, 각 아이템의 상태도 존재유무 정도로 단순하기 때문에 리스트나 딕셔너리 대신 비트 플래그 방식으로 경량화한 구조
  - 런타임 관리와 저장 구조를 단순하게 맞추기 좋다는 장점

### 3. 미니게임
<details open>
  <summary>미니게임 기능 구현 코드 일부</summary>
  
```csharp
// MiniGame.cs
protected abstract void PrepareGame();

// PINInput.cs (MiniGame 상속)
protected override void PrepareGame()
{
    StopModeBTimer();
    SetupSlotsForLength_B(pinLength);
    ...
}

public void AddDigitA(int digit)
{
    if (mode != PinMode.A) return;
    if (digit < 0 || digit > 9) return;
    if (inputA.Length >= pinLength) return;

    inputA += (char)('0' + digit);
    RefreshDisplayA();

    if (inputA.Length == pinLength)
    {
        ValidateA();
    }
}
```
</details>

- **구현 방식**
  - MiniGame 추상 클래스로 공통 구조를 정의하고, 각 미니게임은 상속을 통해 개별 로직만 구현
  - 공통 생명주기인 설정, 시작, 정리, 복구, 일시정지/재개 흐름을 베이스 클래스에서 통합 관리
  - MiniGameManager가 현재 스테이지에 맞는 미니게임을 생성·등록·교체·복구하도록 구성
  - 클리어 UI, 복구 대기 UI 같은 공통 연출 요소도 별도 컴포넌트로 분리해 여러 미니게임에서 재사용 가능하게 설계
  - 스테이지 난이도와 일차 정보를 주입해, 같은 미니게임도 진행도에 따라 다르게 동작할 수 있도록 구성
 
- 구현이유
  - 많은 개수의 미니게임 관리를 위해 공통 구조는 상속 기반 베이스 클래스에 모으고, 각 미니게임은 자신의 규칙만 구현하는 구조로 설계
  - 클리어 처리나 UI 연출, 정지/재개 같은 공통 기능도 일관되게 유지할 수 있다는 장점

### 4. 일차별 게임 데이터

- **구현 방식**
  - (level, day) 조합을 기준으로 해당 일차에 사용할 단서 설정과 데이터셋을 조회하도록 구성
  - 기본 단서 규칙과 특정 슬롯 강제 규칙을 분리해 일차별 단서 구성을 제어
  - 용의자, 키워드 같은 실제 플레이 데이터도 별도 데이터셋으로 관리
  - 단서 타입별 별칭 문자열과 카테고리별 표시 스타일도 데이터 에셋으로 분리
  - 로컬라이제이션 키와 UI 색상 정보까지 포함해 콘텐츠와 표현을 함께 관리할 수 있도록 구성

- **구현 이유**
  -  게임 진행이 일차별로 달라지는 구조에서는, 단순히 텍스트만 바꾸는 것이 아니라 단서 규칙, 데이터셋, 표현 방식 전체가 함께 바뀔 수 있기 때문에 이를 전부 데이터 중심 구조로 분리
  -  코드 분기 추가 없이 에디터에서 직접 관리할 수 있어 유지보수와 확장에 유리

### 5. 사운드 매니저

- **구현 방식**
  - BGM과 SFX를 각각 BGM_SO, SFX_SO 라이브러리로 분리해 타입 기반으로 조회
  - enum과 Entry 구조를 사용해 사운드 클립, 반복 여부, Fade In/Out 옵션을 데이터로 관리
  - 2D SFX는 AudioSource 풀링 방식으로 재사용해 반복 생성 비용을 줄이도록 구성
  - 3D 사운드는 WorldSoundNode.cs를 통해 씬 오브젝트 단위로 등록하고, 노드 ID와 그룹 단위로 재생/정지/뮤트 제어 가능하도록 구현
  - 사용자 설정 볼륨을 PlayerPrefs에 저장하고, 변경 시 BGM/2D/3D 사운드에 즉시 반영되도록 설계

- **구현 이유**
  - 개별관리가 힘들것을 예상하여 재생 책임은 중앙 매니저에 모으고, 실제 사운드 데이터는 SO 라이브러리로 분리하는 구조를 선택
  - 코드 수정 없이 데이터만 조정할 수 있고, 볼륨 조절, 페이드 처리, 그룹 뮤트 같은 공통 기능도 일관되게 유지할 수 있다는 장점
  - 2D/3D 사운드를 분리해 UI 효과음과 월드 환경음을 서로 다른 방식으로 안정적으로 관리
