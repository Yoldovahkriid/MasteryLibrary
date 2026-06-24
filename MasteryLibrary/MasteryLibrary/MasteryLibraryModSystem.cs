using HarmonyLib;
using MasteryLibrary.src.Behaviors.CollectibleBehaviors;
using MasteryLibrary.src.Behaviors.EntityBehaviors;
using MasteryLibrary.src.Config;
using MasteryLibrary.src.Core.Abilities;
using MasteryLibrary.src.Core.Effects;
using MasteryLibrary.src.Core.Effects.DefaultEffects;
using MasteryLibrary.src.Core.Masteries.Data;
using MasteryLibrary.src.Core.Masteries.Instances;
using MasteryLibrary.src.Networking;
using MasteryLibrary.src.Networking.Client;
using MasteryLibrary.src.Networking.Server;
using MasteryLibrary.src.UI.Components;
using MasteryLibrary.src.UI.Windows;
using MasteryLibrary.src.Utilities;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using VSImGui;

namespace MasteryLibrary
{
    public delegate void MasterySkillEventHandler(MasteryInstance mastery, SkillInstance skill);
    public delegate void PlayerSkillEventHandler(IServerPlayer player, Skill skill);
    public delegate void EntityEffectEventHandler(EntityAgent entity, EffectInstance effect);

    public class MasteryLibraryAPI : ModSystem
    {
        public event MasterySkillEventHandler OnSkillUnlocked;
        public event MasterySkillEventHandler OnSkillLeveledUp;
        public event PlayerSkillEventHandler OnUniqueSkillAquired;
        public event EntityEffectEventHandler OnEffectApplied;
        public MasteryDefinitions MasteryDefinitions { get; private set; }
        public EffectRegistry EffectRegistry { get; private set; }
        public AbilityRegistry AbilityRegistry { get; private set; }
        public INetworkService? NetworkService { get; private set; }
        private MasteryGUIWindow MasteryWindow { get; set; }
        private AbilityHotbarGUI AbilityHotbar { get; set; }
        private UICustomizationWindow CustomisationWindow { get; set; }
        private Harmony? harmony;

        public override void Start(ICoreAPI api)
        {
            MasteryDefinitions = new MasteryDefinitions(api);
            EffectRegistry = new EffectRegistry();
            AbilityRegistry = new AbilityRegistry();

            this.EffectRegistry.RegisterEffect(new StatBoostEffect());

            api.RegisterEntityBehaviorClass("PlayerMasteries", typeof(EntityBehaviorPlayerMasteries));
            api.RegisterEntityBehaviorClass("EntityEffects", typeof(EntityBehaviorEffects));
            api.RegisterCollectibleBehaviorClass("CanInflictEffects", typeof(CanInflictEffects));

            if (!Harmony.HasAnyPatches(Mod.Info.ModID))
            {
                harmony = new Harmony(Mod.Info.ModID);
                harmony.PatchAllUncategorized();
            }
        }
        public void RaiseSkillUnlocked(MasteryInstance mastery, SkillInstance skill)
        {
            OnSkillUnlocked?.Invoke(mastery, skill);
        }
        public void RaiseSkillLeveledUp(MasteryInstance mastery, SkillInstance skill)
        {
            OnSkillLeveledUp?.Invoke(mastery, skill);
        }

        public void RaiseUniqueSkillAquired(IServerPlayer player, Skill skill)
        {
            OnUniqueSkillAquired?.Invoke(player, skill);
        }
        public void RaiseEffectApplied(EntityAgent entity, EffectInstance effect)
        {
            OnEffectApplied?.Invoke(entity, effect);
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            string[] targetTags = { "weapon-melee", "weapon-sword", "tool-sword" };
            api.CollectibleTagRegistry.TryCreateTagSet(out TagSet weaponTagset, targetTags);

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj?.Tags == null || !obj.Tags.Overlaps(weaponTagset)) continue;

                if (obj.HasBehavior<CanInflictEffects>()) continue;

                obj.CollectibleBehaviors = obj.CollectibleBehaviors.Append(new CanInflictEffects(obj)).ToArray<CollectibleBehavior>();
            }
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            base.StartServerSide(api);

            NetworkService = new NetworkServiceServer(api);

            api.ChatCommands.Create("mastery")
                .RequiresPrivilege(Privilege.chat)
                .BeginSubCommand("giveexp")
                .RequiresPrivilege(Privilege.commandplayer)
                .WithArgs(api.ChatCommands.Parsers.OnlinePlayer("Player"), api.ChatCommands.Parsers.Float("Amount"))
                .HandleWith((TextCommandCallingArgs args) =>
                {
                    if (args[0] is not IServerPlayer serverPlayer)
                        return TextCommandResult.Error("Player is unable to be refferenced");

                    var behavior = serverPlayer.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
                    if (behavior == null)
                        return TextCommandResult.Error("Player does not have mastery data");

                    float amount = (float)args[1];
                    behavior.PlayerMasteryData.GainExperience(amount);
                    return TextCommandResult.Success($"Successfully awarded {serverPlayer.PlayerName} {amount} exp!");
                })
                .EndSubCommand();

            api.Event.PlayerNowPlaying += (IServerPlayer player) =>
            {
                (this.NetworkService as NetworkServiceServer)?.SendMasterySyncPacket(player);
                EntityBehaviorPlayerMasteries? behavior = player.Entity?.GetBehavior<EntityBehaviorPlayerMasteries>();
                PassiveStatUpdater.UpdatePassivePlayerStats(behavior);
            };

            api.Event.OnEntitySpawn += (entity) =>
            {
                if (entity is EntityAgent)
                {
                    var behavior = new EntityBehaviorEffects(entity);
                    entity.AddBehavior(behavior);
                    behavior.Initialize(entity.Properties, entity.Properties.Attributes);
                }
            };

            api.Event.OnEntityLoaded += (entity) =>
            {
                if (entity is EntityAgent && !entity.HasBehavior<EntityBehaviorEffects>())
                {
                    var behavior = new EntityBehaviorEffects(entity);
                    entity.AddBehavior(behavior);
                    behavior.Initialize(entity.Properties, entity.Properties.Attributes);
                }
            };

            api.Event.OnEntityDeath += (entity, source) =>
            {
                if (entity is EntityAgent && entity.HasBehavior<EntityBehaviorEffects>())
                {
                    entity.GetBehavior<EntityBehaviorEffects>()?.EffectManager.ClearAllEffects();
                }
            };
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            NetworkService = new NetworkServiceClient(api);

            MasteryWindow = new MasteryGUIWindow(api, this.NetworkService as NetworkServiceClient);
            CustomisationWindow = new UICustomizationWindow(api);
            AbilityHotbar = new AbilityHotbarGUI(api, this.NetworkService as NetworkServiceClient);

            ImGuiModSystem? guiModSystem = api.ModLoader.GetModSystem<ImGuiModSystem>();
            if (guiModSystem != null)
            {
                guiModSystem.Draw += MasteryWindow.Draw;
                guiModSystem.Draw += AbilityHotbar.Draw;
                guiModSystem.Draw += CustomisationWindow.Draw;
            }

            api.Input.RegisterHotKey("masterygui", Lang.Get("masterylib:hotkey-open-mastery-menu"), GlKeys.K, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("masterygui", (a) =>
            {
                (NetworkService as NetworkServiceClient)?.RequestMasteryState();
                MasteryWindow.Toggle();
                return true;
            });

            api.Input.RegisterHotKey("masteryuicustomize", Lang.Get("masterylib:hotkey-ui-customize"), GlKeys.U, HotkeyType.GUIOrOtherControls);
            api.Input.SetHotKeyHandler("masteryuicustomize", (a) =>
            {
                CustomisationWindow?.Toggle();
                return true;
            });

            api.Input.RegisterHotKey(
                "masterylib-hotbar-toggle",
                Lang.Get("masterylib:hotkey-toggle-hotbar", "Toggle Hotbar Visibility"),
                GlKeys.H,
                HotkeyType.GUIOrOtherControls
            );
            api.Input.SetHotKeyHandler("masterylib-hotbar-toggle", (a) =>
            {
                AbilityHotbar?.ToggleVisibility();
                return true;
            });

            for (int i = 0; i < MasteryLibConfigCommon.Loaded.MaxEquippedActiveSkills; i++)
            {
                int slot = i;
                string hotkeyName = $"masterylib-skill-{slot}";
                GlKeys defaultKey = GlKeys.Unknown;

                api.Input.RegisterHotKey(hotkeyName, Lang.Get("masterylib:hotkey-use-active-skill", slot + 1), defaultKey, HotkeyType.CharacterControls);
                api.Input.SetHotKeyHandler(hotkeyName, (a) =>
                {
                    api.Logger.Debug("Pressed hotkey for active skill in slot {0}", slot);
                    (NetworkService as NetworkServiceClient)?.RequestSkillActivation(slot);
                    return true;
                });
            }
        }

        public override void Dispose()
        {
            harmony?.UnpatchAll($"{Mod.Info.ModID}");
        }
    }
}
