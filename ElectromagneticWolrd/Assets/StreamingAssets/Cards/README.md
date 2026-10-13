# 카드 이미지와 문구 교체 안내

체험 카드의 사진과 문구는 이 폴더에서 **앱을 실행할 때마다 읽어 옵니다.**
파일만 바꾸고 앱을 다시 켜면 바로 반영됩니다(다시 빌드하지 않아도 됩니다).

- 개발 PC: `Assets/StreamingAssets/Cards/`
- 설치한 PC: `<앱 폴더>/ElectromagneticWorld_Data/StreamingAssets/Cards/`

## 폴더 구조

```
Cards/
  Card_01/
    card.json        제목, 분류, 설명
    Original.png     레일 위에서 흐를 때 보이는 원본(맨눈으로 본) 이미지 (선택)
    Radio.png        라디오파
    Microwave.png    마이크로파
    Infrared.png     적외선
    Visible.png      가시광선
    Ultraviolet.png  자외선
    XRay.png         X선
    Gamma.png        감마선
  Card_02/
  ...
```

- 이미지는 **512 x 512 정사각형**을 권장합니다. PNG 또는 JPG를 쓸 수 있습니다(`.png`, `.jpg`, `.jpeg`).
- 파일 이름은 위 표와 대소문자까지 똑같이 맞춥니다.
- 그 파장의 이미지가 없으면 파일을 넣지 않습니다. 화면에는 "이미지 없음" 안내와 다른 파장의 썸네일이 대신 보입니다.
- `Original.png`가 없으면 `Visible.png`를 원본으로 씁니다.
- 파장 이미지가 하나도 없는 폴더는 건너뜁니다.

## card.json

```json
{
    "title": "게 성운",
    "category": "천체",
    "description": "1054년에 폭발한 초신성의 잔해입니다."
}
```

- `category`는 `자연`, `사물`, `천체` 중 하나입니다.
- 저장할 때 인코딩은 **UTF-8**로 합니다(메모장 기본값).
- 파일이 없으면 폴더 이름이 제목으로 보입니다.

## 카드 추가, 삭제, 순서

- **추가:** 새 폴더(예: `Card_10`)를 만들고 이미지와 `card.json`을 넣습니다.
- **삭제:** 폴더를 지웁니다.
- **순서:** 레일에 나오는 순서는 폴더 이름순입니다. 순서를 바꾸려면 폴더 이름의 번호를 바꿉니다.

## 문제가 생겼을 때

파일이 깨졌거나 이름이 틀려도 앱은 멈추지 않습니다. 해당 이미지만 빠지고 로그에 기록됩니다.
- 로그 위치: `%USERPROFILE%/AppData/LocalLow/DefaultCompany/ElectromagneticWorld/Player.log`
