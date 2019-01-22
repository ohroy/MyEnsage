using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ensage;
using Ensage.SDK.Helpers;
using Ensage.SDK.Renderer.Particle;
using Ensage.SDK.Service;
using SharpDX;
using wtf.Models;

namespace wtf.Parts
{
    [Export("particle_manager")]
    class ParticleManager
    {
        private Hero _owner;

        [Import("menu")]
        private Menu _menu;
        [Import("abilities")]
        private Abilities _abilities;
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
            if (_menu.DrawUltEnabled && _abilities.Scythe.Ability?.Level > 0)
            {
                _particleManager.DrawRange(_owner, "ult_range", _abilities.Scythe.CastRange, Color.Red);
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

            if (_menu.DrawPulseEnabled 
                && _abilities.Pulse.Ability?.Level>0
                )
            {
                _particleManager.DrawRange(_owner, "pulse_range", _abilities.Pulse.Radius, Color.Purple);
            }
            else
            {
                _particleManager.Remove("pulse_range");
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
            }
        }





        public void Uninstall()
        {
            _menu.DrawEnabled.PropertyChanged -= isDrawChanged;
        }
    }
}
