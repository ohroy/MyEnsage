using System;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Abilities.Aggregation;
using Ensage.SDK.Abilities.Items;
using Ensage.SDK.Abilities.npc_dota_hero_night_stalker;
using Ensage.SDK.Abilities.npc_dota_hero_skywrath_mage;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;
using Ensage.SDK.Inventory;
using Ensage.SDK.Inventory.Metadata;
using Ensage.SDK.Service;
using wtf.Sdk.Abilities.npc_dota_hero_necrolyte;

namespace wtf.Models
{




    [Export("abilities")]
    public class Abilities
    {



        private AbilityFactory _factory;
        private IInventoryManager _inventoryManager;

        //死亡脉冲
        public necrolyte_death_pulse Pulse { get; set; }
        //幽魂护罩
        public necrolyte_sadist Sadist { get; set; }
        //蝎心光环
        public necrolyte_heartstopper_aura HeartAura { get; set; }
        //死神镰刀
        public necrolyte_reapers_scythe Scythe { get; set; }

        public Dagon Dagon
        {
            get
            {
                return Dagon1 ?? Dagon2 ?? Dagon3 ?? Dagon4 ?? (Dagon)Dagon5;
            }
        }

        [ItemBinding]
        public item_sheepstick Hex { get; set; }

        [ItemBinding]
        public item_orchid Orchid { get; set; }

        [ItemBinding]
        public item_bloodthorn Bloodthorn { get; set; }

        [ItemBinding]
        public item_rod_of_atos RodofAtos { get; set; }

        [ItemBinding]
        public item_veil_of_discord Veil { get; set; }

        [ItemBinding]
        public item_ethereal_blade Ethereal { get; set; }

        [ItemBinding]
        public item_dagon Dagon1 { get; set; }

        [ItemBinding]
        public item_dagon_2 Dagon2 { get; set; }

        [ItemBinding]
        public item_dagon_3 Dagon3 { get; set; }

        [ItemBinding]
        public item_dagon_4 Dagon4 { get; set; }

        [ItemBinding]
        public item_dagon_5 Dagon5 { get; set; }

        [ItemBinding]
        public item_force_staff ForceStaff { get; set; }

        [ItemBinding]
        public item_cyclone Eul { get; set; }

        [ItemBinding]
        public item_blink Blink { get; set; }

        [ItemBinding]
        public item_shivas_guard Shivas { get; set; }

        [ItemBinding]
        public item_nullifier Nullifier { get; set; }

        [ItemBinding]
        public item_urn_of_shadows UrnOfShadows { get; set; }

        [ItemBinding]
        public item_spirit_vessel SpiritVessel { get; set; }

        private Hero _owner;


        [ImportingConstructor]
        public Abilities([Import] IServiceContext context)
        {
            _owner=context.Owner as Hero;
            _owner.Stop();
            _factory = context.AbilityFactory;
            _inventoryManager = context.Inventory;

        }

        public void install()
        {
            //cause sdk doesnot has the nec ,so need do it self
            // or sometime I will pull request to sdk....
            // but now, DIY

            Pulse = new necrolyte_death_pulse(_owner.Spellbook.Spell1);
            Sadist = new necrolyte_sadist(_owner.Spellbook.Spell2);
            HeartAura = new necrolyte_heartstopper_aura(_owner.Spellbook.Spell3);
            Scythe = new necrolyte_reapers_scythe(_owner.Spellbook.Spell4);



            /*
1--------------------------------
area_of_effect,1
heal,4
projectile_speed,1
#AbilityDamage,4
2--------------------------------
duration,4
heal_bonus,1
movement_speed,4
slow_aoe,1
bonus_damage,1
3--------------------------------
aura_radius,1
aura_damage,4
health_regen,4
mana_regen,4
regen_duration,1
hero_multiplier,1
4--------------------------------
damage_per_health,3
stun_duration,1
cooldown_scepter,3
respawn_constant,3
             Console.WriteLine("1--------------------------------");
            foreach (var data in Pulse.Ability.AbilitySpecialData)
            {
                Console.WriteLine($"{data.Name},{data.Count}");
            }

            Console.WriteLine("2--------------------------------");
            foreach (var data in Sadist.Ability.AbilitySpecialData)
            {
                Console.WriteLine($"{data.Name},{data.Count}");
            }

            Console.WriteLine("3--------------------------------");
            foreach (var data in HeartAura.Ability.AbilitySpecialData)
            {
                Console.WriteLine($"{data.Name},{data.Count}");
            }

            Console.WriteLine("4--------------------------------");
            foreach (var data in Scythe.Ability.AbilitySpecialData)
            {
                Console.WriteLine($"{data.Name},{data.Count}");
            }*/

            //Console.WriteLine($"{Pulse.RawDamage}");
            UpdateManager.BeginInvoke(() =>
                {
                    _inventoryManager.Attach(this);
                },
                3000);
        }

        public void uninstall()
        {
            _inventoryManager.Detach(this);
        }
    }
}
