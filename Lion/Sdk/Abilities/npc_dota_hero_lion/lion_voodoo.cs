using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Extensions;

namespace wtf.lion.Sdk.Abilities.npc_dota_hero_lion
{
    using Ensage.SDK.Abilities.Components;

    public class lion_voodoo : RangedAbility, IHasTargetModifier
    {
        public lion_voodoo(Ability ability)
            : base(ability)
        {
        }

        public string TargetModifierName { get; } = "modifier_lion_voodoo";

        public virtual bool UseAbility(Unit target)
        {
            if (!this.CanBeCasted)
            {
                return false;
            }

            bool result;
            var talent = this.Owner.GetAbilityById(AbilityId.special_bonus_unique_lion_4);
            if (talent != null && talent.Level > 0)
            {
                result = this.Ability.UseAbility(target.NetworkPosition);
            }
            else if ((this.Ability.AbilityBehavior & AbilityBehavior.UnitTarget) == AbilityBehavior.UnitTarget)
            {
                result = this.Ability.UseAbility(target);
            }
            else if ((this.Ability.AbilityBehavior & AbilityBehavior.Point) == AbilityBehavior.Point)
            {
                result = this.Ability.UseAbility(target.NetworkPosition);
            }
            else
            {
                result = this.Ability.UseAbility();
            }

            if (result)
            {
                this.LastCastAttempt = Game.RawGameTime;
            }

            return result;
        }
    }
}