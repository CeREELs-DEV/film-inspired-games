# 게임 개요

## 상태

- 생성일: 2026-07-13
- 프로젝트명: `film-inspired-games`
- Unity: `6000.0.67f1`
- 템플릿: Universal 2D

## 현재 방향

영화에서 출발한 짧은 2D 인터랙션 게임 실험 모음.

첫 실험은 영화 `버닝`, `양들의 침묵`을 모티브로 삼고, `Florence`처럼 짧은 터치 조작과 감정 변화 중심으로 전개.

## 현재 정해진 것

- 기존 `odie-in-wtf-company` 추리게임과 별도 프로젝트로 진행
- 세로형 2D 화면 기준
- 강한 추리 판정보다 장면, 감정, 손가락 조작, 대화 긴장감 우선
- 첫 목표는 한 장면 안에서 짧은 상호작용 2～3개를 플레이해보는 프로토타입
- 버닝 기본 진행: `C01 → C02 → C03 → C04 → C06 → C07 → C08 → C09 → C10 → C11 → C12 → C13 → C14 → C15 → C16 → C17 → C18 → C19`
- C05는 없음. C04 완료 후 클릭 시 C06으로 이동
- 각 Playable 씬 완료 후 다음 Playable 씬으로 이동
- C19 소등 후 검은 화면에서 종료
- C04: 손잡이를 시계 방향으로 180도 돌리면 캡슐 등장. 캡슐을 눌러 열면 시계 획득
- 장면 이동은 마우스 클릭 기준. 드래그 중 클릭은 장면 이동으로 처리하지 않음

## 버닝 1막 실행

- 통합 플레이 씬: `Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity`
- 씬 재생성: `Tools > Burning > Build Act 1 Playable Scene`
- 일반 실행 시 `Burn my nest` 타이틀 화면부터 시작
- 타이틀 메뉴: 새로운 게임, 챕터, 종료
- 새로운 게임 선택 시 저장 기록 초기화 후 `Chapter.1` 자막과 함께 C01 시작
- 챕터 메뉴의 Chapter 01 선택 시 마지막 저장 챕터부터 이어서 시작
- 타이틀 UI: `Assets/Game/Burning/Common/Scripts/BurningTitleScreen.cs`
- 진행 저장: `Assets/Game/Burning/Common/Scripts/BurningProgress.cs`
- 타이틀 글꼴: `Assets/Game/Burning/Resources/Fonts/`
- C02 완료 전 C03 이동 차단
- C01·C02·C03 전환 시점별 연출 신호 제공

## 챕터 디버깅

- 디버그 창: `Tools > Burning > Chapter Debugger`
- Play Mode 시작 시 디버그 창 자동 표시
- C01～C19 버튼으로 해당 챕터의 시작 화면부터 재생
- 묶음 Playable의 중간 챕터도 앞 장면을 건너뛰고 바로 시작
- C05는 구현 없음으로 버튼 목록에서 제외
- 현재 재생 중인 챕터 버튼은 강조색 표시. 같은 버튼을 눌러 해당 챕터 재시작 가능
- `Open Title`로 타이틀부터 재실행
- 저장 위치 지정·초기화와 저장된 챕터 바로 실행 지원
- 디버그 시작값은 에디터에서 한 번만 사용. 일반 실행과 WebGL 빌드는 타이틀부터 시작
- `Chapter Transition Timing`에서 전체 챕터 전환의 Fade Out, Black Hold, Fade In 수정
- 기본 전환 속도: Fade Out 0.65초, Black Hold 0.18초, Fade In 0.8초

## 버닝 C08-C12 실행

- 단독 플레이 씬: `Assets/Game/Burning/C08/Scenes/Burning_C08_C12_Playable.unity`
- 씬 재생성: `Tools > Burning > Build C08-C12 Playable Scene`
- C08: 어두운 배경에서 밝은 배경으로 느린 디졸브 후 블랙 전환
- C09: 블랙 화면의 불빛 점멸 후 종수 접근, 담배불 접촉 시 해미 이미지 디졸브
- C10: 종수의 눈 감기와 힐끔 동작 반복 후 해미에게 시선 집중
- C11: 화면 터치 시 종수가 시계를 내미는 동작 재생
- C12: 시계를 받은 해미의 미소에서 웃는 표정으로 디졸브
- 재생 중 `C08ToC12Sequence` 인스펙터에서 현재 챕터와 장면 확인 가능

## 버닝 C06-C07 실행

- 단독 플레이 씬: `Assets/Game/Burning/C06/Scenes/Burning_C06_C07_Playable.unity`
- 씬 재생성: `Tools > Burning > Build C06-C07 Playable Scene`
- C06: 해미의 질문 뒤 완성된 직소 퍼즐이 예시 3 배치로 흩어짐. 20개 조각을 모두 제자리에 맞추면 C07로 이동
- C07: 종수의 표정이 깨달음으로 바뀌고 느낌표가 탄력 있게 나타나는 연출

## 버닝 C13 실행

- 단독 플레이 씬: `Assets/Game/Burning/C13/Scenes/Burning_C13_Playable.unity`
- 씬 재생성: `Tools > Burning > Build C13 Playable Scene`
- 분침 주변을 드래그하면 시침이 1/12 속도로 따라 움직임
- 걷는 장면, 종수의 시선, 멈춘 발걸음 표시 두 단계, 봉천동 포차 순서로 표시
- 마지막 포차 화면에서 클릭하면 C14로 이동

## 버닝 C14 실행

- 단독 플레이 씬: `Assets/Game/Burning/C14/Scenes/Burning_C14_Playable.unity`
- 씬 재생성: `Tools > Burning > Build C14 Playable Scene`
- 첫 클릭 시 포차 화면으로 천천히 이동한 뒤 소주병과 두 잔이 순서대로 등장
- 잔을 길게 누르면 잔의 술이 차고 병의 술이 줄어듦. 가득 찬 잔을 누르면 마신 뒤 빈 잔으로 복귀
- 두 잔을 마시면 배경과 소주 세트가 옅어지고 Part 2 컷이 서로 겹치며 순서대로 등장

## 아직 정할 것

- 첫 실험의 가제
- 플레이어가 맡는 인물
- 상대 인물과의 관계
- 첫 장면의 장소
- 핵심 조작 2～3개
- 실패와 재시도의 처리 방식

## 참고

- `버닝`: 모호한 실종, 계급 감각, 불안, 타고 남은 흔적
- `양들의 침묵`: 심문, 심리전, 답을 아는 인물과의 거래
- `Florence`: 짧은 터치 조작으로 관계와 감정을 표현
- Florence 챕터별 연출 분석: `docs/references/florence_interaction_analysis.md`
- Florence 장면 모음 이미지: `assets-src/reference/florence_chapters/contact_sheets/`
- 버닝 1막 장면 설계와 C02 연결 방법: `docs/references/burning_act1_scene_plan.md`
