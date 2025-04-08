using System.ComponentModel;

namespace SimpleCompare
{
    internal enum ItemBonusType : byte
    {
        [Description("Strength")]
        STRENGTH = 1,
        [Description("Dexterity")]
        DEXTERITY = 2,
        [Description("Vitality")]
        VITALITY = 3,
        [Description("Intelligence")]
        INTELLIGENCE = 4,
        [Description("Mind")]
        MIND = 5,
        [Description("Piety")]
        PIETY = 6,
        [Description("GP")]
        GP = 10,
        [Description("CP")]
        CP = 11,
        [Description("Tenacity")]
        TENACITY = 19,
        [Description("Direct Hit Rate")]
        DIRECT_HIT_RATE = 22,
        [Description("Critical Hit")]
        CRITICAL_HIT = 27,
        [Description("Determination")]
        DETERMINATION = 44,
        [Description("Skill Speed")]
        SKILL_SPEED = 45,
        [Description("Spell Speed")]
        SPELL_SPEED = 46,
        [Description("Craftsmanship")]
        CRAFTSMANSHIP = 70,
        [Description("Control")]
        CONTROL = 71,
        [Description("Gathering")]
        GATHERING = 72,
        [Description("Perception")]
        PERCEPTION = 73,

        [Description("Defense")]
        DEFENSE = 21,
        [Description("Magic Defense")]
        MAGIC_DEFENSE = 24,

        [Description("Block Strength")]
        BLOCK_STRENGTH = 17,
        [Description("Block Rate")]
        BLOCK_RATE = 18,

        [Description("Physical Damage")]
        PHYSICAL_DAMAGE = 12,
        [Description("Magic Damage")]
        MAGIC_DAMAGE = 13,
    }
}
