using System;
using System.ComponentModel.Composition;
using Ensage.Common;
using Ensage.SDK.Renderer;
using Ensage.SDK.Service;
using SharpDX;
using wtf.lion.Models;
using Color = System.Drawing.Color;


namespace wtf.lion.Parts
{
    [Export("render")]
    class Render
    {

        [Import("satellite")]
        private Satellite _satellite;
        [Import("menu")]
        private Menu _menu;

        private readonly IRendererManager renderer;


        [ImportingConstructor]
        public Render([Import] IServiceContext context)
        {
            //Owner = context.Owner as Hero; ;
            renderer = context.Renderer;
        }


        public void Install()
        {
            renderer.Draw += OnDraw;
        }

        public void Uninstall()
        {
            renderer.Draw -= OnDraw;
        }
        private void Text(string text, Vector2 pos, System.Drawing.Color color)
        {
            renderer.DrawText(pos, text, color,13f, "Arial");
        }

        private void showHpBar()
        {
            Damage[] damageList=new Damage[_satellite.DamageList.Count];
            _satellite.DamageList.CopyTo(damageList);
            foreach (var data in damageList)
            {
                var target = data.GetTarget;
                var hpBarPosition = HUDInfo.GetHPbarPosition(target);
                if (!hpBarPosition.IsZero)
                {
                    var hpBarSizeX = HUDInfo.GetHPBarSizeX(target);
                    var hpBarSizeY = HUDInfo.GetHpBarSizeY(target);
                    var myHudSizeY = HUDInfo.GetHpBarSizeY(target)*0.5f;
                    var hpBarPos = hpBarPosition + new Vector2(1, hpBarSizeY *0.5f);

                    var health = data.GetHealth;
                    var readyDamage = data.GetReadyDamage;
                    var readyDamageBar = Math.Max(readyDamage, 0) / target.MaximumHealth;

                    if (readyDamageBar > 0)
                    {
                        //剩余和最大生命值的比例
                        var readyDamagePos = Math.Max(health - readyDamage, 0) / target.MaximumHealth;
                        //计算剩余血量终点的位置，刚好是预计伤害的起点
                        var readyDamagePosition = new Vector2(hpBarPos.X + hpBarSizeX * readyDamagePos, hpBarPos.Y);
                        //计算预计伤害的长度hpBarSizeX*readyDamageBar，防止溢出,要减去溢出的伤害血量
                        //这是一种取巧的写法，普通应该是判断溢出直接等同于当前血量的终点
                        var readyDamageSize = new Vector2(hpBarSizeX * (readyDamageBar + Math.Min(health - readyDamage, 0) / target.MaximumHealth), myHudSizeY);
                        var readyDamageColor = (readyDamage-health) < 0 ? Color.FromArgb(200, 100, 0,0 ) : Color.FromArgb(200,191, 255, 0);
                        var rect = new RectangleF(readyDamagePosition.X, readyDamagePosition.Y, readyDamageSize.X,
                            readyDamageSize.Y);
                        renderer.DrawFilledRectangle(rect, Color.Black,readyDamageColor,1f);
                    }

                    var damage = data.GetDamage;
                    var damageBar = Math.Max(damage, 0) / target.MaximumHealth;
                    if (damageBar > 0)
                    {
                        var damagePos = Math.Max(health - damage, 0) / target.MaximumHealth;
                        var damagePosition = new Vector2(hpBarPos.X + (hpBarSizeX * damagePos), hpBarPos.Y);
                        var damageSize = new Vector2(hpBarSizeX * (damageBar + Math.Min(health - damage, 0) / target.MaximumHealth), myHudSizeY);
                        var damageColor = (damage - health) <0? Color.FromArgb(255,0, 255, 0) : Color.Aqua;
                        var rect = new RectangleF(damagePosition.X, damagePosition.Y, damageSize.X,
                            damageSize.Y);
                        renderer.DrawFilledRectangle(rect,Color.Black, damageColor,1f);
                    }
                }
            }
        }

        private void OnDraw(object sender, EventArgs eventArgs)
        {
            if (_menu.IsShowHpBarEnabled)
            {
                showHpBar();
            }

        }
    }
}
