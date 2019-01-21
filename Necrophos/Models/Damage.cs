using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;

namespace wtf.Models
{
    public class Damage
    {
        public Hero GetTarget { get; }

        public float GetDamage { get; }

        public float GetReadyDamage { get; }

        public float GetTotalDamage { get; }

        public uint GetHealth { get; }

        public Damage(Hero target, float damage, float readyDamage, float totalDamage, uint health)
        {
            GetTarget = target;
            GetDamage = damage;
            GetReadyDamage = readyDamage;
            GetTotalDamage = totalDamage;
            GetHealth = health;
        }
    }
}
