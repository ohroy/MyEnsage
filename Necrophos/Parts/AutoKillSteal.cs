using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.Common.Threading;
using Ensage.SDK.Extensions;
using Ensage.SDK.Handlers;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using PlaySharp.Toolkit.Extensions;
using wtf.Models;

namespace wtf.Parts
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
        [ImportingConstructor]
        public  AutoKillSteal([Import] IServiceContext context)
        {
            _owner = context.Owner as Hero;
        }

        public void Install()
        {
            _handler = UpdateManager.Run(ExecuteAsync, true, false);
            _handler.RunAsync();
            //主逻辑检测当前目标是不是已经挂了，如果挂了就停止
            _update= UpdateManager.Subscribe(Stop, 0, false);
        }

        public void Uninstall()
        {
            _handler?.Cancel();
        }
        private async Task ExecuteAsync(CancellationToken token)
        {
            try
            {
                if (Game.IsPaused || !_owner.IsValid || !_owner.IsAlive || _owner.IsStunned())
                {
                    return;
                }

                var damageCalculation = _satellite.DamageList.Where(x => (x.GetHealth - x.GetDamage) / x.GetTarget.MaximumHealth <= 0.0f).ToList();
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
                    var ult = _abilities.Scythe;
                    if (ult.CanBeCasted
                        && ult.CanHit(target))
                    {
                        ult.UseAbility(target);
                        Console.WriteLine($"{ult.GetCastDelay(target)}");
                        await Await.Delay(ult.GetCastDelay(target) + 200, token);
                        Console.WriteLine($"---");
                    }

                    var pulse = _abilities.Pulse;
                    if (pulse.CanBeCasted
                        && pulse.CanHit(target))
                    {
                        Console.WriteLine("一技能!");
                        pulse.UseAbility();
                        Console.WriteLine($"{pulse.GetCastDelay(target)}");
                        await Await.Delay(pulse.GetCastDelay(target), token);
                        Console.WriteLine("完了!");
                    }

                    // Veil
                    var veil = _abilities.Veil;
                    if (veil != null
                        && veil.CanBeCasted
                        && veil.CanHit(target))
                    {
                        veil.UseAbility(target.Position);
                        await Await.Delay(veil.GetCastDelay(target.Position), token);
                    }

                    // Ethereal
                    var ethereal = _abilities.Ethereal;
                    if (ethereal != null
                        && ethereal.CanBeCasted
                        && ethereal.CanHit(target))
                    {
                        ethereal.UseAbility(target);
                        _helper.MultiSleeper.Sleep(ethereal.GetHitTime(target), "ethereal");
                        await Await.Delay(ethereal.GetCastDelay(target), token);
                    }

                    // Shivas
                    var shivas = _abilities.Shivas;
                    if (shivas != null
                        && shivas.CanBeCasted
                        && shivas.CanHit(target))
                    {
                        shivas.UseAbility();
                        await Await.Delay(shivas.GetCastDelay(), token);
                    }

                        // Dagon
                    var dagon = _abilities.Dagon;
                    if (dagon != null
                        && dagon.CanBeCasted
                        && dagon.CanHit(target))
                    {
                        Console.WriteLine("大根!");
                        dagon.UseAbility(target);
                        await Await.Delay(dagon.GetCastDelay(target), token);
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
