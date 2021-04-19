using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using JotunnLib.Managers;
using JotunnLib.Entities;
using JotunnLib.Utils;

namespace Soravyn.CarryWeight
{
    [BepInPlugin("soravyn.CarryWeight", "CarryWeight", "0.1.0")]
    [BepInDependency(JotunnLib.JotunnLib.ModGuid)]
    [BepInProcess("valheim.exe")]
    public class CarryWeight : BaseUnityPlugin
    {
        public readonly Harmony harmony = new Harmony("soravyn.CarryWeight");
        private static ConfigEntry<float> cwGainPerLevel;
        private static ConfigEntry<float> skillThreshold;
        private static ConfigEntry<float> runningMultiplier;
        private static ConfigEntry<float> swimmingMultiplier;

        public static Skills.SkillType CWSkill = 0;

        void Awake()
        {
            cwGainPerLevel = Config.Bind<float>("General", "CarryWeightGainedPerLevel", 3.0f, "Carry Weight increase per skill level");
            skillThreshold = Config.Bind<float>("General", "CarryWeightSkillThreshold", .8f, "Percent of max weight needed to be carrying in order to have skill gain xp. By default player needs to be carrying > 80% of their max capacity to gain xp. Set to 0 to remove threshold");
            runningMultiplier = Config.Bind<float>("General", "RunningMultiplier", .4f, "How fast the skill will gain xp while running. \n For reference, the running skill has a multiplier of 0.2, \n meaning carry weight levels up twice as fast as the running skill");
            swimmingMultiplier = Config.Bind<float>("General", "SwimmingMultiplier", .9f, "How fast the skill will gain xp while swimming. \n For reference, the swimming skill has a multiplier of 0.3, \n meaning carry weight levels up 3x as fast as the swimming skill");

            harmony.PatchAll();

            CWSkill = SkillManager.Instance.RegisterSkill(new SkillConfig()
            {
                Identifier = "CWSkill",
                Name = "Carry Weight",
                Description = "Increases the amount you can carry",
                Icon = loadSprite("weight_icon_32.png"),
                IncreaseStep = 1f
            });
            
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
        static Sprite loadSprite(string filename)
        {
            Texture2D texture = AssetUtils.LoadTexture(filename);
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), Vector2.zero);
        }

        [HarmonyPatch(typeof(Player), "RaiseSkill")]
        class RaiseSkill_Patch
        {
            static void Postfix(Skills.SkillType skill, ref Player __instance)
            {
                
                if (skill == Skills.SkillType.Run && raiseSkillCondition(__instance))
                {
                    __instance.RaiseSkill(CWSkill, runningMultiplier.Value);
                }
                if (skill == Skills.SkillType.Swim && raiseSkillCondition(__instance))
                {
                    __instance.RaiseSkill(CWSkill, swimmingMultiplier.Value);
                }
            }

            static bool raiseSkillCondition(Player player)
            {
                Inventory inv = player.GetInventory();
                return inv.GetTotalWeight() > (player.GetMaxCarryWeight() * skillThreshold.Value);
            }
        }

        [HarmonyPatch(typeof(Player), "GetMaxCarryWeight")]
        class MaxCarry_Patch
        {
            static void Postfix(ref float __result, ref Player __instance)
            {
                float cwSkill = __instance.GetSkills().GetSkillFactor(CWSkill) * 100;
                __result = __result + (cwSkill * cwGainPerLevel.Value);
            }
        }
    }
}
