using MasteryLibrary.src.Core.Masteries.Instances;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;

namespace MasteryLibrary.src.Behaviors.EntityBehaviors
{
    public class EntityBehaviorPlayerMasteries : EntityBehavior
    {
        public PlayerMasteryData PlayerMasteryData;
        private MasteryLibraryAPI MasteryLibAPI;
        private ICoreAPI Api;

        public EntityBehaviorPlayerMasteries(Entity entity) : base(entity)
        {
            PlayerMasteryData = new PlayerMasteryData(entity.Api);
            MasteryLibAPI = entity.Api.ModLoader.GetModSystem<MasteryLibraryAPI>();
            Api = entity.Api;
        }

        public override string PropertyName()
        {
            return "PlayerMasteries";
        }

        public override void Initialize(EntityProperties properties, JsonObject attributes)
        {
            base.Initialize(properties, attributes);

            if (entity.Api.Side == EnumAppSide.Server)
            {
                LoadData();

                PlayerMasteryData.OnDataChanged += SaveData;
            }
        }

        public override void OnEntityDespawn(EntityDespawnData despawn)
        {
            base.OnEntityDespawn(despawn); 
            if (entity.Api.Side == EnumAppSide.Server)
            {
                SaveData();
                PlayerMasteryData.OnDataChanged -= SaveData;
            }
        }

        private void SaveData()
        {
            entity.Attributes.SetString($"{MasteryLibAPI.Mod.Info.ModID}-PlayerMasteryData", JsonSerializer.Serialize(PlayerMasteryData.ToState()));
        }

        private void LoadData()
        {
            string json = entity.Attributes.GetString($"{MasteryLibAPI.Mod.Info.ModID}-PlayerMasteryData", string.Empty);
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    var state = JsonSerializer.Deserialize<PlayerMasteryState>(json);
                    if (state != null)
                    {
                        PlayerMasteryData.FromState(state);
                    }
                } catch(Exception e) 
                { 
                   Api.Logger.Error($"[{MasteryLibAPI.Mod.Info.ModID}]Failed to load player mastery data: {e.Message}");
                }
            }
        }
    }
}
