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
}
