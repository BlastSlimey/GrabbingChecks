
using System;
using System.Collections.Generic;
using System.IO;
using BepInEx.Logging;
using HarmonyLib;
using Unity.VisualScripting;
using UnityEngine;

public class PatchesHandler {
    
    public static ManualLogSource Logger;

}

[HarmonyPatch(typeof(ArmScript_v2))]
public class ArmScript_v2Patch {

    public static HashSet<ClimbingSurface.SurfaceType> CheckedSurfaces = [];
    
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPostfix() {
        PatchesHandler.Logger.LogInfo("Started ArmScript_v2");
    }

    [HarmonyPatch("UpdateFrictionScaler")]
    [HarmonyPrefix]
    private static bool UpdateFrictionScalerPrefix(ArmScript_v2 __instance) {
        float scale = 0.4f + 0.2f * ConnectionHandler.GripStrengths;
        if (__instance.otherArm.grabbedSurface != null) {
            scale *= 0.75f;
        }
        Util.SetPrivateField(typeof(ArmScript_v2), __instance, "frictionScaler", scale);
        if (__instance.isDynamicFriction && __instance.grabbedSurface != null) {
            __instance.frictionJoint.maxForce = __instance.grabbedSurface.frictionForceLimit * __instance.grabbedSurface.dynamicMu * scale;
        }
        // PatchesHandler.Logger.LogInfo("frictionScaler = "+scale);
        // PatchesHandler.Logger.LogInfo("frictionJoint.maxForce = "+__instance.frictionJoint.maxForce);
        return false;
    }

    [HarmonyPatch("GrabActiveSurface")]
    [HarmonyPostfix]
    private static void GrabActiveSurfacePostfix(ArmScript_v2 __instance) {
        if (__instance.activeSurface != null) {
            ClimbingSurface.SurfaceType surfaceType = __instance.activeSurface.surfaceType;
            if (!CheckedSurfaces.Contains(surfaceType)) {
                CheckedSurfaces.Add(surfaceType);
                string name = __instance.activeSurface.surfaceType.ToString();
                PatchesHandler.Logger.LogInfo("Checked surface type "+name);
                ConnectionHandler.CheckLocation(name);
            }
        }
    }

}

[HarmonyPatch(typeof(FadeToWhiteScript))]
public class FadeToWhiteScriptPatch {
    
    [HarmonyPatch("FadeToWhite")]
    [HarmonyPostfix]
    private static void FadeToWhitePostfix() {
        // Fading to white seems to be a guaranteed indication for reaching the top
        PatchesHandler.Logger.LogInfo("Fading to white (is this the goal?)");
        ConnectionHandler.Goal();
    }

}

[HarmonyPatch(typeof(SideCogScript))]
public class SideCogScriptPatch {
    
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPostfix() {
        PatchesHandler.Logger.LogInfo("Started SideCogScript");
    }

    [HarmonyPatch("MoveBox")]
    [HarmonyPrefix]
    private static bool MoveBoxPrefix() {
        // Only run method if not halted
        return ConnectionHandler.SideCogHaltings <= 0;
    }

}

[HarmonyPatch(typeof(RotatingCogScript))]
public class RotatingCogScriptPatch {
    
    [HarmonyPatch("FixedUpdate")]
    [HarmonyPrefix]
    private static bool FixedUpdatePrefix(RotatingCogScript __instance) {
        // Only run method if not halted
        if (ConnectionHandler.RotatingCogHaltings <= 0) {
            int direction = (bool)Util.GetPrivateField(typeof(RotatingCogScript), __instance, "clockwise") ? -1 : 1;
            // Reverse direction if not repaired
            if (ConnectionHandler.RotatingCogRepairs == 0) direction *= -1;
            Rigidbody2D rb = (Rigidbody2D)Util.GetPrivateField(typeof(RotatingCogScript), __instance, "rb");
            rb.MoveRotation(
                rb.rotation + direction * (float)Util.GetPrivateField(typeof(RotatingCogScript), __instance, "speed") * Time.deltaTime
            );
        }
        // Replacement for original method
        return false;
    }

}

[HarmonyPatch(typeof(MetalBeamScript))]
public class MetalBeamScriptPatch {
    
    [HarmonyPatch("Start")]
    [HarmonyPostfix]
    private static void StartPostfix(MetalBeamScript __instance) {
        PatchesHandler.Logger.LogInfo("Started MetalBeamScript");
    }

    [HarmonyPatch("FixedUpdate")]
    [HarmonyPrefix]
    private static bool FixedUpdatePrefix(MetalBeamScript __instance) {
        Rigidbody2D rb = (Rigidbody2D)Util.GetPrivateField(typeof(MetalBeamScript), __instance, "rb");
        // Has to be activated first
        if (ConnectionHandler.SwingingMetalBeams > 0) {
            float jointAngle = ((HingeJoint2D)Util.GetPrivateField(typeof(MetalBeamScript), __instance, "joint")).jointAngle;
            double maxAngle = 5.5 * (1 + ConnectionHandler.MetalBeamAngleIncreases);
            if ((double)Mathf.Abs(jointAngle) > maxAngle) {
                rb.angularVelocity *= 0.99f;
            }
            // PatchesHandler.Logger.LogInfo("jointAngle "+jointAngle);
            return true;
        } else {
            rb.angularVelocity = 0f;
            return false;
        }
    }
    
}

[HarmonyPatch(typeof(SaveSystemJ))]
public class SaveSystemJPatch {

    public static string uuid;
    
    [HarmonyPatch("GetDataPath")]
    [HarmonyPostfix]
    private static void GetDataPathPostfix(ref string __result) {
        if (ConnectionHandler.Success != null) {
            __result += "-" + ConnectionHandler.Session.RoomState.Seed + "-" + ConnectionHandler.Success.Slot;
        }
    }

    [HarmonyPatch("GetSlotPath")]
    [HarmonyPostfix]
    private static void GetSlotPathPostfix(ref string __result) {
        if (ConnectionHandler.Success != null) {
            __result += "-" + ConnectionHandler.Session.RoomState.Seed + "-" + ConnectionHandler.Success.Slot;
        }
    }

}

[HarmonyPatch(typeof(PauseMenu))]
public class PauseMenuPatch {

    public static void ToggleDeafness() {
        if (ConnectionHandler.DeafnessTraps > 0) AudioListener.pause = true;
    }
    
    [HarmonyPatch("ResumeGame")]
    [HarmonyPostfix]
    private static void ResumeGamePostfix() {
        ToggleDeafness();
    }

}
