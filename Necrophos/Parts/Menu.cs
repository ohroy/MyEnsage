using System.Collections.Generic;
using System.ComponentModel.Composition;
using Ensage.Common.Menu;
using Ensage.SDK.Menu;
using Ensage.SDK.Service;
using SharpDX;

namespace wtf.Parts
{
    //卫星类，用来远程计算伤害
    [Export("menu")]
    public class Menu
    {
        private MenuFactory _factory;
        public MenuItem<AbilityToggler> ItemToggler;
        
        public MenuItem<bool> DrawEnabled;
        public MenuItem<bool> DrawPulseEnabled;
        public MenuItem<bool> DrawUltEnabled;
        public MenuItem<bool> DrawBlinkEnabled;



        public MenuItem<bool> IsAutoKillStealEnabled;
        public MenuItem<bool> IsShowHpBarEnabled;

        private Dictionary<string, bool> ComboItems { get; } = new Dictionary<string, bool>
        {
            { "item_blink",true},
            { "item_glimmer_cape",true},
            { "item_shivas_guard",true},
            { "item_bottle",true},
            { "item_soul_ring",true},
            { "item_veil_of_discord",true},
            { "item_rod_of_atos",true},
            { "item_sheepstick",true},
            { "item_ghost",true},
            { "item_ethereal_blade",true},
            { "item_dagon",true}
        };

        private Dictionary<string, bool> LinkenBreaker { get; } = new Dictionary<string, bool>
        {
            {"item_force_staff",true},
            {"item_cyclone",true},
        };

        private Dictionary<string, bool> DefenseSkills { get; } = new Dictionary<string, bool>
        {
            {"necrolyte_sadist",false},
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
            _factory=MenuFactory.CreateWithTexture("wtf.nec", "npc_dota_hero_necrolyte");
            _factory.Target.SetFontColor(Color.YellowGreen);
            var comboMenu = _factory.Menu("Combo");
            var itemsMenu = comboMenu.Menu("Items");
            ItemToggler = itemsMenu.Item("ComboItems: ", new AbilityToggler(ComboItems));

            var defenseMenu = _factory.Menu("Defence");
            defenseMenu.Item("DefenseSkills", new AbilityToggler(DefenseSkills));

            var drawMenu = _factory.Menu("Drawing");
            DrawEnabled = drawMenu.Item("Draw", true);
            DrawBlinkEnabled =drawMenu.Item("Blink Range", true);
            DrawPulseEnabled=drawMenu.Item("Pulse Range", true);
            DrawUltEnabled=drawMenu.Item("Ult Range", true);

            //_factory.Item("Killsteal Key", new KeyBind(32));
            //_factory.Item("autoDisable", true);
            IsShowHpBarEnabled = _factory.Item("Show HpBar", true);
            IsAutoKillStealEnabled = _factory.Item("Auto Killsteal", true);

        }
    }
}