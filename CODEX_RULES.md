# CODEX_RULES

## 프로젝트 개요

- 프로젝트명: After Hours
- 장르: 3D 체험형 퍼즐 탈출 게임
- Unity 버전: Unity 6.3 URP

## 주요 에셋

- Kenney Space Station Kit
- Stylized Astronaut
- GrabPack 형태의 Sketchfab 모델

## 핵심 플레이

- Grab Pack으로 Energy Core를 끌어당김
- Core Station에 Energy Core를 넣음
- 충전 후 Security Door가 열림
- 여러 구역을 지나 탈출

## 코드 규칙

- MonoBehaviour 기반으로 작성
- 주요 Inspector 조절 값은 `[SerializeField] private` 필드 사용
- `public` 필드 남발 금지
- `GameObject.Find` 남발 금지
- `Update` 안에서 무거운 처리 금지
- 클래스는 기능별로 분리
- 한국어 주석 작성
- 코드 작성 후 Inspector 연결 방법을 반드시 설명
- 각 기능 구현 후 테스트 방법을 함께 작성
