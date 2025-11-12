namespace TD.Systems.Ticking
{
    /// <summary>
    /// Lightweight interface for systems that need per-frame ticking without owning Update methods.
    /// </summary>
    public interface ITickable
    {
        void Tick(float deltaTime);
    }
}
