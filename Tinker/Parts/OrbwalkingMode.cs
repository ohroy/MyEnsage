using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Threading;
using Ensage.SDK.Extensions;
using Ensage.SDK.Orbwalker;
using Ensage.SDK.Orbwalker.Modes;
using Ensage.SDK.Service;
using wtf.tinker.Models;
using AbilityExtensions = Ensage.Common.Extensions.AbilityExtensions;
using Menu = wtf.tinker.Parts.Menu;

namespace wtf.tinker.Parts
{
    [Export("orb_mode")]
    public class OrbwalkingMode : OrbwalkingModeAsync
    {

        [Import("abilities")]
        private wtf.tinker.Models.Abilities _abilities;
        [Import("menu")]
        private Menu _menu;
        [Import("target_selector")]
        private TargetSelector _targetSelector;
        private Hero _owner;
        private bool canExecute;
        private IOrbwalker _orbwalker;
        [Import("helper")]
        private Helper _helper;

        private IServiceContext _context;


        public override bool CanExecute
        {
            get
            {
                return this.canExecute;
            }
        }


        [ImportingConstructor]
        public OrbwalkingMode([Import] IServiceContext context) : base(context)
        {
            _owner = context.Owner as Hero;
            _orbwalker=context.Orbwalker;
            _context = context;
        }

        public void Install()
        {
            _context.Orbwalker.RegisterMode(this);
            if (this._menu.ComboKeyItem != null)
            {
                this._menu.ComboKeyItem.PropertyChanged += this.MenuKeyOnPropertyChanged;
            }

        }

        public void Uninstall()
        {
            _context.Orbwalker.UnregisterMode(this);
            if (this._menu.ComboKeyItem != null)
            {
                this._menu.ComboKeyItem.PropertyChanged -= this.MenuKeyOnPropertyChanged;
            }
        }


        private void MenuKeyOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (this._menu.ComboKeyItem)
            {
                this.canExecute = true;
            }
            else
            {
                this.canExecute = false;
                this.Cancel();
            }
        }


        private bool isTargetControled(Hero target)
        {
            var buff = new string[]
            {
                _abilities.Hex?.TargetModifierName,
                "modifier_sheepstick_debuff",
                //_abilities.Skill2?.TargetModifierName,
                //_abilities.Skill1?.TargetModifierName,
                _abilities.Nullifier?.TargetModifierName
            };
            var modifiers = target.Modifiers.Any(x => (x.IsStunDebuff
                                                        || x.Name.In(buff))
            && x.IsValid && x.RemainingTime > 0.1f);
            return modifiers;
            //return modifiers.Any(e => e.IsValid && e.RemainingTime > 0.3f);
        }

        private async Task tryControl(Hero target,CancellationToken token)
        {

            if (!_helper.CanCombo(target))
            {
                return;
            }

            if (!isTargetControled(target))
            {
                // 变羊
                var skill2 = _abilities.Skill2;
                if (skill2 != null
                    && _menu.AbilityToggler.Value.IsEnabled(skill2.ToString())
                    && skill2.CanBeCasted
                    && skill2.CanHit(target))
                {
                    skill2.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(skill2,target), token);
                    return;
                }
                // 羊刀
                var hex = _abilities.Hex;
                if (hex != null
                    && _menu.ItemToggler.Value.IsEnabled(hex.ToString())
                    && hex.CanBeCasted
                    && hex.CanHit(target))
                {
                    hex.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(hex, target), token);
                    return;
                }
                // 穿刺
                var skill1 = _abilities.Skill1;
                if (skill1 != null
                    && _menu.AbilityToggler.Value.IsEnabled(skill1.ToString())
                    && skill1.CanBeCasted
                    && skill1.CanHit(target))
                {
                    skill1.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(skill1, target), token);
                    return;
                }

                // 否决
                var nullifier = _abilities.Nullifier;
                if (nullifier != null
                    && _menu.ItemToggler.Value.IsEnabled(nullifier.ToString())
                    && nullifier.CanBeCasted
                    && nullifier.CanHit(target))
                {
                    nullifier.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(nullifier, target), token);
                    return;
                }
                // RodofAtos
                var atosDebuff = target.Modifiers.Any(x => x.IsValid && x.Name == "modifier_rod_of_atos_debuff" && x.RemainingTime > 0.5f);
                var rodofAtos = _abilities.RodofAtos;
                if (rodofAtos != null
                    && _menu.ItemToggler.Value.IsEnabled(rodofAtos.ToString())
                    && rodofAtos.CanBeCasted
                    && rodofAtos.CanHit(target)
                    && !atosDebuff)
                {
                    rodofAtos.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(rodofAtos, target), token);
                }

            }


        }


        public override async Task ExecuteAsync(CancellationToken token)
        {
            var target = _targetSelector.Target;
            if (!_helper.CanCombo(target))
            {
                //_orbwalker.Move(Game.MousePosition);
                _orbwalker.OrbwalkTo(null);
                return;
            }
            if (!target.IsBlockingAbilities())
            {
                var comboBreaker = _helper.ComboBreaker(target);
                var stunDebuff = target.Modifiers.FirstOrDefault(x => x.IsStunDebuff);
                var hexDebuff = target.Modifiers.FirstOrDefault(x => x.Name == "modifier_sheepstick_debuff");


                await tryControl(target, token);
                // 紫苑
                var orchid = _abilities.Orchid;
                if (orchid != null
                    && _menu.ItemToggler.Value.IsEnabled(orchid.ToString())
                    && orchid.CanBeCasted
                    && orchid.CanHit(target)
                    && !comboBreaker)
                {
                    orchid.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(orchid, target), token);
                }

                // 血荆
                var bloodthorn = _abilities.Bloodthorn;
                if (bloodthorn != null
                    && _menu.ItemToggler.Value.IsEnabled(bloodthorn.ToString())
                    && bloodthorn.CanBeCasted
                    && bloodthorn.CanHit(target)
                    && !comboBreaker)
                {
                    bloodthorn.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(bloodthorn, target), token);
                }

                // Veil
                var veil = _abilities.Veil;
                if (veil != null
                    && _menu.ItemToggler.Value.IsEnabled(veil.ToString())
                    && veil.CanBeCasted
                    && veil.CanHit(target))
                {
                    veil.UseAbility(target.Position);
                    await Await.Delay(_helper.GetAbilityWaitTime(veil, target), token);
                }

                // Ethereal
                var ethereal = _abilities.Ethereal;
                if (ethereal != null
                    && _menu.ItemToggler.Value.IsEnabled(ethereal.ToString())
                    && ethereal.CanBeCasted
                    && ethereal.CanHit(target)
                    && !comboBreaker)
                {
                    ethereal.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(ethereal, target), token);
                }
                //大招
                var skill4 = _abilities.Skill4;
                if (skill4 != null
                    && _menu.AbilityToggler.Value.IsEnabled(skill4.ToString())
                    && skill4.CanBeCasted
                    && skill4.CanHit(target))
                {
                    skill4.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(skill4, target), token);
                    return;
                }
                // Shivas
                var shivas = _abilities.Shivas;
                if (shivas != null
                    && _menu.ItemToggler.Value.IsEnabled(shivas.ToString())
                    && shivas.CanBeCasted
                    && shivas.CanHit(target))
                {
                    shivas.UseAbility();
                    await Await.Delay(_helper.GetAbilityWaitTime(shivas, target), token);
                }

                // Dagon
                var dagon = _abilities.Dagon;
                    if (dagon != null
                        && _menu.ItemToggler.Value.IsEnabled("item_dagon_5")
                        && dagon.CanBeCasted
                        && dagon.CanHit(target)
                        && !comboBreaker)
                    {
                        dagon.UseAbility(target);
                        await Await.Delay(_helper.GetAbilityWaitTime(dagon, target), token);
                    return;
                    }
                // UrnOfShadows
                var urnOfShadows = _abilities.UrnOfShadows;
                if (urnOfShadows != null
                    && _menu.ItemToggler.Value.IsEnabled(urnOfShadows.ToString())
                    && urnOfShadows.CanBeCasted
                    && urnOfShadows.CanHit(target)
                    && !comboBreaker)
                {
                    urnOfShadows.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(urnOfShadows, target), token);
                }

                // SpiritVessel
                var spiritVessel = _abilities.SpiritVessel;
                if (spiritVessel != null
                    && _menu.ItemToggler.Value.IsEnabled(spiritVessel.ToString())
                    && spiritVessel.CanBeCasted
                    && spiritVessel.CanHit(target)
                    && !comboBreaker)
                {
                    spiritVessel.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(spiritVessel, target), token);
                }
                Orbwalker.OrbwalkTo(target);
            }
            else
            {
                var skill3 = _abilities.Skill3;
                if (skill3 != null
                    //&& _menu.AbilityToggler.Value.IsEnabled(skill3.ToString())
                    && skill3.CanBeCasted
                    && skill3.CanHit(target))
                {
                    skill3.UseAbility(target);
                    await Await.Delay(_helper.GetAbilityWaitTime(skill3, target), token);
                }
            }
            return;
        }

    }
}