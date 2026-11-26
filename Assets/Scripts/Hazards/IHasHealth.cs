/// <summary>Anything that can show a health bar: 1 = full health, 0 = destroyed.</summary>
public interface IHasHealth
{
    float HealthFraction { get; }
}
