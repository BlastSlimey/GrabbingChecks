
using BepInEx.Configuration;

public class ConfigHandler {

    public static ConfigEntry<string> ConnectionsList;
    public static ConfigEntry<int> ActiveSlot;
    public static ConfigEntry<string> OfflineItems;

    public static void InitConfig(ConfigFile Config) {
        ConnectionsList = Config.Bind(
            "General", "ConnectionList", 
            "[{'slot':'SlotName', 'addressPort':'archipelago.gg:38281', 'password':'psswrd123'}, "+
            "{'slot':'AnotherPlayer', 'addressPort':'your.APServer.com', 'password':'top.scrt'}, "+
            "{'slot':'Player1', 'addressPort':'localhost:38281'}]", 
            "A list of connection details, so you don't have to re-enter them every time you play another slot. " + 
            "Has to be formatted as a JSON string."
        );
        ActiveSlot = Config.Bind(
            "General", "ActiveSlot", -1, 
            "The ConnectionList entry to use for connecting to a sever. Begins with 0 as the first entry. Use -1 to disable connecting."
        );
        OfflineItems = Config.Bind(
            "General", "OfflineItems",
            "{'Grip Strength':3, 'Swinging Metal Beam':1, 'Metal Beam Angle Increase':2, 'Deafness Trap':0, 'Rotating Cog Repair':1, "+
            "'Rotating Cog Halting':0, 'Side Cog Halting':0}",
            "Define a set inventory that should be used if the game is not connected to any Archipelago server. " + 
            "Has to be formatted as a JSON string."
        );
    }

}
