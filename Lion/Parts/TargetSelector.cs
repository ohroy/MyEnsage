using System.ComponentModel.Composition;
using System.Linq;
using Ensage;
using Ensage.SDK.Service;
using Ensage.SDK.TargetSelector;

namespace wtf.lion.Parts
{
    [Export("target_selector")]
    class TargetSelector
    {
        public Hero Target { get; set; }
        private ITargetSelector targetSelector;
        [ImportingConstructor]
        public TargetSelector([Import] IServiceContext context)
        {
            //_owner = context.Owner as Hero; ;
            targetSelector = context.TargetSelector;
            if (!targetSelector.IsActive)
            {
                targetSelector.Activate();
            }
        }

        public Hero SelectTarget()
        {

            Target = targetSelector.GetTargets().FirstOrDefault() as Hero;
            return Target;
        }
    }
}
