using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using wtf.lion.Models;

namespace wtf.lion.Parts
{




    //卫星类，用来远程计算伤害

    [Export("satellite")]
    public class Satellite
    {
        private Hero Owner;
        public List<Damage> DamageList { get; } = new List<Damage>();

        [Import("abilities")]
        private Abilities _abilities;
        [Import("helper")]
        private Helper _helper;

        [ImportingConstructor]
        public Satellite([Import] IServiceContext context)
        {
            Owner=context.Owner as Hero;;
        }


        public void Install()
        {
            UpdateManager.Subscribe(OnUpdate, 50);
        }

        public void Uninstall()
        {
            UpdateManager.Unsubscribe(OnUpdate);
        }


        public void OnUpdate()
        {
            var heroes = EntityManager<Hero>.Entities.Where(x => x.IsValid && !x.IsIllusion).ToList();

            DamageList.Clear();

            foreach (var target in heroes.Where(x => x.IsAlive && x.IsEnemy(Owner)).ToList())
            {
                List<BaseAbility> abilities = new List<BaseAbility>();
                var damageByAura = 0.0f;
                var extendUltDamage = 0.0f;
                var damageByAttack = 0.0f;
                var damageOfMisc = 0f;
                bool canAttack = true;
                if (target.IsVisible)
                {

                    var skill2 = _abilities.Skill2;
                    if (skill2.Ability.Level > 0)
                    {
                        abilities.Add(skill2);
                    }
                    // Veil
                    var veil = _abilities.Veil;
                    if (veil != null && veil.Ability.IsValid)
                    {
                        abilities.Add(veil);
                    }

                    // Ethereal
                    var ethereal = _abilities.Ethereal;
                    if (ethereal != null && ethereal.Ability.IsValid)
                    {
                        abilities.Add(ethereal);
                        canAttack = false;
                    }

                    // Shivas
                    var shivas = _abilities.Shivas;
                    if (shivas != null && shivas.Ability.IsValid)
                    {
                        abilities.Add(shivas);
                    }
                    //1
                    var skill1 = _abilities.Skill1;
                    if (skill1.Ability.Level > 0)
                    {
                        abilities.Add(skill1);
                    }
                    // Dagon
                    var dagon = _abilities.Dagon;
                    if (dagon != null && dagon.Ability.IsValid)
                    {
                        abilities.Add(dagon);
                    }

                    if (canAttack)
                    {
                        damageByAttack = Owner.GetAttackDamage(target, true);
                    }

                    // ult
                    var ult = _abilities.Skill4;
                    if (ult.Ability.Level > 0)
                    {
                        abilities.Add(ult);
                    }
                    
                }

                var damageCalculation = new Combo(abilities.ToArray());
                //没有计算来自装备的伤害加深
                damageOfMisc = damageByAttack;
                //Console.WriteLine($"{damageByAttack}/{damageByAura}/{extendUltDamage}/{DamageHelpers.GetSpellDamage(extendUltDamage, Owner.GetSpellAmplification(), damageReduction)}");
                var livingArmor =_helper.LivingArmor(target, heroes, damageCalculation.Abilities);
                //目前就能打死的
                var damage = _helper.DamageReCalc(damageCalculation.GetDamage(target) + damageOfMisc, target, heroes,
                    abilities.ToArray());
                //上去就能干死的
                var readyDamage = _helper.DamageReCalc(damageCalculation.GetDamage(target,true, false) + damageOfMisc, target, heroes,
                    abilities.ToArray());
                //所有状态完美的总体伤害
                var totalDamage = 0;
                //Console.WriteLine($"{damage}/{readyDamage}/{totalDamage}");
                DamageList.Add(new Damage(target, damage, readyDamage, totalDamage, target.Health, abilities.ToArray()));
            }
        }
    }
}