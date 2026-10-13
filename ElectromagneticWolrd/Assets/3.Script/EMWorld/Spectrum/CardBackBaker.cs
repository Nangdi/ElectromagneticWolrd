using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 카드 뒷면(정보면)을 시작할 때 카드마다 텍스처 한 장으로 구워 둔다.
// 뒷면을 여러 요소(배경, 글자) 그대로 7개 파장 뷰에 두면, 경계에서 섞일 때 겹친 요소가 비쳐 글자가 흐려진다.
// 한 장짜리 이미지로 만들어 두면 모든 칸에서 같은 이미지가 겹쳐져 이음새 없이 보인다.
public class CardBackBaker : MonoBehaviour
{
    [SerializeField] private CardBackView backPrefab;

    // 화면 밖 먼 곳에 임시 카메라와 캔버스를 만들어 찍고 바로 지운다.
    private static readonly Vector3 BakePosition = new Vector3(0f, -100000f, 0f);

    public Texture2D Bake(CardData data)
    {
        var size = Vector2Int.RoundToInt(((RectTransform)backPrefab.transform).sizeDelta);
        int uiLayer = LayerMask.NameToLayer("UI");

        var root = new GameObject("CardBackBake");
        root.transform.position = BakePosition;
        RenderTexture rt = RenderTexture.GetTemporary(size.x, size.y, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        try
        {
            var cam = new GameObject("BakeCamera").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false);
            cam.transform.localPosition = new Vector3(0f, 0f, -100f);
            cam.enabled = false;
            cam.orthographic = true;
            cam.orthographicSize = size.y * 0.5f;
            cam.nearClipPlane = 1f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.clear;
            cam.cullingMask = 1 << uiLayer;
            cam.targetTexture = rt;

            var canvasGo = new GameObject("BakeCanvas", typeof(RectTransform));
            canvasGo.layer = uiLayer;
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = cam;
            ((RectTransform)canvasGo.transform).sizeDelta = size;

            CardBackView view = Instantiate(backPrefab, canvasGo.transform);
            var viewRect = (RectTransform)view.transform;
            viewRect.anchoredPosition = Vector2.zero;
            viewRect.localRotation = Quaternion.identity;
            viewRect.localScale = Vector3.one;
            view.Set(data);

            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in canvasGo.GetComponentsInChildren<TMP_Text>(true))
                text.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            cam.Render();

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false) { name = $"CardBack_{data?.id}" };
            tex.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
            tex.Apply(false, true); // 시스템 메모리 사본은 버린다
            RenderTexture.active = prev;
            return tex;
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
            DestroyImmediate(root); // 같은 프레임에 다음 카드를 구울 때 겹쳐 찍히지 않도록 바로 지운다
        }
    }
}
