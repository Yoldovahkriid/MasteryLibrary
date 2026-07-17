using MasteryLibrary.src.Core.Effects;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace MasteryLibrary.src.Behaviors.EntityBehaviors
{
    public class EntityBehaviorEffects: EntityBehavior
    {
        public EffectManager EffectManager { get; private set; }
        public EntityBehaviorEffects(Entity entity) : base(entity)
        {
            if (entity is EntityAgent agent)
            {
                this.EffectManager = new EffectManager(agent, agent.Api);
            }
        }
        public override string PropertyName() => "EntityEffects";

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            if (entity.Api.Side == EnumAppSide.Server)
            {
                LoadData();
            }
        }
        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            base.OnEntityDespawn(despawn);
            if (entity.Api.Side == EnumAppSide.Server)
            {
                SaveData();
            }
        }

        private void SaveData()
        {
            if (EffectManager == null) return;

            var state = EffectManager.ToState();
            string json = JsonSerializer.Serialize(state);
            entity.Attributes.SetString($"{entity.Api.ModLoader.GetModSystem<MasteryLibraryAPI>()?.Mod.Info.ModID}-EntityEffects", json);
        }

        private void LoadData()
        {
            if (EffectManager == null) return;
            string json = entity.Attributes.GetString($"{entity.Api.ModLoader.GetModSystem<MasteryLibraryAPI>()?.Mod.Info.ModID}-EntityEffects", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var states = JsonSerializer.Deserialize<List<EffectState>>(json);
                    if (states != null)
                    {
                        EffectManager.FromState(states);
                    }
                }
                catch (Exception e)
                {
                    entity.Api.Logger.Error($"[{entity.Api.ModLoader.GetModSystem<MasteryLibraryAPI>()?.Mod.Info.ModID}] Failed to load entity effect data: {e.Message}");
                }
            }
        }
    }
}
