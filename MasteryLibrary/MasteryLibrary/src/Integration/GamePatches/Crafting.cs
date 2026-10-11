using HarmonyLib;
using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Core.Masteries.Data;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private sealed class SkillRequirement
        {
            public string? RequiredSkill;
            public int RequiredLevel;

            public string? ScalesWithSkill;
            public StatConfiguration? OutputScaling;
        }

        private static readonly SkillRequirement None = new();
        private static readonly ConditionalWeakTable<RecipeBase, SkillRequirement> Cache = new();

        private static SkillRequirement GetRequirement(RecipeBase recipe)
        {
            if (Cache.TryGetValue(recipe, out var cached)) return cached;

            var attrs = recipe.Attributes;
            SkillRequirement result = None;

            if (attrs != null && attrs.Exists)
            {
                string? req = attrs["requiresSkill"]?.AsString();
                string? scales = attrs["scalesWithSkill"]?.AsString();

                if (!string.IsNullOrEmpty(req) || !string.IsNullOrEmpty(scales))
                {
                    result = new SkillRequirement
                    {
                        RequiredSkill = string.IsNullOrEmpty(req) ? null : req,
                        RequiredLevel = attrs["requiresLevel"]?.AsInt(1) ?? 1,
                        ScalesWithSkill = string.IsNullOrEmpty(scales) ? null : scales,
                        OutputScaling = string.IsNullOrEmpty(scales)
                            ? null
                            : attrs["outputScaling"]?.AsObject<StatConfiguration>()
                    };
                }
            }

            Cache.Add(recipe, result);
            return result;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GridRecipe), nameof(GridRecipe.Matches))]
        public static void MatchesPostfix(GridRecipe __instance, IPlayer forPlayer, ref bool __result)
        {
            if (!__result || forPlayer == null) return;

            var req = GetRequirement(__instance);
            if (req.RequiredSkill == null) return;

            var masteries = forPlayer.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
            var skill = masteries?.PlayerMasteryData.GetSkillInstance(req.RequiredSkill);

            if (skill == null || skill.Level < req.RequiredLevel)
                __result = false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GridRecipe), nameof(GridRecipe.GenerateOutputStack))]
        public static void Postfix(GridRecipe __instance, ItemSlot[] inputSlots, ItemSlot outputSlot)
        {
            if (outputSlot.Itemstack == null) return;

            var req = GetRequirement(__instance);
            if (req.ScalesWithSkill == null || req.OutputScaling == null) return;

            var player = GetPlayerFromSlots(inputSlots);
            var masteries = player?.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
            var skill = masteries?.PlayerMasteryData.GetSkillInstance(req.ScalesWithSkill);
            if (skill == null) return;

            int scaled = (int)StatScalingUtil.GetScaledValue(req.OutputScaling, skill.Level);
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
