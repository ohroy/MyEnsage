using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.SDK.Abilities;

namespace wtf.tinker.Sdk.Abilities.npc_dota_hero_tinker
{
    using System.Linq;

    using Ensage.SDK.Abilities.Components;
    using Ensage.SDK.Extensions;

    public class tinker_heat_seeking_missile : RangedAbility
    {
        public tinker_heat_seeking_missile(Ability ability)
            : base(ability)
        {
        }

        public override DamageType DamageType
        {
            get
            {
                return DamageType.Magical;
            }
        }
        protected override float RawDamage
        {
            get
            {
                var damage = this.Ability.GetAbilitySpecialData("damage");
                return damage;
            }
        }
        public override float Speed
        {
            get
            {
                return this.Ability.GetAbilitySpecialData("speed");
            }
        }
    }
}
