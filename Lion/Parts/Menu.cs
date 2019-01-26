using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using Ensage.Common.Menu;
using Ensage.SDK.Menu;
using Ensage.SDK.Service;
using SharpDX;

namespace wtf.lion.Parts
{
    //卫星类，用来远程计算伤害
    [Export("menu")]
    public class Menu
    {
        private MenuFactory _factory;
        public MenuItem<AbilityToggler> ItemToggler;
        public MenuItem<AbilityToggler> AbilityToggler;
        
        public MenuItem<bool> DrawEnabled;
        public MenuItem<bool> DrawSkill1Enabled;
        public MenuItem<bool> DrawSkill2Enabled;
        public MenuItem<bool> DrawSkill3Enabled;
        public MenuItem<bool> DrawUltEnabled;
        public MenuItem<bool> DrawBlinkEnabled;
        public MenuItem<bool> DrawTargetEnabled;



        public MenuItem<bool> IsAutoKillStealEnabled;
        public MenuItem<bool> IsAutoKillStealEnabledWhenCombo;
        public MenuItem<bool> IsShowHpBarEnabled;
        public MenuItem<KeyBind> ComboKeyItem;

        private Dictionary<string, bool> ComboItems { get; } = new Dictionary<string, bool>
        {
            {"item_orchid",true},
            {"item_bloodthorn",true},
            { "item_nullifier",true},
            { "item_glimmer_cape",true},
            { "item_shivas_guard",true},
            { "item_bottle",true},
            { "item_soul_ring",true},
            { "item_veil_of_discord",true},
            { "item_rod_of_atos",true},
            { "item_sheepstick",true},
            { "item_ghost",true},
            { "item_ethereal_blade",true},
            { "item_dagon_5",true}
        };
        private Dictionary<string, bool> ComboAbilities { get; } = new Dictionary<string, bool>
        {
            { "lion_impale",true},
            { "lion_voodoo",true},
            { "lion_finger_of_death",true},
        };

        private Dictionary<string, bool> LinkenBreaker { get; } = new Dictionary<string, bool>
        {
            {"item_force_staff",true},
            {"item_cyclone",true},
        };


        [ImportingConstructor]
        public Menu([Import] IServiceContext context)
        {

        }

        public void uninstall()
        {
            _factory.Dispose();
        }

        public void install()
        {
            _factory=MenuFactory.CreateWithTexture("wtf.lion", "npc_dota_hero_lion");
            _factory.Target.SetFontColor(Color.YellowGreen);
            var comboMenu = _factory.Menu("Combo");
            var itemsMenu = comboMenu.Menu("Items");
            var abilitiesMenu= comboMenu.Menu("Abilities");
            ItemToggler = itemsMenu.Item("ComboItems: ", new AbilityToggler(ComboItems));
            AbilityToggler = abilitiesMenu.Item("ComboItems: ", new AbilityToggler(ComboAbilities));
            var drawMenu = _factory.Menu("Drawing");
            DrawEnabled = drawMenu.Item("Draw", true);
            DrawBlinkEnabled =drawMenu.Item("Blink Range", true);

            DrawSkill1Enabled=drawMenu.Item("Impale Range", true);
            DrawSkill2Enabled=drawMenu.Item("Voodoo Range", true);
            DrawSkill3Enabled=drawMenu.Item("Mana Drain Range", true);
            DrawUltEnabled=drawMenu.Item("Ult Range", true);
            DrawTargetEnabled = drawMenu.Item("Target Line", true);

            //_factory.Item("Killsteal Key", new KeyBind(32));
            //_factory.Item("autoDisable", true);

        

            var autoKillStealMenu= _factory.Menu("Auto Killstea");
            IsAutoKillStealEnabled = autoKillStealMenu.Item("Enable", true);
            IsAutoKillStealEnabledWhenCombo = autoKillStealMenu.Item("Enable when combo", false);
            IsShowHpBarEnabled = autoKillStealMenu.Item("Show HpBar", true);

            ComboKeyItem = comboMenu.Item("Combo Key", new KeyBind('D'));
        }
    }
}