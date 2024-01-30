using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace realisticcoilhead
{
    // Plugin definition for BepInEx
    [BepInPlugin("realisticcoilhead", "realisticcoildhead", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        // Static variable to hold the visual GameObject to be replaced
        public static GameObject Visuals;

        // Harmony instance for patching methods
        public static Harmony harmonyInstance;

        // Awake is called when the script instance is being loaded
        private void Awake()
        {
            // Log the loading of the plugin
            Logger.LogInfo("Plugin realistic coil is loaded!");

            // Create a new Harmony instance for patching
            harmonyInstance = new Harmony("realisticcoilhead");

            // Apply all annotated Harmony patches in the current assembly
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            // Construct the full path to the AssetBundle
            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "coil");

            // Load the AssetBundle at the specified path
            AssetBundle loadedBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (loadedBundle == null)
            {
                Logger.LogError("Failed to load AssetBundle from: " + assetBundlePath);
                return;
            }

            // Load the 'coil.prefab' GameObject from the AssetBundle
            Visuals = loadedBundle.LoadAsset<GameObject>("Assets/coil.prefab");
            if (Visuals == null)
            {
                Logger.LogError("Failed to load 'coil.prefab' from AssetBundle.");
                return;
            }

            // Set the layer of all Renderer components to 'Enemies'
            Renderer[] renderers = Visuals.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.gameObject.layer = LayerMask.NameToLayer("Enemies");
            }
        }
    }

    // Static class to hold plugin information
    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "realisticcoilhead";
        public const string PLUGIN_NAME = "realisticcoilhead";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    // Harmony patch for the SpringManAI's Update method
    [HarmonyPatch(typeof(SpringManAI), "Update")]
    internal class SpringManPatch
    {
        // Flag to check if the model has already been replaced
        private static bool isModelReplaced = false;

        // Method called after SpringManAI's Update method
        private static void Postfix(SpringManAI __instance)
        {
            // Check if the model has already been replaced
            if (isModelReplaced)
            {
                return; // Exit if the model has been replaced
            }

            // Check if the Visuals GameObject is loaded
            if (Plugin.Visuals == null)
            {
                UnityEngine.Debug.LogError("Visuals GameObject is null.");
                return;
            }

            // Find the 'SpringManModel' transform in the SpringManAI instance
            Transform springManModelTransform = __instance.transform.Find("SpringManModel");
            if (springManModelTransform == null)
            {
                UnityEngine.Debug.LogError("SpringManModel Transform not found.");
                return;
            }

            // Disable the original body and head renderers
            SkinnedMeshRenderer originalBodyRenderer = springManModelTransform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();
            MeshRenderer originalHeadRenderer = springManModelTransform.Find("Head")?.GetComponent<MeshRenderer>();
            if (originalBodyRenderer != null)
                originalBodyRenderer.enabled = false;
            if (originalHeadRenderer != null)
                originalHeadRenderer.enabled = false;

            // Instantiate the new model and set its transform properties
            GameObject newModel = Object.Instantiate(Plugin.Visuals);
            newModel.transform.SetParent(springManModelTransform);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;

            // Find specific parts in the new model
            Transform coilheadArmature = newModel.transform.Find("Coilhead/Armature");
            Transform meshCoilhead = newModel.transform.Find("Coilhead/mesh_coilhead");

            // Error handling if transforms are not found
            if (coilheadArmature == null)
                UnityEngine.Debug.LogError("Coilhead Armature Transform not found in the new model.");
            if (meshCoilhead == null)
                UnityEngine.Debug.LogError("Mesh Coilhead Transform not found in the new model.");

            // Replace the original metarig with the new armature
            Transform originalMetarigTransform = springManModelTransform.Find("AnimContainer/metarig");
            if (originalMetarigTransform != null && coilheadArmature != null)
            {
                coilheadArmature.SetParent(originalMetarigTransform.parent, false);
                coilheadArmature.localScale = originalMetarigTransform.localScale;
                coilheadArmature.localRotation = originalMetarigTransform.localRotation;
                coilheadArmature.localPosition = originalMetarigTransform.localPosition;
                originalMetarigTransform.name = "old-metarig";
            }

            // Set up the SkinnedMeshRenderer for the new model
            if (meshCoilhead != null)
            {
                SkinnedMeshRenderer newMeshRenderer = meshCoilhead.GetComponent<SkinnedMeshRenderer>();
                if (newMeshRenderer != null)
                {
                    newMeshRenderer.rootBone = coilheadArmature;
                }
                else
                {
                    UnityEngine.Debug.LogError("SkinnedMeshRenderer on new model not found.");
                }
            }

            // Set the flag to true after the model has been replaced
            isModelReplaced = true;
        }
    }
}