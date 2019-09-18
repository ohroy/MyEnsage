using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.Common.Threading;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;

using SharpDX;


namespace wtf.tinker
{
    [ExportPlugin(name: "wtf.tinker", author: "rozbo", version: "3.1.0.1", units: HeroId.npc_dota_hero_tinker)]
    internal class Main : Plugin
    {
        [ImportingConstructor]
        public Main([Import] IEntityContext<Unit> entityContext)
        {
            Owner = entityContext.Owner as Hero;
        }
        [Import("abilities")]
        private wtf.tinker.Models.Abilities _abilities;
        [Import("helper")]
        private Helper _helper;
        private Task RearmBlink { get; set; }

        private int[] RearmTime { get; } = { 3010, 1510, 760 };

        private int Time { get; set; }

        private int GetRearmTime(Ability s) => RearmTime[s.Level - 1];

        private int HIDE_AWAY_RANGE { get; } = 130;

        private bool Iscreated { get; set; }

        private Abilities Abilities;

        private Ability Laser => Abilities.Laser;

        private Ability Rocket => Abilities.Rocket;

        private Ability March => Abilities.March;

        private Ability Refresh => Abilities.Refresh;

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

        private Menu Menu { get; } = new Menu("wtf.tinker", "tinker", true, "npc_dota_hero_tinker", true).SetFontColor(Color.Aqua);

        private Menu _Combo { get; } = new Menu("Combo", "Combo");

        private Menu _RocketSpam { get; } = new Menu("Rocket Spam", "Rocket Spam");

        private Menu _MarchSpam { get; } = new Menu("March Spam", "March Spam");

        private int Red => Menu.Item("red").GetValue<Slider>().Value;

        private int Green => Menu.Item("green").GetValue<Slider>().Value;

        private int Blue => Menu.Item("blue").GetValue<Slider>().Value;

        private bool BlockRearm => Menu.Item("BlockRearm").GetValue<bool>();

        private bool NoBlockRearmFountain => Menu.Item("NoBlockRearmFountain").GetValue<bool>();

        private bool NoBlockRearmTeleporting => Menu.Item("NoBlockRearmTeleporting").GetValue<bool>();

        private bool FastRearmBlink => Menu.Item("FastRearmBlink").GetValue<KeyBind>().Active;

        private Dictionary<string, bool> ComboSkills { get; } = new Dictionary<string, bool>
        {
            { "tinker_rearm",true},
            { "tinker_march_of_the_machines",true},
            { "tinker_heat_seeking_missile",true},
            { "tinker_laser",true}
        };

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
            {"tinker_laser",true}
        };

        private Dictionary<string, bool> RocketSpamSkills { get; } = new Dictionary<string, bool>
        {
            {"tinker_rearm",true},
            {"tinker_heat_seeking_missile",true},
        };

        private Dictionary<string, bool> RocketSpamItems { get; } = new Dictionary<string, bool>
        {
            {"item_blink",false},
            {"item_glimmer_cape",true},
            {"item_bottle",true},
            {"item_soul_ring",true},
            {"item_ghost",true},
            {"item_ethereal_blade",false},
        };

        private Dictionary<string, bool> MarchSpamItems { get; } = new Dictionary<string, bool>
        {
            {"item_blink",false},
            {"item_glimmer_cape",false},
            {"item_bottle",true},
            {"item_soul_ring",true},
            {"item_ghost",false}
        };

        private string[] SoulringSpells { get; } =
        {
            "tinker_heat_seeking_missile",
            "tinker_rearm",
            "tinker_march_of_the_machines"
        };

        private int[] Laser_mana { get; } = new int[4] { 110, 130, 150, 170 };

        private int[] Rocket_mana { get; } = new int[4] { 80, 100, 120, 140 };

        private int[] Rearm_mana { get; } = new int[3] { 100, 200, 300 };

        private int[] Dagondistance { get; } = new int[5] { 600, 650, 700, 750, 800 };

        private int Ensage_error { get; } = 50;

        private int Castrange { get; set; } = 0;

        private double Angle { get; set; }

        private ParticleEffect Rangedisplay_dagger { get; set; }

        private ParticleEffect Rangedisplay_rocket { get; set; }

        private ParticleEffect Rangedisplay_laser { get; set; }

        private ParticleEffect Effect2 { get; set; }

        private ParticleEffect Effect3 { get; set; }

        private ParticleEffect Effect4 { get; set; }

        private ParticleEffect Blinkeffect { get; set; }

        private int Range_dagger { get; set; }

        private int Range_rocket { get; set; }

        private int Range_laser { get; set; }

        protected override void OnActivate()
        {
            //初始化技能
            _abilities.install();
            UpdateManager.BeginInvoke(() =>
            {
                // Menu Options	                                                          
                _Combo.AddItem(new MenuItem("ComboSkills: ", "Skills:").SetValue(new AbilityToggler(ComboSkills)));
                _Combo.AddItem(new MenuItem("ComboItems: ", "Items:").SetValue(new AbilityToggler(ComboItems)));
                _Combo.AddItem(new MenuItem("LinkenBreaker: ", "Linken Breaker:").SetValue(new AbilityToggler(LinkenBreaker)));
                Menu.AddSubMenu(_Combo);

                _RocketSpam.AddItem(new MenuItem("RocketSpamSkills: ", "Skills:").SetValue(new AbilityToggler(RocketSpamSkills)));
                _RocketSpam.AddItem(new MenuItem("RocketSpamItems: ", "Items:").SetValue(new AbilityToggler(RocketSpamItems)));
                Menu.AddSubMenu(_RocketSpam);

                _MarchSpam.AddItem(new MenuItem("MarchSpamItems: ", "Items:").SetValue(new AbilityToggler(MarchSpamItems)));
                Menu.AddSubMenu(_MarchSpam);

                var _autopush = new Menu("Auto Push", "Auto Push");
                _autopush.AddItem(new MenuItem("autoPush", "Enable auto push helper").SetValue(false));
                _autopush.AddItem(new MenuItem("autoRearm", "Enable auto rearm in fountain when travel boots on cooldown").SetValue(false));
                _autopush.AddItem(new MenuItem("pushFount", "Use auto push if I have modif Fountain").SetValue(false));
                _autopush.AddItem(new MenuItem("pushSafe", "Use march only after blinking to a safe spot").SetValue(false));
                Menu.AddSubMenu(_autopush);

                var _ranges = new Menu("Drawing", "Drawing");
                _ranges.AddItem(new MenuItem("Blink Range", "Show Blink Dagger Range").SetValue(true));
                _ranges.AddItem(new MenuItem("Blink Range Incoming TP", "Show incoming TP Blink Range").SetValue(true));
                _ranges.AddItem(new MenuItem("Rocket Range", "Show Rocket Range").SetValue(true));
                _ranges.AddItem(new MenuItem("Laser Range", "Show Laser Range").SetValue(true));
                _ranges.AddItem(new MenuItem("Show Direction", "Show Direction Vector on Rearming").SetValue(true));
                _ranges.AddItem(new MenuItem("Show Target Effect", "Show Target Effect").SetValue(true));
                _ranges.AddItem(new MenuItem("red", "Red").SetValue(new Slider(0, 0, 255)).SetFontColor(Color.Red));
                _ranges.AddItem(new MenuItem("green", "Green").SetValue(new Slider(255, 0, 255)).SetFontColor(Color.Green));
                _ranges.AddItem(new MenuItem("blue", "Blue").SetValue(new Slider(255, 0, 255)).SetFontColor(Color.Blue));
                Menu.AddSubMenu(_ranges);

                var _blockrearm = new Menu("Blocker Rearm", "Blocker Rearm");
                Menu.AddSubMenu(_blockrearm);
                _blockrearm.AddItem(new MenuItem("BlockRearm", "Block Rearm").SetValue(true)).SetTooltip("It does not allow double-cast rearm");
                _blockrearm.AddItem(new MenuItem("NoBlockRearmFountain", "No Block Rearm in Fountain").SetValue(true));
                _blockrearm.AddItem(new MenuItem("NoBlockRearmTeleporting", "No Block Rearm with Teleporting").SetValue(true));

                var _settings = new Menu("Settings", "Settings UI");
                _settings.AddItem(new MenuItem("HitCounter", "Enable target hit counter").SetValue(true));
                _settings.AddItem(new MenuItem("RocketCounter", "Enable target rocket counter").SetValue(true));
                _settings.AddItem(new MenuItem("TargetCalculator", "Enable target dmg calculator").SetValue(true));
                _settings.AddItem(new MenuItem("Calculator", "Enable UI calculator").SetValue(true));
                _settings.AddItem(new MenuItem("BarPosX", "UI Calculator Position X").SetValue(new Slider(600, -1500, 1500)));
                _settings.AddItem(new MenuItem("BarPosY", "UI Calculator Position Y").SetValue(new Slider(0, -1500, 1500)));
                _settings.AddItem(new MenuItem("CalculatorRkt", "Enable Rocket calculator").SetValue(true));
                _settings.AddItem(new MenuItem("BarPosXr", "Rocket Calc Position X").SetValue(new Slider(950, -1500, 1500)));
                _settings.AddItem(new MenuItem("BarPosYr", "Rocket Calc Position Y").SetValue(new Slider(-300, -1500, 1500)));
                _settings.AddItem(new MenuItem("ComboModeDrawing", "Enable Combo Mode drawing").SetValue(true));
                _settings.AddItem(new MenuItem("debug", "Enable debug").SetValue(false));
                Menu.AddSubMenu(_settings);

                Menu.AddItem(new MenuItem("Combo Key", "Combo Key").SetValue(new KeyBind('D', KeyBindType.Press)));
                Menu.AddItem(new MenuItem("ComboMode", "Combo Mode")).SetValue(new StringList(new[] { "Fast", "MpSaving" }));
                Menu.AddItem(new MenuItem("TargetLock", "Target Lock")).SetValue(new StringList(new[] { "Free", "Lock" }));
                Menu.AddItem(new MenuItem("Chase", "Chase Toggle").SetValue(new KeyBind('F', KeyBindType.Toggle, false)).SetTooltip("Toggle for chasing"));

                Menu.AddItem(new MenuItem("Rocket Spam Key", "Rocket Spam Key").SetValue(new KeyBind('W', KeyBindType.Press)));
                Menu.AddItem(new MenuItem("March Spam Key", "March Spam Key").SetValue(new KeyBind('E', KeyBindType.Press)));
                Menu.AddItem(new MenuItem("FastRearmBlink", "Fast Rearm Blink").SetValue(new KeyBind(32, KeyBindType.Press)));

                Menu.AddItem(new MenuItem("autoDisable", "Auto disable/counter enemy").SetValue(true));
                Menu.AddItem(new MenuItem("autoKillsteal", "Auto killsteal enemy").SetValue(true));
                Menu.AddToMainMenu();

                Orbwalking.Load();

                OnUpdateAbility();
                UpdateManager.Subscribe(OnUpdateAbility, 500);

                Game.OnUpdate += ComboEngine;
                Game.OnUpdate += AD;

                GameDispatcher.OnUpdate += OnUpdate;
                Player.OnExecuteOrder += OnExecuteOrder;

                Drawing.OnDraw += Information;

                UpdateManager.Subscribe(DrawRanges, 50);
                ParticleDraw();
            },
            5000);
        }

        protected override void OnDeactivate()
        {
            _abilities?.uninstall();
            Menu.RemoveFromMainMenu();
        }

        public async void OnUpdate(EventArgs args)
        {
            if (FastRearmBlink && Utils.SleepCheck("updateAdd"))
            {
                var safeRange = 1200 + Castrange;
                var blinkparticlerange = Game.MousePosition;

                if (Owner.Distance2D(Game.MousePosition) > safeRange + Ensage_error)
                {
                    var tpos = Owner.Position;
                    var a = tpos.ToVector2().FindAngleBetween(Game.MousePosition.ToVector2(), true);

                    safeRange -= (int)Owner.HullRadius;
                    blinkparticlerange = new Vector3(tpos.X + safeRange * (float)Math.Cos(a), tpos.Y + safeRange * (float)Math.Sin(a), 100);
                }

                Blinkeffect?.Dispose();
                Blinkeffect = new ParticleEffect("materials/ensage_ui/particles/tinker_blink.vpcf", blinkparticlerange);
                Blinkeffect.SetControlPoint(1, new Vector3(0, 255, 255));
                Blinkeffect.SetControlPoint(2, new Vector3(255));
                Effects.Add(Blinkeffect);
                Utils.Sleep(2000, "updateAdd");
            }
            else if (FastRearmBlink && Utils.SleepCheck("updateRemover"))
            {
                DelayAction.Add(Time, () =>
                {
                    Blinkeffect?.Dispose();
                });

                Utils.Sleep(2000, "updateRemover");
            }

            if (RearmBlink != null && !RearmBlink.IsCompleted)
            {
                return;
            }

            if (FastRearmBlink)
            {
                RearmBlink = Action(new CancellationTokenSource().Token);

                try
                {
                    await RearmBlink;
                    RearmBlink = null;
                }
                catch (OperationCanceledException)
                {
                    RearmBlink = null;
                }
            }
        }

        private async Task Action(CancellationToken cancellationToken)
        {
            if (Utils.SleepCheck("FASTBLINK"))
            {
                Owner.MoveToDirection(Game.MousePosition);
                Utils.Sleep(100, "FASTBLINK");
            }

            var fastblink = Game.MousePosition;
            var rearm = Refresh;
            if (rearm.CanBeCasted())
            {
                DelayAction.Add(50, () =>
                {
                    var blinkrange = 1200 + Castrange;
                    if (Owner.Distance2D(Game.MousePosition) > blinkrange + Ensage_error)
                    {
                        var tpos = Owner.Position;
                        var a = tpos.ToVector2().FindAngleBetween(Game.MousePosition.ToVector2(), true);

                        blinkrange -= (int)Owner.HullRadius;
                        fastblink = new Vector3(
                            tpos.X + blinkrange * (float)Math.Cos(a),
                            tpos.Y + blinkrange * (float)Math.Sin(a),
                            100);
                    }
                    rearm?.UseAbility();
                });

                Time = (int)(GetRearmTime(rearm) + Game.Ping + 50 + rearm.FindCastPoint() * 1000);
                await Task.Delay(Time, cancellationToken);
            }

            Blink?.UseAbility(fastblink);
            await Task.Delay(0, cancellationToken);

            Blink?.UseAbility(fastblink);
            await Task.Delay(10, cancellationToken);

            Blink?.UseAbility(fastblink);
            await Task.Delay(20, cancellationToken);

            Blink?.UseAbility(fastblink);
            await Task.Delay(30, cancellationToken);

            Blink?.UseAbility(fastblink);
            await Task.Delay(50, cancellationToken);
        }
        private void OnExecuteOrder(Player sender, ExecuteOrderEventArgs args)
        {
            if (!BlockRearm)
            {
                return;
            }

            if ((!Owner.HasModifier("modifier_fountain_aura_buff") || !NoBlockRearmFountain) && (!Owner.HasModifier("modifier_teleporting") || !NoBlockRearmTeleporting))
            {
                if (args.Ability?.Name == "tinker_rearm" && args.OrderId == OrderId.Ability && (Owner.IsChanneling() || args.Ability.IsInAbilityPhase))
                {
                    args.Process = false;
                }
            }
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

            var safe = GetClosestToVector(SafePos, Owner);

            Castrange = 0;

            var aetherLens = Owner.Inventory.Items.FirstOrDefault(x => x.Id == AbilityId.item_aether_lens);
            if (aetherLens != null)
            {
                Castrange += (int)aetherLens.AbilitySpecialData.First(x => x.Name == "cast_range_bonus").Value;
            }

            var talent10 = Owner.Spellbook.Spells.First(x => x.Name == "special_bonus_cast_range_100");
            if (talent10.Level > 0)
            {
                Castrange += (int)talent10.AbilitySpecialData.First(x => x.Name == "value").Value;
            }

            //Auto Push
            // ghost -> glimmer -> march -> move or blink to safe -> soulring -> rearm
            if (Menu.Item("autoPush").IsActive()
                && !Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key)
                && !Game.IsKeyDown(Menu.Item("Rocket Spam Key").GetValue<KeyBind>().Key)
                && !Game.IsKeyDown(Menu.Item("March Spam Key").GetValue<KeyBind>().Key)
                && !Game.IsChatOpen
                && Owner.IsAlive)
            {
                if ((Owner.HasModifier("modifier_fountain_aura_buff") && Menu.Item("pushFount").IsActive()))
                {
                    if (Owner.IsChanneling() || Owner.HasModifier("modifier_tinker_rearm") || Refresh == null) return;

                    if (creeps.Count(x => x.Distance2D(Owner) <= 1100) >= 1)
                    {
                        if (Ghost != null
                            && Ghost.CanBeCasted()
                            && Utils.SleepCheck("ghost"))
                        {
                            Ghost.UseAbility();
                            Utils.Sleep(250, "ghost");
                        }

                        if (Glimmer != null
                            && Glimmer.CanBeCasted()
                            && creeps.Count(x => x.Distance2D(Owner) <= 1100) >= 2
                            && Owner.Distance2D(safe) >= HIDE_AWAY_RANGE
                            && Utils.SleepCheck("glimmer"))
                        {
                            Glimmer.UseAbility(Owner);
                            Utils.Sleep(250, "glimmer");
                        }

                        if (March != null && March.CanBeCasted()
                            && !Refresh.IsChanneling
                            && (Owner.Distance2D(safe) <= HIDE_AWAY_RANGE
                            || !Menu.Item("pushSafe").IsActive())
                            && creeps.Count(x => x.Distance2D(Owner) <= 900) >= 2
                            && Utils.SleepCheck("March"))
                        {
                            var closestCreep = ObjectManager.GetEntities<Creep>().Where(creep =>
                                   (creep.NetworkName == "CDOTA_BaseNPC_Creep_Lane"
                                   || creep.NetworkName == "CDOTA_BaseNPC_Creep_Siege"
                                   || creep.NetworkName == "CDOTA_BaseNPC_Creep_Neutral"
                                   || creep.NetworkName == "CDOTA_BaseNPC_Creep") &&
                                   creep.IsAlive && creep.Team != Owner.Team && creep.IsVisible && creep.IsSpawned).MinOrDefault(x => x.Distance2D(Owner)).Position;

                            March.UseAbility(Vector3.Add(Owner.Position, Vector3.Multiply(Vector3.Subtract(closestCreep, Owner.Position), 0.1f)));
                            Utils.Sleep(500, "March");
                        }

                        if (March != null && March.CanBeCasted()
                            && !Refresh.IsChanneling
                            && (creeps.Count(x => x.Distance2D(safe) <= 900) <= 1 || Owner.Distance2D(safe) >= 1190 + Castrange)
                            && creeps.Count(x => x.Distance2D(Owner) <= 900) >= 2
                            && Utils.SleepCheck("March"))
                        {
                            var closestCreep = ObjectManager.GetEntities<Creep>().Where(creep =>
                                   (creep.NetworkName == "CDOTA_BaseNPC_Creep_Lane"
                                   || creep.NetworkName == "CDOTA_BaseNPC_Creep_Siege"
                                   || creep.NetworkName == "CDOTA_BaseNPC_Creep_Neutral"
                                   || creep.NetworkName == "CDOTA_BaseNPC_Creep") &&
                                   creep.IsAlive && creep.Team != Owner.Team && creep.IsVisible && creep.IsSpawned).MinOrDefault(x => x.Distance2D(Owner)).Position;

                            March.UseAbility(Vector3.Add(Owner.Position, Vector3.Multiply(Vector3.Subtract(closestCreep, Owner.Position), 0.1f)));
                            Utils.Sleep(500, "March");
                        }

                        if (March != null && !March.CanBeCasted()
                            && !Refresh.IsChanneling
                            && Owner.Distance2D(safe) >= (1190 + Castrange)
                            && Utils.SleepCheck("March"))
                        {
                            Owner.Move(safe);
                            Utils.Sleep(500, "March");
                        }

                        if (Blink != null
                            && Owner.CanCast()
                            && (Menu.Item("pushSafe").IsActive()
                            || !March.CanBeCasted())
                            && !Refresh.IsChanneling
                            && Blink.CanBeCasted())
                        {
                            if (Owner.Distance2D(safe) <= (1190 + Castrange)
                                && Owner.Distance2D(safe) >= 100
                                && Utils.SleepCheck("blink"))
                            {
                                Blink.UseAbility(safe);
                                Game.ExecuteCommand("dota_player_units_auto_attack_mode 0");
                                Utils.Sleep(500, "blink");
                            }
                        }
                    }

                    if (Soulring != null
                        && Soulring.CanBeCasted()
                        && !Owner.IsChanneling()
                        && Owner.Health >= (Owner.MaximumHealth * 0.5)
                        && Utils.SleepCheck("soulring"))
                    {
                        Soulring.UseAbility();
                        Utils.Sleep(250, "soulring");
                    }

                    if (Refresh != null
                        && Refresh.CanBeCasted()
                        && Travel != null
                        && !Travel.CanBeCasted()
                        && Owner.Distance2D(fount.First().Position) <= 900
                        && !Owner.IsChanneling()
                        && Utils.SleepCheck("Rearms"))
                    {
                        Refresh.UseAbility();
                        if (Refresh.Level == 1)
                        {
                            Utils.Sleep(3010, "Rearms");
                        }

                        if (Refresh.Level == 2)
                        {
                            Utils.Sleep(1510, "Rearms");
                        }

                        if (Refresh.Level == 3)
                        {
                            Utils.Sleep(760, "Rearms");
                        }
                    }
                }

                if (Refresh.IsChanneling || Owner.HasModifier("modifier_tinker_rearm") || Owner.IsChanneling())
                {
                    return;
                }

                if (Owner.Distance2D(safe) >= 150)
                {
                    return;
                }

                if (Soulring != null
                    && Soulring.CanBeCasted()
                    && !Refresh.IsChanneling
                    && Owner.Health >= (Owner.MaximumHealth * 0.5)
                    && Owner.Distance2D(safe) <= HIDE_AWAY_RANGE
                    && Utils.SleepCheck("soul"))
                {
                    Soulring.UseAbility();
                    Utils.Sleep(250, "soul");

                    Travel.UseAbility(fount.First().Position);
                    Utils.Sleep(300, "travel");
                }

                if (Travel != null
                    && Travel.CanBeCasted()
                    && !Refresh.IsChanneling
                    && Owner.Mana <= Refresh.ManaCost + 75
                    && Owner.Distance2D(safe) <= HIDE_AWAY_RANGE
                    && Utils.SleepCheck("travel"))
                {
                    Travel.UseAbility(fount.First().Position);
                    Utils.Sleep(300, "travel");
                }

                if (Travel != null
                    && Travel.CanBeCasted()
                    && creeps.Count(x => x.Distance2D(Owner) <= 1100) <= 2
                    && !Refresh.IsChanneling
                    && Owner.Distance2D(safe) <= HIDE_AWAY_RANGE
                    && Utils.SleepCheck("travel"))
                {
                    Travel.UseAbility(fount.First().Position);
                    Utils.Sleep(300, "travel");
                }
                else

                if (Refresh != null
                    && Refresh.CanBeCasted()
                    && !March.CanBeCasted()
                    && creeps.Count(x => x.Distance2D(Owner) >= 1100) >= 2
                    && !Refresh.IsChanneling
                    && Owner.Mana >= Refresh.ManaCost + 75
                    && Owner.Distance2D(safe) <= HIDE_AWAY_RANGE
                    && Utils.SleepCheck("Rearms"))
                {
                    Refresh.UseAbility();
                    if (Refresh.Level == 1)
                    {
                        Utils.Sleep(3010, "Rearms");
                    }

                    if (Refresh.Level == 2)
                    {
                        Utils.Sleep(1510, "Rearms");
                    }

                    if (Refresh.Level == 3)
                    {
                        Utils.Sleep(760, "Rearms");
                    }
                }
            }

            //Rocket Spam Mode
            if (Game.IsKeyDown(Menu.Item("Rocket Spam Key").GetValue<KeyBind>().Key)
                && Utils.SleepCheck("RocketSpam")
                && !Game.IsChatOpen)
            {
                if (Blink != null && Blink.CanBeCasted() && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Blink.Name)
                    && !Owner.IsChanneling()
                    && Utils.SleepCheck("Rearms")
                    && (!Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture") || (Owner.Distance2D(Game.MousePosition) > 1325 && Castrange != 0))
                    && (Owner.Distance2D(Game.MousePosition) > 650 + Castrange + Ensage_error)
                    && Utils.SleepCheck("Blinks"))
                {
                    var safeRange = 1200 + Castrange;
                    var p = Game.MousePosition;

                    if (Owner.Distance2D(Game.MousePosition) > safeRange)
                    {
                        var tpos = Owner.Position;
                        var a = tpos.ToVector2().FindAngleBetween(Game.MousePosition.ToVector2(), true);

                        safeRange -= (int)Owner.HullRadius;
                        p = new Vector3(
                            tpos.X + safeRange * (float)Math.Cos(a),
                            tpos.Y + safeRange * (float)Math.Sin(a),
                            100);
                    }

                    Blink.UseAbility(p);
                    Utils.Sleep(50, "Blinks");
                }

                if (Bottle != null
                    && Bottle.CanBeCasted()
                    && !Owner.IsChanneling()
                    && (Blink == null || (Blink != null && Owner.Distance2D(Game.MousePosition) <= 650 + Castrange + Ensage_error))
                    && !Owner.Modifiers.Any(x => x.Name == "modifier_bottle_regeneration")
                    && (Owner.MaximumMana - Owner.Mana) > 60 && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Bottle.Name)
                    && Utils.SleepCheck("Rearms"))
                {
                    Bottle.UseAbility();
                }

                foreach (var e in EntityManager<Hero>.Entities.Where(x => x.IsVisible && x.IsAlive && x.Team == Owner.GetEnemyTeam() && !x.IsIllusion))
                {
                    if ((Rocket != null && Rocket.CanBeCasted() || (Soulring.CanBeCasted() && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))
                        || !Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Rocket.Name))
                        && Owner.Distance2D(e) < 2500
                        && !Owner.IsChanneling()
                        && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                        && Utils.SleepCheck("Rearms"))
                    {
                        if (Soulring != null
                            && Soulring.CanBeCasted()
                            && !Owner.IsChanneling()
                            && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name)
                            && (Soulring.CanBeCasted() || Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name))
                            && Utils.SleepCheck("Rearms"))
                        {
                            Soulring.UseAbility();
                        }

                        if (Ghost != null && Ghost.CanBeCasted()
                            && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Ghost.Name)
                            && (Ghost.CanBeCasted() || Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name))
                            && Utils.SleepCheck("Rearms"))
                        {
                            Ghost.UseAbility();
                        }

                        if (Ethereal != null && Ghost == null && Ethereal.CanBeCasted()
                            && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name)
                            && (Ethereal.CanBeCasted() || Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name))
                            && !Owner.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal")
                            && Utils.SleepCheck("Rearms"))
                        {
                            Ethereal.UseAbility(Owner);
                        }

                        if (Glimmer != null && Glimmer.CanBeCasted()
                            && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Glimmer.Name)
                            && (Glimmer.CanBeCasted() || Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name))
                            && !Owner.Modifiers.Any(y => y.Name == "modifier_invisible")
                            && Utils.SleepCheck("Rearms"))
                        {
                            Glimmer.UseAbility(Owner);
                        }
                        else

                        if (Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Rocket.Name)
                            && (Rocket.CanBeCasted() || Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name)))
                        {
                            Rocket.UseAbility();
                        }
                    }

                    if ((Soulring == null || !Soulring.CanBeCasted() || !Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))

                        && Owner.Distance2D(e) <= 2500
                        && (!Rocket.CanBeCasted() || Rocket.Level <= 0 || !Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Rocket.Name))
                        && (Refresh.Level >= 0 && Refresh.CanBeCasted())
                        && !Owner.IsChanneling()
                        && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                        && Utils.SleepCheck("Rearms")
                        && Utils.SleepCheck("Blinks"))
                    {
                        if (Menu.Item("RocketSpamSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name))
                        {
                            Refresh.UseAbility();
                            if (Refresh.Level == 1)
                            {
                                Utils.Sleep(3010, "Rearms");
                            }

                            if (Refresh.Level == 2)
                            {
                                Utils.Sleep(1510, "Rearms");
                            }

                            if (Refresh.Level == 3)
                            {
                                Utils.Sleep(760, "Rearms");
                            }
                        }
                    }
                }

                if ((Blink != null && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Blink.Name)
                    && Owner.Distance2D(Game.MousePosition) > 650 + Castrange + Ensage_error)
                    && (Refresh.Level >= 0 && Refresh.CanBeCasted())
                    && (!Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture") || (Owner.Distance2D(Game.MousePosition) > 1325 && Castrange != 0))
                    && !Owner.IsChanneling()
                    && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                    && Utils.SleepCheck("Rearms")
                    && Utils.SleepCheck("Blinks"))
                {
                    if (Soulring != null
                        && Soulring.CanBeCasted()
                        && !Owner.IsChanneling()
                        && (Blink != null && Owner.Distance2D(Game.MousePosition) > 650 + Castrange + Ensage_error)
                        && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name)
                        && Utils.SleepCheck("Rearms"))
                    {
                        Soulring.UseAbility();
                    }

                    Refresh.UseAbility();
                    if (Refresh.Level == 1)
                    {
                        Utils.Sleep(3010, "Rearms");
                    }

                    if (Refresh.Level == 2)
                    {
                        Utils.Sleep(1510, "Rearms");
                    }

                    if (Refresh.Level == 3)
                    {
                        Utils.Sleep(760, "Rearms");
                    }
                }

                if ((Blink == null
                    || (Blink != null
                    && Owner.Distance2D(Game.MousePosition) <= 650 + Castrange + Ensage_error) && Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Blink.Name))
                    && !Owner.IsChanneling()
                    && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                    && !Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture")
                    && Utils.SleepCheck("Rearms"))
                {
                    Owner.Move(Game.MousePosition);
                }

                if (Blink != null
                    && !Owner.IsChanneling()
                    && !Menu.Item("RocketSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Blink.Name)
                    && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                    && !Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture")
                    && Utils.SleepCheck("Rearms"))
                {
                    Owner.Move(Game.MousePosition);
                }

                if (Utils.SleepCheck("Autoattack"))
                {
                    Game.ExecuteCommand("dota_player_units_auto_attack_mode 0");
                    Utils.Sleep(10000, "Autoattack");
                }

                Utils.Sleep(120, "RocketSpam");
            }

            //March Spam Mode
            if (Game.IsKeyDown(Menu.Item("March Spam Key").GetValue<KeyBind>().Key) && Utils.SleepCheck("MarchSpam") && !Game.IsChatOpen)
            {
                if (Blink != null && Blink.CanBeCasted()
                    && Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Blink.Name)
                    && !Owner.IsChanneling()
                    && Utils.SleepCheck("Rearms")
                    && (!Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture") || (Owner.Distance2D(Game.MousePosition) > 1325 && Castrange != 0))
                    && (Owner.Distance2D(Game.MousePosition) > 650 + Castrange + Ensage_error))
                {
                    var safeRange = 1200 + Castrange;
                    var p = Game.MousePosition;

                    if (Owner.Distance2D(Game.MousePosition) > safeRange)
                    {
                        var tpos = Owner.Position;
                        var a = tpos.ToVector2().FindAngleBetween(Game.MousePosition.ToVector2(), true);

                        safeRange -= (int)Owner.HullRadius;
                        p = new Vector3(
                            tpos.X + safeRange * (float)Math.Cos(a),
                            tpos.Y + safeRange * (float)Math.Sin(a),
                            100);
                    }

                    Blink.UseAbility(p);
                    Utils.Sleep(250, "Blinks");
                }

                if (Soulring != null
                    && Soulring.CanBeCasted()
                    && !Owner.IsChanneling()
                    && Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name)
                    && Utils.SleepCheck("Rearms"))
                {
                    Soulring.UseAbility();
                }

                if (Ghost != null
                    && Ghost.CanBeCasted()
                    && !Owner.IsChanneling()
                    && Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Ghost.Name)
                    && Utils.SleepCheck("Rearms"))
                {
                    Ghost.UseAbility();
                }

                if (Bottle != null
                    && Bottle.CanBeCasted()
                    && !Owner.IsChanneling()
                    && !Owner.Modifiers.Any(x => x.Name == "modifier_bottle_regeneration")
                    && Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Bottle.Name)
                    && Utils.SleepCheck("Rearms"))
                {
                    Bottle.UseAbility();
                }

                if (March != null
                    && March.CanBeCasted()
                    && (Blink == null
                    || !Blink.CanBeCasted()
                    || Owner.Distance2D(Game.MousePosition) <= 650 + Castrange + Ensage_error
                    || !Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled("item_blink"))
                    && !Owner.IsChanneling()
                    && Utils.SleepCheck("Rearms")) //&& me.Mana >= March.ManaCost + 75 
                {
                    March.UseAbility(Game.MousePosition);
                }

                if ((Soulring == null
                    || !Soulring.CanBeCasted()
                    || !Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))
                    && (Blink == null
                    || !Blink.CanBeCasted()
                    || Owner.Distance2D(Game.MousePosition) <= 650 + Castrange + Ensage_error
                    || !Menu.Item("MarchSpamItems: ").GetValue<AbilityToggler>().IsEnabled("item_blink"))
                    && (!March.CanBeCasted() || March.Level <= 0)
                    && (Refresh.Level >= 0 && Refresh.CanBeCasted())
                    && !Owner.IsChanneling()
                    && Utils.SleepCheck("Rearms"))
                {
                    Refresh.UseAbility();
                    if (Refresh.Level == 1)
                    {
                        Utils.Sleep(3010, "Rearms");
                    }

                    if (Refresh.Level == 2)
                    {
                        Utils.Sleep(1510, "Rearms");
                    }

                    if (Refresh.Level == 3)
                    {
                        Utils.Sleep(760, "Rearms");
                    }
                }

                if (Utils.SleepCheck("Autoattack"))
                {
                    Game.ExecuteCommand("dota_player_units_auto_attack_mode 0");
                    Utils.Sleep(10000, "Autoattack");
                }

                Utils.Sleep(150, "MarchSpam");
            }

            //Combo Mode
            if (!Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key))
            {
                Target = null;
            }

            if ((Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key))
                && (!Menu.Item("Chase").GetValue<KeyBind>().Active)
                && !Game.IsChatOpen)
            {
                var targetLock = Menu.Item("TargetLock").GetValue<StringList>().SelectedIndex;
                if (Utils.SleepCheck("UpdateTarget") && (Target == null || !Target.IsValid || !Target.IsAlive || !Target.IsVisible || (Target.IsVisible && targetLock == 0)))
                {
                    Target = TargetSelector.ClosestToMouse(Owner, 2000);
                    Utils.Sleep(250, "UpdateTarget");
                }

                if (Target != null
                    && Target.IsAlive
                    && !Target.IsIllusion
                    && !Owner.IsChanneling()
                    && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase))
                {
                    if (Utils.SleepCheck("FASTCOMBO"))
                    {
                        uint elsecount = 0;
                        bool magicimune = (!Target.IsMagicImmune() && !Target.Modifiers.Any(x => x.Name == "modifier_eul_cyclone"));
                        // soulring -> glimmer -> sheep -> veil-> ghost ->  ->   -> ethereal -> dagon ->  laser -> rocket -> shivas 

                        if (Soulring != null && Soulring.CanBeCasted()
                            && Target.NetworkPosition.Distance2D(Owner) <= 2500
                            && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))
                        {
                            Soulring.UseAbility();
                        }
                        else
                        {
                            elsecount += 1;
                        }

                        if (Glimmer != null && Glimmer.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Glimmer.Name))
                        {
                            Glimmer.UseAbility(Owner);
                        }
                        else
                        {
                            elsecount += 1;
                        }

                        if (Blink != null && Blink.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Blink.Name)
                            && !Owner.IsChanneling() && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                            && (Owner.Distance2D(Game.MousePosition) > 650 + Castrange + Ensage_error)
                            && (!Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture") || (Owner.Distance2D(Game.MousePosition) > 1325 && Castrange != 0))
                            && (Target.NetworkPosition.Distance2D(Owner) <= 1200 + 650 + Ensage_error * 2 + Castrange * 2)
                            && Utils.SleepCheck("Blinks"))
                        {
                            var safeRange = 1200 + Castrange;
                            var p13 = Game.MousePosition;

                            if (Owner.Distance2D(Game.MousePosition) > safeRange + Ensage_error)
                            {
                                var tpos = Owner.Position;
                                var a = tpos.ToVector2().FindAngleBetween(Game.MousePosition.ToVector2(), true);

                                safeRange -= (int)Owner.HullRadius;
                                p13 = new Vector3(
                                    tpos.X + safeRange * (float)Math.Cos(a),
                                    tpos.Y + safeRange * (float)Math.Sin(a),
                                    100);
                            }

                            Blink.UseAbility(p13);
                            Utils.Sleep(200, "Blinks");
                        }
                        else
                        {
                            elsecount += 1;
                        }

                        if (!Owner.IsChanneling()
                            && Owner.CanAttack()
                            && !Target.IsAttackImmune()
                            && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                            && OneHitLeft(Target)
                            && Target.NetworkPosition.Distance2D(Owner) <= Owner.GetAttackRange() + 50)
                        {
                            Owner.Attack(Target);
                        }
                        else
                        {
                            elsecount += 1;
                        }

                        if (Target.IsLinkensProtected() && Utils.SleepCheck("combo2"))
                        {
                            if (Forcestaff != null && Forcestaff.CanBeCasted() && Menu.Item("LinkenBreaker: ").GetValue<AbilityToggler>().IsEnabled(Forcestaff.Name))
                            {
                                Forcestaff.UseAbility(Target);
                            }
                            else if (Cyclone != null && Cyclone.CanBeCasted() && Menu.Item("LinkenBreaker: ").GetValue<AbilityToggler>().IsEnabled(Cyclone.Name))
                            {
                                Cyclone.UseAbility(Target);
                            }
                            else if (Laser.Level >= 1 && Laser.CanBeCasted() && Menu.Item("LinkenBreaker: ").GetValue<AbilityToggler>().IsEnabled(Laser.Name))
                            {
                                Laser.UseAbility(Target);
                            }

                            Utils.Sleep(200, "combo2");
                        }
                        else
                        {
                            if (Atos != null && Atos.CanBeCasted()
                                && magicimune
                                && Target.NetworkPosition.Distance2D(Owner) <= 1150 + Castrange + Ensage_error
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Atos.Name)
                                && Utils.SleepCheck("Blinks"))
                            {
                                Atos.UseAbility(Target);
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Sheep != null && Sheep.CanBeCasted()
                                && magicimune
                                && Target.NetworkPosition.Distance2D(Owner) <= 800 + Castrange + Ensage_error
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Sheep.Name)
                                && Utils.SleepCheck("Blinks"))
                            {
                                Sheep.UseAbility(Target);
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Veil != null && Veil.CanBeCasted()
                                && magicimune
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Veil.Name)
                                && Target.NetworkPosition.Distance2D(Owner) <= 1600 + Castrange + Ensage_error
                                && !(Target.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && !Target.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff")
                                && Utils.SleepCheck("Blinks"))
                            {
                                if (Owner.Distance2D(Target) > 1000 + Castrange + Ensage_error)
                                {
                                    var a = Owner.Position.ToVector2().FindAngleBetween(Target.Position.ToVector2(), true);
                                    var p1 = new Vector3(
                                        Owner.Position.X + (Owner.Distance2D(Target) - 500) * (float)Math.Cos(a),
                                        Owner.Position.Y + (Owner.Distance2D(Target) - 500) * (float)Math.Sin(a),
                                        100);
                                    Veil.UseAbility(p1);
                                }
                                else if (Owner.Distance2D(Target) <= 1000 + Castrange + Ensage_error)
                                {
                                    Veil.UseAbility(Target.NetworkPosition);
                                }
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Ghost != null && Ethereal == null && Ghost.CanBeCasted()
                                && Target.NetworkPosition.Distance2D(Owner) <= 800 + Castrange + Ensage_error
                                && !OneHitLeft(Target)
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ghost.Name))
                            {
                                Ghost.UseAbility();
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            var comboMode = Menu.Item("ComboMode").GetValue<StringList>().SelectedIndex;
                            if (Rocket.Level > 0 && Rocket.CanBeCasted()
                                && Target.NetworkPosition.Distance2D(Owner) <= 2500
                                && !OneHitLeft(Target)
                                && magicimune
                                && (!Target.Modifiers.Any(y => y.Name == "modifier_item_blade_mail_reflect") || Owner.IsMagicImmune())
                                && (!Target.Modifiers.Any(y => y.Name == "modifier_nyx_assassin_spiked_carapace") || Owner.IsMagicImmune())
                                && (((Veil == null
                                || !Veil.CanBeCasted()
                                || Target.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff")
                                | !Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Veil.Name))))
                                && (((Ethereal == null
                                || (Ethereal != null && !Ethereal.CanBeCasted())
                                || IsCasted(Ethereal)
                                | !Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))))
                                && (Laser == null || !Laser.CanBeCasted() || comboMode == 0)
                                && (Dagon == null || !Dagon.CanBeCasted() || comboMode == 0)
                                && !(Target.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && Menu.Item("ComboSkills: ").GetValue<AbilityToggler>().IsEnabled(Rocket.Name)
                                && Utils.SleepCheck("Blinks"))
                            {
                                Rocket.UseAbility();
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Ethereal != null && Ethereal.CanBeCasted()
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name)
                                && (!Veil.CanBeCasted()
                                || Target.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff")
                                || Veil == null | !Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Veil.Name))
                                && magicimune
                                && !OneHitLeft(Target)
                                && (!CanReflectDamage(Target) || Owner.IsMagicImmune())
                                && Target.NetworkPosition.Distance2D(Owner) <= 800 + Castrange + Ensage_error
                                && !(Target.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && Utils.SleepCheck("Blinks"))
                            {
                                Ethereal.UseAbility(Target);
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Dagon != null && Dagon.CanBeCasted()
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_dagon")
                                && (!Veil.CanBeCasted()
                                || Target.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff")
                                || Veil == null | !Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Veil.Name))
                                && (Ethereal == null
                                || (Ethereal != null
                                && !IsCasted(Ethereal)
                                && !Ethereal.CanBeCasted())
                                || Target.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal")
                                | !Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))
                                && magicimune
                                && (!CanReflectDamage(Target) || Owner.IsMagicImmune())
                                && !OneHitLeft(Target)
                                && Target.NetworkPosition.Distance2D(Owner) <= Dagondistance[Dagon.Level - 1] + Castrange + Ensage_error
                                && !(Target.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && Utils.SleepCheck("Blinks"))
                            {
                                Dagon.UseAbility(Target);
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Laser.Level > 0 && Laser.CanBeCasted()
                                && Menu.Item("ComboSkills: ").GetValue<AbilityToggler>().IsEnabled(Laser.Name)
                                && !OneHitLeft(Target)
                                && magicimune
                                && (!CanReflectDamage(Target) || Owner.IsMagicImmune())
                                && Target.NetworkPosition.Distance2D(Owner) <= 650 + Castrange + Ensage_error
                                && !(Target.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && Utils.SleepCheck("Blinks"))
                            {
                                Laser.UseAbility(Target);
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (Shiva != null && Shiva.CanBeCasted()
                                && magicimune
                                && !OneHitLeft(Target)
                                && (!Target.Modifiers.Any(y => y.Name == "modifier_item_blade_mail_reflect") || Owner.IsMagicImmune())
                                && (!Target.Modifiers.Any(y => y.Name == "modifier_nyx_assassin_spiked_carapace") || Owner.IsMagicImmune())
                                && Target.NetworkPosition.Distance2D(Owner) <= 900 + Ensage_error
                                && !(Target.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Shiva.Name)
                                && Utils.SleepCheck("Blinks"))
                            {
                                Shiva.UseAbility();
                            }
                            else
                            {
                                elsecount += 1;
                            }

                            if (elsecount == 13
                                && Refresh != null && Refresh.CanBeCasted()
                                && Menu.Item("ComboSkills: ").GetValue<AbilityToggler>().IsEnabled(Refresh.Name)
                                && !Owner.IsChanneling() && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                                && Utils.SleepCheck("Rearm")
                                && Ready_for_refresh()
                                && Utils.SleepCheck("Blinks"))
                            {
                                Refresh.UseAbility();
                                if (Refresh.Level == 1)
                                {
                                    Utils.Sleep(3010, "Rearm");
                                }

                                if (Refresh.Level == 2)
                                {
                                    Utils.Sleep(1510, "Rearm");
                                }

                                if (Refresh.Level == 3)
                                {
                                    Utils.Sleep(760, "Rearm");
                                }
                            }
                            else if (!Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture"))
                            {
                                if (!Owner.IsChanneling()
                                    && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                                    && Owner.CanAttack()
                                    && !Target.IsAttackImmune()
                                    && (!Target.Modifiers.Any(y => y.Name == "modifier_nyx_assassin_spiked_carapace") || Owner.IsMagicImmune())
                                    && Utils.SleepCheck("Rearm"))
                                {
                                    if (Owner.Distance2D(Target) > Owner.GetAttackRange() - 100)
                                    {
                                        Orbwalking.Orbwalk(Target);
                                    }
                                    else
                                    {
                                        Owner.Attack(Target);
                                    }
                                }
                            }

                            Utils.Sleep(150, "FASTCOMBO");
                        }
                    }
                }
                else
                {
                    if (!Owner.IsChanneling()
                        && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                        && !Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture")
                        && Utils.SleepCheck("MousePosition"))
                    {
                        Owner.Move(Game.MousePosition);
                        Utils.Sleep(150, "MousePosition");
                    }
                }
            }

            if ((Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key))
                && (Menu.Item("Chase").GetValue<KeyBind>().Active)
                && !Game.IsChatOpen)
            {
                var targetLock = Menu.Item("TargetLock").GetValue<StringList>().SelectedIndex;
                if (Utils.SleepCheck("UpdateTarget")
                    && (Target == null || !Target.IsValid || !Target.IsAlive || !Target.IsVisible || (Target.IsVisible && targetLock == 0)))
                {
                    Target = TargetSelector.ClosestToMouse(Owner, 2000);
                    Utils.Sleep(250, "UpdateTarget");
                }

                if (Target != null && Target.IsAlive && !Target.IsIllusion && !Owner.IsChanneling() && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase))
                {
                    if (!Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture"))
                    {
                        if (!Owner.IsChanneling() && Owner.CanAttack()
                            && !Target.IsAttackImmune()
                            && (!Target.Modifiers.Any(y => y.Name == "modifier_nyx_assassin_spiked_carapace") || Owner.IsMagicImmune())
                            && Utils.SleepCheck("Rearm"))
                        {
                            if (Owner.Distance2D(Target) > Owner.GetAttackRange() - 100)
                            {
                                Orbwalking.Orbwalk(Target);
                            }
                            else
                            {
                                Owner.Attack(Target);
                            }
                        }
                        else
                        {
                            Owner.Move(Game.MousePosition, false);
                        }
                    }
                }
                else
                {
                    if (!Owner.IsChanneling()
                        && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                        && !Owner.Modifiers.Any(y => y.Name == "modifier_bloodseeker_rupture")
                        && Utils.SleepCheck("SpeedChase"))
                    {
                        Owner.Move(Game.MousePosition);
                        Utils.Sleep(150, "SpeedChase");
                    }
                }
            }
        }
        /*
         *
         * 1===============================tinker_laser
1===============================tinker_heat_seeking_missile
1===============================tinker_march_of_the_machines
1===============================generic_hidden
1===============================generic_hidden
1===============================tinker_rearm
1===============================special_bonus_spell_amplify_8
1===============================special_bonus_cast_range_100
1===============================special_bonus_movement_speed_30
1===============================special_bonus_spell_lifesteal_10
1===============================special_bonus_armor_10
1===============================special_bonus_unique_tinker_2
1===============================special_bonus_unique_tinker
1===============================special_bonus_unique_tinker_3
1===============================14
         */
        private void AD(EventArgs args)
        {
            if (Game.IsPaused)
            {
                return;
            }

            Castrange = 0;
            
            Castrange = (int)_helper.GetBonusRange();

            if (Bottle != null
                && !Owner.IsInvisible()
                && !Owner.IsChanneling()
                && !Owner.Spellbook.Spells.Any(x => x.IsInAbilityPhase)
                && !March.IsInAbilityPhase
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

                    //Laser dodge close enemy
                    if (Laser != null
                        && Laser.CanBeCasted()
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
                        Laser.UseAbility(e);
                        Utils.Sleep(150, "Ghost");
                    }

                    //ghost dodge close enemy
                    if (Ghost != null
                        && Ghost.CanBeCasted()
                        && (Sheep == null || !Sheep.CanBeCasted())
                        && (Laser == null || !Laser.CanBeCasted() || e.Modifiers.Any(y => y.Name == "modifier_juggernaut_omnislash"))
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

                    //cyclone dodge attacking close enemy		
                    if ((Ghost == null || !Ghost.CanBeCasted())
                        && (Sheep == null || !Sheep.CanBeCasted())
                        && (Laser == null || !Laser.CanBeCasted())
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

                    //если цель под ультой ская
                    if ((Ghost == null || !Ghost.CanBeCasted())
                                    && (Sheep == null || !Sheep.CanBeCasted())
                                    && (Laser == null || !Laser.CanBeCasted())
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
                            && (Laser == null || !Laser.CanBeCasted())
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

                //Auto Killsteal
                if (Menu.Item("autoKillsteal").GetValue<bool>()
                    && Owner.IsAlive
                    && Owner.IsVisible
                    && (Menu.Item("Chase").GetValue<KeyBind>().Active || !Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key)))
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

                            if (Soulring != null && Soulring.CanBeCasted()
                                && e.NetworkPosition.Distance2D(Owner) < 2500
                                && magicimune
                                && !OneHitLeft(e)
                                && (((Veil == null
                                || !Veil.CanBeCasted()
                                || e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff"))
                                && e.NetworkPosition.Distance2D(Owner) <= 1600 + Castrange)
                                || ((e.NetworkPosition.Distance2D(Owner) > 1600 + Castrange)
                                && (e.Health < (int)GetRocketDamage() * (1 - e.MagicDamageResist))))
                                && (((Ethereal == null
                                || (Ethereal != null
                                && !Ethereal.CanBeCasted())
                                || IsCasted(Ethereal))
                                && e.NetworkPosition.Distance2D(Owner) <= 800 + Castrange)
                                || ((e.NetworkPosition.Distance2D(Owner) > 800 + Castrange)
                                && (e.Health < (int)GetRocketDamage() * (1 - e.MagicDamageResist)))))
                            {
                                Soulring.UseAbility();
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
                                && (!Veil.CanBeCasted() || e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff") || Veil == null)
                                && (Ethereal == null || (Ethereal != null && !IsCasted(Ethereal) && !Ethereal.CanBeCasted()) || e.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal"))
                                && !OneHitLeft(e)
                                && magicimune
                                && e.NetworkPosition.Distance2D(Owner) <= Dagondistance[Dagon.Level - 1] + Castrange + Ensage_error
                                && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind()))
                            {
                                Dagon.UseAbility(e);
                            }

                            if (Rocket.Level > 0 && Rocket.CanBeCasted()
                                && e.NetworkPosition.Distance2D(Owner) <= 2500
                                && (!EzkillCheck || e.NetworkPosition.Distance2D(Owner) >= 800 + Castrange + Ensage_error)
                                && !OneHitLeft(e)
                                && magicimune
                                && (((Veil == null
                                || !Veil.CanBeCasted()
                                || e.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff"))
                                && e.NetworkPosition.Distance2D(Owner) <= 1600 + Castrange) || (e.NetworkPosition.Distance2D(Owner) > 1600 + Castrange))
                                && (((Ethereal == null || (Ethereal != null && !Ethereal.CanBeCasted()) || IsCasted(Ethereal)) && e.NetworkPosition.Distance2D(Owner) <= 800 + Castrange) || (e.NetworkPosition.Distance2D(Owner) > 800 + Castrange))
                                && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind()))
                            {
                                _abilities.Skill2.UseAbility();
                            }

                            if (Laser.Level > 0 && Laser.CanBeCasted()
                                && !EzkillCheck
                                && !OneHitLeft(e)
                                && magicimune
                                && e.NetworkPosition.Distance2D(Owner) <= 650 + Castrange + Ensage_error
                                && !(e.Modifiers.Any(y => y.Name == "modifier_teleporting") && IsEulhexFind())
                                )
                                Laser.UseAbility(e);

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

            var talent10 = Owner.Spellbook.Spells.First(x => x.Name == "special_bonus_cast_range_100");
            if (talent10.Level > 0)
            {
                Castrange += (int)talent10.AbilitySpecialData.First(x => x.Name == "value").Value;
            }

            if (Menu.Item("Show Direction").GetValue<bool>())
            {
                if (Owner.IsChanneling())
                {
                    if (Effect3 == null)
                    {
                        Effect3 = new ParticleEffect(@"materials\ensage_ui\particles\line.vpcf", Owner);

                        Effect3.SetControlPoint(1, Owner.Position);
                        Effect3.SetControlPoint(2, FindVector(Owner.Position, Owner.Rotation, 1200 + Castrange));
                        Effect3.SetControlPoint(3, new Vector3(100, 70, 10));
                        Effect3.SetControlPoint(4, new Vector3(150, 255, 255));
                    }
                    else
                    {
                        Effect3.SetControlPoint(1, Owner.Position);
                        Effect3.SetControlPoint(2, FindVector(Owner.Position, Owner.Rotation, 1200 + Castrange));
                        Effect3.SetControlPoint(3, new Vector3(100, 70, 10));
                        Effect3.SetControlPoint(4, new Vector3(150, 255, 255));
                    }
                }
                else if (Effect3 != null)
                {
                    Effect3.Dispose();
                    Effect3 = null;
                }

            }

            if (Target != null && Target.IsValid && !Target.IsIllusion && Target.IsAlive && Target.IsVisible && Owner.Distance2D(Target.Position) < 2000 && Menu.Item("Show Target Effect").GetValue<bool>())
            {
                if (Effect4 == null)
                {
                    Effect4 = new ParticleEffect(@"materials\ensage_ui\particles\target.vpcf", Target);
                    Effect4.SetControlPoint(2, Owner.Position);
                    Effect4.SetControlPoint(5, new Vector3(Red, Green, Blue));
                    Effect4.SetControlPoint(6, new Vector3(1, 0, 0));
                    Effect4.SetControlPoint(7, Target.Position);
                }
                else
                {
                    Effect4.SetControlPoint(2, Owner.Position);
                    Effect4.SetControlPoint(5, new Vector3(Red, Green, Blue));
                    Effect4.SetControlPoint(6, new Vector3(1, 0, 0));
                    Effect4.SetControlPoint(7, Target.Position);
                }
            }
            else if (Effect4 != null)
            {
                Effect4.Dispose();
                Effect4 = null;
            }

            if (Menu.Item("Blink Range").GetValue<bool>())
            {
                if (Blink != null)
                {
                    if (Rangedisplay_dagger == null)
                    {
                        Rangedisplay_dagger = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
                        Range_dagger = 1200 + Castrange;
                        Rangedisplay_dagger.SetControlPoint(1, new Vector3(Range_dagger, 255, 5));
                        Rangedisplay_dagger.SetControlPoint(2, new Vector3(150, 255, 255));
                    }

                    if (Range_dagger != 1200 + Castrange)
                    {
                        Range_dagger = 1200 + Castrange;

                        if (Rangedisplay_dagger != null)
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
                    if (Rangedisplay_dagger != null)
                    {
                        Rangedisplay_dagger.Dispose();
                    }

                    Rangedisplay_dagger = null;
                }

            }
            else if (Rangedisplay_dagger != null)
            {
                Rangedisplay_dagger.Dispose();
                Rangedisplay_dagger = null;
            }

            if (Menu.Item("Blink Range Incoming TP").GetValue<bool>())
            {
                if (Blink != null)
                {
                    var units = ObjectManager.GetEntities<Unit>().Where(x =>
                                                                        (x is Hero && x.Team == Owner.Team) ||
                                                                        (x is Creep && x.Team == Owner.Team) ||
                                                                        (x is Building && x.Team == Owner.Team) ||
                                                                        (!(x is Hero) && !(x is Building) && !(x is Creep) &&
                                                                        x.NetworkName != "CDOTA_NPC_TechiesMines" &&
                                                                        x.NetworkName != "CDOTA_NPC_Observer_Ward" &&
                                                                        x.NetworkName != "CDOTA_NPC_Observer_Ward_TrueSight" &&
                                                                        x.Team == Owner.Team)).ToList();

                    foreach (var unit in units)
                    {
                        HandleEffectR(unit);
                        HandleEffectD(unit);
                    }
                }
            }

            if (Menu.Item("Rocket Range").GetValue<bool>())
            {
                if (Rangedisplay_rocket == null)
                {
                    Rangedisplay_rocket = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
                    Range_rocket = 2500;
                    Rangedisplay_rocket.SetControlPoint(1, new Vector3(Range_rocket, 255, 5));
                    Rangedisplay_rocket.SetControlPoint(2, new Vector3(255, 255, 0));
                }
            }
            else if (Rangedisplay_rocket != null)
            {
                Rangedisplay_rocket.Dispose();
                Rangedisplay_rocket = null;
            }



            if (Menu.Item("Laser Range").GetValue<bool>())
            {
                if (Rangedisplay_laser == null)
                {
                    Rangedisplay_laser = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
                    Range_laser = 650 + Castrange;
                    Rangedisplay_laser.SetControlPoint(1, new Vector3(Range_laser, 255, 5));
                    Rangedisplay_laser.SetControlPoint(2, new Vector3(0, 150, 255));
                }
                if (Range_laser != 650 + Castrange)
                {
                    Range_laser = 650 + Castrange;
                    if (Rangedisplay_laser != null)
                        Rangedisplay_laser.Dispose();
                    Rangedisplay_laser = Owner.AddParticleEffect(@"materials\ensage_ui\particles\range_display_mod.vpcf");
                    Rangedisplay_laser.SetControlPoint(1, new Vector3(Range_laser, 255, 5));
                    Rangedisplay_laser.SetControlPoint(2, new Vector3(0, 150, 255));
                }
            }
            else if (Rangedisplay_laser != null)
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
            var retVector = new Vector3(first.X + (float)Math.Cos(Utils.DegreeToRadian(ret)) * distance, first.Y + (float)Math.Sin(Utils.DegreeToRadian(ret)) * distance, 100);

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

        private bool Ready_for_refresh()
        {
            if ((Ghost != null && Ghost.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ghost.Name))
                || (Soulring != null && Soulring.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))
                || (Sheep != null && Sheep.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Sheep.Name))
                || (Laser != null && Laser.CanBeCasted() && Menu.Item("ComboSkills: ").GetValue<AbilityToggler>().IsEnabled(Laser.Name))
                || (Ethereal != null && Ethereal.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))
                || (Dagon != null && Dagon.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_dagon"))
                || (Rocket != null && Rocket.CanBeCasted() && Menu.Item("ComboSkills: ").GetValue<AbilityToggler>().IsEnabled(Rocket.Name))
                || (Shiva != null && Shiva.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Shiva.Name))
                || (Glimmer != null && Glimmer.CanBeCasted() && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Glimmer.Name)))
            {
                return false;
            }

            return true;
        }

        private bool CanReflectDamage(Hero x)
        {
            if (x.Modifiers.Any(m => (m.Name == "modifier_item_blade_mail_reflect") || (m.Name == "modifier_nyx_assassin_spiked_carapace") || (m.Name == "modifier_item_lotus_orb_active")))
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
                                    || (v.HeroId == HeroId.npc_dota_hero_tiny && v.Spellbook.SpellE.Level > 0) || v.IsInvul())
            {
                return true;
            }

            return false;
        }

        private int Manaprocast()
        {
            int manalaser = 0, manarocket = 0, manarearm = 0, manadagon = 0, manaveil = 0, manasheep = 0, manaethereal = 0, manashiva = 0, manasoulring = 0;

            if (Laser != null && Laser.Level > 0)
            {
                manalaser = Laser_mana[Laser.Level - 1];
            }
            else
            {
                manalaser = 0;
            }

            if (Rocket != null && Rocket.Level > 0)
            {
                manarocket = Rocket_mana[Rocket.Level - 1];
            }
            else
            {
                manarocket = 0;
            }


            if (Refresh != null && Refresh.Level > 0)
            {
                manarearm = Rearm_mana[Refresh.Level - 1];
            }
            else
            {
                manarearm = 0;
            }

            if (Dagon != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_dagon"))
            {
                manadagon = 180;
            }
            else
            {
                manadagon = 0;
            }

            if (Ethereal != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Ethereal.Name))
            {
                manaethereal = 100;
            }
            else
            {
                manaethereal = 0;
            }

            if (Veil != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Veil.Name))
            {
                manaveil = 50;
            }
            else
            {
                manaveil = 0;
            }

            if (Sheep != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Sheep.Name))
            {
                manasheep = 100;
            }
            else
            {
                manasheep = 0;
            }

            if (Shiva != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Shiva.Name))
            {
                manashiva = 100;
            }
            else
            {
                manashiva = 0;
            }

            if (Soulring != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))
            {
                manasoulring = 150;
            }
            else
            {
                manasoulring = 0;
            }

            return manalaser + manarocket + manadagon + manaethereal + manaveil + manasheep + manashiva - manasoulring;
        }

        private int Manaonerocket()
        {
            int manarocket = 0, manarearm = 0, manasoulring = 0;

            if (Rocket != null && Rocket.Level > 0)
            {
                manarocket = Rocket_mana[Rocket.Level - 1];
            }
            else
            {
                manarocket = 0;
            }

            if (Refresh != null && Refresh.Level > 0)
            {
                manarearm = Rearm_mana[Refresh.Level - 1];
            }
            else
            {
                manarearm = 0;
            }

            if (Soulring != null && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled(Soulring.Name))
            {
                manasoulring = 150;
            }
            else
            {
                manasoulring = 0;
            }

            return manarocket - manasoulring;
        }

        private int ManaFactDamage(Hero en)
        {
            if (en != null && en.IsAlive && en.IsValid)
            {
                int manalaser = 0, manarocket = 0, manarearm = 0, manadagon = 0, dagondist = 0, manaethereal = 0, manashiva = 0, manasoulring = 0;

                if (Laser != null && Laser.Level > 0 && Laser.CanBeCasted())
                {
                    manalaser = Laser_mana[Laser.Level - 1];
                }
                else
                {
                    manalaser = 0;
                }

                if (Rocket != null && Rocket.Level > 0 && Rocket.CanBeCasted())
                {
                    manarocket = Rocket_mana[Rocket.Level - 1];
                }
                else
                {
                    manarocket = 0;
                }

                if (Refresh != null && Refresh.Level > 0 && Refresh.CanBeCasted())
                {
                    manarearm = Rearm_mana[Refresh.Level - 1];
                }
                else
                {
                    manarearm = 0;
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
                return (Owner.Distance2D(en) < 650 + Castrange + Ensage_error ? manalaser : 0)
                    + (Owner.Distance2D(en) < 2500 ? manarocket : 0)
                    + (Owner.Distance2D(en) < 800 + Castrange + Ensage_error ? manaethereal : 0)
                    + (Owner.Distance2D(en) < dagondist + Castrange + Ensage_error ? manadagon : 0)
                    + (Owner.Distance2D(en) < 900 + Ensage_error ? manashiva : 0)
                    - manasoulring;
            }

            return 0;
        }

        private int ProcastCounter(Hero en)
        {
            if (!en.IsMagicImmune() && !en.IsInvul())
            {
                if (IsPhysDamageImune(en))
                {
                    return (int)Math.Ceiling(en.Health / GetComboDamage());
                }

                return (int)Math.Ceiling(en.Health / GetComboDamage() - GetOneAutoAttackDamage(en) / en.Health);
            }

            return 999;
        }

        private int RktCount(Hero en)
        {
            if (!en.IsMagicImmune() && !en.IsInvul())
            {
                if (((int)((en.Health - GetComboDamage(en) + GetOneAutoAttackDamage(en)) / GetRocketDamage())) <= 0)
                {
                    return 0;
                }

                return ((int)((en.Health - GetComboDamage(en) + GetOneAutoAttackDamage(en)) / GetRocketDamage()));
            }

            return 999;
        }

        private int OnlyRktCount(Hero en)
        {
            if (!en.IsMagicImmune() && !en.IsInvul())
            {
                return ((int)(en.Health / GetRocketDamage() + 1));
            }

            return 999;
        }

        private int OnlyRktCountDmg(Hero en)
        {
            if (!en.IsMagicImmune() && !en.IsInvul())
            {
                return ((int)(en.Health / (int)GetRocketDamage() + 1) * (int)GetRocketDamage());
            }

            return 999;
        }

        private int HitCount(Hero en)
        {
            if (Owner.CanAttack() && !en.IsAttackImmune() && !en.IsInvul())
            {
                if ((int)Math.Ceiling((en.Health - GetComboDamage(en) + 2 * GetOneAutoAttackDamage(en)) / GetOneAutoAttackDamage(en)) <= 0)
                {
                    return 0;
                }

                return ((int)Math.Ceiling((en.Health - GetComboDamage(en) + 2 * GetOneAutoAttackDamage(en)) / GetOneAutoAttackDamage(en)));
            }

            return 999;
        }

        private bool OneHitLeft(Hero en)
        {

            if (((en.Health < GetComboDamageByDistance(en)) && (en.Health > GetComboDamageByDistance(en) - GetOneAutoAttackDamage(en)))
                && !IsPhysDamageImune(en)
                && Owner.Distance2D(en) < Owner.GetAttackRange() + 50)
            {
                return true;
            }

            return false;
        }

        private void Information(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsWatchingGame)
            {
                return;
            }

            Hero targetInf = null;

            targetInf = Owner.ClosestToMouseTarget(2000);

            if (targetInf != null && targetInf.IsValid && !targetInf.IsIllusion && targetInf.IsAlive && targetInf.IsVisible)
            {
                if (Menu.Item("TargetCalculator").GetValue<bool>())
                {
                    var start = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(0, HUDInfo.GetHpBarSizeY() - 70);
                    var starts = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(1, HUDInfo.GetHpBarSizeY() - 69);
                    var start2 = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(0, HUDInfo.GetHpBarSizeY() - 90);
                    var start2s = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(1, HUDInfo.GetHpBarSizeY() - 89);
                    var start3 = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(0, HUDInfo.GetHpBarSizeY() - 110);
                    var start3s = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(1, HUDInfo.GetHpBarSizeY() - 109);
                    var start4 = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(-25, HUDInfo.GetHpBarSizeY() - 13);
                    var start4s = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(-24, HUDInfo.GetHpBarSizeY() - 12);

                    Drawing.DrawText(EZKill(targetInf) ? GetEZKillDamage(targetInf).ToString() + " ez" : GetEZKillDamage(targetInf).ToString(), starts, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    Drawing.DrawText(EZKill(targetInf) ? GetEZKillDamage(targetInf).ToString() + " ez" : GetEZKillDamage(targetInf).ToString(), start, new Vector2(21, 21), EZKill(targetInf) ? Color.Lime : Color.Red, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);

                    Drawing.DrawText((GetComboDamage(targetInf) + GetOneAutoAttackDamage(targetInf)).ToString(), start2s, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    Drawing.DrawText((GetComboDamage(targetInf) + GetOneAutoAttackDamage(targetInf)).ToString(), start2, new Vector2(21, 21), (targetInf.Health < (GetComboDamage(targetInf) + GetOneAutoAttackDamage(targetInf))) ? Color.Lime : Color.Red, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);

                    Drawing.DrawText(GetComboDamageByDistance(targetInf).ToString(), start3s, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    Drawing.DrawText(GetComboDamageByDistance(targetInf).ToString(), start3, new Vector2(21, 21), (targetInf.Health < GetComboDamageByDistance(targetInf)) ? Color.Lime : Color.Red, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);

                    Drawing.DrawText("x" + ProcastCounter(targetInf).ToString(), start4s, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    Drawing.DrawText("x" + ProcastCounter(targetInf).ToString(), start4, new Vector2(21, 21), Color.White, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);

                }

                if (Menu.Item("HitCounter").GetValue<bool>())
                {
                    var hitcounter = HitCount(targetInf);
                    var starthit = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(117, HUDInfo.GetHpBarSizeY() - 13);
                    var starthits = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(118, HUDInfo.GetHpBarSizeY() - 12);
                    Drawing.DrawText(hitcounter.ToString() + " hits", starthits, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    Drawing.DrawText(hitcounter.ToString() + " hits", starthit, new Vector2(21, 21), (hitcounter <= 1) ? Color.Lime : Color.White, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                }

                if (Menu.Item("RocketCounter").GetValue<bool>() && Rocket.Level > 0)
                {
                    var startrocket = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(117, HUDInfo.GetHpBarSizeY() + 6);
                    var startrockets = HUDInfo.GetHPbarPosition(targetInf) + new Vector2(118, HUDInfo.GetHpBarSizeY() + 7);
                    Drawing.DrawText(RktCount(targetInf).ToString() + " rkts", startrockets, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    Drawing.DrawText(RktCount(targetInf).ToString() + " rkts", startrocket, new Vector2(21, 21), (RktCount(targetInf) <= 1) ? Color.Lime : Color.Yellow, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);

                    if (Refresh != null && Refresh.Level > 0)
                    {
                        Drawing.DrawText("          (x" + OnlyRktCount(targetInf).ToString() + ") ", startrockets, new Vector2(21, 21), Color.Black, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                        Drawing.DrawText("          (x" + OnlyRktCount(targetInf).ToString() + ") ", startrocket, new Vector2(21, 21), (Math.Ceiling((Owner.Mana - Manaonerocket()) / (Manaonerocket() + Rearm_mana[Refresh.Level - 1])) >= OnlyRktCount(targetInf)) ? Color.Lime : Color.Red, FontFlags.AntiAlias | FontFlags.Additive | FontFlags.DropShadow);
                    }
                }
            }

            if (Menu.Item("Calculator").GetValue<bool>())
            {
                var coordX = Menu.Item("BarPosX").GetValue<Slider>().Value;
                var coordY = Menu.Item("BarPosY").GetValue<Slider>().Value;

                Drawing.DrawText("Full cast:", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 210 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("Full cast:", new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 210 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                Drawing.DrawText("x1", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x1", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                Drawing.DrawText("x2", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 285 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x2", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 285 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x3", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 310 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x3", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 310 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText(GetComboDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText(GetComboDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((2 * GetComboDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 285 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((2 * GetComboDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 285 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((3 * GetComboDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 310 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((3 * GetComboDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 310 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);

                if (Menu.Item("debug").IsActive())
                {
                    Drawing.DrawText("laser dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("laser dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 360 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                    Drawing.DrawText(GetLaserDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(GetLaserDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 360 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);

                    Drawing.DrawText("rocket dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("rocket dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 385 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                    Drawing.DrawText(GetRocketDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(GetRocketDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 385 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);

                    Drawing.DrawText("dagon dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("dagon dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 410 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                    Drawing.DrawText(GetDagonDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(GetDagonDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 410 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);

                    Drawing.DrawText("eblade dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("eblade dmg:", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordX, HUDInfo.ScreenSizeY() / 2 + 435 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                    Drawing.DrawText(GetEtherealBladeDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(GetEtherealBladeDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 100 + coordX, HUDInfo.ScreenSizeY() / 2 + 435 + coordY), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                    if (targetInf != null && targetInf.IsValid && !targetInf.IsIllusion && targetInf.IsAlive && targetInf.IsVisible)
                    {
                        Drawing.DrawText("enemy magic res:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText("enemy magic res:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 360 + coordY), new Vector2(30, 200), Color.White,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(targetInf.MagicDamageResist.ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 280,
                                HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(targetInf.MagicDamageResist.ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 280,
                                HUDInfo.ScreenSizeY() / 2 + 360 + coordY), new Vector2(30, 200), Color.LimeGreen,
                            FontFlags.AntiAlias);

                        Drawing.DrawText("enemy combo dmg:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText("enemy combo dmg:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 385 + coordY), new Vector2(30, 200), Color.White,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(GetComboDamage(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 280,
                                HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(GetComboDamage(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 280,
                                HUDInfo.ScreenSizeY() / 2 + 385 + coordY), new Vector2(30, 200), Color.LimeGreen,
                            FontFlags.AntiAlias);

                        Drawing.DrawText("enemy combo dmg by distance:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText("enemy combo dmg by distance:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 410 + coordY), new Vector2(30, 200), Color.White,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(GetComboDamageByDistance(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 420,
                                HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(GetComboDamageByDistance(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 420,
                                HUDInfo.ScreenSizeY() / 2 + 410 + coordY), new Vector2(30, 200), Color.LimeGreen,
                            FontFlags.AntiAlias);

                        Drawing.DrawText("my distance to enemy:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText("my distance to enemy:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 50,
                                HUDInfo.ScreenSizeY() / 2 + 435 + coordY), new Vector2(30, 200), Color.White,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(Owner.Distance2D(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX + 420,
                                HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(Owner.Distance2D(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX + 420,
                                HUDInfo.ScreenSizeY() / 2 + 435 + coordY), new Vector2(30, 200), Color.LimeGreen,
                            FontFlags.AntiAlias);

                        Drawing.DrawText("EZKill?:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX - 600,
                                HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText("EZKill?:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX - 600,
                                HUDInfo.ScreenSizeY() / 2 + 360 + coordY), new Vector2(30, 200), Color.White,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(EZKill(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX - 400,
                                HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(EZKill(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX - 400,
                                HUDInfo.ScreenSizeY() / 2 + 360 + coordY), new Vector2(30, 200), Color.LimeGreen,
                            FontFlags.AntiAlias);

                        Drawing.DrawText("EZKill damage:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX - 600,
                                HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText("EZKill damage:",
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX - 600,
                                HUDInfo.ScreenSizeY() / 2 + 385 + coordY), new Vector2(30, 200), Color.White,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(GetEZKillDamage(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + coordX - 400,
                                HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordY), new Vector2(30, 200), Color.Black,
                            FontFlags.AntiAlias);
                        Drawing.DrawText(GetEZKillDamage(targetInf).ToString(),
                            new Vector2(HUDInfo.ScreenSizeX() / 2 + coordX - 400,
                                HUDInfo.ScreenSizeY() / 2 + 385 + coordY), new Vector2(30, 200), Color.LimeGreen,
                            FontFlags.AntiAlias);

                    }
                }

                Drawing.DrawText("dmg", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 232 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("dmg", new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordX, HUDInfo.ScreenSizeY() / 2 + 232 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);


                Drawing.DrawText("mana", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 232 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("mana", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 232 + coordY), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                if (Refresh != null && Refresh.Level > 0)
                {
                    Drawing.DrawText(Manaprocast().ToString() + " (" + (-Manaprocast() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(Manaprocast().ToString() + " (" + (-Manaprocast() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + coordY), new Vector2(30, 200), (Owner.Mana > Manaprocast()) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((2 * Manaprocast() + Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(2 * Manaprocast() + Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 285 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((2 * Manaprocast() + Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(2 * Manaprocast() + Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 285 + coordY), new Vector2(30, 200), (Owner.Mana > (2 * Manaprocast() + Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((3 * Manaprocast() + 2 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(3 * Manaprocast() + 2 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 310 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((3 * Manaprocast() + 2 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(3 * Manaprocast() + 2 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 310 + coordY), new Vector2(30, 200), (Owner.Mana > (3 * Manaprocast() + 2 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                }
                else
                {
                    Drawing.DrawText(Manaprocast().ToString() + " (" + (-Manaprocast() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordY), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(Manaprocast().ToString() + " (" + (-Manaprocast() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordX, HUDInfo.ScreenSizeY() / 2 + 260 + coordY), new Vector2(30, 200), (Owner.Mana > Manaprocast()) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                }
            }

            if (Menu.Item("CalculatorRkt").GetValue<bool>())
            {
                var coordXr = Menu.Item("BarPosXr").GetValue<Slider>().Value;
                var coordYr = Menu.Item("BarPosYr").GetValue<Slider>().Value;

                Drawing.DrawText("Rockets:", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 210 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("Rockets:", new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 210 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                Drawing.DrawText("x1", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x1", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x2", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 285 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x2", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 285 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x3", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 310 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x3", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 310 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x4", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 335 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x4", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 335 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x5", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x5", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 360 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x6", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x6", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 385 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x7", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x7", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 410 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x8", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x8", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 435 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);
                Drawing.DrawText("x9", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 460 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("x9", new Vector2(HUDInfo.ScreenSizeX() / 2 - 240 + coordXr, HUDInfo.ScreenSizeY() / 2 + 460 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);


                Drawing.DrawText("dmg", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 232 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("dmg", new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 232 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                Drawing.DrawText(GetRocketDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText(GetRocketDamage().ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((2 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 285 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((2 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 285 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((3 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 310 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((3 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 310 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((4 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 335 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((4 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 335 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((5 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((5 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 360 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((6 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((6 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 385 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((7 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((7 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 410 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((8 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((8 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 435 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);
                Drawing.DrawText((9 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 460 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText((9 * GetRocketDamage()).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 460 + coordYr), new Vector2(30, 200), Color.LimeGreen, FontFlags.AntiAlias);

                Drawing.DrawText("mana", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 232 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText("mana", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 232 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                if (Refresh != null && Refresh.Level > 0)
                {
                    Drawing.DrawText("               x" + Math.Ceiling((Owner.Mana - Manaonerocket()) / (Manaonerocket() + Rearm_mana[Refresh.Level - 1])).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 210 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("               x" + Math.Ceiling((Owner.Mana - Manaonerocket()) / (Manaonerocket() + Rearm_mana[Refresh.Level - 1])).ToString(), new Vector2(HUDInfo.ScreenSizeX() / 2 - 200 + coordXr, HUDInfo.ScreenSizeY() / 2 + 210 + coordYr), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                    Drawing.DrawText(Manaonerocket().ToString() + " (" + (-Manaonerocket() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(Manaonerocket().ToString() + " (" + (-Manaonerocket() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + coordYr), new Vector2(30, 200), (Owner.Mana > Manaonerocket()) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((2 * Manaonerocket() + Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(2 * Manaonerocket() + Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 285 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((2 * Manaonerocket() + Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(2 * Manaonerocket() + Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 285 + coordYr), new Vector2(30, 200), (Owner.Mana > (2 * Manaonerocket() + Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((3 * Manaonerocket() + 2 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(3 * Manaonerocket() + 2 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 310 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((3 * Manaonerocket() + 2 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(3 * Manaonerocket() + 2 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 310 + coordYr), new Vector2(30, 200), (Owner.Mana > (3 * Manaonerocket() + 2 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((4 * Manaonerocket() + 3 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(4 * Manaonerocket() + 3 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 335 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((4 * Manaonerocket() + 3 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(4 * Manaonerocket() + 3 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 335 + coordYr), new Vector2(30, 200), (Owner.Mana > (4 * Manaonerocket() + 3 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((5 * Manaonerocket() + 4 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(5 * Manaonerocket() + 4 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 360 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((5 * Manaonerocket() + 4 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(5 * Manaonerocket() + 4 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 360 + coordYr), new Vector2(30, 200), (Owner.Mana > (5 * Manaonerocket() + 4 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((6 * Manaonerocket() + 5 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(6 * Manaonerocket() + 5 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 385 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((6 * Manaonerocket() + 5 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(6 * Manaonerocket() + 5 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 385 + coordYr), new Vector2(30, 200), (Owner.Mana > (6 * Manaonerocket() + 5 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((7 * Manaonerocket() + 6 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(7 * Manaonerocket() + 6 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 410 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((7 * Manaonerocket() + 6 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(7 * Manaonerocket() + 6 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 410 + coordYr), new Vector2(30, 200), (Owner.Mana > (7 * Manaonerocket() + 6 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((8 * Manaonerocket() + 7 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(8 * Manaonerocket() + 7 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 435 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((8 * Manaonerocket() + 7 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(8 * Manaonerocket() + 7 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 435 + coordYr), new Vector2(30, 200), (Owner.Mana > (8 * Manaonerocket() + 7 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                    Drawing.DrawText((9 * Manaonerocket() + 8 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(9 * Manaonerocket() + 8 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 460 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((9 * Manaonerocket() + 8 * Rearm_mana[Refresh.Level - 1]).ToString() + " (" + (-(9 * Manaonerocket() + 8 * Rearm_mana[Refresh.Level - 1]) + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 460 + coordYr), new Vector2(30, 200), (Owner.Mana > (9 * Manaonerocket() + 8 * Rearm_mana[Refresh.Level - 1])) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                }
                else
                {
                    Drawing.DrawText(Manaonerocket().ToString() + " (" + (-Manaonerocket() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + 2 + coordYr), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(Manaonerocket().ToString() + " (" + (-Manaonerocket() + (int)Owner.Mana).ToString() + ")", new Vector2(HUDInfo.ScreenSizeX() / 2 - 80 + coordXr, HUDInfo.ScreenSizeY() / 2 + 260 + coordYr), new Vector2(30, 200), (Owner.Mana > Manaonerocket()) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                }
            }

            if (Menu.Item("ComboModeDrawing").GetValue<bool>())
            {
                if (Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key) && !Game.IsChatOpen)
                {
                    Drawing.DrawText(" ON!", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2 + 150, HUDInfo.ScreenSizeY() / 2 + 235 + 2), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText(" ON!", new Vector2(HUDInfo.ScreenSizeX() / 2 + 150, HUDInfo.ScreenSizeY() / 2 + 235), new Vector2(30, 200), Menu.Item("Chase").GetValue<KeyBind>().Active == true ? Color.Red : Color.LimeGreen, FontFlags.AntiAlias);

                }

                if (Game.IsKeyDown(Menu.Item("Rocket Spam Key").GetValue<KeyBind>().Key) && !Game.IsChatOpen)
                {
                    Drawing.DrawText("Rocket Spam!", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2, HUDInfo.ScreenSizeY() / 2 + 185 + 2), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("Rocket Spam!", new Vector2(HUDInfo.ScreenSizeX() / 2, HUDInfo.ScreenSizeY() / 2 + 185), new Vector2(30, 200), Color.Yellow, FontFlags.AntiAlias);
                }

                if (Game.IsKeyDown(Menu.Item("March Spam Key").GetValue<KeyBind>().Key) && !Game.IsChatOpen)
                {
                    Drawing.DrawText("March Spam!", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2, HUDInfo.ScreenSizeY() / 2 + 210 + 2), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText("March Spam!", new Vector2(HUDInfo.ScreenSizeX() / 2, HUDInfo.ScreenSizeY() / 2 + 210), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                }

                Drawing.DrawText(Menu.Item("Chase").GetValue<KeyBind>().Active == true ? "Chase Mode" : "Combo Mode", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2, HUDInfo.ScreenSizeY() / 2 + 235 + 2), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText(Menu.Item("Chase").GetValue<KeyBind>().Active == true ? "Chase Mode" : "Combo Mode", new Vector2(HUDInfo.ScreenSizeX() / 2, HUDInfo.ScreenSizeY() / 2 + 235), new Vector2(30, 200), Menu.Item("Chase").GetValue<KeyBind>().Active == true ? Color.Red : Color.LimeGreen, FontFlags.AntiAlias);

                Drawing.DrawText(Menu.Item("TargetLock").GetValue<StringList>().SelectedIndex == 0 ? "Target: Free" : "Target: Lock", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2, HUDInfo.ScreenSizeY() / 2 + 285 + 2), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                Drawing.DrawText(Menu.Item("TargetLock").GetValue<StringList>().SelectedIndex == 0 ? "Target: Free" : "Target: Lock", new Vector2(HUDInfo.ScreenSizeX() / 2, HUDInfo.ScreenSizeY() / 2 + 285), new Vector2(30, 200), Color.White, FontFlags.AntiAlias);

                if (Menu.Item("autoKillsteal").GetValue<bool>())
                {
                    Drawing.DrawText((Menu.Item("Chase").GetValue<KeyBind>().Active == true || !Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key)) ? "KS: on" : "KS: off", new Vector2(HUDInfo.ScreenSizeX() / 2 + 2, HUDInfo.ScreenSizeY() / 2 + 260 + 2), new Vector2(30, 200), Color.Black, FontFlags.AntiAlias);
                    Drawing.DrawText((Menu.Item("Chase").GetValue<KeyBind>().Active == true || !Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key)) ? "KS: on" : "KS: off", new Vector2(HUDInfo.ScreenSizeX() / 2, HUDInfo.ScreenSizeY() / 2 + 260), new Vector2(30, 200), (Menu.Item("Chase").GetValue<KeyBind>().Active == true || !Game.IsKeyDown(Menu.Item("Combo Key").GetValue<KeyBind>().Key)) ? Color.LimeGreen : Color.Red, FontFlags.AntiAlias);
                }
            }
        }

        private float GetComboDamage()
        {
            var etheral_blade_magic_reduction = 0.0f;
            var veil_of_discord_magic_reduction = 0.0f;
            var base_magic_res = 0.25f;

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

            var totalMagicResistance = ((1 - base_magic_res) * (1 + etheral_blade_magic_reduction) * (1 + veil_of_discord_magic_reduction));
            return ((GetEtherealBladeDamage() + GetRocketDamage() + GetDagonDamage()) * totalMagicResistance) + GetLaserDamage();
        }

        private float GetComboDamage(Hero enemy)
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
            return ((GetEtherealBladeDamage() + GetRocketDamage() + GetDagonDamage()) * totalMagicResistance) + GetLaserDamage();
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

        private float GetComboDamageByDistance(Hero enemy)
        {
            if (enemy != null && enemy.IsAlive && enemy.IsValid)
            {
                var comboDamageByDistance = 0.0f;
                var etheral_blade_magic_reduction = 0.0f;
                var veil_of_discord_magic_reduction = 0.0f;

                var eblade = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_ethereal_blade"));

                if (((eblade != null && eblade.CanBeCasted())
                    || (eblade != null && IsCasted(eblade)))
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_ethereal_blade")
                    && !enemy.Modifiers.Any(y => y.Name == "modifier_item_ethereal_blade_ethereal"))
                {
                    etheral_blade_magic_reduction = 0.4f;
                }

                var veil = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_veil_of_discord"));
                if (veil != null
                    && veil.CanBeCasted()
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_veil_of_discord")
                    && !enemy.Modifiers.Any(y => y.Name == "modifier_item_veil_of_discord_debuff"))
                {
                    veil_of_discord_magic_reduction = 0.25f;
                }

                var totalMagicResistance = ((1 - enemy.MagicDamageResist) * (1 + etheral_blade_magic_reduction) * (1 + veil_of_discord_magic_reduction));
                var dagon = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_dagon"));
                if (dagon != null
                    && dagon.CanBeCasted()
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_dagon")
                    && (Owner.Distance2D(enemy) < dagon.AbilitySpecialData.First(x => x.Name == "#AbilityCastRange").GetValue(dagon.Level - 1) + Castrange + Ensage_error))
                {
                    comboDamageByDistance += GetDagonDamage() * totalMagicResistance;
                }

                if (((eblade != null && eblade.CanBeCasted())
                    || (eblade != null && IsCasted(eblade)))
                    && Menu.Item("ComboItems: ").GetValue<AbilityToggler>().IsEnabled("item_ethereal_blade")
                    && (Owner.Distance2D(enemy) < 800 + Castrange + Ensage_error))
                {
                    comboDamageByDistance += GetEtherealBladeDamage() * totalMagicResistance;
                }

                var laser = Owner.Spellbook.SpellQ;
                if (laser != null
                    && laser.Level > 0
                    && laser.CanBeCasted()
                    && (Owner.Distance2D(enemy) < 650 + Castrange + Ensage_error))
                {
                    comboDamageByDistance += GetLaserDamage();
                }

                //Distance Calculation
                var rocket = Owner.Spellbook.SpellW;
                if ((rocket != null && rocket.Level > 0 && rocket.CanBeCasted()) || (rocket != null && rocket.Level > 0 && IsCasted(rocket)))
                {
                    if (Owner.Distance2D(enemy) < 800 + Castrange + Ensage_error)
                    {
                        comboDamageByDistance += GetRocketDamage() * totalMagicResistance;
                    }
                    else if (Owner.Distance2D(enemy) >= 800 + Castrange + Ensage_error && Owner.Distance2D(enemy) < 1600 + Castrange + Ensage_error)
                    {
                        comboDamageByDistance += GetRocketDamage() * ((1 - enemy.MagicDamageResist) * (1 + veil_of_discord_magic_reduction));
                    }
                    else if (Owner.Distance2D(enemy) >= 1600 + Castrange + Ensage_error && Owner.Distance2D(enemy) < 2500)
                    {
                        comboDamageByDistance += GetRocketDamage() * ((1 - enemy.MagicDamageResist));
                    }
                }

                if (Owner.CanAttack()
                    && !enemy.IsAttackImmune()
                    && Owner.Distance2D(enemy) < Owner.GetAttackRange() + 50
                    && !enemy.IsInvul()
                    && !IsPhysDamageImune(enemy))
                {
                    comboDamageByDistance += (enemy.DamageTaken(Owner.BonusDamage + Owner.DamageAverage, DamageType.Physical, Owner));
                }

                return comboDamageByDistance;
            }

            return 0;
        }

        private float GetLaserDamage()
        {
            return _abilities.Skill1.GetDamage();
        }

        private float GetRocketDamage()
        {
            return _abilities.Skill2.GetDamage();
        }

        private float GetDagonDamage()
        {
            return _abilities.Dagon != null ? _abilities.Dagon.GetDamage() : 0;
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
            return _abilities.Ethereal!=null? _abilities.Ethereal.GetDamage():0;
        }

        private Vector3 GetClosestToVector(Vector3[] coords, Unit z)
        {
            var closestVector = coords.First();
            foreach (var v in coords.Where(v => closestVector.Distance2D(z) > v.Distance2D(z)))
            {
                closestVector = v;
            }

            return closestVector;
        }

        private void ParticleDraw()
        {
            for (int i = 0; i < SafePos.Count(); ++i)
            {
                if (!Iscreated)
                {
                    var effect = new ParticleEffect(EffectPath, SafePos[i]);
                    effect.SetControlPoint(1, new Vector3(HIDE_AWAY_RANGE, 0, 0));
                    Effects.Add(effect);
                }
            }

            Iscreated = true;
        }

        private Vector3[] SafePos { get; } =
        {
            new Vector3(-7305, -5016, 384),
            new Vector3(-7328, -4768, 384),
            new Vector3(-7264, -4505, 384),
            new Vector3(-7136, -4384, 384),
            new Vector3(-7072, -1120, 384),
            new Vector3(-7072, -672, 384),
            new Vector3(-7200, -288, 384),
            new Vector3(-6880, 288, 384),
            new Vector3(-6944, 1568, 384),
            new Vector3(-6688, 3488, 384),
            new Vector3(-6752, 3616, 384),
            new Vector3(-6816, 3744, 384),
            new Vector3(-6816, 4448, 384),
            new Vector3(-5152, 5088, 384),
            new Vector3(-3936, 5536, 384),
            new Vector3(-5152, 6624, 384),
            new Vector3(-3680, 6624, 384),
            new Vector3(-2720, 6752, 384),
            new Vector3(-2720, 5536, 384),
            new Vector3(-1632, 6688, 384),
            new Vector3(-1056, 6752, 384),
            new Vector3(-736, 6816, 384),
            new Vector3(-992, 5536, 384),
            new Vector3(-1568, 5536, 384),
            new Vector3(608, 7008, 384),
            new Vector3(1632, 6752, 256),
            new Vector3(2336, 7136, 384),
            new Vector3(1568, 3040, 384),
            new Vector3(1824, 3296, 384),
            new Vector3(-2976, 480, 384),
            new Vector3(736, 1056, 256),
            new Vector3(928, 1248, 256),
            new Vector3(928, 1696, 256),
            new Vector3(2784, 992, 256),
            new Vector3(-2656, -1440, 256),
            new Vector3(-2016, -2464, 256),
            new Vector3(-2394, -3110, 256),
            new Vector3(-1568, -3232, 256),
            new Vector3(-2336, -4704, 256),
            new Vector3(-416, -7072, 384),
            new Vector3(2336, -5664, 384),
            new Vector3(2464, -5728, 384),
            new Vector3(2848, -5664, 384),
            new Vector3(2400, -6817, 384),
            new Vector3(3040, -6624, 384),
            new Vector3(4256, -6624, 384),
            new Vector3(4192, -6880, 384),
            new Vector3(5024, -5408, 384),
            new Vector3(5856, -6240, 384),
            new Vector3(6304, -6112, 384),
            new Vector3(6944, -5472, 384),
            new Vector3(7328, -5024, 384),
            new Vector3(7200, -3296, 384),
            new Vector3(7200, -2272, 384),
            new Vector3(6944, -992, 384),
            new Vector3(6816, -224, 384),
            new Vector3(7200, 480, 384),
            new Vector3(7584, 2080, 256),
            new Vector3(7456, 2784, 384),
            new Vector3(5344, 2528, 384),
            new Vector3(7200, 5536, 384),
            new Vector3(4192, 6944, 384),
            new Vector3(5472, 6752, 384),
            new Vector3(-6041, -6883, 384),
            new Vector3(-5728, -6816, 384),
            new Vector3(-5408, -7008, 384),
            new Vector3(-5088, -7072, 384),
            new Vector3(-4832, -7072, 384),
            new Vector3(-3744, -7200, 384)
        };
    }
}
