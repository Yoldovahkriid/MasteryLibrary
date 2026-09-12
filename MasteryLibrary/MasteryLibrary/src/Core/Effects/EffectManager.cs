using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MasteryLibrary.src.Core.Effects
{
    public class EffectManager
    {
        private readonly EntityAgent entity;
        private readonly ICoreAPI api;
        private readonly Dictionary<string, EffectInstance> activeEffects = new();
        private long listenerId;
        private MasteryLibraryAPI MasteryApi;

        public EffectManager(EntityAgent entity, ICoreAPI api) { this.entity = entity; this.api = api; this.MasteryApi = api.ModLoader.GetModSystem<MasteryLibraryAPI>(); }

        public void AddEffect(EffectInstance incoming)
        {
            incoming.AppliedTime = api.World.ElapsedMilliseconds;
            var effect = incoming.Effect;

            if (!effect.CanStack)
            {
                var existingKvp = activeEffects.FirstOrDefault(kvp => kvp.Value.Effect.Code == effect.Code);
                if (!existingKvp.Equals(default(KeyValuePair<string, EffectInstance>)))
                {
                    var result = effect.OnStack(existingKvp.Value, incoming);
                    RemoveEffectInstance(existingKvp.Key, RemovalReason.Replaced);
                    AddInstanceInternal(result);
                    return;
                }
            }

            AddInstanceInternal(incoming);
        }

        private void AddInstanceInternal(EffectInstance instance)
        {
            string id = $"{instance.Effect.Code}_{Guid.NewGuid()}";
            activeEffects.Add(id, instance);
            instance.Effect.OnApply(instance, entity);
            MasteryApi.Events.RaiseEffectApplied(entity, instance);

            if (listenerId == 0) listenerId = api.Event.RegisterGameTickListener(OnTick, 250);
        }

        private void OnTick(float dt)
        {
            long dtMs = (long)(dt * 1000);
            var expired = new List<string>();

            foreach (var kvp in activeEffects)
            {
                kvp.Value.Duration -= dtMs;
                kvp.Value.Effect.OnTick(kvp.Value, entity, dt);
                if (kvp.Value.Duration <= 0) expired.Add(kvp.Key);
            }

            foreach (var key in expired) RemoveEffectInstance(key, RemovalReason.Expired);
            if (activeEffects.Count == 0 && listenerId != 0) { api.Event.UnregisterGameTickListener(listenerId); listenerId = 0; }
        }

        public void RemoveEffect(string code, RemovalReason reason = RemovalReason.Dispelled)
        {
            var targets = activeEffects.Where(k => k.Value.Effect.Code == code).Select(k => k.Key).ToList();
            targets.ForEach(k => RemoveEffectInstance(k, reason));
        }

        private void RemoveEffectInstance(string key, RemovalReason reason)
        {
            if (activeEffects.Remove(key, out var instance))
            {
                instance.Effect.OnRemove(instance, entity, reason);
                MasteryApi.Events.RaiseEffectRemoved(entity, instance);
            }
        }

        public bool HasEffect(string effectcode)
        {
            foreach (var effect in activeEffects.Values)
            {
                if (effect.Effect.Code == effectcode) return true;
            }
            return false;
        }

        public List<EffectInstance> GetActiveEffects()
        {
            return activeEffects.Values.ToList();
        }

        public void ClearAllEffects(RemovalReason reason = RemovalReason.Death)
        {
            var keys = activeEffects.Keys.ToList();

            foreach (var key in keys)
            {
                RemoveEffectInstance(key, reason);
            }

            if (activeEffects.Count == 0 && listenerId != 0)
            {
                api.Event.UnregisterGameTickListener(listenerId);
                listenerId = 0;
            }
        }

        public List<EffectState> ToState()
        {
            return activeEffects.Values.Select(inst => new EffectState
            {
                Effect = inst.Effect.Code,
                Source = inst.Source,
                AppliedTime = inst.AppliedTime,
                Duration = inst.Duration,
                Magnitude = inst.Magnitude,
                CustomData = inst.CustomData
            }).ToList();
        }

        public void FromState(List<EffectState> states)
        {
            EffectRegistry registry = MasteryApi.EffectRegistry;
            foreach (var state in states)
            {
                var effect = registry.GetEffect(state.Effect);
                if (effect == null) continue;

                var instance = new EffectInstance(effect, state.Source, state.Duration, state.Magnitude)
                {
                    AppliedTime = state.AppliedTime
                };

                foreach (var kvp in state.CustomData)
                {
                    instance.CustomData[kvp.Key] = kvp.Value;
                }

                string id = $"{instance.Effect.Code}_{Guid.NewGuid()}";
                activeEffects.Add(id, instance);

                instance.Effect.OnApply(instance, entity);
            }

            if (activeEffects.Count > 0 && listenerId == 0)
            {
                listenerId = api.Event.RegisterGameTickListener(OnTick, 250);
            }
        }
    }
}