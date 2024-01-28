using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace realisticcoilhead
{
    [BepInPlugin("realisticcoilhead", "realisticcoildhead", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static GameObject Visuals;
        public static Harmony harmonyInstance;

        private void Awake()
        {
            Logger.LogInfo("Plugin realistic coil is loaded!");
            harmonyInstance = new Harmony("realisticcoilhead");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            string assetBundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "coil");
            AssetBundle loadedBundle = AssetBundle.LoadFromFile(assetBundlePath);
            if (loadedBundle == null)
            {
                Logger.LogError("Failed to load AssetBundle from: " + assetBundlePath);
                return;
            }

            Visuals = loadedBundle.LoadAsset<GameObject>("Assets/coil.prefab");
            if (Visuals == null)
            {
                Logger.LogError("Failed to load 'coil.prefab' from AssetBundle.");
                return;
            }

            Renderer[] renderers = Visuals.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                renderer.gameObject.layer = LayerMask.NameToLayer("Enemies");
            }
        }
    }

    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID = "realisticcoilhead";
        public const string PLUGIN_NAME = "realisticcoilhead";
        public const string PLUGIN_VERSION = "1.0.0";
    }

    [HarmonyPatch(typeof(SpringManAI), "Update")]
    internal class SpringManPatch
    {
        private static bool isModelReplaced = false;  // Flag to check if the model has been replaced

        private static void Postfix(SpringManAI __instance)
        {
           

            if (Plugin.Visuals == null)
            {
                UnityEngine.Debug.LogError("Visuals GameObject is null.");
                return;
            }

            Transform springManModelTransform = __instance.transform.Find("SpringManModel");
            if (springManModelTransform == null)
            {
                UnityEngine.Debug.LogError("SpringManModel Transform not found.");
                return;
            }

            SkinnedMeshRenderer originalBodyRenderer = springManModelTransform.Find("Body")?.GetComponent<SkinnedMeshRenderer>();
            MeshRenderer originalHeadRenderer = springManModelTransform.Find("Head")?.GetComponent<MeshRenderer>();
            Transform originalMetarigTransform = springManModelTransform.Find("AnimContainer/metarig");

            if (originalBodyRenderer != null)
                originalBodyRenderer.enabled = false;
            if (originalHeadRenderer != null)
                originalHeadRenderer.enabled = false;

            GameObject newModel = Object.Instantiate(Plugin.Visuals);
            newModel.transform.SetParent(springManModelTransform);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;
           // UnityEngine.Debug.Log("New model instantiated and set.");

            Transform coilheadArmature = newModel.transform.Find("Coilhead/Armature");
            Transform meshCoilhead = newModel.transform.Find("Coilhead/mesh_coilhead");

            if (coilheadArmature == null)
                UnityEngine.Debug.LogError("Coilhead Armature Transform not found in the new model.");
            if (meshCoilhead == null)
                UnityEngine.Debug.LogError("Mesh Coilhead Transform not found in the new model.");

            if (originalMetarigTransform != null && coilheadArmature != null)
            {
                coilheadArmature.SetParent(originalMetarigTransform.parent, false);
                coilheadArmature.localScale = originalMetarigTransform.localScale;
                coilheadArmature.localRotation = originalMetarigTransform.localRotation;
                coilheadArmature.localPosition = originalMetarigTransform.localPosition;
                //UnityEngine.Debug.Log("Coilhead armature set to replace original metarig.");
                originalMetarigTransform.name = "old-metarig";
            }

            if (meshCoilhead != null)
            {
                SkinnedMeshRenderer newMeshRenderer = meshCoilhead.GetComponent<SkinnedMeshRenderer>();
                if (newMeshRenderer != null)
                {
                    newMeshRenderer.rootBone = coilheadArmature;
                   // UnityEngine.Debug.Log("New mesh renderer root bone set.");
                }
                else
                {
                    UnityEngine.Debug.LogError("SkinnedMeshRenderer on new model not found.");
                }
            }

            // Set the flag to true after the model has been replaced
            
        }
    }
}
