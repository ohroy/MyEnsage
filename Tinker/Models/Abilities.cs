using System;
using System.ComponentModel.Composition;
using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Abilities.Aggregation;
using Ensage.SDK.Abilities.Items;
using Ensage.SDK.Abilities.npc_dota_hero_lion;
using Ensage.SDK.Helpers;
using Ensage.SDK.Inventory;
using Ensage.SDK.Inventory.Metadata;
using Ensage.SDK.Service;

namespace wtf.tinker.Models
{




    [Export("abilities")]
    public class Abilities
    {



        private AbilityFactory _factory;
        private IInventoryManager _inventoryManager;

        public Sdk.Abilities.npc_dota_hero_tinker.tinker_laser Skill1 { get; set; }
        public Sdk.Abilities.npc_dota_hero_tinker.tinker_heat_seeking_missile Skill2 { get; set; }
        public Sdk.Abilities.npc_dota_hero_tinker.tinker_march_of_the_machines Skill3 { get; set; }
        public Sdk.Abilities.npc_dota_hero_tinker.tinker_rearm Skill4 { get; set; }

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
            //_owner.Stop();
            _factory = context.AbilityFactory;
            _inventoryManager = context.Inventory;

        }

        public void install()
        {

            Skill1=new Sdk.Abilities.npc_dota_hero_tinker.tinker_laser(_owner.Spellbook.Spell1);
            Skill2= new Sdk.Abilities.npc_dota_hero_tinker.tinker_heat_seeking_missile(_owner.Spellbook.Spell2);
            Skill3 = new Sdk.Abilities.npc_dota_hero_tinker.tinker_march_of_the_machines(_owner.Spellbook.Spell3);
            Skill4 = new Sdk.Abilities.npc_dota_hero_tinker.tinker_rearm(_owner.Spellbook.Spell4);
//            foreach (var data in Skill3.Ability.AbilitySpecialData)
//            {
//                Console.WriteLine($"{data.Name},{data.Count}");
//            }
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
