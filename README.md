# D.U.B.B.I.O
<img width="32%" height="auto" alt="Image" src="https://github.com/user-attachments/assets/aac0fab7-6bc1-4632-a464-35356a0ac075" />
<img width="32%" height="auto" alt="image" src="https://github.com/user-attachments/assets/6c047fa2-325e-46de-9572-346706468e75" />
<img width="32%" height="auto" alt="image" src="https://github.com/user-attachments/assets/9f4529a5-df32-4685-9f56-286a094afe1e" />
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

### 1. 미니게임 콘텐츠
<div align="center">
  <img width="75%" height="auto" alt="image" src="https://github.com/user-attachments/assets/6c047fa2-325e-46de-9572-346706468e75" />
  <br/>
  <b><인게임 미니게임 이미지></b>
</div>

- **기능**
  - MiniGame 추상 클래스로 공통 구조를 정의하고, 각 미니게임은 상속을 통해 개별 로직만 구현
  - 클리어 UI, 복구 대기 UI 같은 공통 연출 요소도 별도 컴포넌트로 분리해 여러 미니게임에서 재사용 가능하게 설계
  - 스테이지 난이도와 일차 정보를 주입해, 같은 미니게임도 진행도에 따라 다르게 동작할 수 있도록 구성
 
- **설계**
  - 많은 개수의 미니게임 관리를 위해 공통 구조는 상속 기반 베이스 클래스에 모으고, 각 미니게임은 자신의 규칙만 구현하는 구조로 설계
  - 각 기능을 별도 컴포넌트로 분리함으로써 해당 미니게임에 필요한 요소들만 적용할 수 있도록 설계 

### 2. 추리 퍼즐 로직
<div align="center">
  <img width="75%" height="auto" alt="image" src="https://github.com/user-attachments/assets/9f4529a5-df32-4685-9f56-286a094afe1e" />
  <br/>
  <b><인게임 추리 퍼즐 이미지></b>
</div>

- **기능**
  - 라운드별 정답 데이터와 키워드 조합을 기반으로, 추리에 사용할 단서를 단계적으로 생성하는 Clue 시스템을 구현
  - IF, OR, IF-OR, 참/거짓 속성 단서, 거짓말 단서까지 전략별로 분리해 다양한 형태의 단서가 나오도록 구성
  - 단서 생성은 가중치와 강제 규칙을 적용해 제어할 수 있도록 만들었고, 중복 소모와 후보 고갈도 세션 단위로 관리
  - 생성된 단서는 로그 UI와 추리 보드 UI에 연결하고, 로컬라이징 및 문법 치환까지 지원해 실제 플레이 흐름에서 바로 사용할 수 있게 구현

- **설계**
  - 전체 구조를 CaseManager → RoundClueContext → GenerationSession → ClueBatchGenerator 흐름으로 나누어, 데이터 준비와 단서 생성 책임을 분리
  - 각 단서 전략은 ScriptableObject 기반으로 확장성을 높였고, 가중치·강제 규칙을 조정하는 데이터도 ScriptableObject 기반 제작하여 수정 및 참조가 편하도록 설계
  - 출력 단계에서는 단서 생성 로직과 UI 표시 로직을 분리해, 로그/보드/다국어 처리까지 각각 독립적으로 관리하도록 설계

### 3. 세이브 / 로드 기능
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

- **기능**
  - Task 기반 비동기 파일 입출력을 사용해 저장/불러오기 중 메인 스레드 정지를 최소화하도록 구현
  - .tmp 임시 파일 저장 후 기존 파일 백업, 최종 파일 교체 방식으로 저장
  - 저장 직전 이벤트를 호출해 외부 시스템의 최신 데이터를 세이브 데이터에 반영하도록 구현
  - 플레이 세션 시간을 계산해 누적 플레이 타임도 함께 저장

- **설계**
  - 단순히 파일만 저장하는 구조가 아니라, 게임 진행 데이터 관리와 실제 파일 저장 책임을 한 곳에 모으기 위해 구성
  - 저장 과정에서 프레임이 멈추거나 파일이 손상되는 문제를 줄이기 위해 비동기 IO와 임시 파일 교체 방식을 사용
  - 각 시스템이 직접 세이브 파일을 건드리지 않으면서 최신 상태를 안전하게 기록하기 위해 이벤트 호출 방식 선택.

### 4. 인벤토리 기능
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

- **기능**
  - 아이템 보유 상태를 int 하나의 비트 플래그로 관리
  - 획득, 제거, 보유 등 기능함수에서 비트 연산을 사용하여 처리
  - 전체 아이템 목록(ItemSO)을 기준으로 현재 보유 중인 아이템만 추려서 반환
  - 특정 플래그값을 기준으로 아이템 이름 배열을 만들어 저장 데이터 미리보기나 UI 출력에 활용 가능하도록 구성

- **설계**
  - 아이템 종류가 많지 않고, 각 아이템의 상태도 존재유무 정도로 단순하기 때문에 리스트나 딕셔너리 대신 비트 플래그 방식으로 경량화한 구조
  - 런타임 관리와 저장 구조가 int형 변수 1개로 단순하게 맞추기 좋도록 설계

### 5. 사운드 매니저

- **기능**
  - SoundLibrarySO 단일 라이브러리에서 BGM과 SFX를 key 기반으로 조회하도록 구성
  - SoundEntry에는 category와 clips만 저장하고, 반복 재생·Fade In/Out·3D 옵션은 재생 시 전달되는 BGMPlayOptions, SFXPlayOptions로 제어
  - SoundSFXPlayer에서 AudioSource 풀링을 사용해 2D / 3D 효과음을 공통 구조로 관리
  - Play, PlayAt, PlayAttached API를 통해 일반 효과음, 위치 기반 효과음, 추적형 3D 사운드를 구분해 재생
  - 반복 재생 사운드는 SoundHandle로 관리해 정지, 볼륨 조절, 위치 전환, 추적 대상 변경 가능
  - 사용자 설정 볼륨을 PlayerPrefs에 저장하고 현재 재생 중인 BGM / SFX에 즉시 반영

- **설계**
  - 사운드 추가 시 enum과 코드를 함께 수정해야 하는 구조를 제거하고, 데이터 중심으로 유지보수할 수 있는 구조로 개선
  - 재생 정책을 SO에 고정하지 않고 호출 시점 옵션으로 분리해 같은 사운드를 여러 상황에서 재사용할 수 있도록 구성
  - 3D 사운드를 오브젝트 등록 방식이 아닌 위치/추적 기반 재생 방식으로 단순화해 사용성과 확장성을 높임
  - BGM과 SFX를 내부적으로 분리해 책임을 명확히 하면서도, 외부에서는 SoundManager 하나로 일관되게 접근할 수 있도록 설계

## 4. 트러블슈팅
### 1. 세이브 / 로드 비동기 리팩토링
- 문제 상황: 초기 세이브 / 로드 구조를 구현한 뒤 오프라인 행사에서 테스트를 진행했을 때, **저장 중 화면이 오래 멈춘다**는 피드백을 받았습니다. 실제로 저사양 노트북 환경에서 저장 시 **약 3초 정도 화면이 정지하는 현상**을 확인했고, 이는 플레이 경험을 크게 해치는 문제라고 판단했습니다.

- 해결 방식: 저장 대상 데이터를 다시 점검해 불필요한 데이터를 줄이고 **전체 저장 구조를 경량화**했습니다. 이후 플레이어가 로딩 상태를 명확히 인지할 수 있도록 **로딩 화면을 추가**했으며, 세이브 / 로드 처리 또한 **비동기 방식**으로 리팩토링해 메인 흐름이 멈춰 보이는 문제를 완화했습니다.

- 결과: 세이브 / 로드 소요 시간은 같은 노트북 기준으로 1~2초 수준으로 줄어들었습니다. 또한 순간적인 프레임 정지가 발생하더라도 로딩 화면을 통해 현재 저장 또는 불러오기가 진행 중이라는 점을 즉시 전달할 수 있게 되었고, 플레이어가 체감하는 대기 시간과 불편함 역시 함께 줄일 수 있었습니다.

### 2. 사운드 매니저 리팩토링
- 문제 상황: 이전 사운드 구조는 enum, BGM_SO, SFX_SO, WorldSoundNode를 함께 사용하는 방식이었고, 새로운 사운드를 추가하거나 **수정할 때마다 enum 값 추가**, **SO Entry 등록**, 경우에 따라 씬 내 오브젝트에 **사운드용 컴포넌트 부착까지 필요**했습니다. 이 때문에 **기획자도 현재 코드 구조와 씬 구성 방식을 어느 정도 이해**해야 했고, 프로그래머 역시 단순한 사운드 변경에도 반복적으로 구조를 수정해야 해 **관리가 매우 불편**했습니다.

- 해결 방식: 사운드 시스템을 **기획자는 정해진 폴더에 Audio Clip을 넣고 사용할 이름만 정해 전달**하면 되고, **프로그래머는 그 이름을 코드에서 호출**만 하면 되도록 리팩토링했습니다. 이를 위해 enum 기반 식별 구조를 제거하고, SoundLibrarySO 단일 라이브러리에서 **key 문자열 기준으로 BGM과 SFX를 함께 관리하도록 변경**했습니다. 기존처럼 씬 내 오브젝트에 WorldSoundNode를 붙여 등록하는 방식 대신 Play, PlayAt, PlayAttached, SoundHandle 구조로 **2D/3D/반복 사운드를 일관되게 제어**하도록 개선했습니다.

- 결과: 사운드 추가 시 더 이상 enum 수정이나 별도 노드 컴포넌트 관리가 필요 없어졌고, **기획자는 클립과 이름만 관리하면 되며 프로그래머는 전달받은 key를 코드에 작성해 바로 재생할 수 있는 구조**가 되었습니다. 그 결과 사운드 시스템이 코드와 씬 설정에 강하게 묶인 구조에서 벗어나, **데이터 관리와 재생 책임이 분리된 형태로 개선**되었고, **협업 편의성과 유지보수성이 크게 향상**되었습니다.
