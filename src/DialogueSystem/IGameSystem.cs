namespace DialogueSystem;

/// <summary>
/// An adapter that is responsible handling the game invocations in dialogue.
/// This enables dialogue to query game state or execute commands.
/// </summary>
public interface IGameSystem
{
    public string Query(string query);

    public void Execute(string command);
}
