using Godot;

public interface IPersistable
{
    /// <summary>
    /// Saves the current state of the scene.
    /// </summary>
    /// <returns>A Resource containing the saved data.</returns>
    SaveResource Save(SaveResource newSave);

    /// <summary>
    /// Loads the state of the scene from the provided Resource.
    /// </summary>
    /// <param name="data">The Resource containing the saved data.</param>
    void Load(SaveResource data);


    public int GetSaveVersion()
    {
        // Default implementation returns 0, can be overridden by implementing classes
        return 0;
    }

    /// <summary>
    /// Gets the load priority of the scene. Higher values indicate higher priority. i.e priority 10 will load before priority 5.
    /// </summary>
    public int GetLoadPriority()
    {
        // Default implementation returns 0, can be overridden by implementing classes
        return 0;
    }
}
