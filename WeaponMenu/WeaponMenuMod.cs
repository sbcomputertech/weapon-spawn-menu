using HarmonyLib;
using modweaver.core;
using Unity.Netcode;
using UnityEngine;

namespace WeaponSpawnMenu
{
    [ModMainClass]
    public class WeaponMenuMod : Mod
    {
        public static ElementLists lists;
        internal List<GameObject> spawnedFromMenu = new();

        public override void Init()
        {
            var harmony = new Harmony(Metadata.id);
            harmony.PatchAll();
            Logger.Info("Weapon spawn menu is initialized!");
        }

        public override void Ready()
        {
            
        }

        public override void OnGUI(ModsMenuPopup ui)
        {
            
        }
        
        internal void DoSpawn(string enumName)
        {
            if(!Enum.TryParse<SerializationWeaponName>(enumName, out var name))
            {
                Logger.Error("Couldn't parse supplies serialization name: {}", enumName);
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
                    var spawnedWeapon = UnityEngine.Object.Instantiate(spawnable.weaponObject, health.transform.position, Quaternion.identity);
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
