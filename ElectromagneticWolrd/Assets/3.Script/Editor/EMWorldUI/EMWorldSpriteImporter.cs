using UnityEditor;

// Assets/4.Sprite 아래 이미지는 항상 UI 스프라이트 설정으로 들여온다.
// 디자이너가 PNG를 덮어쓰거나 새로 넣어도 설정을 따로 만질 필요가 없다.
public class EMWorldSpriteImporter : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(EMWorldUIKit.SpriteRoot + "/")) return;

        var importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
        importer.filterMode = UnityEngine.FilterMode.Bilinear;

        if (assetPath.StartsWith(EMWorldUIKit.PartsDir + "/"))
        {
            // 작은 부품은 선명도를 위해 무압축 (크기가 4의 배수가 아니면 블록 압축도 안 된다)
            importer.maxTextureSize = 4096;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
        else
        {
            // 파장 영역 정적 레이어가 6720px이라 8192까지 허용한다
            importer.maxTextureSize = 8192;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
