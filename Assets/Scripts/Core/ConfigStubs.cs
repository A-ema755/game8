using UnityEngine;

namespace GeneForge.Core
{
    // STUB: Replaced when body-part-system.md is implemented
    public class BodyPartConfig : ConfigBase
    {
        [SerializeField] DamageForm formAccess;

        /// <summary>Damage form this part grants access to.</summary>
        public DamageForm FormAccess => formAccess;
    }

    // STUB: Replaced when status-effect-system.md is implemented
    public class StatusEffectConfig : ConfigBase { }

    // STUB: Replaced when encounter-system.md is implemented
    public class EncounterConfig : ConfigBase { }

    // STUB: Replaced when ai-decision-system.md is implemented
    public class AIPersonalityConfig : ConfigBase { }
}
