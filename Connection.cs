
using System;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using BepInEx.Logging;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ConnectionHandler {

    public static ArchipelagoSession Session;
    public static LoginSuccessful Success;
    public static ManualLogSource Logger;
    public static int GripStrengths = 0;
    public static int SwingingMetalBeams = 0;
    public static int MetalBeamAngleIncreases = 0;
    public static int DeafnessTraps = 0;
    public static int RotatingCogRepairs = 0;
    public static int RotatingCogHaltings = 0;
    public static int SideCogHaltings = 0;

    public static void Connect(ManualLogSource logger) {

        Logger = logger;

        JArray list = JArray.Parse(ConfigHandler.ConnectionsList.Value);
        int active = ConfigHandler.ActiveSlot.Value;
        if (active < -1) {
            Logger.LogWarning($"Illegal ActiveSlot value: {active}");
            ProcessOfflineList();
            return;
        } else if (active >= list.Count) {
            Logger.LogWarning($"ActiveSlot value out of range: {active}");
            ProcessOfflineList();
            return;
        } else if (active == -1) {
            Logger.LogInfo("Connecting disabled as per config");
            ProcessOfflineList();
            return;
        }
        JToken activeList = list[active];
        string activeAddressPort = activeList.Value<string>("addressPort") ?? "localhost:38281";
        string activeSlot = activeList.Value<string>("slot") ?? "Player1";
        string activePassword = activeList.Value<string>("password") ?? "";

        Session = ArchipelagoSessionFactory.CreateSession(activeAddressPort);
        LoginResult result;
        try {
            result = Session.TryConnectAndLogin(
                "A Difficult Game About Climbing", activeSlot, ItemsHandlingFlags.AllItems, password: activePassword
            );
        } catch (Exception e) {
            result = new LoginFailure(e.GetBaseException().Message);
        }

        if (!result.Successful) {
            LoginFailure failure = (LoginFailure)result;
            string errorMessage = $"Failed to Connect to {activeAddressPort} as {activeSlot}:";
            foreach (string error in failure.Errors) {
                errorMessage += $"\n    {error}";
            }
            foreach (ConnectionRefusedError error in failure.ErrorCodes) {
                errorMessage += $"\n    {error}";
            }
            logger.LogError(errorMessage);
        } else {
            Success = (LoginSuccessful)result;
            logger.LogInfo("Connection successful");
            Session.Items.ItemReceived += (ReceivedItemsHelper receivedItemsHelper) => {
                ProcessReceived(receivedItemsHelper.DequeueItem());
            };
            Session.MessageLog.OnMessageReceived += (LogMessage message) => {
                logger.LogInfo(message.ToString());
            };
        }

    }

    public static void ProcessOfflineList() {
        
        Logger.LogInfo("Processing offline inventory...");
        JObject inv = JObject.Parse(ConfigHandler.OfflineItems.Value);
        GripStrengths = inv.Value<int?>("Grip Strength") ?? 3;
        SwingingMetalBeams = inv.Value<int?>("Swinging Metal Beam") ?? 1;
        MetalBeamAngleIncreases = inv.Value<int?>("Metal Beam Angle Increase") ?? 2;
        DeafnessTraps = inv.Value<int?>("Deafness Trap") ?? 0;
        RotatingCogRepairs = inv.Value<int?>("Rotating Cog Repair") ?? 1;
        RotatingCogHaltings = inv.Value<int?>("Rotating Cog Halting") ?? 0;
        SideCogHaltings = inv.Value<int?>("Side Cog Halting") ?? 0;
        PauseMenuPatch.ToggleDeafness();
        Logger.LogInfo(
            $"[OfflineList] GripStrengths: {GripStrengths}, SwingingMetalBeams: {SwingingMetalBeams}, "+
            $"MetalBeamAngleIncreases: {MetalBeamAngleIncreases}, DeafnessTraps: {DeafnessTraps}, "+
            $"RotatingCogRepairs: {RotatingCogRepairs}, RotatingCogHaltings: {RotatingCogHaltings}, SideCogHaltings: {SideCogHaltings}"
        );

    }

    public static void ProcessReceived(ItemInfo itemInfo) {
        
        switch (itemInfo.ItemName) {
            case "Grip Strength":
                GripStrengths++;
                break;
            case "Swinging Metal Beam":
                SwingingMetalBeams++;
                break;
            case "Metal Beam Angle Increase":
                MetalBeamAngleIncreases++;
                break;
            case "Deafness Trap":
                DeafnessTraps++;
                PauseMenuPatch.ToggleDeafness();
                break;
            case "Rotating Cog Repair":
                RotatingCogRepairs++;
                break;
            case "Rotating Cog Halting":
                RotatingCogHaltings++;
                break;
            case "Side Cog Halting":
                SideCogHaltings++;
                break;
            default:
                Logger.LogError("Unknown item "+itemInfo.ItemName);
                return;
        }
        Logger.LogInfo($"Received {itemInfo.ItemName}");

    }

    public static void CheckLocation(string name) {
        if (Success == null) return;
        CheckLocation(Session.Locations.GetLocationIdFromName("A Difficult Game About Climbing", name), name);
    }

    public static void CheckLocation(long id, string name = null) {
        if (Success == null) return;
        Logger.LogInfo($"Checking location {(name == null ? id : name)}");
        Session.Locations.CompleteLocationChecks([id]);
    }

    public static void Goal() {
        if (Success == null) return;
        StatusUpdatePacket packet = new StatusUpdatePacket();
        packet.Status = ArchipelagoClientState.ClientGoal;
        Session.Socket.SendPacket(packet);
    }

}
