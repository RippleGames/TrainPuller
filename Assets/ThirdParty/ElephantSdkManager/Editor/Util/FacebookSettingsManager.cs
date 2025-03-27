using UnityEngine;
using UnityEditor;
using Facebook.Unity.Settings;
using Facebook.Unity.Editor;
using System;
using System.IO;

namespace ElephantSdkManager.Util
{
    public static class FacebookSettingsManager 
    {
        private const string FacebookSettingsPath = "Assets/FacebookSDK/SDK/Resources";

        public static void SetupFacebookSettings(string appId, string clientToken)
        {
            if (string.IsNullOrEmpty(appId) || string.IsNullOrEmpty(clientToken))
            {
                Debug.LogError("Facebook App ID or Client Token is empty. Skipping Facebook settings setup.");
                return;
            }

            try
            {
                if (!Directory.Exists(FacebookSettingsPath))
                {
                    Directory.CreateDirectory(FacebookSettingsPath);
                    Debug.Log("Created Facebook SDK Resources directory");
                }

                var instance = FacebookSettings.NullableInstance;
                if (instance == null)
                {
                    CreateFacebookSettingsAsset();
                    instance = FacebookSettings.NullableInstance;
                }

                if (instance == null)
                {
                    Debug.LogError("Could not create or find FacebookSettings instance.");
                    return;
                }
                
                if (FacebookSettings.AppIds.Count == 0)
                {
                    FacebookSettings.AppIds.Add("0");
                    FacebookSettings.AppLabels.Add("New App");
                    FacebookSettings.ClientTokens.Add(string.Empty);
                    FacebookSettings.AppLinkSchemes.Add(new FacebookSettings.UrlSchemes());
                }
                
                FacebookSettings.AppIds[0] = appId;
                FacebookSettings.ClientTokens[0] = clientToken;
                
                EditorUtility.SetDirty(instance);
                AssetDatabase.SaveAssets();

                ManifestMod.GenerateManifest();

                AssetDatabase.Refresh();

                Debug.Log($"Successfully updated Facebook Settings:\nApp ID: {appId}\nClient Token: {clientToken}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error updating Facebook Settings: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void CreateFacebookSettingsAsset()
        {
            try
            {
                var instance = ScriptableObject.CreateInstance<FacebookSettings>();
                var fullPath = Path.Combine(
                    FacebookSettingsPath,
                    "FacebookSettings.asset"
                );

                AssetDatabase.CreateAsset(instance, fullPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("Created new FacebookSettings asset");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to create FacebookSettings asset: {ex.Message}");
                throw;
            }
        }
    }
}