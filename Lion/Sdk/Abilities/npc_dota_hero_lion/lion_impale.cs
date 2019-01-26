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

    public class lion_impale : LineAbility, IHasTargetModifier
    {
        public lion_impale(Ability ability)
            : base(ability)
        {
        }

        public override float Radius => 325f+this.Ability.GetAbilitySpecialData("width");

        public override float Range => this.CastRange;

        public string TargetModifierName { get; } = "modifier_lion_impale";

        public override float CastRange
        {
            get
            {
                var bonusRange = 0.0f;

                var talent = this.Owner.Spellbook.Spells.FirstOrDefault(x => x.Level > 0 && x.Name.StartsWith("special_bonus_cast_range_"));
                if (talent != null)
                {
                    bonusRange += talent.GetAbilitySpecialData("value");
                }

                var aetherLens = this.Owner.GetItemById(AbilityId.item_aether_lens);
                if (aetherLens != null)
                {
                    bonusRange += aetherLens.GetAbilitySpecialData("cast_range_bonus");
                }
                return this.BaseCastRange + bonusRange;
            }
        }

        protected override float BaseCastRange
        {
            get
            {
                return this.Ability.CastRange;
            }
        }
    }
}
