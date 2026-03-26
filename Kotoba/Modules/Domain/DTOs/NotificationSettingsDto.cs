namespace Kotoba.Modules.Domain.DTOs
{
    public class NotificationSettingsDto
    {
        public bool MasterEnabled { get; set; } = true;
        public bool DirectMessages { get; set; } = true;
        public bool GroupMessages { get; set; } = true;
        public bool Mentions { get; set; } = true;
        public bool SomeoneOnline { get; set; } = false;
        public bool SoundEnabled { get; set; } = true;
        public int Volume { get; set; } = 70;
        public bool QuietHoursEnabled { get; set; } = false;
        public string QuietFrom { get; set; } = "22:00";
        public string QuietTo { get; set; } = "08:00";
        public bool ShowPreview { get; set; } = true;
        public bool ShowSender { get; set; } = true;

        public NotificationSettingsDto Clone() =>
            (NotificationSettingsDto)MemberwiseClone();
    }
}
