using System.ComponentModel;
using System.ComponentModel.Composition;
using Ensage;
using Ensage.SDK.Helpers;
using Ensage.SDK.Renderer.Particle;
using Ensage.SDK.Service;
using SharpDX;
using wtf.lion.Models;

namespace wtf.lion.Parts
{
    [Export("particle_manager")]
    class ParticleManager
    {
        private Hero _owner;

        [Import("menu")]
        private Menu _menu;
        [Import("abilities")]
        private Abilities _abilities;


        [Import("target_selector")]
        private TargetSelector _targetSelector;

        private IParticleManager _particleManager;


        [ImportingConstructor]
        public ParticleManager([Import] IServiceContext context)
        {
            _owner = context.Owner as Hero;
            _particleManager = context.Particle;
        }



        public void Install()
        {
            _menu.DrawEnabled.PropertyChanged += isDrawChanged;
            //try to active first time!
            isDrawChanged(null, null);

        }




        private void onUpdate()
        {
            if (_menu.DrawUltEnabled && _abilities.Skill4.Ability?.Level > 0)
            {
                _particleManager.DrawRange(_owner, "ult_range", _abilities.Skill4.CastRange, Color.Red);
            }
            else
            {
                _particleManager.Remove("ult_range");
            }

            if (_menu.DrawBlinkEnabled && _abilities.Blink!=null)
            {
                _particleManager.DrawRange(_owner, "blink_range", _abilities.Blink.CastRange, Color.Gray);
            }
            else
            {
                _particleManager.Remove("blink_range");
            }

            if (_menu.DrawSkill1Enabled 
                && _abilities.Skill1.Ability?.Level>0
                )
            {
                _particleManager.DrawRange(_owner, "skill1_range", _abilities.Skill1.CastRange, Color.Azure);
            }
            else
            {
                _particleManager.Remove("skill1_range");
            }

            if (_menu.DrawSkill2Enabled
                && _abilities.Skill2.Ability?.Level > 0
            )
            {
                _particleManager.DrawRange(_owner, "skill2_range", _abilities.Skill2.CastRange, Color.Beige);
            }
            else
            {
                _particleManager.Remove("skill2_range");
            }

            if (_menu.DrawSkill3Enabled
                && _abilities.Skill3.Ability?.Level > 0
            )
            {
                _particleManager.DrawRange(_owner, "skill3_range", _abilities.Skill3.CastRange, Color.CadetBlue);
            }
            else
            {
                _particleManager.Remove("skill3_range");
            }

            var target = _targetSelector.SelectTarget();
            if (_menu.DrawTargetEnabled && target != null)
            {
                _particleManager.DrawTargetLine(
                    _owner,
                    "target_line",
                    target.Position,
                    _menu.ComboKeyItem
                        ? Color.DarkRed
                        : Color.Gray);
            }
            else
            {
                _particleManager.Remove("target_line");
            }
        }


        private void isDrawChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            if (_menu.DrawEnabled)
            {
                UpdateManager.Subscribe(onUpdate, 25);
            }
            else
            {
                UpdateManager.Unsubscribe(onUpdate);
                freeParticle();
            }
        }

        private void freeParticle()
        {
            _particleManager.Remove("ult_range");
            _particleManager.Remove("blink_range");
            _particleManager.Remove("skill1_range");
            _particleManager.Remove("skill2_range");
            _particleManager.Remove("skill3_range");
            _particleManager.Remove("target_line");
        }



        public void Uninstall()
        {
            _menu.DrawEnabled.PropertyChanged -= isDrawChanged;
            UpdateManager.Unsubscribe(onUpdate);
            freeParticle();
        }
    }
}
