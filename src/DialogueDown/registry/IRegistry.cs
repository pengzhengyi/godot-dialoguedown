namespace DialogueDown;

internal interface IRegistry<T>
{
    public bool TryGet(string id, out T stored);

    public bool TryStore(string id, T toStore);
}
