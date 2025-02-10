namespace Planetarium.Stats
{
    public interface ITaggable
    {
        void OnTagAdded(GameplayTag tag);
        void OnTagRemoved(GameplayTag tag);
    }
}
