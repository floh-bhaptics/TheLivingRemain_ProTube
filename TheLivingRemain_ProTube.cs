using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;


using MelonLoader;
using HarmonyLib;
using Il2Cpp;
using System.Text.Json;

[assembly: MelonInfo(typeof(TheLivingRemain_ProTube.TheLivingRemain_ProTube), "TheLivingRemain_ProTube", "1.0.1", "Florian Fahrenberger")]
[assembly: MelonGame("Five Finger Studios", "TheLivingRemain")]


namespace TheLivingRemain_ProTube
{
    public class TheLivingRemain_ProTube : MelonMod
    {
        public static bool isRightHanded = true;
        public static string configPath = Directory.GetCurrentDirectory() + "\\UserData\\";
        public static bool dualWield = false;

        public override void OnInitializeMelon()
        {
            InitializeProTube();
        }

        public static void saveChannel(string channelName, string proTubeName)
        {
            string fileName = configPath + channelName + ".pro";
            File.WriteAllText(fileName, proTubeName, Encoding.UTF8);
        }

        public static string readChannel(string channelName)
        {
            string fileName = configPath + channelName + ".pro";
            if (!File.Exists(fileName)) return "";
            return File.ReadAllText(fileName, Encoding.UTF8);
        }

        public static void dualWieldSort()
        {
            //MelonLogger.Msg("Channels: " + ForceTubeVRInterface.ListChannels());
            JsonDocument doc = JsonDocument.Parse(ForceTubeVRInterface.ListChannels());
            JsonElement pistol1 = doc.RootElement.GetProperty("channels").GetProperty("pistol1");
            JsonElement pistol2 = doc.RootElement.GetProperty("channels").GetProperty("pistol2");
            if ((pistol1.GetArrayLength() > 0) && (pistol2.GetArrayLength() > 0))
            {
                dualWield = true;
                MelonLogger.Msg("Two ProTube devices detected, player is dual wielding.");
                if ((readChannel("rightHand") == "") || (readChannel("leftHand") == ""))
                {
                    MelonLogger.Msg("No configuration files found, saving current right and left hand pistols.");
                    saveChannel("rightHand", pistol1[0].GetProperty("name").ToString());
                    saveChannel("leftHand", pistol2[0].GetProperty("name").ToString());
                }
                else
                {
                    string rightHand = readChannel("rightHand");
                    string leftHand = readChannel("leftHand");
                    MelonLogger.Msg("Found and loaded configuration. Right hand: " + rightHand + ", Left hand: " + leftHand);
                    ForceTubeVRInterface.ClearChannel(4);
                    ForceTubeVRInterface.ClearChannel(5);
                    ForceTubeVRInterface.AddToChannel(4, rightHand);
                    ForceTubeVRInterface.AddToChannel(5, leftHand);
                }
            }
        }

        private async void InitializeProTube()
        {
            MelonLogger.Msg("Initializing ProTube gear...");
            await ForceTubeVRInterface.InitAsync(true);
            Thread.Sleep(10000);
            dualWieldSort();
        }

        #region Recoil

        [HarmonyPatch(typeof(Handgun), "FireGun", new Type[] { })]
        public class bhaptics_FireHandGun
        {
            [HarmonyPostfix]
            public static void Postfix(Handgun __instance)
            {
                if (__instance.canFire) return;
                bool isRight = (__instance.holdingHand == Il2CppVRTK.GrabAttachMechanics.Hand.right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!isRight) myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Kick(180, myChannel);
            }
        }

        [HarmonyPatch(typeof(HandgunRevolver), "FireProjectile", new Type[] { })]
        public class bhaptics_FireRevolverGun
        {
            [HarmonyPostfix]
            public static void Postfix(HandgunRevolver __instance)
            {
                bool isRight = (__instance.holdingHand == HandgunRevolver.HoldingHand.right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                if (!isRight) myChannel = ForceTubeVRChannel.pistol2;
                ForceTubeVRInterface.Kick(200, myChannel);
            }
        }

        [HarmonyPatch(typeof(MachineGunBack), "FireGun", new Type[] { })]
        public class bhaptics_FireShotgun
        {
            [HarmonyPostfix]
            public static void Postfix(MachineGunBack __instance)
            {
                if (__instance.canFire) return;
                bool isRight = (__instance.holdingHand == Il2CppVRTK.GrabAttachMechanics.Hand.right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                ForceTubeVRChannel secondaryChannel = ForceTubeVRChannel.pistol2;
                if (!isRight) { myChannel = ForceTubeVRChannel.pistol2; secondaryChannel = ForceTubeVRChannel.pistol1; }
                ForceTubeVRInterface.Kick(120, secondaryChannel);
                ForceTubeVRInterface.Shoot(255, 200, 40f, myChannel);
            }
        }

        [HarmonyPatch(typeof(MachineGunFront), "GunFired", new Type[] { })]
        public class bhaptics_FireShotgunFront
        {
            [HarmonyPostfix]
            public static void Postfix(MachineGunFront __instance)
            {
                if (__instance.controllerReference == null) return;
                if (!__instance.grabbed) return;
                bool isRight = (__instance.controllerReference.hand == Il2CppVRTK.SDK_BaseController.ControllerHand.Right);
                ForceTubeVRChannel myChannel = ForceTubeVRChannel.pistol1;
                ForceTubeVRChannel secondaryChannel = ForceTubeVRChannel.pistol2;
                if (!isRight) { myChannel = ForceTubeVRChannel.pistol2; secondaryChannel = ForceTubeVRChannel.pistol1; }
                ForceTubeVRInterface.Kick(120, secondaryChannel);
                ForceTubeVRInterface.Shoot(220, 150, 30f, myChannel);
            }
        }
        [HarmonyPatch(typeof(Minigun), "FireProjectile", new Type[] { })]
        public class bhaptics_FireMinigun
        {
            [HarmonyPostfix]
            public static void Postfix(Minigun __instance)
            {
                if (__instance.isHeldByRightController) ForceTubeVRInterface.Kick(200, ForceTubeVRChannel.pistol1);
                if (__instance.isHeldByLeftController) ForceTubeVRInterface.Kick(200, ForceTubeVRChannel.pistol2);
            }
        }

        /*
        [HarmonyPatch(typeof(Melee), "PlayHapticsWithDelay", new Type[] { typeof(float) })]
        public class bhaptics_KnifeHapticsDelay
        {
            [HarmonyPostfix]
            public static void Postfix(Melee __instance)
            {
                tactsuitVr.LOG("DelayHaptics");
                bool isRight = (__instance.hand == Il2CppVRTK.GrabAttachMechanics.Hand.right);
                tactsuitVr.Recoil("Knife", isRight);
            }
        }
        */
        #endregion



    }
}
