using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Prediction;

namespace wtf.tinker.Sdk.Abilities.npc_dota_hero_tinker
{
    using System.Linq;

    using Ensage.SDK.Abilities.Components;
    using Ensage.SDK.Extensions;
    //有
    public class tinker_laser : RangedAbility, IHasTargetModifier
    {
        public tinker_laser(Ability ability)
            : base(ability)
        {
        }
        //致盲效果
        public string TargetModifierName { get; } = "modifier_tinker_laser_blind";


        //纯粹伤害
        public override DamageType DamageType
        {
            get
            {
                return DamageType.Pure;
            }
        }

        protected override float RawDamage
        {
            get
            {
                var damage = this.Ability.GetAbilitySpecialData( "laser_damage");
                var talent = this.Owner.GetAbilityById(AbilityId.special_bonus_unique_tinker);
                if (talent?.Level > 0)
                {
                    damage += talent.GetAbilitySpecialData("value");
                }

                return damage;
            }
        }
    }
}
