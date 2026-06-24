using HarmonyLib;
using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Masteries.Data;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Common;

namespace MasteryLibrary.src.Integration.GamePatches
{
    [HarmonyPatch]
    public static class GridRecipePatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GridRecipe), nameof(GridRecipe.Matches))]
        public static void MatchesPostfix(GridRecipe __instance, IPlayer forPlayer, ref bool __result)
        {
            if (!__result || forPlayer == null) return;
            if (__instance.Attributes?["requiresSkill"] == null) return;

            string? requiredSkill = __instance.Attributes["requiresSkill"].AsString();
            int requiredLevel = __instance.Attributes["requiresLevel"].AsInt(1);

            EntityBehaviorPlayerMasteries? masteries = forPlayer.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
            if (masteries == null)
            {
                __result = false;
                return;
            }

            SkillInstance? skill = masteries.PlayerMasteryData.GetSkillInstance(requiredSkill);
            if (skill == null || skill.Level < requiredLevel)
            {
                __result = false;
            }
        }

                [HarmonyPostfix]
        [HarmonyPatch(typeof(RecipeBase), nameof(RecipeBase.GenerateOutputStack))]
        public static void Postfix(RecipeBase __instance, ItemSlot[] inputSlots, ItemSlot outputSlot)
        {
            if (outputSlot.Itemstack == null) return;
            if (__instance.Attributes?["scalesWithSkill"] == null) return;
            if (__instance.Attributes?["outputScaling"] == null) return;

            IPlayer? player = GetPlayerFromSlots(inputSlots);
            if (player == null) return;

            EntityBehaviorPlayerMasteries? masteries = player.Entity
                .GetBehavior<EntityBehaviorPlayerMasteries>();
            if (masteries == null) return;

            string? requiredSkill = __instance.Attributes["scalesWithSkill"].AsString();
            StatConfiguration? statConfig = __instance.Attributes["outputScaling"].AsObject<StatConfiguration>();

            if (string.IsNullOrEmpty(requiredSkill)) return;
            SkillInstance? skill = masteries.PlayerMasteryData.GetSkillInstance(requiredSkill);
            if (skill == null || statConfig == null) return;

            int scaled = (int)StatScalingUtil.GetScaledValue(statConfig, skill.Level);
            outputSlot.Itemstack.StackSize = Math.Max(1, scaled);
        }

        private static IPlayer? GetPlayerFromSlots(ItemSlot[] slots)
        {
            foreach (var slot in slots)
            {
                if (slot?.Inventory is InventoryCraftingGrid inv)
                {
                    return inv.Player;
                }
            }
            return null;
        }
    }


    [HarmonyPatch(typeof(SlideshowGridRecipeTextComponent), MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(ICoreClientAPI), typeof(GridRecipe[]), typeof(double), typeof(EnumFloat), typeof(Action<ItemStack>), typeof(ItemStack[]) })]
    public static class SlideshowGridRecipeTextComponentPatch
    {
        [HarmonyPostfix]
        public static void Postfix(SlideshowGridRecipeTextComponent __instance, ICoreClientAPI api)
        {
            var recipes = __instance.GridRecipesAndUnnamedIngredients;
            if (recipes == null) return;

            var extraTextsField = AccessTools.Field(typeof(SlideshowGridRecipeTextComponent), "extraTexts");
            if (extraTextsField == null) return;

            var extraTexts = (Dictionary<int, LoadedTexture>)extraTextsField.GetValue(__instance);
            if (extraTexts == null) return;

            bool addedSkillHeight = false;

            for (int i = 0; i < recipes.Length; i++)
            {
                var recipe = recipes[i]?.Recipe;
                if (recipe?.Attributes?["requiresSkill"] != null)
                {
                    string? requiredSkill = recipe.Attributes["requiresSkill"].AsString();
                    int requiredLevel = recipe.Attributes["requiresLevel"].AsInt(1);

                    if (string.IsNullOrEmpty(requiredSkill)) continue;
                    Skill? skill = api.ModLoader.GetModSystem<MasteryLibraryAPI>()?.MasteryDefinitions.GetSkill(requiredSkill);

                    if (skill == null) continue;
                    string skillName = skill.GetDisplayName(requiredLevel);
                    string skillText = Lang.Get("masterylib:gridrecipe-requiresskill", skillName, requiredLevel);

                    string finalText = skillText;

                    if (recipe.RequiresTrait != null)
                    {
                        string traitText = Lang.Get("gridrecipe-requirestrait", Lang.Get("traitname-" + recipe.RequiresTrait));
                        finalText = traitText + "\n" + skillText;
                    }

                    if (extraTexts.TryGetValue(i, out LoadedTexture existingTexture))
                    {
                        existingTexture?.Dispose();
                    }

                    extraTexts[i] = api.Gui.TextTexture.GenTextTexture(finalText, CairoFont.WhiteDetailText());

                    if (!addedSkillHeight)
                    {
                        __instance.BoundsPerLine[0].Height += GuiElement.scaled(20);
                        addedSkillHeight = true;
                    }
                }
            }
        }
    }
}
