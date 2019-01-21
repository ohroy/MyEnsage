using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.Common;
using Ensage.SDK.Service;
using SharpDX;

namespace wtf.Parts
{
    [Export("render")]
    class Render
    {

        [Import("satellite")]
        private Satellite _satellite;
        [ImportingConstructor]
        public Render([Import] IServiceContext context)
        {
            //Owner = context.Owner as Hero; ;
        }


        public void Install()
        {
            Drawing.OnDraw += OnDraw;
        }

        public void Uninstall()
        {
            Drawing.OnDraw -= OnDraw;
        }
        private void Text(string text, Vector2 pos, Color color)
        {
            Drawing.DrawText(text, "Arial", pos, new Vector2(21), color, FontFlags.None);
        }
        private void OnDraw(EventArgs args)
        {
            foreach (var data in _satellite.DamageList)
            {
                var target = data.GetTarget;
                var hpBarPosition = HUDInfo.GetHPbarPosition(target);
                if (!hpBarPosition.IsZero)
                {
                    var hpBarSizeX = HUDInfo.GetHPBarSizeX(target);
                    var hpBarSizeY = HUDInfo.GetHpBarSizeY(target) / 1.7f;
                    var hpBarPos = hpBarPosition + new Vector2(0, hpBarSizeY * (84 / 70f));

                    var health = data.GetHealth;
                    var readyDamage = data.GetReadyDamage;
                    var readyDamageBar = Math.Max(readyDamage, 0) / target.MaximumHealth;
                    if (readyDamageBar > 0)
                    {
                        var readyDamagePos = Math.Max(health - readyDamage, 0) / target.MaximumHealth;
                        var readyDamagePosition = new Vector2(hpBarPos.X + ((hpBarSizeX + readyDamageBar) * readyDamagePos), hpBarPos.Y);
                        var readyDamageSize = new Vector2(hpBarSizeX * (readyDamageBar + Math.Min(health - readyDamage, 0) / target.MaximumHealth), hpBarSizeY);
                        var readyDamageColor = ((float)health / target.MaximumHealth) - readyDamageBar > 0 ? new Color(100, 0, 0, 200) : new Color(191, 255, 0, 200);

                        Drawing.DrawRect(readyDamagePosition, readyDamageSize, readyDamageColor);
                        Drawing.DrawRect(readyDamagePosition, readyDamageSize, Color.Black, true);
                    }

                    var damage = data.GetDamage;
                    var damageBar = Math.Max(damage, 0) / target.MaximumHealth;
                    if (damageBar > 0)
                    {
                        var damagePos = Math.Max(health - damage, 0) / target.MaximumHealth;
                        var damagePosition = new Vector2(hpBarPos.X + ((hpBarSizeX + damageBar) * damagePos), hpBarPos.Y);
                        var damageSize = new Vector2(hpBarSizeX * (damageBar + Math.Min(health - damage, 0) / target.MaximumHealth), hpBarSizeY);
                        var damageColor = ((float)health / target.MaximumHealth) - damageBar > 0 ? new Color(0, 255, 0) : Color.Aqua;

                        Drawing.DrawRect(damagePosition, damageSize, damageColor);
                        Drawing.DrawRect(damagePosition, damageSize, Color.Black, true);
                    }
                }
            }
        }
    }
}
