namespace TelegramBot.Data.Entities;

public class AppUser
{
    public long Id { get; set; }
    public long chatId { get; set; }
    public string Username { get; set; } = string.Empty;
}