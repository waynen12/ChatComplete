namespace ChatCompletion.Config
{
    public static class SettingsProvider
    {
        public static ChatCompleteSettings Settings { get; private set; }

        public static void Initialize(ChatCompleteSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
    }
}
