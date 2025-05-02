namespace SimpleCompare.Models;

/// <summary>
/// Represents the different types of item bonuses available in the game.
/// </summary>
internal enum ItemBonusType : uint
{
    STRENGTH = 1,
    DEXTERITY = 2,
    VITALITY = 3,
    INTELLIGENCE = 4,
    MIND = 5,
    PIETY = 6,
    GP = 10,
    CP = 11,
    TENACITY = 19,
    DIRECT_HIT_RATE = 22,
    CRITICAL_HIT = 27,
    DETERMINATION = 44,
    SKILL_SPEED = 45,
    SPELL_SPEED = 46,
    CRAFTSMANSHIP = 70,
    CONTROL = 71,
    GATHERING = 72,
    PERCEPTION = 73,

    DEFENSE = 21,
    MAGIC_DEFENSE = 24,

    BLOCK_STRENGTH = 17,
    BLOCK_RATE = 18,

    PHYSICAL_DAMAGE = 12,
    MAGIC_DAMAGE = 13
}