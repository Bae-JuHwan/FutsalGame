using UnityEditor;
using UnityEngine;

public class FutsalAthleteImporter : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.EndsWith("/Players/Athlete.fbx")) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Legacy;
        importer.importAnimation = true;
        importer.animationCompression = ModelImporterAnimationCompression.Optimal;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        importer.importCameras = false;
        importer.importLights = false;
        importer.addCollider = false;
    }

    private void OnPreprocessTexture()
    {
        if (!assetPath.Contains("/Resources/Players/")) return;
        var importer = (TextureImporter)assetImporter;
        importer.maxTextureSize = assetPath.EndsWith("Skin.png") ? 2048 : 1024;
        importer.mipmapEnabled = true;
        importer.alphaIsTransparency = assetPath.EndsWith("Hair.png");
        importer.textureCompression = TextureImporterCompression.Compressed;
    }
}
