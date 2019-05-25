using Ensage;
using Ensage.SDK.Abilities;

namespace wtf.tinker.Models
{
    public class Damage
    {
        public Hero GetTarget { get; }

        public float GetDamage { get; }

        public float GetReadyDamage { get; }

        public float GetTotalDamage { get; }

        public uint GetHealth { get; }

        public BaseAbility[] ComboAbility { get; }

        public Damage(Hero target, float damage, float readyDamage, float totalDamage, uint health, BaseAbility[] comboAbility)
        {
            GetTarget = target;
            GetDamage = damage;
            GetReadyDamage = readyDamage;
            GetTotalDamage = totalDamage;
            GetHealth = health;
            ComboAbility = comboAbility;
        }
    }
}
