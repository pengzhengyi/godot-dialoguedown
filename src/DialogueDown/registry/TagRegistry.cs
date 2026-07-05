using DialogueDown;

internal sealed class TagRegistry : ITagRegistry<Tag>
{
    private readonly Dictionary<string, Tag> _idToTag = new();

    private int _nextInternalId = 1;

    private TagRegistry()
    {

    }

    public Tag GetOrCreate(string id, string name)
    {
        if (TryGet(id, out var stored))
        {
            return stored;
        }
        else
        {
            var tag = new Tag(id, name, _nextInternalId++);
            _idToTag.Add(id, tag);
            return tag;
        }
    }

    public bool TryGet(string id, out Tag stored) => _idToTag.TryGetValue(id, out stored!);

    public bool TryStore(string id, Tag toStore)
    {
        if (_idToTag.ContainsKey(id))
        {
            return false;
        }
        else
        {
            _idToTag.Add(id, toStore);
            return true;
        }
    }
}
