using System;
using System.Collections.Generic;
using System.Text;

namespace MasteryLibrary.src.Core.Abilities
{
    public class AbilityRegistry
    {
        private readonly Dictionary<string, Ability> abilities = new Dictionary<string, Ability>();
        
        public void RegisterAbility(Ability ability)
        {
            if (ability == null || string.IsNullOrEmpty(ability.Code))
            {
                throw new ArgumentException("Ability and its code must not be null or empty.");
            }
            abilities[ability.Code] = ability;
        }

        public AbilityResult ActivateAbility(string AbilityCode, AbilityContext context)
        {
            if (!abilities.TryGetValue(AbilityCode, out Ability ability))
            {
                return AbilityResult.FailureResult($"Ability with code '{AbilityCode}' not found.");
            }
            return ability.Activate(context);
        }
    }
}
