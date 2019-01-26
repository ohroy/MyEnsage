using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using Ensage;
using Ensage.Common.Objects.UtilityObjects;
using Ensage.SDK.Abilities;
using Ensage.SDK.Abilities.Components;
using Ensage.SDK.Extensions;
using Ensage.SDK.Helpers;
using Ensage.SDK.Service;
using wtf.lion.Models;

namespace wtf.lion
{
    [Export("helper")]
    public class Helper
    {
        public MultiSleeper MultiSleeper { get; }
        private Hero Owner;
        [Import("abilities")]
        private Abilities _abilities;
        [ImportingConstructor]
        public Helper([Import] IServiceContext context)
        {
            MultiSleeper=new MultiSleeper();
            Owner = context.Owner as Hero;
        }

        public bool DuelAghanimsScepter(Hero target)
        {
            var duelAghanimsScepter = false;
            if (target.HasModifier("modifier_legion_commander_duel"))
            {
                duelAghanimsScepter = EntityManager<Hero>.Entities.Any(x =>
                    x.HeroId == HeroId.npc_dota_hero_legion_commander &&
                    x.IsValid &&
                    x.IsVisible &&
                    x.IsAlive &&
                    x.HasAghanimsScepter());
            }

            return duelAghanimsScepter;
        }

        public bool ComboBreaker(Hero target, bool menu = true)
        {
            var comboBreaker = target.GetItemById(AbilityId.item_aeon_disk);
            if (comboBreaker != null && comboBreaker.Cooldown <= 0)
            {
                return true;
            }

            return false;
        }


        private string[] CancelModifiers { get; } =
        {
            "modifier_abaddon_borrowed_time",
            "modifier_item_combo_breaker_buff",
            "modifier_winter_wyvern_winters_curse_aura",
            "modifier_winter_wyvern_winters_curse",
            "modifier_oracle_fates_edict"
        };

        public bool CanCombo(Hero target)
        {
            return target!=null
                   &&!target.IsMagicImmune()
                   && !target.IsInvulnerable()
                   && !DuelAghanimsScepter(target)
                   && !target.HasAnyModifiers(CancelModifiers);
        }



        public int GetAbilityWaitTime(ActiveAbility ability,Hero target)
        {
            if (ability.CanBeCasted && ability.CanHit(target) && ability.UseAbility(target))
            {
                int delay;

                // wait for projectile to hit, so that the amplifier is actually applied for the rest of the combo
                if (ability is IHasDamageAmplifier && ability.Speed != float.MaxValue)
                {
                    delay = ability.GetHitTime(target);
                }
                else
                {
                    delay = ability.GetCastDelay(target);
                }

                var diff = (int)(50 - Game.Ping);
                if (diff > 0)
                {
                    delay += diff;
                }

                return delay;
            }

            return 0;
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


        public float LivingArmor(Hero target, List<Hero> heroes, IReadOnlyCollection<BaseAbility> abilities)
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


        private float DamageAmplifyByItem()
        {
            var ampByItem = 1f;
            var kaya = Owner.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_kaya") || x.Name.Contains("item_yasha_and_kaya") || x.Name.Contains("item_kaya_and_sange"));
            if (kaya != null)
            {
                var kayaAmp = (kaya.AbilitySpecialData.First(x => x.Name == "spell_amp").Value) / 100.0f;

                ampByItem *= kayaAmp;
            }
            return ampByItem;
        }

        public float DamageReCalc(float damage, Hero target, List<Hero> heroes, IReadOnlyCollection<BaseAbility> abilities)
        {

            if (target.IsInvulnerable() || target.HasAnyModifiers(BlockModifiers))
            {
                return 0;
            }
            var damageReduction = DamageReduction(target, heroes);
            var livingArmor = LivingArmor(target, heroes, abilities);
            damage = DamageBlock(target, heroes) + damage;
            var amp = DamageAmplifyByItem();
            return DamageHelpers.GetSpellDamage(damage, 0, damageReduction) - livingArmor;
        }




        public float DamageReduction(Hero target, List<Hero> heroes)
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

            return -value;
        }



        public float DamageBlock(Hero target, List<Hero> heroes)
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
    }
}
