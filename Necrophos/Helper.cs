using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;

namespace wtf
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
    }
}
