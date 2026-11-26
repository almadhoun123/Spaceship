namespace Spaceship
{
    /// <summary>Which half of the touch wall a player owns.</summary>
    public enum PlayerSide { Left = 0, Right = 1 }

    public enum WeaponType { Laser, MachineGun, AreaDestruction }

    /// <summary>The five predefined ship positions on the rail (GDD 2.4).</summary>
    public enum RailSlot { Center, Up, Down, Left, Right }
}
