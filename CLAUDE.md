# film-inspired-games 작업 지침

## 공통 기준

- 이 프로젝트의 현재 게임 방향은 `docs/game_overview.md` 먼저 확인
- Codex 세션도 같은 기준을 보도록 repo 루트 `AGENTS.md`를 함께 유지
- 이 파일과 `AGENTS.md` 중 하나를 수정하면 다른 하나도 확인

## 프로젝트 기준

- Unity 버전: `6000.0.67f1`
- 템플릿: Universal 2D
- 목표 화면: 세로 9:16, 기본 창 크기 `540x960`
- 현재 시작 씬: `Assets/Game/Burning/Scenes/Burning_Act1_Playable.unity`
- 런타임 에셋: `Assets/Game/`
- 원본·reference: `assets-src/`
- 큰 런타임 아트: Addressables 도입 전까지 직접 참조로 시작, 커지면 이동 검토
- 작은 UI 조각: 직접 참조 + SpriteAtlas 우선 검토
- `Resources`는 필요할 때만 만들고 유지 이유 기록

## 현재 방향

- 영화 모티브 기반 2D 인터랙션 게임 실험 모음
- 첫 실험: `버닝`, `양들의 침묵` 모티브 + `Florence`식 짧은 감정 상호작용
- 기존 `odie-in-wtf-company` 추리게임과 별도 프로젝트로 진행

## Unity 작업 원칙

- 새 씬, UI, 에셋 구조가 생기면 `docs/game_overview.md` 또는 별도 구조 문서에 위치 기록
- 단순 C# 수정은 씬을 다시 저장하지 않음
- Unity 자동 변경이 생기면 git diff를 확인하고, 의도한 파일만 남김
- 빌드 산출물은 `Builds/` 아래 저장하고 git에 포함하지 않음
