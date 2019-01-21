using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Ensage;
using Ensage.SDK.Abilities;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using wtf.Models;

namespace wtf.Parts
{




    //卫星类，用来远程计算伤害

    [Export("satellite")]
    public class Satellite
    {
        private Hero Owner;
        public List<Damage> DamageList { get; } = new List<Damage>();

        [Import("abilities")]
        private Abilities _abilities;


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

        private string[] BlockModifiers { get; } =
        {
            "modifier_abaddon_borrowed_time",
            "modifier_item_combo_breaker_buff",
            "modifier_winter_wyvern_winters_curse_aura",
            "modifier_winter_wyvern_winters_curse",
            "modifier_templar_assassin_refraction_absorb",
            "modifier_oracle_fates_edict"
        };
        private float LivingArmor(Hero target, List<Hero> heroes, IReadOnlyCollection<BaseAbility> abilities)
        {
            if (!target.HasModifier("modifier_treant_living_armor"))
            {
                return 0;
            }

            var treant = heroes.FirstOrDefault(x => x.IsEnemy(Owner) && x.HeroId == HeroId.npc_dota_hero_treant);
            var ability = treant.GetAbilityById(AbilityId.treant_living_armor);
            var block = ability.GetAbilitySpecialData("damage_block");

            var count = abilities.Where(x => x.GetDamage(target) > block).Count();

            return count * block;
        }
        private float DamageReduction(Hero target, List<Hero> heroes)
        {
            var value = 0.0f;

            // Bristleback
            var bristleback = target.GetAbilityById(AbilityId.bristleback_bristleback);
            if (bristleback != null && bristleback.Level != 0)
            {
                var brist = bristleback.Owner as Hero;
                if (brist.FindRotationAngle(Owner.Position) > 1.90f)
                {
                    value -= bristleback.GetAbilitySpecialData("back_damage_reduction") / 100f;
                }
                else if (brist.FindRotationAngle(Owner.Position) > 1.20f)
                {
                    value -= bristleback.GetAbilitySpecialData("side_damage_reduction") / 100f;
                }
            }

            // Modifier Centaur Stampede
            if (target.HasModifier("modifier_centaur_stampede"))
            {
                var centaur = heroes.FirstOrDefault(x => x.IsEnemy(Owner) && x.HeroId == HeroId.npc_dota_hero_centaur);
                if (centaur.HasAghanimsScepter())
                {
                    var ability = centaur.GetAbilityById(AbilityId.centaur_stampede);

                    value -= ability.GetAbilitySpecialData("damage_reduction") / 100f;
                }
            }

            // Modifier Kunkka Ghostship
            if (target.HasModifier("modifier_kunkka_ghost_ship_damage_absorb"))
            {
                var kunkka = heroes.FirstOrDefault(x => x.IsEnemy(Owner) && x.HeroId == HeroId.npc_dota_hero_kunkka);
                var ability = kunkka.GetAbilityById(AbilityId.kunkka_ghostship);

                value -= ability.GetAbilitySpecialData("ghostship_absorb") / 100f;
            }

            // Modifier Wisp Overcharge
            if (target.HasModifier("modifier_wisp_overcharge"))
            {
                var wisp = heroes.FirstOrDefault(x => x.IsEnemy(Owner) && x.HeroId == HeroId.npc_dota_hero_wisp);
                var ability = wisp.GetAbilityById(AbilityId.wisp_overcharge);

                value += ability.GetAbilitySpecialData("bonus_damage_pct") / 100f;
            }

            // Modifier Bloodseeker Bloodrage
            if (target.HasModifier("modifier_bloodseeker_bloodrage") || Owner.HasModifier("modifier_bloodseeker_bloodrage"))
            {
                var bloodseeker = heroes.FirstOrDefault(x => x.HeroId == HeroId.npc_dota_hero_bloodseeker);
                var ability = bloodseeker.GetAbilityById(AbilityId.bloodseeker_bloodrage);

                value += ability.GetAbilitySpecialData("damage_increase_pct") / 100f;
            }

            // Modifier Medusa Mana Shield
            if (target.HasModifier("modifier_medusa_mana_shield"))
            {
                var ability = target.GetAbilityById(AbilityId.medusa_mana_shield);

                if (target.Mana >= 50)
                {
                    value -= ability.GetAbilitySpecialData("absorption_tooltip") / 100f;
                }
            }

            // Modifier Ursa Enrage
            if (target.HasModifier("modifier_ursa_enrage"))
            {
                var ability = target.GetAbilityById(AbilityId.ursa_enrage);
                value -= ability.GetAbilitySpecialData("damage_reduction") / 100f;
            }

            // Modifier Chen Penitence
            if (target.HasModifier("modifier_chen_penitence"))
            {
                var chen = heroes.FirstOrDefault(x => x.IsAlly(Owner) && x.HeroId == HeroId.npc_dota_hero_chen);
                var ability = chen.GetAbilityById(AbilityId.chen_penitence);

                value += ability.GetAbilitySpecialData("bonus_damage_taken") / 100f;
            }

            // Modifier Shadow Demon Soul Catcher
            if (target.HasModifier("modifier_shadow_demon_soul_catcher"))
            {
                var shadowDemon = heroes.FirstOrDefault(x => x.IsAlly(Owner) && x.HeroId == HeroId.npc_dota_hero_shadow_demon);
                var ability = shadowDemon.GetAbilityById(AbilityId.shadow_demon_soul_catcher);

                value += ability.GetAbilitySpecialData("bonus_damage_taken") / 100f;
            }

            return value;
        }
        private float DamageBlock(Hero target, List<Hero> heroes)
        {
            var value = 0.0f;

            // Modifier Hood Of Defiance Barrier
            if (target.HasModifier("modifier_item_hood_of_defiance_barrier"))
            {
                var item = target.GetItemById(AbilityId.item_hood_of_defiance);
                if (item != null)
                {
                    value -= item.GetAbilitySpecialData("barrier_block");
                }
            }

            // Modifier Pipe Barrier
            if (target.HasModifier("modifier_item_pipe_barrier"))
            {
                var pipehero = heroes.FirstOrDefault(x => x.IsEnemy(Owner) && x.Inventory.Items.Any(v => v.Id == AbilityId.item_pipe));
                if (pipehero != null)
                {
                    var ability = pipehero.GetItemById(AbilityId.item_pipe);

                    value -= ability.GetAbilitySpecialData("barrier_block");
                }
            }

            // Modifier Infused Raindrop
            if (target.HasModifier("modifier_item_infused_raindrop"))
            {
                var item = target.GetItemById(AbilityId.item_infused_raindrop);
                if (item != null && item.Cooldown <= 0)
                {
                    value -= item.GetAbilitySpecialData("magic_damage_block");
                }
            }

            // Modifier Abaddon Aphotic Shield
            if (target.HasModifier("modifier_abaddon_aphotic_shield"))
            {
                var abaddon = heroes.FirstOrDefault(x => x.IsEnemy(Owner) && x.HeroId == HeroId.npc_dota_hero_abaddon);
                var ability = abaddon.GetAbilityById(AbilityId.abaddon_aphotic_shield);

                value -= ability.GetAbilitySpecialData("damage_absorb");

                var talent = abaddon.GetAbilityById(AbilityId.special_bonus_unique_abaddon);
                if (talent != null && talent.Level > 0)
                {
                    value -= talent.GetAbilitySpecialData("value");
                }
            }

            // Modifier Ember Spirit Flame Guard
            if (target.HasModifier("modifier_ember_spirit_flame_guard"))
            {
                var ability = target.GetAbilityById(AbilityId.ember_spirit_flame_guard);
                if (ability != null)
                {
                    value -= ability.GetAbilitySpecialData("absorb_amount");

                    var emberSpirit = ability.Owner as Hero;
                    var talent = emberSpirit.GetAbilityById(AbilityId.special_bonus_unique_ember_spirit_1);
                    if (talent != null && talent.Level > 0)
                    {
                        value -= talent.GetAbilitySpecialData("value");
                    }
                }
            }

            return value;
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
                    var pulseSkill = _abilities.Pulse;
                    if (pulseSkill.Ability.Level > 0)
                    {
                        abilities.Add(pulseSkill);
                    }
                    // Dagon
                    var dagon = _abilities.Dagon;
                    if (dagon != null && dagon.Ability.IsValid)
                    {
                        abilities.Add(dagon);
                    }

                    // ult
                    var ultSkill = _abilities.Scythe;
                    if (ultSkill.Ability.Level > 0)
                    {
                        abilities.Add(ultSkill);
                    }

                    var auraSkill = _abilities.HeartAura;
                    if (auraSkill.Ability.Level > 0)
                    {
                        damageByAura = auraSkill.GetDamage(target, 1);
                    }

                    if (canAttack)
                    {
                        damageByAttack = Owner.GetAttackDamage(target, true);
                    }

                    if (ultSkill.CanBeCasted && ultSkill.CanHit(target))
                    {
                        extendUltDamage = (damageByAttack + damageByAura) * ultSkill.DamagePerHealth;
                    }


                }

                var damageCalculation = new Combo(abilities.ToArray());
                var damageReduction = -DamageReduction(target, heroes);
                var damageBlock = DamageBlock(target, heroes);
                //没有计算来自装备的伤害加深
                damageOfMisc = damageByAttack + damageByAura + DamageHelpers.GetSpellDamage(extendUltDamage,Owner.GetSpellAmplification(), damageReduction);
                //Console.WriteLine($"{damageByAttack}/{damageByAura}/{extendUltDamage}/{DamageHelpers.GetSpellDamage(extendUltDamage, Owner.GetSpellAmplification(), damageReduction)}");
                var livingArmor = LivingArmor(target, heroes, damageCalculation.Abilities);
                //目前就能打死的
                var damage = DamageHelpers.GetSpellDamage((damageCalculation.GetDamage(target) + damageOfMisc) + damageBlock, 0, damageReduction) - livingArmor;
                //上去就能干死的
                var readyDamage = DamageHelpers.GetSpellDamage(damageCalculation.GetDamage(target, true, false) + damageOfMisc+damageBlock, 0, damageReduction) - livingArmor;
                //所有状态完美的总体伤害
                var totalDamage = DamageHelpers.GetSpellDamage(damageCalculation.GetDamage(target, false, false) + damageBlock, 0, damageReduction) - livingArmor;

                if (target.IsInvulnerable() || target.HasAnyModifiers(BlockModifiers))
                {
                    damage = 0.0f;
                    readyDamage = 0.0f;
                }
                //Console.WriteLine($"{damage}/{readyDamage}/{totalDamage}");
                DamageList.Add(new Damage(target, damage, readyDamage, totalDamage, target.Health));
            }
        }
    }
}