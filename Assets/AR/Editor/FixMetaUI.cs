using UnityEngine;
using UnityEditor;

public class FixMetaUI
{
    [MenuItem("Tools/Force Create Credential Storage")]
    public static void CreateStorageAsset()
    {
        // 1. Force Unity to spawn the Meta Credential Storage object in memory
        ScriptableObject asset = ScriptableObject.CreateInstance("Meta.XR.BuildingBlocks.AIBlocks.CredentialStorage");
        
        if (asset == null)
        {
            Debug.LogError("Could not find the CredentialStorage class in the Meta SDK!");
            return;
        }

        // 2. Ensure the Resources folder exists so it won't get stripped in the standalone build
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // 3. Save it as a physical file in the correct location
        string assetPath = "Assets/Resources/CredentialStorage.asset";
        AssetDatabase.CreateAsset(asset, assetPath);
        AssetDatabase.SaveAssets();

        // 4. Highlight it for you
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        
        Debug.Log("<color=green>SUCCESS:</color> Credential Storage created at " + assetPath);
    }
}