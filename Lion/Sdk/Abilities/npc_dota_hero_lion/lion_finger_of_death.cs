using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.SDK.Abilities;

namespace wtf.lion.Sdk.Abilities.npc_dota_hero_lion
{
    using System.Linq;

    using Ensage.SDK.Abilities.Components;
    using Ensage.SDK.Extensions;

    public class lion_finger_of_death : RangedAbility, IHasTargetModifier, IAreaOfEffectAbility, IHasModifier
    {
        public lion_finger_of_death(Ability ability)
            : base(ability)
        {
        }

        public string TargetModifierName { get; } = "modifier_lion_finger_of_death";
        public string TargetDelayModifierName { get; } = "modifier_lion_finger_of_death_delay";


        private float TalentDamage
        {
            get
            {
                var dmgValue = 0f;
                var talent = this.Owner.GetAbilityById(AbilityId.special_bonus_unique_lion_3);
                if (talent != null && talent.Level > 0)
                {
                     dmgValue = talent.GetAbilitySpecialData("value");
                }

                return dmgValue;
            }
        }

        protected float BaseDamage
        {
            get
            {
                if (this.Owner.HasAghanimsScepter())
                {
                    return this.Ability.GetAbilitySpecialData("damage_scepter");
                }

                return this.Ability.GetAbilitySpecialData("damage");
            }
        }

        protected override float RawDamage => BaseDamage+ TalentDamage + ExtraDamage;

        public float KillCounter
        {
            get
            {
                var modifier = this.Owner.GetModifierByName(this.ModifierName);
                return modifier?.StackCount ?? 0;
            }
        }

        public float ExtraDamage
        {
            get
            {
                return KillCounter * this.Ability.GetAbilitySpecialData("damage_per_kill");
            }
        }

        public float Radius
        {
            get
            {
                return this.Ability.GetAbilitySpecialData("splash_radius_scepter");
            }
        }

        public string ModifierName { get; } = "modifier_lion_finger_of_death_kill_counter";
    }
}
