using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ensage;
using Ensage.Common;
using Ensage.Common.Enums;
using Ensage.Common.Extensions;
using Ensage.Common.Menu;
using Ensage.Common.Threading;
using Ensage.SDK.Helpers;
using Ensage.SDK.Logger;
using Ensage.SDK.Renderer.Particle;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;

using SharpDX;
using wtf.Models;
using wtf.Parts;
using AbilityId = Ensage.AbilityId;
using Menu = Ensage.Common.Menu.Menu;
using ParticleManager = wtf.Parts.ParticleManager;
using UnitExtensions = Ensage.SDK.Extensions.UnitExtensions;

namespace wtf
{
    [ExportPlugin(name: "wtf.nec", author: "rozbo", version: "1.2.0.1", units: HeroId.npc_dota_hero_necrolyte)]
    internal class Necrophos : Plugin
    {

        private IServiceContext serviceContext;

        private Hero Owner { get; }
        [ImportingConstructor]
        public Necrophos([Import] IEntityContext<Unit> entityContext, [Import] IServiceContext context)
        {
            Owner = entityContext.Owner as Hero;
            serviceContext = context;
        }

        [Import("menu")]
        private wtf.Parts.Menu _menu;

        
        [Import("abilities")]
        private Abilities _abilities;


        [Import("satellite")]
        private Satellite _satellite;

        [Import("render")]
        private Render _render;


        [Import("autokillsteal")]
        private AutoKillSteal _autoKillSteal;


        [Import("particle_manager")]
        private ParticleManager _particleManager;


        protected override void OnActivate()
        {
            _menu.install();
            _abilities.install();
            _satellite.Install();
            _render.Install();
            _autoKillSteal.Install();
            _particleManager.Install();
        }

        protected override void OnDeactivate()
        {
            _menu?.uninstall();
            _abilities?.uninstall();
            _satellite?.Uninstall();
            _render?.Uninstall();
            _autoKillSteal?.Uninstall();
            _particleManager?.Uninstall();
        }

    }
}
