using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Extensions;

namespace wtf.tinker.Sdk.Abilities.npc_dota_hero_tinker
{
    using Ensage.SDK.Abilities.Components;

    public class tinker_rearm : RangedAbility, IHasModifier
    {
        public tinker_rearm(Ability ability)
            : base(ability)
        {
        }

        public string ModifierName { get; } = "modifier_tinker_rearm";
    }
}