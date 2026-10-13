// 화면 왼쪽(긴 파장)부터 오른쪽(짧은 파장) 순서의 전자기파 파장 영역.
// 값의 순서가 곧 화면 칸 순서이고, 카드 이미지 파일 이름(Cards/<카드ID>/<이름>.png)으로도 쓰인다.
public enum BandType
{
    Radio,
    Microwave,
    Infrared,
    Visible,
    Ultraviolet,
    XRay,
    Gamma,
}

public static class BandInfo
{
    public const int Count = 7;

    private static readonly string[] DisplayNames = { "라디오파", "마이크로파", "적외선", "가시광선", "자외선", "X선", "감마선" };

    public static string DisplayName(BandType band) => DisplayNames[(int)band];
}
