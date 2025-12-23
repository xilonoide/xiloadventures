using System;
using System.Threading.Tasks;

namespace XiloAdventures.Engine.Interfaces;

/// <summary>
/// Interface for managing game audio (music and sound effects).
/// </summary>
public interface ISoundManager : IDisposable
{
    /// <summary>
    /// Gets or sets whether sound is enabled.
    /// </summary>
    bool SoundEnabled { get; set; }

    /// <summary>
    /// Gets or sets the music volume (0.0 to 1.0).
    /// </summary>
    double MusicVolume { get; set; }

    /// <summary>
    /// Gets or sets the sound effects volume (0.0 to 1.0).
    /// </summary>
    double FxVolume { get; set; }

    /// <summary>
    /// Plays background music from a Base64-encoded string.
    /// </summary>
    /// <param name="base64Music">The music data in Base64 format.</param>
    /// <param name="loop">Whether to loop the music.</param>
    /// <returns>Task representing the async operation.</returns>
    Task PlayMusicFromBase64Async(string base64Music, bool loop = true);

    /// <summary>
    /// Plays a sound effect from a Base64-encoded string.
    /// </summary>
    /// <param name="base64Fx">The sound effect data in Base64 format.</param>
    /// <returns>Task representing the async operation.</returns>
    Task PlayFxFromBase64Async(string base64Fx);

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    void StopMusic();

    /// <summary>
    /// Stops all currently playing sound effects.
    /// </summary>
    void StopAllFx();

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    void PauseMusic();

    /// <summary>
    /// Resumes the paused music.
    /// </summary>
    void ResumeMusic();

    /// <summary>
    /// Gets the ID of the currently playing music.
    /// </summary>
    string? CurrentMusicId { get; }

    /// <summary>
    /// Sets the current music ID without playing it.
    /// </summary>
    /// <param name="musicId">The music ID to set.</param>
    void SetCurrentMusicId(string? musicId);
}
