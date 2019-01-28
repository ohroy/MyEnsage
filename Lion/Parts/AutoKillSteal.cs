using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Threading;
using Ensage.SDK.Abilities;
using Ensage.SDK.Extensions;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using wtf.lion.Models;

namespace wtf.lion.Parts
{
    [Export("autokillsteal")]
    class AutoKillSteal
    {
        private TaskHandler _handler;
        private IUpdateHandler _update;
        private Damage _damage;
        private Hero _owner;
        [Import("helper")]
        private Helper _helper;
        [Import("satellite")]
        private Satellite _satellite;
        [Import("abilities")]
        private Abilities _abilities;
        [Import("menu")]
        private Menu _menu;


        [ImportingConstructor]
        public  AutoKillSteal([Import] IServiceContext context)
        {
            _owner = context.Owner as Hero;
        }

        public void Install()
        {
            _handler = UpdateManager.Run(ExecuteAsync, true, false);
            //主逻辑检测当前目标是不是已经挂了，如果挂了就停止
            _menu.IsAutoKillStealEnabled.PropertyChanged += IsAutoKillStealChanged;
            IsAutoKillStealChanged(null, null);
        }

        public void Uninstall()
        {
            _menu.IsAutoKillStealEnabled.PropertyChanged -= IsAutoKillStealChanged;
            _handler?.Cancel();
            UpdateManager.Unsubscribe(Stop);
        }


        private void IsAutoKillStealChanged(object sender, PropertyChangedEventArgs e)
        {
            if (_menu.IsAutoKillStealEnabled)
            {
                _handler.RunAsync();
                _update = UpdateManager.Subscribe(Stop, 0, false);
            }
            else
            {
                _handler?.Cancel();
                UpdateManager.Unsubscribe(Stop);
            }
        }

        private async Task<bool> tryKillStealFast(Hero target, CancellationToken token)
        {

            List<BaseAbility> abilities = new List<BaseAbility>();
            //先尝试大招能不能秒杀
            var ult = _abilities.Skill4;
            if (ult.Ability.Level > 0)
            {
                //Console.WriteLine($"{ult.Ability.GetAbilitySpecialDataWithTalent(_owner, "damage") }");
                abilities.Add(ult);
            }

            var ret = await tryKillFast(abilities, target, token);
            if (ret)
            {
                return ret;
            }
            //不行就计算大根加大招
            var dagon = _abilities.Dagon;
            if (dagon != null && dagon.Ability.IsValid)
            {
                abilities.Add(dagon);
            }
            ret = await tryKillFast(abilities, target, token);
            if (ret)
            {
                return ret;
            }
            //如果还不行加上虚灵刀
            var ethereal = _abilities.Ethereal;
            if (ethereal != null && ethereal.Ability.IsValid)
            {
                abilities.Add(ethereal);
            }
            ret = await tryKillFast(abilities, target, token);
            if (ret)
            {
                return ret;
            }

            return false;
        }

        private async Task<bool> tryKillFast(List<BaseAbility> abilities, Hero target, CancellationToken token)
        {
           
            var heroes = EntityManager<Hero>.Entities.Where(x => x.IsValid && !x.IsIllusion).ToList();

            var combo = new Combo(abilities.ToArray());
            if (combo.IsInRange(target))
            {
                var realDamage = combo.GetDamage(target);
                var damage = _helper.DamageReCalc(realDamage, target, heroes, abilities.ToArray());
                //Console.WriteLine($"damage:{realDamage},{damage}");
                if (damage > target.Health)
                {
                    await combo.Execute(target,token);
                    //await Await.Delay((int)200, token);
                    //Console.WriteLine(target.Health);

                    return true;
                }
            }

            return false;
            // Dagon


        }

        private async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                if (Game.IsPaused || !_owner.IsValid || !_owner.IsAlive || _owner.IsStunned())
                {
                    return;
                }

                //当连招时禁用抢人头
                if (!_menu.IsAutoKillStealEnabledWhenCombo && _menu.ComboKeyItem)
                {
                    return;
                }


                var damageCalculation = _satellite.DamageList.Where(x => (x.GetTarget.Health - x.GetDamage) <= 0.0f).ToList();
                _damage = damageCalculation.OrderByDescending(x => x.GetHealth).OrderByDescending(x => x.GetTarget.Player.Kills).FirstOrDefault();

                if (_damage == null)
                {
                    return;
                }

                if (!_update.IsEnabled)
                {
                    _update.IsEnabled = true;
                }

                var target = _damage.GetTarget;

                if (!Cancel(target) || _helper.ComboBreaker(target, false))
                {
                    return;
                }

                if (!target.IsBlockingAbilities())
                {

                    var isFastKilled=await tryKillStealFast(target, token);
                    if (isFastKilled)
                    {
                        return;
                    }
                    Combo combo = new Combo(_damage.ComboAbility);
                    if (combo.IsInRange(target))
                    {
                        await combo.Execute(target, token);
                    }
                }
                else
                {
                   // Config.LinkenBreaker.Handler.RunAsync();
                }
            }
            catch (TaskCanceledException)
            {
                // canceled
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private bool Cancel(Hero target)
        {
            return !_owner.IsInvisible()
                && !target.IsMagicImmune()
                && !target.IsInvulnerable()
                && !target.HasAnyModifiers("modifier_dazzle_shallow_grave", "modifier_necrolyte_reapers_scythe")
                && !_helper.DuelAghanimsScepter(target)
                && !Reincarnation(target);
        }

        private bool Reincarnation(Hero target)
        {
            var reincarnation = target.GetAbilityById(AbilityId.skeleton_king_reincarnation);
            return reincarnation != null && reincarnation.Cooldown == 0 && reincarnation.Level > 0;
        }

        private void Stop()
        {
            if (_damage == null)
            {
                _update.IsEnabled = false;
                return;
            }

            var stop = EntityManager<Hero>.Entities.Any(x => !x.IsAlive && x == _damage.GetTarget);
            if (stop && _owner.Animation.Name.Contains("cast"))
            {
                _owner.Stop();
                _update.IsEnabled = false;
            }
        }
    }
}
