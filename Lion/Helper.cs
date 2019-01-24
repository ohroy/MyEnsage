using System.ComponentModel.Composition;
using System.Linq;
using Ensage;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;

namespace wtf.lion
{
    [Export("helper")]
    public class Helper
    {
        public MultiSleeper MultiSleeper { get; }
        public Helper()
        {
            MultiSleeper=new MultiSleeper();
            
        }

        public bool DuelAghanimsScepter(Hero target)
        {
            var duelAghanimsScepter = false;
            if (target.HasModifier("modifier_legion_commander_duel"))
            {
                duelAghanimsScepter = EntityManager<Hero>.Entities.Any(x =>
                    x.HeroId == HeroId.npc_dota_hero_legion_commander &&
                    x.IsValid &&
                    x.IsVisible &&
                    x.IsAlive &&
                    x.HasAghanimsScepter());
            }

            return duelAghanimsScepter;
        }

        public bool ComboBreaker(Hero target, bool menu = true)
        {
            var comboBreaker = target.GetItemById(AbilityId.item_aeon_disk);
            if (comboBreaker != null && comboBreaker.Cooldown <= 0)
            {
                return true;
            }

            return false;
        }


        private string[] CancelModifiers { get; } =
        {
            "modifier_abaddon_borrowed_time",
            "modifier_item_combo_breaker_buff",
            "modifier_winter_wyvern_winters_curse_aura",
            "modifier_winter_wyvern_winters_curse",
            "modifier_oracle_fates_edict"
        };

        public bool CanCombo(Hero target)
        {
            return target!=null
                   &&!target.IsMagicImmune()
                   && !target.IsInvulnerable()
                   && !DuelAghanimsScepter(target)
                   && !target.HasAnyModifiers(CancelModifiers);
        }
    }
}
