using BepInEx;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace WeaponSpawnMenu
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class Main : BaseUnityPlugin
    {
        private const string ModName = "Weapon Spawner";
        private const string ModAuthor  = "reddust9";
        private const string ModGuid = "com.reddust9.weaponspawner";
        private const string ModVersion = "1.0.1";

        public static ElementLists lists;
        internal List<GameObject> spawnedFromMenu = new();

        internal bool menuOpen;
        internal Vector2 menuScroll;
        internal Vector2 menuElementSize = new(200, 50);
        internal void Awake()
        {
            var harmony = new Harmony(ModGuid);
            harmony.PatchAll();
            Logger.LogInfo($"{ModName} successfully loaded! Made by {ModAuthor}");
        }
        internal void Update()
        {
            if(Keyboard.current.equalsKey.wasPressedThisFrame)
            {
                menuOpen = !menuOpen;
            }
        }
        internal void OnGUI()
        {
            if (!menuOpen) return;
            var values = Enum.GetNames(typeof(SerializationWeaponName));
            menuScroll = GUILayout.BeginScrollView(menuScroll, GUILayout.Width(menuElementSize.x + 30));
            GUILayout.Label("Weapon Spawn Menu\nMade by reddust9 :)", GUILayout.Width(menuElementSize.x), GUILayout.Height(menuElementSize.y));
            if (GUILayout.Button("Destroy all weapons\nfrom this menu", GUILayout.Width(menuElementSize.x), GUILayout.Height(menuElementSize.y)))
            {
                foreach(var obj in spawnedFromMenu)
                {
                    Destroy(obj);
                }
            }
            GUILayout.Label("All weapons:", GUILayout.Width(menuElementSize.x), GUILayout.Height(menuElementSize.y));
            foreach (var value in values)
            {
                if (GUILayout.Button(value, GUILayout.Width(menuElementSize.x), GUILayout.Height(menuElementSize.y))) {
                    DoSpawn(value);
                }
            }
            GUILayout.EndScrollView();
        }
        internal void DoSpawn(string enumName)
        {
            if(!Enum.TryParse<SerializationWeaponName>(enumName, out var name))
            {
                Logger.LogError("Couldn't parse supplies serialization name!");
                return;
            }
            // look i know this looks bad but it's how the game actually does it normally
            foreach(var w in lists.allWeapons)
            {
                if(w.serializationWeaponName == name)
                {
                    var controllers = LobbyController.instance.GetPlayerControllers();
                    // Logger.LogInfo("Controllers length: " + controllers.Length);

                    // var health = AccessTools.FieldRefAccess<PlayerController, SpiderHealthSystem>("_spiderHealthSystem")
                    //     (controllers.First(c => c.isLocalPlayer));

                    var health = (SpiderHealthSystem) AccessTools.PropertyGetter(typeof(PlayerController), "spiderHealthSystem")
                        .Invoke(controllers.First(c => c.isLocalPlayer), Array.Empty<object>());
                    
                    // Logger.LogInfo("Health: " + health);

                    var spawnable = new SpawnableWeapon(w, 1);
                    var spawnedWeapon = Instantiate(spawnable.weaponObject, health.transform.position, Quaternion.identity);
                    spawnedFromMenu.Add(spawnedWeapon);
                    var netComp = spawnedWeapon.GetComponent<NetworkObject>();
                    netComp.Spawn(true);
                    netComp.DestroyWithScene = true;
                }
            }
        }

        [HarmonyPatch(typeof(CustomTiersScreen), "Start")]
        public static class GetListsPatch
        {
            public static void Postfix(ref CustomTiersScreen __instance)
            {
                lists = AccessTools.FieldRefAccess<CustomTiersScreen, ElementLists>("allElements")(__instance);
            }
        }
    }
}
