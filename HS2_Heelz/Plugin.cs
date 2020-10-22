﻿using AIChara;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI.Chara;

namespace Heelz
{
    [BepInPlugin(Constant.GUID, Constant.NAME, Constant.VERSION)]
    [BepInDependency(Sideloader.Sideloader.GUID)]
    public class HeelzPlugin : BaseUnityPlugin
    {
        private static ManualLogSource _logger;
        private static ConfigEntry<bool> LoadDevXML { get; set; }

        private void Start()
        {
            _logger = Logger;
            Util.Logger.logSource = _logger;
            LoadDevXML = Config.Bind("Heelz", "Load Developer XML", false,
                new ConfigDescription("Make Heelz Plugin load heel_manifest.xml file from game root folder. Useful for developing heels. Useless for most of users."));
            CharacterApi.RegisterExtraBehaviour<HeelsController>(Constant.GUID);
            HarmonyWrapper.PatchAll(typeof(HeelzPlugin));
            _logger.LogInfo("[Heelz] Heels mode activated: destroy all foot");
            var loadedManifests = Sideloader.Sideloader.Manifests.Values;
            foreach (var manifest in loadedManifests) XMLLoader.LoadXML(manifest.manifestDocument);
            if (LoadDevXML.Value) XMLLoader.StartWatchDevXML();
        }

        /*
         * ANIMATION BASED INTERACTIONS
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(HScene), nameof(HScene.ChangeAnimation))]
        // ReSharper disable once UnusedMember.Global InconsistentNaming
        public static void OnChangeAnimation(HScene __instance, HScene.AnimationListInfo _info, bool _isForceResetCamera, bool _isForceLoopAction = false, bool _UseFade = true)
        {
            var isGroundAnimation = Constant.StandingAnimations.Contains(_info.id);
            var females = __instance.GetFemales();
            foreach (var female in females)
            {
                var femaleApiController = GetAPIController(female);
                if (femaleApiController == null) continue;
                femaleApiController.GroundAnim = isGroundAnimation;
                femaleApiController?.UpdateHover();
            }
        }

        /*
         *  CLOTHES RELATED INTERACTIONS 
         */
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.ChangeCustomClothes))]
        // ReSharper disable once UnusedMember.Global InconsistentNaming
        public static void ChangeCustomClothes(ChaControl __instance, int kind)
        {
            if (kind == 7) GetAPIController(__instance)?.SetUpShoes();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), nameof(ChaControl.SetClothesState))]
        // ReSharper disable once UnusedMember.Global InconsistentNaming
        public static void SetClothesState(ChaControl __instance, int clothesKind, byte state, bool next = true)
        {
            // What the fuck? somehow set clothes state getting called every single frame?
            if (clothesKind == Constant.ShoeCategory) GetAPIController(__instance)?.UpdateHover();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(ChaControl), "LateUpdateForce")]
        public static void LateUpdateForce(ChaControl __instance)
        {
            var heelsController = GetAPIController(__instance);
            if (heelsController == null) return;
            heelsController.UpdateHover();
            if (!__instance.fullBodyIK.isActiveAndEnabled) heelsController.IKArray();
        }

        // ReSharper disable once SuggestBaseTypeForParameter
        private static HeelsController GetAPIController(ChaControl character)
        {
            return character?.gameObject?.GetComponent<HeelsController>();
        }
    }
}