using System.Media;

namespace MetroREX;

/// <summary>Every sound effect in the app. The file for each one is assets/sounds/&lt;name&gt;.wav.</summary>
internal enum Sound
{
    AddToCart,
    RemoveFromCart,
    OrderPlaced,
    Error,
    Welcome,
    Tick,
    StationChime,
    Roar,
}

/// <summary>
/// Plays the short sound effects. Missing files or a machine without audio never break the app: the
/// sound is simply skipped. Only one effect plays at a time (a new one replaces the previous).
/// </summary>
internal static class Sfx
{
    private static readonly Dictionary<Sound, SoundPlayer?> Players = new();

    /// <summary>When true nothing plays (toggled with Ctrl+M in the shop).</summary>
    public static bool Muted { get; set; }

    private static string FileName(Sound sound) => sound switch
    {
        Sound.AddToCart => "add-to-cart",
        Sound.RemoveFromCart => "remove-from-cart",
        Sound.OrderPlaced => "order-placed",
        Sound.Error => "error",
        Sound.Welcome => "welcome",
        Sound.Tick => "tick",
        Sound.StationChime => "station-chime",
        Sound.Roar => "roar",
        _ => throw new ArgumentOutOfRangeException(nameof(sound)),
    };

    public static void Play(Sound sound)
    {
        if (Muted)
        {
            return;
        }

        try
        {
            if (!Players.TryGetValue(sound, out var player))
            {
                var path = Path.Combine(AppContext.BaseDirectory, "assets", "sounds", FileName(sound) + ".wav");
                if (File.Exists(path))
                {
                    // Read into memory so the file is never locked while the app runs
                    player = new SoundPlayer(new MemoryStream(File.ReadAllBytes(path)));
                    player.Load();
                }

                Players[sound] = player;
            }

            player?.Play(); // asynchronous: does not block the UI
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException
                                              or UnauthorizedAccessException or TimeoutException
                                              or PlatformNotSupportedException)
        {
            Players[sound] = null; // do not retry a broken sound on every click
        }
    }
}
