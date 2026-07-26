# 버닝 1막 장면 설계

## 자료 구분

- 원본 콘티와 이야기 문서의 이전 위치: `C:\Users\EunbinLee\Desktop\버닝` (현재 저장소에 미포함)
- Florence 분석: 감정 전달 방식과 짧은 손 조작 참고
- 본 문서: 버닝 1막의 실제 장면 순서와 구현 기준
- Florence 연출 분석표에 원본 콘티를 합치지 않음. 두 문서의 역할 분리

## 1막 흐름

1. 종수가 마트에 상품을 운반
2. 해미와 재회하고 경품 추첨 진행
3. 해미를 기억하는 퍼즐과 초점 맞추기
4. 담배, 술, 걷기 동작으로 두 사람의 거리 축소
5. 해미의 집에서 1막 종료

## C01

- 종수의 발과 걸음을 낮은 시점에서 보여주는 시작 장면
- 현재 이미지는 걷는 무게와 반복을 전달하는 용도
- 추가 예정: 발소리, 상자 마찰음, 옷감 소리, 약한 화면 흔들림, 바닥 먼지
- 소리는 발이 바닥에 닿는 순간에 맞추고, 모든 걸음의 크기를 같게 하지 않음

## C02

### 장면 순서

1. 종수가 상자를 든 모습을 세로 화면에 잡음
2. 화면이 짧게 이동하며 창고 쪽으로 연결
3. 배경 밝기가 약 38% 어두워짐
4. 가운데 빈 선반과 상자 7개가 나타남
5. 상자 7개를 모두 놓으면 게임 화면이 사라짐
6. 화면이 종수에게 돌아옴
7. 종수 이미지가 카메라를 보는 모습으로 바뀜

### 상자 배치 결정

- 다른 선반 칸까지 사용하지 않음
- 가운데 빈 선반 한 칸 안에서 7개 배치
- 권장 배치: 아래 3개, 중간 2개, 위 2개
- 원본 비율을 유지하되 화면에서 잡기 어려운 상자는 15~25% 확대
- 상자 사이 간격을 조금 남겨 완벽한 테트리스보다 불안정한 노동의 느낌 유지

### 구현 파일

- 바로 재생할 씬: `Assets/Game/Burning/C02/Scenes/Burning_C02_Playable.unity`
- 장면 진행: `Assets/Game/Burning/C02/Scripts/C02SequenceController.cs`
- 상자 놓기: `Assets/Game/Burning/C02/Scripts/C02BoxStackGame.cs`
- 상자 드래그: `Assets/Game/Burning/C02/Scripts/C02DraggableBox.cs`
- 실행용 이미지: `Assets/Game/Burning/C02/Art/`

### 시작과 재생

- 프로젝트가 코드를 읽으면 C02 플레이 씬을 한 번 자동 생성
- 자동 생성이 안 되거나 다시 만들 때 `Tools > Burning > Build C02 Playable Scene` 실행
- `Burning_C02_Playable` 씬에서 Play 버튼 클릭
- 시작 후 0.8초 동안 종수 표시
- 이후 창고 화면과 상자 게임 자동 시작
- 상자 7개를 놓으면 종수 화면으로 돌아가 카메라를 보는 모습 표시

## C02 Unity 연결

### Canvas 구성

```text
Canvas
├─ SceneViewport
│  └─ SceneImage
├─ Darkness
└─ StackGame
   ├─ Shelf
   ├─ Slots
   │  ├─ Slot01 ... Slot07
   ├─ Boxes
   │  ├─ Box01 ... Box07
   └─ DragLayer
```

### 장면 이미지

- `SceneImage`에 `C02_Jongsu.png` 사용
- `C02SequenceController.sceneImage`와 `sceneRect`에 `SceneImage` 연결
- `jongsuSprite`에 `C02_Jongsu.png` 연결
- `jongsuLookSprite`에 `C02_JongsuLook.png` 연결
- 종수 구도에서 `jongsuFramePosition` 기록
- 창고 구도로 옮긴 뒤 `warehouseFramePosition` 기록
- 기본 `warehouseFramePosition -720, 0`은 시작값. 실제 Canvas에서 눈으로 조정

### 어두운 화면

- `Darkness`는 화면 전체를 덮는 검정 `Image`와 `CanvasGroup`으로 구성
- Raycast Target 해제
- `darkness`에 해당 `CanvasGroup` 연결

### 상자 게임

- `StackGame`에 `CanvasGroup`, `C02BoxStackGame` 추가
- `Shelf`에 `C02_Shelf.png` 사용
- `Box01`부터 `Box07`까지 각각 `C02DraggableBox` 추가
- `Slots` 아래 빈 `RectTransform` 7개 생성
- 놓을 자리는 가운데 선반 안에 3-2-2 배치
- `boxes`와 `targetSlots`를 아래에서 위 순서로 연결
- `DragLayer`는 `StackGame`의 마지막 자식으로 두고 전체 화면 크기로 설정
- 기본 `snapDistance` 110. 손가락 놓기가 답답하면 130까지 확대
- 상자 비율 보존을 위해 `fitBoxToSlot`은 해제

### 입력

- `EventSystem`에는 `Input System UI Input Module` 사용
- `Standalone Input Module` 사용 금지
- 상자 `Image`의 Raycast Target 활성화

### 장면 진행

- 빈 오브젝트에 `C02SequenceController` 추가
- `stackGameGroup`, `stackGame`, `darkness` 연결
- `playOnStart` 활성화 시 장면 시작 후 자동 진행
- `onWarehouseShown`, `onStackCompleted`, `onJongsuLooksAtCamera`에 효과음이나 다음 장면 연결 가능

## C03

- 해미와 추첨 통이 중심인 장면
- 손을 통 안으로 움직여 빨간 공을 찾는 조작 예정
- C02의 상자 놓기와 다른 감각이 필요하므로 별도 구현
- 빠른 성공보다 공 사이를 헤매는 시간이 인물 사이의 어색함을 담당
