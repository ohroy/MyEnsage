using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Enums;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.Common.Threading;
using Ensage.SDK.Helpers;
using Ensage.SDK.Logger;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;

using SharpDX;
using AbilityId = Ensage.AbilityId;
using UnitExtensions = Ensage.SDK.Extensions.UnitExtensions;

namespace wtf
{
    [ExportPlugin(name: "wtf.nec", author: "rozbo", version: "0.0.3.0", units: HeroId.npc_dota_hero_necrolyte)]
    internal class Necrophos : Plugin
    {
        [ImportingConstructor]
        public Necrophos([Import] IEntityContext<Unit> entityContext)
        {
            Owner = entityContext.Owner as Hero;
        }

        private Task RearmBlink { get; set; }

        private int[] RearmTime { get; } = { 3010, 1510, 760 };

        private int Time { get; set; }

        private int GetRearmTime(Ability s) => RearmTime[s.Level - 1];

        private int HIDE_AWAY_RANGE { get; } = 130;

        private bool Iscreated { get; set; }

        private Abilities Abilities;

        private Ability PulseAbility => Abilities.PulseAbility;

        private Ability GhostAbility => Abilities.GhostAbility;

        private Ability UltAbility => Abilities.UltAbility;


        private Item Blink => Abilities.Blink;

        private Item Dagon => Abilities.Dagon;

        private Item Sheep => Abilities.Sheep;

        private Item Soulring => Abilities.Soulring;

        private Item Ethereal => Abilities.Ethereal;

        private Item Shiva => Abilities.Shiva;

        private Item Ghost => Abilities.Ghost;

        private Item Cyclone => Abilities.Cyclone;

        private Item Forcestaff => Abilities.Forcestaff;

        private Item Glimmer => Abilities.Glimmer;

        private Item Bottle => Abilities.Bottle;

        private Item Travel => Abilities.Travel;

        private Item Veil => Abilities.Veil;

        private Item Atos => Abilities.Atos;

        private Hero Owner { get; }

        private Hero Target { get; set; }
        
        private Dictionary<Unit, ParticleEffect> VisibleUnit { get; } = new Dictionary<Unit, ParticleEffect>();

        private List<ParticleEffect> Effects { get; } = new List<ParticleEffect>();

        private string EffectPath { get; } = @"materials\ensage_ui\particles\other_range_blue.vpcf";        

        private Menu Menu { get; } = new Menu("wtf.nec", "wtf.nec", true, "npc_dota_hero_necrolyte", true).SetFontColor(Color.Aqua);
        private Menu _Combo { get; } = new Menu("killsteal", "killsteal");
        private Menu _Defense { get; } = new Menu("Defense", "Defense");
        private int Red => Menu.Item("red").GetValue<Slider>().Value;

        private int Green => Menu.Item("green").GetValue<Slider>().Value;

        private int Blue => Menu.Item("blue").GetValue<Slider>().Value;


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



        private int[] Dagondistance { get; } = new int[5] { 600, 650, 700, 750, 800 };
        
		private int Ensage_error { get; } = 50;

        //额外的施法距离
		private int Castrange { get; set; } = 0;

        private double Angle { get; set; }

        private ParticleEffect Rangedisplay_dagger { get; set; }

        private ParticleEffect Rangedisplay_rocket { get; set; }

        private ParticleEffect Rangedisplay_laser { get; set; }

        private ParticleEffect Effect2 { get; set; }


        private int Range_dagger { get; set; }

        private int Range_rocket { get; set; }

        private int Range_laser { get; set; }

        protected override void OnActivate()
        {
            UpdateManager.BeginInvoke(() =>
            {
                // Menu Options	                                                          

                _Combo.AddItem(new MenuItem("ComboItems: ", "Items:").SetValue(new AbilityToggler(ComboItems)));
                _Combo.AddItem(new MenuItem("LinkenBreaker: ", "Linken Breaker:").SetValue(new AbilityToggler(LinkenBreaker)));
                Menu.AddSubMenu(_Combo);
                _Defense.AddItem(
                    new MenuItem("DefenseSkills", "DefenseSkills").SetValue(new AbilityToggler(DefenseSkills)));
                Menu.AddSubMenu(_Defense);

                var _ranges = new Menu("Drawing", "Drawing");
                _ranges.AddItem(new MenuItem("Blink Range", "Show Blink Dagger Range").SetValue(true));
                _ranges.AddItem(new MenuItem("Pulse Range", "Show Pulse Range").SetValue(true));
                _ranges.AddItem(new MenuItem("Ult Range", "Show Ult Range").SetValue(true));
                _ranges.AddItem(new MenuItem("red", "Red").SetValue(new Slider(0, 0, 255)).SetFontColor(Color.Red));
                _ranges.AddItem(new MenuItem("green", "Green").SetValue(new Slider(255, 0, 255)).SetFontColor(Color.Green));
                _ranges.AddItem(new MenuItem("blue", "Blue").SetValue(new Slider(255, 0, 255)).SetFontColor(Color.Blue));
                Menu.AddSubMenu(_ranges);

                Menu.AddItem(new MenuItem("Killsteal Key", "Killsteal Key").SetValue(new KeyBind(32, KeyBindType.Press)));
             
                Menu.AddItem(new MenuItem("autoDisable", "Auto disable/counter enemy").SetValue(true));
                Menu.AddToMainMenu();

                Orbwalking.Load();

                OnUpdateAbility();
                UpdateManager.Subscribe(OnUpdateAbility, 500);

                Game.OnUpdate += ComboEngine;
                Game.OnUpdate += AD;
                UpdateManager.Subscribe(DrawRanges, 50);
            }, 
            5000);
        }

        protected override void OnDeactivate()
        {
            Menu.RemoveFromMainMenu();
        }


        
        private void ComboEngine(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsWatchingGame)
            {
                return;
            } 

            var fount = EntityManager<Unit>.Entities.Where(x => x.Team == Owner.Team && x.NetworkName == "CDOTA_Unit_Fountain").ToList();
            var creeps = EntityManager<Creep>.Entities.Where(creep =>
                   (creep.NetworkName == "CDOTA_BaseNPC_Creep_Lane"
                   || creep.NetworkName == "CDOTA_BaseNPC_Creep_Siege"
                   || creep.NetworkName == "CDOTA_BaseNPC_Creep_Neutral"
                   || creep.NetworkName == "CDOTA_BaseNPC_Creep") &&
                   creep.IsAlive && creep.Team != Owner.Team && creep.IsVisible && creep.IsSpawned).ToList();




            Castrange = 0;
            //加上以太之镜的施法距离
            var aetherLens = Owner.Inventory.Items.FirstOrDefault(x => x.Id == AbilityId.item_aether_lens);
            if (aetherLens != null)
            {
                Castrange += (int)aetherLens.AbilitySpecialData.First(x => x.Name == "cast_range_bonus").Value;
            }
        }

        private void AD(EventArgs args)
        {
            if (Game.IsPaused)
            {
                return;
            }

            Castrange = 0;

            var aetherLens = Owner.Inventory.Items.FirstOrDefault(x => x.Id == AbilityId.item_aether_lens);
            if (aetherLens != null)
            {
                Castrange += (int)aetherLens.AbilitySpecialData.First(x => x.Name == "cast_range_bonus").Value;
            }


            // 自动瓶子
            if (Bottle != null 
                && !Owner.IsInvisible() 
                && !Owner.IsChanneling() 
                && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase) 
                //&& !March.IsInAbilityPhase 
                && Owner.Modifiers.Any(x => x.Name == "modifier_fountain_aura_buff") 
                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Bottle.Name) 
                && Utils.SleepCheck("bottle1"))
            {
                if (!Owner.Modifiers.Any(x => x.Name == "modifier_bottle_regeneration") && (Owner.Health < Owner.MaximumHealth || Owner.Mana < Owner.MaximumMana))
                {
                    Bottle.UseAbility();
                }
                    
                var alies = EntityManager<Hero>.Entities.Where(x => 
                                                               x.Team == Owner.Team && 
                                                               x != Owner && 
                                                               (x.Health < x.MaximumHealth || x.Mana < x.MaximumMana) && 
                                                               !x.Modifiers.Any(y => y.Name == "modifier_bottle_regeneration") && 
                                                               x.IsAlive && 
                                                               !x.IsIllusion && 
                                                               x.Distance2D(Owner) <= Bottle.CastRange).ToList();

                foreach (Hero v in alies)
                {
                    if (v != null)
                    {
                        Bottle.UseAbility(v);
                    }
                }
                    
                Utils.Sleep(255, "bottle1");
            }






            foreach (var e in EntityManager<Hero>.Entities.Where(x => x.IsVisible && x.IsAlive && x.Team == Owner.GetEnemyTeam() && !x.IsIllusion))
            {
                if (e == null)
                {
                    return;
                }
                    
                Angle = Math.Abs(e.FindAngleR() - Utils.DegreeToRadian(e.FindAngleForTurnTime(Owner.NetworkPosition)));

                if (Menu.Item("autoDisable").GetValue<bool>() && Owner.IsAlive && Owner.IsVisibleToEnemies)
                {
                    //break linken if tp
                    if (!Owner.IsChanneling()
                        && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                        && Owner.Distance2D(e) >= 300 + Ensage_error
                        && e.Modifiers.Any(y => y.Name == "modifier_teleporting")
                        && e.IsLinkensProtected()
                        && Utils.SleepCheck("tplink"))
                    {
                        if ((Cyclone != null && Cyclone.CanBeCasted()) || (Sheep != null && Sheep.CanBeCasted()))
                        {
                            if (Atos != null && Atos.CanBeCasted())
                            {
                                Atos.UseAbility(e);
                            }
                            else if (Owner.Spellbook.SpellQ != null && Owner.Spellbook.SpellQ.CanBeCasted())
                            {
                                Owner.Spellbook.SpellQ.UseAbility(e);
                            }
                            else if (Ethereal != null && Ethereal.CanBeCasted())
                            {
                                Ethereal.UseAbility(e);
                            }
                            else if (Dagon != null && Dagon.CanBeCasted())
                            {
                                Dagon.UseAbility(e);
                            }
                            else if ((Sheep != null && Sheep.CanBeCasted()) && (Cyclone != null && Cyclone.CanBeCasted()))
                            {
                                Sheep.UseAbility(e);
                            }
                        }

                        Utils.Sleep(150, "tplink");
                    }

                    //break TP 
                    if (!Owner.IsChanneling()
                        && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                        && e.Modifiers.Any(y => y.Name == "modifier_teleporting")
                        && !e.IsHexed()
                        && !e.Modifiers.Any(y => y.Name == "modifier_eul_cyclone")
                        && !e.IsLinkensProtected()
                        && Utils.SleepCheck("tplink1"))
                    {
                        if (Sheep != null && Sheep.CanBeCasted())
                        {
                            Sheep.UseAbility(e);
                        }
                        else if (Cyclone != null && Cyclone.CanBeCasted())
                        {
                            Cyclone.UseAbility(e);
                        }
                            
                        Utils.Sleep(150, "tplink1");
                    }

                    //break channel by Hex
                    if (!Owner.IsChanneling()
                        && Sheep != null && Sheep.CanBeCasted()
                        && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                        && !e.Modifiers.Any(y => y.Name == "modifier_eul_cyclone")
                        && !e.IsSilenced()
                        && !e.IsMagicImmune()
                        && !e.IsLinkensProtected()
                        && !e.Modifiers.Any(y => y.Name == "modifier_teleporting")
                        && Utils.SleepCheck(e.Handle.ToString())
                        && (e.IsChanneling()
                            || (Blink != null && IsCasted(Blink))
                            //break escape spells (1 hex, 2 seal) no need cyclone
                            || e.HeroId == HeroId.npc_dota_hero_queenofpain && e.FindSpell("queenofpain_blink").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_antimage && e.FindSpell("antimage_blink").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_storm_spirit && e.FindSpell("storm_spirit_ball_lightning").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_shredder && e.FindSpell("shredder_timber_chain").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_weaver && e.FindSpell("weaver_time_lapse").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_faceless_void && e.FindSpell("faceless_void_time_walk").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_phoenix && e.FindSpell("phoenix_icarus_dive").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_magnataur && e.FindSpell("magnataur_skewer").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_morphling && e.FindSpell("morphling_waveform").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_phantom_assassin && e.FindSpell("phantom_assassin_phantom_strike").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_riki && e.FindSpell("riki_blink_strike").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_spectre && e.FindSpell("spectre_haunt").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_furion && e.FindSpell("furion_sprout").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_phantom_lancer && e.FindSpell("phantom_lancer_doppelwalk").IsInAbilityPhase



                            //break special (1 hex, 2 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_riki && Owner.Modifiers.Any(y => y.Name == "modifier_riki_smoke_screen")
                            || e.HeroId == HeroId.npc_dota_hero_spirit_breaker && e.Modifiers.Any(y => y.Name == "modifier_spirit_breaker_charge_of_darkness")
                            || e.HeroId == HeroId.npc_dota_hero_phoenix && e.Modifiers.Any(y => y.Name == "modifier_phoenix_icarus_dive")
                            || e.HeroId == HeroId.npc_dota_hero_magnataur && e.Modifiers.Any(y => y.Name == "modifier_magnataur_skewer_movement")



                            //break rats shadow blades and invis (1 hex, 2 seal, 3 cyclone)
                            || e.IsMelee && Owner.Distance2D(e) <= 350 //test
                            || e.HeroId == HeroId.npc_dota_hero_legion_commander && e.FindSpell("legion_commander_duel").Cooldown < 2 && Owner.Distance2D(e) < 480 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_tiny && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_pudge && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_nyx_assassin && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_bounty_hunter && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_nevermore && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_weaver && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_riki && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_clinkz && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_broodmother && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_slark && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_ursa && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_earthshaker && (e.Spellbook.SpellQ.Cooldown <= 1 || e.Spellbook.SpellR.Cooldown <= 1)
                            || e.HeroId == HeroId.npc_dota_hero_alchemist && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_troll_warlord && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()

                            //break rats blinkers (1 hex, 2 seal, 3 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_ursa && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_phantom_assassin && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_riki && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_spectre && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_antimage && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()

                            || e.HeroId == HeroId.npc_dota_hero_templar_assassin && Owner.Distance2D(e) <= e.GetAttackRange() + 50 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_morphling && Owner.Distance2D(e) <= e.GetAttackRange() + 50 && !Owner.IsAttackImmune()

                            || e.HeroId == HeroId.npc_dota_hero_queenofpain && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_puck && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_storm_spirit && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_phoenix && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_magnataur && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_faceless_void && Owner.Distance2D(e) <= 800 + Castrange + Ensage_error


                            //break mass dangerous spells (1 hex, 2 seal, 3 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_necrolyte && e.FindSpell("necrolyte_reapers_scythe").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_faceless_void && e.FindSpell("faceless_void_chronosphere").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_magnataur && e.FindSpell("magnataur_reverse_polarity").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_doom_bringer && e.FindSpell("doom_bringer_doom").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_tidehunter && e.FindSpell("tidehunter_ravage").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_enigma && e.FindSpell("enigma_black_hole").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_rattletrap && e.FindSpell("rattletrap_power_cogs").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_luna && e.FindSpell("luna_eclipse").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_nevermore && e.FindSpell("nevermore_requiem").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_spirit_breaker && e.FindSpell("spirit_breaker_nether_strike").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_naga_siren && e.FindSpell("naga_siren_song_of_the_siren").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_medusa && e.FindSpell("medusa_stone_gaze").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_treant && e.FindSpell("treant_overgrowth").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_antimage && e.FindSpell("antimage_mana_void").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_warlock && e.FindSpell("warlock_rain_of_chaos").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_terrorblade && e.FindSpell("terrorblade_sunder").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_dark_seer && e.FindSpell("dark_seer_wall_of_replica").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_dark_seer && e.FindSpell("dark_seer_surge").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_dazzle && e.FindSpell("dazzle_shallow_grave").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_omniknight && e.FindSpell("omniknight_guardian_angel").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_omniknight && e.FindSpell("omniknight_repel").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_beastmaster && e.FindSpell("beastmaster_primal_roar").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.FindSpell("chaos_knight_reality_rift").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.FindSpell("chaos_knight_phantasm").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_life_stealer && e.FindSpell("life_stealer_infest").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_sven && e.FindSpell("sven_gods_strength").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_drow_ranger && e.FindSpell("drow_ranger_wave_of_silence").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_nyx_assassin && e.FindSpell("nyx_assassin_mana_burn").IsInAbilityPhase

                            || e.HeroId == HeroId.npc_dota_hero_mirana && e.Spellbook.SpellW.IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_bounty_hunter && e.Spellbook.SpellR.IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_phoenix && e.FindSpell("phoenix_icarus_dive").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_earth_spirit && e.FindSpell("earth_spirit_magnetize").IsInAbilityPhase


                            //break stun spells (1 hex, 2 seal, 3 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_ogre_magi && e.FindSpell("ogre_magi_fireblast").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_axe && e.FindSpell("axe_berserkers_call").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_lion && e.FindSpell("lion_impale").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_nyx_assassin && e.FindSpell("nyx_assassin_impale").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_rubick && e.FindSpell("rubick_telekinesis").IsInAbilityPhase
                            || (e.HeroId == HeroId.npc_dota_hero_rubick && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + Ensage_error)
                            //|| (e.HeroId == HeroId.CDOTA_Unit_Hero_Alchemist && e.FindSpell("alchemist_unstable_concoction_throw").IsInAbilityPhase)


                            //break flying stun spells if enemy close (1 hex, 2 seal, 3 cyclone)  have cyclone
                            || (e.HeroId == HeroId.npc_dota_hero_sniper && e.Spellbook.SpellR.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)//e.FindSpell("sniper_assassinate").Cooldown > 0 && me.Modifiers.Any(y => y.Name == "modifier_sniper_assassinate"))
                            || (e.HeroId == HeroId.npc_dota_hero_windrunner && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.1 && Owner.Distance2D(e) <= 400)
                            || (e.HeroId == HeroId.npc_dota_hero_sven && e.Spellbook.SpellQ.IsInAbilityPhase && Owner.Distance2D(e) <= 300)
                            || (e.HeroId == HeroId.npc_dota_hero_skeleton_king && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)
                            || (e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)
                            || (e.HeroId == HeroId.npc_dota_hero_vengefulspirit && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)


                            //break flying stun spells if enemy close (1 hex, 2 seal, 3 cyclone)  no cyclone
                            || (e.HeroId == HeroId.npc_dota_hero_sniper && e.Spellbook.SpellR.IsInAbilityPhase && Angle <= 0.03 && (Cyclone == null || !Cyclone.CanBeCasted()))//e.FindSpell("sniper_assassinate").Cooldown > 0 && me.Modifiers.Any(y => y.Name == "modifier_sniper_assassinate"))
                            || (e.HeroId == HeroId.npc_dota_hero_windrunner && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.1 && (Cyclone == null || !Cyclone.CanBeCasted()))
                            || (e.HeroId == HeroId.npc_dota_hero_sven && e.Spellbook.SpellQ.IsInAbilityPhase && (Cyclone == null || !Cyclone.CanBeCasted()))
                            || (e.HeroId == HeroId.npc_dota_hero_skeleton_king && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && (Cyclone == null || !Cyclone.CanBeCasted()))
                            || (e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && (Cyclone == null || !Cyclone.CanBeCasted()))
                            || (e.HeroId == HeroId.npc_dota_hero_vengefulspirit && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && (Cyclone == null || !Cyclone.CanBeCasted()))




                            //break common dangerous spell (1 hex, 2 seal) //no need cyclone
                            || e.HeroId == HeroId.npc_dota_hero_bloodseeker && e.FindSpell("bloodseeker_rupture").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_mirana && e.FindSpell("mirana_invis").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_riki && e.FindSpell("riki_smoke_screen").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_riki && e.FindSpell("riki_tricks_of_the_trade").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_viper && e.FindSpell("viper_viper_strike").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_chen && e.FindSpell("chen_hand_of_god").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_death_prophet && e.FindSpell("death_prophet_silence").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_death_prophet && e.FindSpell("death_prophet_exorcism").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_invoker // =)
                            || e.HeroId == HeroId.npc_dota_hero_ember_spirit // =)



                            //break hex spell
                            || (e.HeroId == HeroId.npc_dota_hero_lion && e.Spellbook.SpellW.Level > 0 && e.Spellbook.SpellW.Cooldown < 1 && Owner.Distance2D(e) < e.Spellbook.SpellW.GetCastRange() + Ensage_error)
                            || (e.HeroId == HeroId.npc_dota_hero_shadow_shaman && e.Spellbook.SpellW.Level > 0 && e.Spellbook.SpellW.Cooldown < 1 && Owner.Distance2D(e) < e.Spellbook.SpellW.GetCastRange() + Ensage_error)
                            || (Sheep != null && Sheep.Cooldown < 1 && Owner.Distance2D(e) < Sheep.GetCastRange() + Ensage_error)




                            || e.HeroId == HeroId.npc_dota_hero_omniknight && e.FindSpell("omniknight_purification").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_ursa && e.FindSpell("ursa_overpower").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_silencer && e.FindSpell("silencer_last_word").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_silencer && e.FindSpell("silencer_global_silence").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_shadow_shaman && e.FindSpell("shadow_shaman_mass_serpent_ward").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_queenofpain && e.FindSpell("queenofpain_sonic_wave").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_obsidian_destroyer && e.FindSpell("obsidian_destroyer_astral_imprisonment").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_obsidian_destroyer && e.FindSpell("obsidian_destroyer_sanity_eclipse").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_pugna && e.FindSpell("pugna_nether_ward").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_lich && e.FindSpell("lich_chain_frost").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_storm_spirit && e.FindSpell("storm_spirit_electric_vortex").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_zuus && e.FindSpell("zuus_thundergods_wrath").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_brewmaster && e.FindSpell("brewmaster_primal_split").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_bane && e.FindSpell("bane_fiends_grip").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_bane && e.FindSpell("bane_nightmare").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_undying && e.FindSpell("undying_tombstone").IsInAbilityPhase

                            )
                        )
                    {
                        Sheep.UseAbility(e);
                        Utils.Sleep(200, e.Handle.ToString());
                    }


                    //break channel by cyclone if not hex
                    if (!Owner.IsChanneling()
                        && Cyclone != null
                        && Cyclone.CanBeCasted()
                        && (Sheep == null || !Sheep.CanBeCasted() || e.IsLinkensProtected())
                        && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                        && !e.IsHexed()
                        && !e.IsMagicImmune()
                        && !e.IsSilenced()
                        && !e.Modifiers.Any(y => y.Name == "modifier_skywrath_mystic_flare_aura_effect")

                        && !e.Modifiers.Any(y => y.Name == "modifier_teleporting")
                        && Utils.SleepCheck(e.Handle.ToString())
                        && (e.IsChanneling()
                            || (Blink != null && IsCasted(Blink))

                            //break rats shadow blades and invis if they appear close(1 hex, 2 seal, 3 cyclone)
                            || (e.IsMelee && Owner.Distance2D(e) <= 350 && (Owner.Spellbook.SpellR == null || !Owner.Spellbook.SpellR.CanBeCasted())) //test
                            || e.HeroId == HeroId.npc_dota_hero_legion_commander && e.FindSpell("legion_commander_duel").Cooldown < 2 && Owner.Distance2D(e) < 480 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_tiny && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_pudge && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_nyx_assassin && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_bounty_hunter && Owner.Distance2D(e) <= 350
                            || e.HeroId == HeroId.npc_dota_hero_weaver && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_clinkz && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_broodmother && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_slark && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_earthshaker && (e.Spellbook.SpellQ.Cooldown <= 1 || e.Spellbook.SpellR.Cooldown <= 1)
                            || e.HeroId == HeroId.npc_dota_hero_alchemist && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()
                            || e.HeroId == HeroId.npc_dota_hero_troll_warlord && Owner.Distance2D(e) <= 350 && !Owner.IsAttackImmune()


                            //break rats blinkers (1 hex, 2 seal, 3 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_queenofpain && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_puck && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_storm_spirit && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                            || e.HeroId == HeroId.npc_dota_hero_faceless_void && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error


                            //break mass dangerous spells (1 hex, 2 seal, 3 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_necrolyte && e.FindSpell("necrolyte_reapers_scythe").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_faceless_void && e.FindSpell("faceless_void_chronosphere").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_magnataur && e.FindSpell("magnataur_reverse_polarity").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_doom_bringer && e.FindSpell("doom_bringer_doom").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_tidehunter && e.FindSpell("tidehunter_ravage").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_enigma && e.FindSpell("enigma_black_hole").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_rattletrap && e.FindSpell("rattletrap_power_cogs").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_luna && e.FindSpell("luna_eclipse").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_nevermore && e.FindSpell("nevermore_requiem").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_spirit_breaker && e.FindSpell("spirit_breaker_nether_strike").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_naga_siren && e.FindSpell("naga_siren_song_of_the_siren").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_medusa && e.FindSpell("medusa_stone_gaze").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_treant && e.FindSpell("treant_overgrowth").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_antimage && e.FindSpell("antimage_mana_void").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_warlock && e.FindSpell("warlock_rain_of_chaos").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_terrorblade && e.FindSpell("terrorblade_sunder").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_dark_seer && e.FindSpell("dark_seer_wall_of_replica").IsInAbilityPhase
                            //|| e.HeroId == HeroId.CDOTA_Unit_Hero_DarkSeer && e.FindSpell("dark_seer_surge").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_dazzle && e.FindSpell("dazzle_shallow_grave").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_omniknight && e.FindSpell("omniknight_guardian_angel").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_omniknight && e.FindSpell("omniknight_repel").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_beastmaster && e.FindSpell("beastmaster_primal_roar").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.FindSpell("chaos_knight_reality_rift").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.FindSpell("chaos_knight_phantasm").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_life_stealer && e.FindSpell("life_stealer_infest").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_sven && e.FindSpell("sven_gods_strength").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_drow_ranger && e.FindSpell("drow_ranger_wave_of_silence").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_nyx_assassin && e.FindSpell("nyx_assassin_mana_burn").IsInAbilityPhase

                            || e.HeroId == HeroId.npc_dota_hero_phoenix && e.FindSpell("phoenix_icarus_dive").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_earth_spirit && e.FindSpell("earth_spirit_magnetize").IsInAbilityPhase

                            || e.HeroId == HeroId.npc_dota_hero_furion && e.FindSpell("furion_sprout").IsInAbilityPhase


                            //break stun spells (1 hex, 2 seal, 3 cyclone)
                            || e.HeroId == HeroId.npc_dota_hero_ogre_magi && e.FindSpell("ogre_magi_fireblast").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_axe && e.FindSpell("axe_berserkers_call").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_lion && e.FindSpell("lion_impale").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_nyx_assassin && e.FindSpell("nyx_assassin_impale").IsInAbilityPhase
                            || e.HeroId == HeroId.npc_dota_hero_rubick && e.FindSpell("rubick_telekinesis").IsInAbilityPhase
                            || (e.HeroId == HeroId.npc_dota_hero_rubick && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + Ensage_error)


                            //break hex spell
                            || (e.HeroId == HeroId.npc_dota_hero_lion && e.Spellbook.SpellW.Level > 0 && e.Spellbook.SpellW.Cooldown < 1 && Owner.Distance2D(e) < e.Spellbook.SpellW.GetCastRange() + Ensage_error)
                            || (e.HeroId == HeroId.npc_dota_hero_shadow_shaman && e.Spellbook.SpellW.Level > 0 && e.Spellbook.SpellW.Cooldown < 1 && Owner.Distance2D(e) < e.Spellbook.SpellW.GetCastRange() + Ensage_error)
                            || (Sheep != null && Sheep.Cooldown < 1 && Owner.Distance2D(e) < Sheep.GetCastRange() + Ensage_error)


                            //break flying stun spells if enemy close (1 hex, 2 seal, 3 cyclone)
                            || (e.HeroId == HeroId.npc_dota_hero_sniper && e.Spellbook.SpellR.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)//e.FindSpell("sniper_assassinate").Cooldown > 0 && me.Modifiers.Any(y => y.Name == "modifier_sniper_assassinate"))
                            || (e.HeroId == HeroId.npc_dota_hero_windrunner && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.1 && Owner.Distance2D(e) <= 400)
                            || (e.HeroId == HeroId.npc_dota_hero_sven && e.Spellbook.SpellQ.IsInAbilityPhase && Owner.Distance2D(e) <= 300)
                            || (e.HeroId == HeroId.npc_dota_hero_skeleton_king && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)
                            || (e.HeroId == HeroId.npc_dota_hero_chaos_knight && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)
                            || (e.HeroId == HeroId.npc_dota_hero_vengefulspirit && e.Spellbook.SpellQ.IsInAbilityPhase && Angle <= 0.03 && Owner.Distance2D(e) <= 300)))
                    {
                        Cyclone.UseAbility(e);
                        Utils.Sleep(50, e.Handle.ToString());
                    }

                    //cyclone dodge	
                    if (Utils.SleepCheck("item_cyclone") && Cyclone != null && Cyclone.CanBeCasted())
                    {
                        //use on me
                        var mod = Owner.Modifiers.FirstOrDefault(x => x.Name == "modifier_lina_laguna_blade" || x.Name == "modifier_lion_finger_of_death");

                        if (Cyclone != null && Cyclone.CanBeCasted() &&
                            (mod != null
                            || (Owner.IsRooted() && !Owner.Modifiers.Any(y => y.Name == "modifier_razor_unstablecurrent_slow"))
                            || (e.HeroId == HeroId.npc_dota_hero_huskar && IsCasted(e.Spellbook.SpellR) && Angle <= 0.15 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 250)
                            || (e.HeroId == HeroId.npc_dota_hero_juggernaut && e.Modifiers.Any(y => y.Name == "modifier_juggernaut_omnislash") && Owner.Distance2D(e) <= 300 && !Owner.IsAttackImmune())

                            //dodge flying stuns
                            || (Ethereal != null && IsCasted(Ethereal) && Angle <= 0.1 && Owner.Distance2D(e) < Ethereal.GetCastRange() + 250)

                            || (e.HeroId == HeroId.npc_dota_hero_sniper && IsCasted(e.Spellbook.SpellR) && Owner.Distance2D(e) > 300 && Owner.Modifiers.Any(y => y.Name == "modifier_sniper_assassinate"))//e.FindSpell("sniper_assassinate").Cooldown > 0 && me.Modifiers.Any(y => y.Name == "modifier_sniper_assassinate"))
                            || (e.HeroId == HeroId.npc_dota_hero_tusk && Angle <= 0.35 && e.Modifiers.Any(y => y.Name == "modifier_tusk_snowball_movement") && Owner.Distance2D(e) <= 575)
                            || (e.HeroId == HeroId.npc_dota_hero_windrunner && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.12 && Owner.Distance2D(e) > 400 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 550)
                            || (e.HeroId == HeroId.npc_dota_hero_sven && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.3 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 500)
                            || (e.HeroId == HeroId.npc_dota_hero_skeleton_king && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.1 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 350)
                            || (e.HeroId == HeroId.npc_dota_hero_chaos_knight && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.1 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 350)
                            || (e.HeroId == HeroId.npc_dota_hero_vengefulspirit && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.1 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 350)
                            || (e.HeroId == HeroId.npc_dota_hero_alchemist && e.FindSpell("alchemist_unstable_concoction_throw").IsInAbilityPhase && Angle <= 0.3 && Owner.Distance2D(e) < e.FindSpell("alchemist_unstable_concoction_throw").GetCastRange() + 500)

                            || (e.HeroId == HeroId.npc_dota_hero_viper && IsCasted(e.Spellbook.SpellR) && Angle <= 0.1 && Owner.Distance2D(e) < e.Spellbook.SpellR.GetCastRange() + 350)
                            || (e.HeroId == HeroId.npc_dota_hero_phantom_assassin && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.1 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 350)
                            || (e.HeroId == HeroId.npc_dota_hero_morphling && IsCasted(e.Spellbook.SpellW) && Angle <= 0.1 && Owner.Distance2D(e) < e.Spellbook.SpellW.GetCastRange() + 350)
                            || (e.HeroId == HeroId.npc_dota_hero_tidehunter && IsCasted(e.Spellbook.SpellQ) && Angle <= 0.1 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellQ.GetCastRange() + 150)
                            || (e.HeroId == HeroId.npc_dota_hero_visage && IsCasted(e.Spellbook.SpellW) && Angle <= 0.1 && Owner.Distance2D(e) > 300 && Owner.Distance2D(e) < e.Spellbook.SpellW.GetCastRange() + 250)
                            || (e.HeroId == HeroId.npc_dota_hero_lich && IsCasted(e.Spellbook.SpellR) && Angle <= 0.5 && Owner.Distance2D(e) < e.Spellbook.SpellR.GetCastRange() + 350)


                            //free silence
                            || (Owner.IsSilenced() && !Owner.IsHexed() && !Owner.Modifiers.Any(y => y.Name == "modifier_doom_bringer_doom") && !Owner.Modifiers.Any(y => y.Name == "modifier_riki_smoke_screen") && !Owner.Modifiers.Any(y => y.Name == "modifier_disruptor_static_storm")))

                            //free debuff
                            || Owner.Modifiers.Any(y => y.Name == "modifier_oracle_fortunes_end_purge")
                            || Owner.Modifiers.Any(y => y.Name == "modifier_life_stealer_open_wounds"))
                        {
                            Cyclone.UseAbility(Owner);
                            Utils.Sleep(150, "item_cyclone");
                            return;
                        }
                    }

                    //幽魂之罩防御物理攻击
                    if (
                        Menu.Item("DefenseSkills").GetValue<AbilityToggler>().IsEnabled(GhostAbility.Name)
                        && GhostAbility != null
                        && GhostAbility.CanBeCasted()
                        && (Sheep == null || !Sheep.CanBeCasted())
                        && !Owner.IsAttackImmune()
                        && !e.IsHexed()
                        && !e.IsMagicImmune()
                        && !Owner.IsChanneling()
                        && Angle <= 0.03
                        && ((e.IsMelee && Owner.Position.Distance2D(e) < 250)
                            || e.HeroId == HeroId.npc_dota_hero_templar_assassin && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_troll_warlord && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_clinkz && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_weaver && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_huskar && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_nevermore && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_windrunner && Owner.Distance2D(e) <= e.GetAttackRange() + 50 && IsCasted(e.Spellbook.SpellR))
                        && e.IsAttacking()
                        && Utils.SleepCheck("Ghost"))
                    {
                        GhostAbility.UseAbility();
                        Utils.Sleep(150, "Ghost");
                    }

                    //绿杖保自己
                    if (Ghost != null
                        && Ghost.CanBeCasted()
                        && (Sheep == null || !Sheep.CanBeCasted())
                        && (GhostAbility == null || !GhostAbility.CanBeCasted() || e.Modifiers.Any(y => y.Name == "modifier_juggernaut_omnislash"))
                        && !Owner.IsAttackImmune()
                        && !e.IsHexed()
                        && (!e.Modifiers.Any(y => y.Name == "modifier_tinker_laser_blind") || e.Modifiers.Any(y => y.Name == "modifier_juggernaut_omnislash"))
                        && !Owner.IsChanneling()
                        && Angle <= 0.03
                        && ((e.IsMelee && Owner.Position.Distance2D(e) < 250)
                            && e.HeroId != HeroId.npc_dota_hero_tiny
                            && e.HeroId != HeroId.npc_dota_hero_shredder
                            && e.HeroId != HeroId.npc_dota_hero_nyx_assassin
                            && e.HeroId != HeroId.npc_dota_hero_meepo
                            && e.HeroId != HeroId.npc_dota_hero_earthshaker
                            && e.HeroId != HeroId.npc_dota_hero_centaur

                            || e.HeroId == HeroId.npc_dota_hero_templar_assassin && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_troll_warlord && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_clinkz && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_weaver && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || e.HeroId == HeroId.npc_dota_hero_huskar && Owner.Distance2D(e) <= e.GetAttackRange() + 50
                            || (e.HeroId == HeroId.npc_dota_hero_windrunner && IsCasted(e.Spellbook.SpellR)))
                        && e.IsAttacking()
                        && Utils.SleepCheck("Ghost"))
                    {
                        Ghost.UseAbility();
                        Utils.Sleep(150, "Ghost");
                    }

                    //风杖吹别人	
                    if ((Ghost == null || !Ghost.CanBeCasted())
                        && (Sheep == null || !Sheep.CanBeCasted())
                        && (GhostAbility == null || !GhostAbility.CanBeCasted())
                        && Cyclone != null
                        && Cyclone.CanBeCasted()
                        && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                        && !Owner.IsAttackImmune()
                        && !e.IsHexed()
                        && !Owner.IsChanneling()
                        && !e.Modifiers.Any(y => y.Name == "modifier_tinker_laser_blind")
                        && !e.Modifiers.Any(y => y.Name == "modifier_skywrath_mystic_flare_aura_effect")
                        && Angle <= 0.03
                        && (e.HeroId == HeroId.npc_dota_hero_ursa
                        || e.HeroId == HeroId.npc_dota_hero_phantom_assassin
                        || e.HeroId == HeroId.npc_dota_hero_riki
                        || e.HeroId == HeroId.npc_dota_hero_sven
                        || e.HeroId == HeroId.npc_dota_hero_spectre
                        || e.HeroId == HeroId.npc_dota_hero_antimage
                        || e.HeroId == HeroId.npc_dota_hero_templar_assassin
                        || e.HeroId == HeroId.npc_dota_hero_morphling)
                        && e.IsAttacking()
                        && Utils.SleepCheck("Ghost"))
                    {
                        Cyclone.UseAbility(e);
                        Utils.Sleep(150, "Ghost");
                    }
                    else

                    //风杖吹自己
                    if ((Ghost == null || !Ghost.CanBeCasted())
                                    && (Sheep == null || !Sheep.CanBeCasted())
                                    && (GhostAbility == null || !GhostAbility.CanBeCasted())
                                    //&& (me.Spellbook.SpellE == null || !me.Spellbook.SpellE.CanBeCasted())
                                    && Cyclone != null
                                    && Cyclone.CanBeCasted()
                                    && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                                    && !Owner.IsAttackImmune()
                                    && !e.IsHexed()
                                    && e.Modifiers.Any(y => y.Name == "modifier_skywrath_mystic_flare_aura_effect") ////!!!!!!!!

                                    && Angle <= 0.03
                                    && (e.HeroId == HeroId.npc_dota_hero_ursa
                                        || e.HeroId == HeroId.npc_dota_hero_phantom_assassin
                                        || e.HeroId == HeroId.npc_dota_hero_riki
                                        || e.HeroId == HeroId.npc_dota_hero_spectre
                                        || e.HeroId == HeroId.npc_dota_hero_antimage

                                        || e.HeroId == HeroId.npc_dota_hero_templar_assassin
                                        || e.HeroId == HeroId.npc_dota_hero_morphling
                                        )

                                    && e.IsAttacking()
                                    && Utils.SleepCheck("Ghost"))
                    {
                        Cyclone.UseAbility(Owner);
                        Utils.Sleep(150, "Ghost");
                    }


                    else 
                    if ( //break special (1 hex, 2 cyclone)
                                    !Owner.IsChanneling()
                                    && Cyclone != null
                                    && Cyclone.CanBeCasted()
                                    && (Sheep == null || !Sheep.CanBeCasted())
                                    && Owner.Distance2D(e) <= 575 + Castrange + Ensage_error
                                    && !e.IsHexed()
                                    && !e.Modifiers.Any(y => y.Name == "modifier_skywrath_mystic_flare_aura_effect") ////!!!!!!!!

                                    && (
                                        //break special (1 hex, 2 cyclone)
                                        e.HeroId == HeroId.npc_dota_hero_riki && Owner.Modifiers.Any(y => y.Name == "modifier_riki_smoke_screen")
                                        || e.HeroId == HeroId.npc_dota_hero_spirit_breaker && e.Modifiers.Any(y => y.Name == "modifier_spirit_breaker_charge_of_darkness")
                                        || e.HeroId == HeroId.npc_dota_hero_phoenix && e.Modifiers.Any(y => y.Name == "modifier_phoenix_icarus_dive")
                                        || e.HeroId == HeroId.npc_dota_hero_magnataur && e.Modifiers.Any(y => y.Name == "modifier_magnataur_skewer_movement")

                                        )
                                    && Utils.SleepCheck("Ghost"))
                    {
                        Cyclone.UseAbility(e);
                        Utils.Sleep(150, "Ghost");
                    }

                    else 
                    if ( // Если ВРка
                            (Ghost == null || !Ghost.CanBeCasted())
                            && (GhostAbility == null || !GhostAbility.CanBeCasted())
                            && Cyclone != null
                            && Cyclone.CanBeCasted()
                            && !Owner.IsAttackImmune()
                            && !e.Modifiers.Any(y => y.Name == "modifier_skywrath_mystic_flare_aura_effect")
                            && e.HeroId == HeroId.npc_dota_hero_windrunner && IsCasted(e.Spellbook.SpellR)//&& e.Modifiers.Any(y => y.Name == "modifier_windrunner_focusfire")
                                                                                                              //&& e.IsAttacking() 
                           && Angle <= 0.03
                            && Utils.SleepCheck("Ghost")
                            )
                    {
                        Cyclone.UseAbility(e);
                        Utils.Sleep(150, "Ghost");
                    }
                }

                //自动抢人头
                if (Owner.IsAlive
                    && Owner.IsVisible
                    &&Game.IsKeyDown(Menu.Item("Killsteal Key").GetValue<KeyBind>().Key)
                    )
                {
                    if (e.Health < GetComboDamageByDistance(e)
                        && Owner.Mana >= ManaFactDamage(e)
                        && (!CanReflectDamage(e) || Owner.IsMagicImmune())
                        && !e.Modifiers.Any(y => y.Name == "modifier_abaddon_borrowed_time_damage_redirect")
                        && !e.Modifiers.Any(y => y.Name == "modifier_obsidian_destroyer_astral_imprisonment_prison")
                        && !e.Modifiers.Any(y => y.Name == "modifier_puck_phase_shift")
                        && !e.Modifiers.Any(y => y.Name == "modifier_eul_cyclone")
                        && !e.Modifiers.Any(y => y.Name == "modifier_dazzle_shallow_grave")
                        && !e.Modifiers.Any(y => y.Name == "modifier_brewmaster_storm_cyclone")
                        && !e.Modifiers.Any(y => y.Name == "modifier_shadow_demon_disruption")
                        && !e.Modifiers.Any(y => y.Name == "modifier_tusk_snowball_movement")
                        && !Owner.Modifiers.Any(y => y.Name == "modifier_pugna_nether_ward_aura")
                        && !Owner.IsSilenced()
                        && !Owner.IsHexed()
                        && !Owner.Modifiers.Any(y => y.Name == "modifier_doom_bringer_doom")
                        && !Owner.Modifiers.Any(y => y.Name == "modifier_riki_smoke_screen")
                        && !Owner.Modifiers.Any(y => y.Name == "modifier_disruptor_static_storm"))
                    {
                        if (Utils.SleepCheck("AUTOCOMBO") && !Owner.IsChanneling())
                        {
                            var EzkillCheck = EZKill(e);
                            var magicimune = (!e.IsMagicImmune() && !e.Modifiers.Any(x => x.Name == "modifier_eul_cyclone"));

                            if (!Owner.IsChanneling()
                                && Owner.CanAttack()
                                && !e.IsAttackImmune()
                                && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                                && OneHitLeft(e)
                                && e.NetworkPosition.Distance2D(Owner) <= Owner.GetAttackRange() + 50)
                            {
                                Owner.Attack(e);
                            }

                            if (Veil != null && Veil.CanBeCasted()
                                && magicimune
                                && e.NetworkPosition.Distance2D(Owner) <= 1600 + Castrange + Ensage_error
                                && !OneHitLeft(e)
                                && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && !e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff"))
                            {
                                if (Owner.Distance2D(e) > 1000 + Castrange + Ensage_error)
                                {
                                    var a = Owner.Position.ToVector2().FindAngleBetween(e.Position.ToVector2(), true);
                                    var p1 = new Vector3(
                                        Owner.Position.X + (Owner.Distance2D(e) - 500) * (float)Math.Cos(a),
                                        Owner.Position.Y + (Owner.Distance2D(e) - 500) * (float)Math.Sin(a),
                                        100);
                                    Veil.UseAbility(p1);
                                }
                                else if (Owner.Distance2D(e) <= 1000 + Castrange + Ensage_error)
                                {
                                    Veil.UseAbility(e.NetworkPosition);
                                }
                            }
                           
                            if (Ethereal != null && Ethereal.CanBeCasted()
                                && (!Veil.CanBeCasted() || e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff") || Veil == null)
                                && !OneHitLeft(e)
                                && magicimune
                                && e.NetworkPosition.Distance2D(Owner) <= 800 + Castrange + Ensage_error
                                && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind()))
                            {
                                Ethereal.UseAbility(e);
                            }

                            if (Dagon != null && Dagon.CanBeCasted()
                                && (!Veil.CanBeCasted() || e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff") || Veil == null )
                                && (Ethereal == null || (Ethereal != null && !IsCasted(Ethereal) && !Ethereal.CanBeCasted()) || e.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal"))
                                && !OneHitLeft(e)
                                && magicimune
                                && e.NetworkPosition.Distance2D(Owner) <= Dagondistance[Dagon.Level - 1] + Castrange + Ensage_error
                                && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind()))
                            {
                                Dagon.UseAbility(e);
                            }
                            if (Shiva != null && Shiva.CanBeCasted()
                                              && (!Veil.CanBeCasted() || e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff") || Veil == null)
                                              && (Ethereal == null || (Ethereal != null && !IsCasted(Ethereal) && !Ethereal.CanBeCasted()) || e.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal"))
                                              && !EzkillCheck
                                              && !OneHitLeft(e)
                                              && magicimune
                                              && e.NetworkPosition.Distance2D(Owner) <= 900 + Ensage_error
                                              && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind()))
                            {
                                Shiva.UseAbility();
                            }
                            if (PulseAbility.CanBeCasted()
                                && PulseAbility.GetAbilityData("area_of_effect") + Castrange >= e.NetworkPosition.Distance2D(Owner)
                            )
                            {
                                PulseAbility.UseAbility();
                            }
                            if (CanUltKill(e))
                            {
                                UltAbility.UseAbility(e);
                            }
                            Utils.Sleep(150, "AUTOCOMBO");
                        }
                    }
                }
            }
        }

        private void DrawRanges()
        {
            Castrange = 0;

            var aetherLens = Owner.Inventory.Items.FirstOrDefault(x => x.Id == AbilityId.item_aether_lens);
            if (aetherLens != null)
            {
                Castrange += (int)aetherLens.AbilitySpecialData.First(x => x.Name == "cast_range_bonus").Value;
            }

            
            if (Menu.Item("Blink Range").GetValue<bool>())
			{
				if (Blink != null)
				{	
					if(Rangedisplay_dagger == null)
					{
						Rangedisplay_dagger = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
						Range_dagger = 1200;						
						Rangedisplay_dagger.SetControlPoint(1, new Vector3(Range_dagger, 255, 5));
                        Rangedisplay_dagger.SetControlPoint(2, new Vector3(150, 255, 255));
                    }

					if (Range_dagger != 1200)
					{
						Range_dagger = 1200;

						if(Rangedisplay_dagger != null)
                        {
                            Rangedisplay_dagger.Dispose();
                        }
							
						Rangedisplay_dagger = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
                        Rangedisplay_dagger.SetControlPoint(1, new Vector3(Range_dagger, 255, 5));
                        Rangedisplay_dagger.SetControlPoint(2, new Vector3(150, 255, 255));
                    }
				}
                else
				{
					if(Rangedisplay_dagger != null)
                    {
                        Rangedisplay_dagger.Dispose();
                    }
						
					Rangedisplay_dagger = null;
				}

			}
			else if (Rangedisplay_dagger!=null)
			{
			    Rangedisplay_dagger.Dispose();
			    Rangedisplay_dagger = null;
			}
			

			if (Menu.Item("Pulse Range").GetValue<bool>())
			{
				if(Rangedisplay_rocket == null)
				{
				    Rangedisplay_rocket = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
				    Range_rocket = 475;
				    Rangedisplay_rocket.SetControlPoint(1, new Vector3(Range_rocket, 255, 5));
                    Rangedisplay_rocket.SetControlPoint(2, new Vector3(255, 255, 0));
                }
			}
			else if (Rangedisplay_rocket!=null)
			{
			    Rangedisplay_rocket.Dispose();
			    Rangedisplay_rocket = null;
			}
			
			
			
			if (Menu.Item("Ult Range").GetValue<bool>())
			{
				if(Rangedisplay_laser == null)
				{
				    Rangedisplay_laser = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
				    Range_laser = 600 + Castrange;
				    Rangedisplay_laser.SetControlPoint(1, new Vector3(Range_laser, 255, 5));
                    Rangedisplay_laser.SetControlPoint(2, new Vector3(0, 150, 255));
                }
				if (Range_laser != 600 + Castrange)
				{
					Range_laser = 600 + Castrange;
					if(Rangedisplay_laser != null)
						Rangedisplay_laser.Dispose();
					Rangedisplay_laser = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
                    Rangedisplay_laser.SetControlPoint(1, new Vector3(Range_laser, 255, 5));
                    Rangedisplay_laser.SetControlPoint(2, new Vector3(0, 150, 255));
                }				
			}
			else if (Rangedisplay_laser!=null)
			{
			    Rangedisplay_laser.Dispose();
			    Rangedisplay_laser = null;
			}
		}
		
        private void HandleEffectR(Unit unit)
        {
            if (unit == null)
            {
                return;
            }

            ParticleEffect effect;
			
            if (unit.Modifiers.Any(y => y.Name == "modifier_boots_of_travel_incoming") && Owner.HasModifier("modifier_teleporting"))
            {
                if (VisibleUnit.TryGetValue(unit, out effect))
                {
                    return;
                }

                effect = unit.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
				Range_dagger = 1200 + Castrange;
				effect.SetControlPoint(1, new Vector3(Range_dagger, 255, 5));
                effect.SetControlPoint(2, new Vector3(150, 255, 255));
                VisibleUnit.Add(unit, effect);
            }
            else
            {
                if (!VisibleUnit.TryGetValue(unit, out effect))
                {
                    return;
                }

                effect.Dispose();
                VisibleUnit.Remove(unit);
            }
        }
		
        private void HandleEffectD(Unit unit)
        {
            if (unit == null)
            {
                return;
            }
			
            if (unit != null && unit.IsValid && unit.IsAlive && unit.Modifiers.Any(y => y.Name == "modifier_boots_of_travel_incoming") && Owner.HasModifier("modifier_teleporting"))
			{
				if (Effect2 == null)
				{
					Effect2 = new ParticleEffect(@"materials\ensage_ui\particles\line.vpcf", unit);     
					Effect2.SetControlPoint(1, unit.Position);
					Effect2.SetControlPoint(2, FindVector(unit.Position, Owner.Rotation, 1200 + Castrange));
                    Effect2.SetControlPoint(3, new Vector3(100, 70, 10));
                    Effect2.SetControlPoint(4, new Vector3(150, 255, 255));
                }
				else 
				{
					Effect2.SetControlPoint(1, unit.Position);
					Effect2.SetControlPoint(2, FindVector(unit.Position, Owner.Rotation, 1200 + Castrange));
                    Effect2.SetControlPoint(3, new Vector3(100, 70, 10));
                    Effect2.SetControlPoint(4, new Vector3(150, 255, 255));
                } 
			}
			if (!Owner.HasModifier("modifier_teleporting") && Effect2 != null)
			{
			   Effect2.Dispose();
			   Effect2 = null;
			}	
        }

        private Vector3 FindVector(Vector3 first, double ret, float distance)
        {
            var retVector = new Vector3(first.X + (float) Math.Cos(Utils.DegreeToRadian(ret)) * distance, first.Y + (float) Math.Sin(Utils.DegreeToRadian(ret)) * distance, 100);

            return retVector;
        }

        private void OnUpdateAbility()
        {
            Abilities = new Abilities(Owner);
        }

        private Vector2 HeroPositionOnScreen(Hero x)
        {
            return new Vector2(HUDInfo.GetHPbarPosition(x).X - 1, HUDInfo.GetHPbarPosition(x).Y - 40);
        }

        private bool CanReflectDamage(Hero x)
        {
            if (x.Modifiers.Any(m => (m.Name == "modifier_item_blade_mail_reflect" ) || (m.Name == "modifier_nyx_assassin_spiked_carapace") || (m.Name == "modifier_item_lotus_orb_active")))
            {
                return true;
            }

            return false;
        }

        private bool IsEulhexFind()
        {
            if ((Cyclone != null && Cyclone.CanBeCasted()) || (Sheep != null && Sheep.CanBeCasted()))
            {
                return true;
            }

            return false;
        }
		
        private bool IsCasted(Ability ability)
        {
            return ability.Level > 0 && ability.CooldownLength > 0 && Math.Ceiling(ability.CooldownLength).Equals(Math.Ceiling(ability.Cooldown));
        }

        private bool IsPhysDamageImune(Hero v)
        {
            if (Owner.Modifiers.Any(x =>
                                    x.Name == "modifier_tinker_laser_blind" || 
                                    x.Name == "modifier_troll_warlord_whirling_axes_blind" || 
                                    x.Name == "modifier_brewmaster_drunken_haze" || 
                                    x.Name == "modifier_pugna_decrepify" || 
                                    x.Name == "modifier_item_ethereal_blade_ethereal") ||
                                    v.Modifiers.Any(x => x.Name == "modifier_omniknight_guardian_angel"
									|| x.Name == "modifier_nyx_assassin_spiked_carapace"
									|| x.Name == "modifier_pugna_decrepify"
									|| x.Name == "modifier_windrunner_windrun"
									|| x.Name == "modifier_winter_wyverny_cold_embrace"
									|| x.Name == "modifier_ghost_state" 
									|| x.Name == "modifier_item_ethereal_blade_ethereal")
                                    || (v.HeroId == HeroId.npc_dota_hero_tiny && v.Spellbook.SpellE.Level > 0)|| v.IsInvul())
            {
                return true;
            }

            return false;
        }


		private int ManaFactDamage(Hero en)
        {
            if (en != null && en.IsAlive && en.IsValid)
            {
				int manadagon = 0, dagondist = 0, manaethereal = 0, manashiva = 0, manasoulring = 0;
                uint pulseMana = 0, ultMana = 0;
				if (PulseAbility != null && PulseAbility.Level> 0 && PulseAbility.CanBeCasted())
				{
				    pulseMana = PulseAbility.ManaCost;
				}
				else
                {
                    pulseMana = 0;
                }
                if (UltAbility != null && UltAbility.Level > 0   && UltAbility.CanBeCasted())
                {
                    ultMana = UltAbility.ManaCost;
                }
				else
                {
                    ultMana = 0;
                }
					
				if (Dagon != null && Dagon.CanBeCasted())
				{
					dagondist = Dagondistance[Dagon.Level - 1];
					manadagon = 180;
				}
				else
				{
					manadagon = 0;		
					dagondist = 0;
				}	

				if (Ethereal != null && Ethereal.CanBeCasted())
                {
                    manaethereal = 100;
                }
				else
                {
                    manaethereal = 0;
                }
					
				if (Shiva != null && Shiva.CanBeCasted())
                {
                    manashiva = 100;
                }
				else
                {
                    manashiva = 0;
                }
					
				if (Soulring != null && Soulring.CanBeCasted())
                {
                    manasoulring = 150;
                }
				else
                {
                    manasoulring = 0;
                }

                //factical mana consume in current range
                var test= (Owner.Distance2D(en) < PulseAbility.CastRange + Castrange + Ensage_error ? pulseMana : 0)
                          + (Owner.Distance2D(en) < UltAbility.CastRange ? ultMana : 0)
                          + (Owner.Distance2D(en) < dagondist + Castrange + Ensage_error ? manadagon : 0)
                          + (Owner.Distance2D(en) < 900 + Ensage_error ? manashiva : 0)
                          - manasoulring;

                return (int)test;
            }

            return 0;
        }



        private bool OneHitLeft(Hero en)
		{
			
			if (((en.Health < GetComboDamageByDistance(en)) && (en.Health > GetComboDamageByDistance(en) - GetOneAutoAttackDamage(en)))
				&& !IsPhysDamageImune(en)
				&& Owner.Distance2D(en) < Owner.GetAttackRange()+50)
            {
                return true;
            }

            return false;
        }


    
        //EZKill will only consider if dagon, ethereal blade and veil will kill the enemy
        private bool EZKill(Hero enemy)
        {
            if (enemy != null && enemy.IsAlive && enemy.IsValid)
            {
                var etheral_blade_magic_reduction = 0.0f;
                var veil_of_discord_magic_reduction = 0.0f;

                var eblade = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_ethereal_blade"));
                if (eblade != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))
                {
                    etheral_blade_magic_reduction = 0.4f;
                }

                var veil = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_veil_of_discord"));
                if (veil != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(veil.Name))
                {
                    veil_of_discord_magic_reduction = 0.25f;
                }

                var totalMagicResistance = ((1 - enemy.MagicDamageResist) * (1 + etheral_blade_magic_reduction) * (1 + veil_of_discord_magic_reduction));
                if (enemy.Health < (GetEtherealBladeDamage() + GetDagonDamage()) * totalMagicResistance)
                {
                    return true;
                }

                return false;
            }

            return false;
        }

        private float GetEZKillDamage(Hero enemy)
        {
            if (enemy != null && enemy.IsAlive && enemy.IsValid)
            {
                var etheral_blade_magic_reduction = 0.0f;
                var veil_of_discord_magic_reduction = 0.0f;

                var eblade = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_ethereal_blade"));
                if (eblade != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))
                {
                    etheral_blade_magic_reduction = 0.4f;
                }

                var veil = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_veil_of_discord"));
                if (veil != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(veil.Name))
                {
                    veil_of_discord_magic_reduction = 0.25f;
                }

                var totalMagicResistance = ((1 - enemy.MagicDamageResist) * (1 + etheral_blade_magic_reduction) * (1 + veil_of_discord_magic_reduction));
                return (GetEtherealBladeDamage() + GetDagonDamage()) * totalMagicResistance;
            }

            return 0.0f;
        }
        //计算对敌伤害加深
        private float GetSpellAmpWithEnemy(Hero enemy)
        {
            var spellAmp = 1f;
            if (enemy.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal"))
            {
                spellAmp *= 140 / 100.0f;
            }

            if (enemy.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff"))
            {
                spellAmp *= 125 / 100.0f;
            }
            return spellAmp;
        }
        //计算自身伤害加深
        private float GetSpellAmp()
        {
            //data from https://dota2.gamepedia.com/Intelligence
            var spellAmp = (100.0f + Owner.TotalIntelligence * 0.0875f) / 100.0f;
            var kaya = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_kaya") || x.Name.Contains("item_yasha_and_kaya") || x.Name.Contains("item_kaya_and_sange"));
            if (kaya != null)
            {
                var kayaAmp = (100f + kaya.AbilitySpecialData.First(x => x.Name == "spell_amp").Value) / 100.0f;
                spellAmp *= kayaAmp;
            }
            return spellAmp;
        }

        //计算连招伤害
        private float GetComboDamageByDistance(Hero enemy)
        {
            if (enemy != null && enemy.IsAlive && enemy.IsValid)
            {
                var comboDamageByDistance = 0.0f;
                var etheral_blade_magic_reduction = 0.0f;
                var veil_of_discord_magic_reduction = 0.0f;
                //判断虚灵刀加深伤害40%
                var eblade = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_ethereal_blade"));

                if (((eblade != null && eblade.CanBeCasted())
                    || (eblade != null && IsCasted(eblade)))
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_ethereal_blade")
                    && !enemy.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal"))
                {
                    etheral_blade_magic_reduction = 0.4f;
                }

                //纷争面纱魔法抗性降低25%
                var veil = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_veil_of_discord"));
                if (veil != null
                    && veil.CanBeCasted()
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_veil_of_discord")
                    && !enemy.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff"))
                {
                    veil_of_discord_magic_reduction = 0.25f;
                }
                //计算魔法加深
                var totalMagiceduction= (1 + etheral_blade_magic_reduction) * (1 + veil_of_discord_magic_reduction);
                var totalMagicResistance = ((1 - enemy.MagicDamageResist) * (1 + etheral_blade_magic_reduction) * (1 + veil_of_discord_magic_reduction));
                //大根
                var dagon = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_dagon"));
                if (dagon != null
                    && dagon.CanBeCasted()
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_dagon")
                    && (Owner.Distance2D(enemy) < dagon.AbilitySpecialData.First(x => x.Name == "#AbilityCastRange").GetValue(dagon.Level - 1) + Castrange + Ensage_error))
                {
                    comboDamageByDistance += GetDagonDamage() * totalMagicResistance;
                }
                //虚灵
                if (((eblade != null && eblade.CanBeCasted())
                    || (eblade != null && IsCasted(eblade)))
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_ethereal_blade")
                    && (Owner.Distance2D(enemy) < 800 + Castrange + Ensage_error))
                {
                    comboDamageByDistance += GetEtherealBladeDamage() * totalMagicResistance;
                }
                //死亡脉冲
                if (PulseAbility != null
                    && PulseAbility.Level > 0
                    && PulseAbility.CanBeCasted()
                    && (Owner.Distance2D(enemy) < 475 + Castrange + Ensage_error))
                {
                    comboDamageByDistance += GetPulseDamage(enemy)* totalMagiceduction;
                }
                
                if (Owner.CanAttack()
                    && !enemy.IsAttackImmune()
                    && Owner.Distance2D(enemy) < Owner.GetAttackRange() + 50
                    && !enemy.IsInvul()
                    && !IsPhysDamageImune(enemy))
                {
                    comboDamageByDistance += (enemy.DamageTaken(Owner.BonusDamage + Owner.DamageAverage, DamageType.Physical, Owner));
                }

                comboDamageByDistance += GetRingDamge(enemy, 0.5f) * totalMagiceduction;

                //计算大招伤害
                if (UltAbility != null
                    && UltAbility.Level > 0
                    && UltAbility.CanBeCasted(enemy))
                {
                    comboDamageByDistance += GetUltDamge(enemy, comboDamageByDistance)*totalMagiceduction;

                }
                return comboDamageByDistance;
            }

            return 0;
        }




        private bool CanUltKill(Hero enemy, float preDamage = 0)
        {
            var damageByAttack = 0f;
            if (Owner.CanAttack()
                && !enemy.IsAttackImmune()
                && Owner.Distance2D(enemy) < Owner.GetAttackRange() + 50
                && !enemy.IsInvul()
                && !IsPhysDamageImune(enemy))
            {
                damageByAttack += (enemy.DamageTaken(Owner.BonusDamage + Owner.DamageAverage, DamageType.Physical, Owner));
            }

            var damageByAura=GetRingDamge(enemy);
            if (UltAbility.CanBeCasted(enemy) && GetUltDamge(enemy,preDamage)+ damageByAttack+ damageByAura >= enemy.Health)
            {
                return true;
            }

            return false;
        }
        private float GetRingDamge(Hero enemy,float m=2)
        {
            var damage = 0.0f;
            var ringAbility = Owner.Spellbook.Spell3;
            if (ringAbility.Level > 0)
            {
                 var damage_per_health = ringAbility.GetAbilityData("aura_damage", ringAbility.Level)/100f;
                damage = enemy.MaximumHealth * damage_per_health * GetSpellAmp() * GetSpellAmpWithEnemy(enemy) * m;
            }
            return damage;
        }
        private float GetUltDamge(Hero enemy,float preDamage=0)
        {
            var damage = 0.0f;
            if (UltAbility.Level > 0)
            {
                var damage_per_health = UltAbility.GetAbilityData("damage_per_health",UltAbility.Level);
                damage = (enemy.MaximumHealth - (enemy.Health - preDamage)) * damage_per_health * GetSpellAmp() *
                         GetSpellAmpWithEnemy(enemy)* (1 - enemy.MagicDamageResist);
            }
            return damage;
        }

        private float GetPulseDamage(Hero enemy)
        {

            var damage = 0.0f;
            if (PulseAbility.Level > 0)
            {
                damage += PulseAbility.GetDamage(PulseAbility.Level - 1);
            }
            damage *= GetSpellAmp();
            damage *= GetSpellAmpWithEnemy(enemy) * (1 - enemy.MagicDamageResist);
            return damage;
        }

        private float GetRocketDamage()
        {
            return 0;
        }

        private float GetDagonDamage()
        {
            var dagonDamage = 0.0f;
            var totalSpellAmp = 0.0f;

            var dagon = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_dagon"));
            if (dagon != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_dagon"))
            {
                dagonDamage += (dagon.AbilitySpecialData.FirstOrDefault(x => x.Name == "damage").GetValue(dagon.Level - 1));
            }
            dagonDamage *= GetSpellAmp();

            return dagonDamage;
        }

        private float GetOneAutoAttackDamage(Hero enemy)
        {
            if (Owner.CanAttack() && !enemy.IsAttackImmune() && !enemy.IsInvul() && !IsPhysDamageImune(enemy))
            {
                return (enemy.DamageTaken(Owner.BonusDamage + Owner.DamageAverage, DamageType.Physical, Owner));
            }

            return 0;
        }

        private float GetEtherealBladeDamage()
        {
            var etherealBladeDamage = 0.0f;
            var totalSpellAmp = 0.0f;

            var eblade = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_ethereal_blade"));

            if (eblade != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))
            {
                etherealBladeDamage += ((Owner.TotalIntelligence * eblade.AbilitySpecialData.FirstOrDefault(x => x.Name == "blast_agility_multiplier").Value) + eblade.AbilitySpecialData.FirstOrDefault(x => x.Name == "blast_damage_base").Value);
            }
            etherealBladeDamage *= GetSpellAmp(); 

            return etherealBladeDamage;
        }
    }
}
