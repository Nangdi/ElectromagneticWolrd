using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static EMWorldUIKit;

// 정적 PNG 누락, 카드 데이터, 비어 있는 인스펙터 연결을 찾아 콘솔에 알려준다.
// 메뉴: Tools > 전자기파로 보는 세상 > 4. 연결 검사
public static class EMWorldUIValidator
{
    private const string CanvasName = "Canvas_EMWorld";

    [MenuItem(MenuRoot + "4. 연결 검사", priority = 4)]
    public static int Validate()
    {
        int problems = 0;

        foreach (var (panel, _) in Panels)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(StaticSpritePath(panel)) == null)
                problems += Report($"정적 레이어 이미지가 없습니다: {StaticSpritePath(panel)}", null);
        }

        foreach (string part in new[] { "Card", "CardBandView", "CardBack", "NoImageThumb" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PartPrefabPath(part));
            if (prefab == null)
                problems += Report($"부품 프리팹이 없습니다: {PartPrefabPath(part)}", null);
            else
                problems += CheckReferences(prefab);
        }

        problems += CheckCards();

        GameObject canvas = null;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == CanvasName) canvas = root;

            foreach (var idle in root.GetComponentsInChildren<IdleResetController>(true))
                problems += CheckReferences(idle.gameObject);
        }

        if (canvas == null)
        {
            problems += Report($"씬에 {CanvasName}가 없습니다. '1. 프리팹 생성 및 씬 적용'을 실행하세요.", null);
        }
        else
        {
            if (!PrefabUtility.IsPartOfPrefabInstance(canvas))
                problems += Report($"{CanvasName}가 프리팹 인스턴스가 아닙니다.", canvas);

            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != StaticLayerName) continue;
                Image image = t.GetComponent<Image>();
                if (image == null || image.sprite == null)
                    problems += Report($"{t.parent.name}: StaticLayer 이미지가 비어 있습니다.", t);
            }

            problems += CheckReferences(canvas);
        }

        if (problems == 0)
            Debug.Log("전자기파로 보는 세상 UI 연결 검사: 문제 없음");
        else
            Debug.LogWarning($"전자기파로 보는 세상 UI 연결 검사: 문제 {problems}건 (위 로그 참고)");
        return problems;
    }

    // StreamingAssets/Cards: 카드 폴더마다 파장 이미지가 하나 이상, card.json이 있는지
    private static int CheckCards()
    {
        if (!Directory.Exists(CardsDir))
            return Report($"카드 폴더가 없습니다: {CardsDir}", null);

        int problems = 0, count = 0;
        foreach (string dir in Directory.GetDirectories(CardsDir))
        {
            string id = Path.GetFileName(dir);
            bool hasImage = false;
            for (int i = 0; i < BandInfo.Count; i++)
                hasImage |= File.Exists(CardImagePath(id, (BandType)i));

            if (!hasImage)
                problems += Report($"파장 이미지가 하나도 없는 카드 폴더입니다 (실행 시 건너뜀): {dir}", null);
            else
                count++;
            if (!File.Exists(CardInfoPath(id)))
                problems += Report($"{CardLibrary.InfoFileName}이 없습니다 (폴더 이름이 제목으로 보임): {dir}", null);
        }

        if (count == 0)
            problems += Report($"카드가 없습니다: {CardsDir}", null);
        return problems;
    }

    // 하위 모든 컴포넌트에서 비어 있는 오브젝트 참조를 찾는다. (프로젝트 스크립트만)
    private static int CheckReferences(GameObject root)
    {
        int problems = 0;
        foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null)
            {
                problems += Report($"{root.name}: 스크립트가 없는(Missing) 컴포넌트가 있습니다.", root);
                continue;
            }

            // TMP, UGUI 등 패키지 컴포넌트는 검사하지 않는다
            if (behaviour.GetType().Assembly.GetName().Name != "Assembly-CSharp") continue;

            var so = new SerializedObject(behaviour);
            SerializedProperty property = so.GetIterator();
            while (property.NextVisible(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.name == "m_Script" || property.objectReferenceValue != null) continue;

                problems += Report($"{GetPath(behaviour.transform)} ({behaviour.GetType().Name}): '{property.displayName}' 연결이 비어 있습니다.", behaviour);
            }
        }
        return problems;
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = $"{t.name}/{path}";
        }
        return path;
    }

    private static int Report(string message, Object context)
    {
        Debug.LogWarning($"[UI 검사] {message}", context);
        return 1;
    }
}
