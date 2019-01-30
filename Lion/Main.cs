using System.ComponentModel.Composition;
using Ensage;
using Ensage.SDK.Service;
using Ensage.SDK.Service.Metadata;
using wtf.lion.Models;
using wtf.lion.Parts;
using ParticleManager = wtf.lion.Parts.ParticleManager;

namespace wtf.lion
{
    [ExportPlugin(name: "wtf.lion", author: "rozbo", version: "1.4.3.0", units: HeroId.npc_dota_hero_lion)]
    internal class Lion : Plugin
    {

        private IServiceContext serviceContext;

        private Hero Owner { get; }
        [ImportingConstructor]
        public Lion([Import] IEntityContext<Unit> entityContext, [Import] IServiceContext context)
        {
            Owner = entityContext.Owner as Hero;
            serviceContext = context;
        }

        [Import("menu")]
        private Menu _menu;

        
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

        [Import("orb_mode")]
        private OrbwalkingMode _orbwalkingMode;


        protected override void OnActivate()
        {
            _menu.install();
            _abilities.install();
            _satellite.Install();
            _render.Install();
            _autoKillSteal.Install();
            _particleManager.Install();
            _orbwalkingMode.Install();;
        }

        protected override void OnDeactivate()
        {
            _menu?.uninstall();
            _abilities?.uninstall();
            _satellite?.Uninstall();
            _render?.Uninstall();
            _autoKillSteal?.Uninstall();
            _particleManager?.Uninstall();
            _orbwalkingMode?.Uninstall();
        }

    }
}
