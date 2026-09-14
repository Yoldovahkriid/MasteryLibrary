using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Server;

namespace MasteryLibrary.src.Core.Abilities
{
    public class AbilityContext
    {
        public IServerPlayer Player { get; set; }
        public int Level { get; set; }
        public Dictionary<string, object> CustomData { get; set; } = new Dictionary<string, object>();
        public string Source { get; set; } = "unknown";
        public ICoreServerAPI API { get; set; }

        public AbilityContext(IServerPlayer player, int level, ICoreServerAPI api, string source = "unknown", Dictionary<string, object> customData = null)
        {
            Player = player;
            Level = level;
            API = api;
            Source = source;
            CustomData = customData ?? new Dictionary<string, object>();
        }
        public T? GetValueSafe<T>(string key)
        {
            if (CustomData.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return default;
        }
    }

    public class AbilityResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public object Data { get; set; }
        public bool? TriggerCooldown { get; set; }

        public AbilityResult(bool success, string message = "", object data = null, bool? triggerCooldown = null)
        {
            Success = success;
            Message = message;
            Data = data;
            TriggerCooldown = triggerCooldown;
        }
        public static AbilityResult SuccessResult(string message = "", object data = null, bool? triggerCooldown = null) => new AbilityResult(true, message, data, triggerCooldown);
        public static AbilityResult FailureResult(string message = "", object data = null, bool? triggerCooldown = null) => new AbilityResult(false, message, data, triggerCooldown);
    }

    public abstract class Ability
    {
        public abstract string Code { get; }

        public virtual AbilityResult CanUse(AbilityContext context)
        {
            return AbilityResult.SuccessResult("Ability can be used.");
        }

        public abstract AbilityResult Execute(AbilityContext context);

        public virtual void OnExecuteComplete(AbilityContext context, AbilityResult result)
        {
        }

        public AbilityResult Activate(AbilityContext context)
        {
            AbilityResult ValidationResult = CanUse(context);
            if (!ValidationResult.Success)
            {
                return ValidationResult;
            }

            AbilityResult ExecuteResult = Execute(context);

            if (ExecuteResult.Success)
            {
                OnExecuteComplete(context, ExecuteResult);
            }

            return ExecuteResult;
        }
    }
}