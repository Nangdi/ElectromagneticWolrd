# 전자기파로 보는 세상 – 이미지 교체 안내

화면 규격: **7680 x 2160** (55형 4K 모니터 2대). 왼쪽 7칸(칸당 960px)은 파장 영역, 맨 오른쪽 1칸은 정보검색.

## 규칙

- **같은 경로, 같은 파일명으로 덮어쓰기만 한다. 지우고 새로 넣지 않는다.** (연결이 끊긴다)
- 크기는 아래 표와 정확히 같게 만든다.
- `_Guide/Guide_xxx.png`의 분홍 박스 자리는 동적 요소 자리이므로 정적 PNG에 그리지 않는다.

## Static (정적 레이어)

| 파일 | 크기 | 내용 |
|---|---|---|
| `Static/Static_Spectrum.png` | 6720 x 2160 | 7개 파장 칸 배경, 칸 구분, 가시광선 무지개, 사인파, 가운데 레일 바탕 (카드와 파장 이름 라벨은 그리지 않음) |
| `Static/Static_InfoMenu.png` | 2160 x 960 | 정보검색 메뉴 화면 배경과 제목 (메뉴 버튼 4개 자리는 비움) |
| `Static/Static_InfoDetail01~04.png` | 2160 x 960 | 세부 정보 화면 4개 (아래쪽 [이전으로] 버튼 자리는 비움) |

정보검색 칸(960x2160)은 90° 돌려 **가로 화면(2160x960)** 하나로 쓴다. 테이블 오른쪽 끝에 선 한 사람 기준으로 그린다.

## Parts (동적 요소 부품)

| 파일 | 크기 | 비고 |
|---|---|---|
| `Parts/Label_<파장>.png` | 320 x 84 | 파장 이름 라벨 7개 (Radio, Microwave, Infrared, Visible, Ultraviolet, XRay, Gamma) |
| `Parts/Button_Info01~04.png` | 900 x 200 | 정보검색 메뉴 버튼 |
| `Parts/Button_Back.png` | 300 x 96 | [이전으로] 버튼 |
| `Parts/NoImage_Message.png` | 460 x 130 | 이미지 없음 안내 문구 (투명 배경) |
| `Parts/NoImage_Sub.png` | 460 x 44 | "이미지가 있는 파장대" 문구 (투명 배경) |
| `Parts/Card_Frame.png` | 30 x 30 | 카드 테두리. **흰색**, 9-slice. 가운데는 그리지 않고(속이 빔) 사진 위에 덮인다 |
| `Parts/Button_CardInfo.png` | 96 x 96 | 카드 오른쪽 아래 뒤집기 버튼 (앞면에서 보임: 정보 보기) |
| `Parts/Button_CardImage.png` | 96 x 96 | 카드 오른쪽 아래 뒤집기 버튼 (뒷면에서 보임: 사진 보기) |
| `Parts/NoImage_Bg.png` | 26 x 26 | 이미지 없음 안내 배경. **흰색**, 9-slice |

## Cards (체험용 이미지) → StreamingAssets로 옮김

카드 사진과 문구는 현장에서 바로 바꿀 수 있도록 이 폴더가 아니라 **`Assets/StreamingAssets/Cards/`** 에 둔다.
앱이 실행할 때 파일을 직접 읽으므로, 설치한 PC에서도 빌드 폴더의 `ElectromagneticWorld_Data/StreamingAssets/Cards/`에서 파일만 바꾸면 된다.
폴더 구조, 파일 이름, `card.json` 형식은 그 폴더의 `README.md`를 본다.

- 카드 뒷면(정보면)은 `card.json`의 분류‧제목‧설명으로 실행할 때 자동으로 만들어진다. 레이아웃은 `Prefabs/EMWorld/Parts/CardBack.prefab`에서 고친다.
- Unity에서 **Tools > 전자기파로 보는 세상 > 5. 카드 폴더 열기**로 폴더를 열 수 있다.
