namespace DialogueDown;

internal interface ITagRegistry<TTag> : IRegistry<TTag>
where TTag : ITag
{
    public TTag GetOrCreate(string id, string name);
}
