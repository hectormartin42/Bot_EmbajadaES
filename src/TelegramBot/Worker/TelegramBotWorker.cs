using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot.Data;
using TelegramBot.Data.Entities;

namespace TelegramBot.Worker;

public class TelegramBotWorker : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TelegramBotWorker> _logger;
    private readonly AppDbContext _dbContext;
    private readonly TelegramBotClient _botClient;
    private readonly ReceiverOptions receiverOptions;

    public TelegramBotWorker(ILogger<TelegramBotWorker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _dbContext = new AppDbContext();
        _botClient = new TelegramBotClient("6726876996:AAFCCW0DhR_77k8OE0hieC_ta5ofbVWwAR4");

        // StartReceiving does not block the caller thread. Receiving is done on the ThreadPool.
        receiverOptions = new ReceiverOptions()
        {
            // receive all update types except ChatMember related updates
            AllowedUpdates = Array.Empty<UpdateType>()
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            pollingErrorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        var me = await _botClient.GetMeAsync();
        _logger.LogInformation("Bot {username} iniciado correctamente !", me.Username);

        // await _redisSubscriber.SubscribeAsync(RedisChannel, (channel, message) => {
        //     _logger.LogInformation("Mensaje desde redis: {channel} {message}", channel, message);
        //     var appUsers = _dbContext.AppUsers.ToArray();

        //     Array.ForEach(appUsers, async u => {
        //         await SendTextMessage(u.chatId, message, stoppingToken);
        //     });
        // });

        while(!stoppingToken.IsCancellationRequested) {
            await CheckDbAndSendMessages(stoppingToken);
        }
    }

    async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        // Only process Message updates: https://core.telegram.org/bots/api#message
        if (update.Message is not { } message)
            return;
        // Only process text messages
        if (message.Text is not { } messageText)
            return;

        // Check if have from data to get userId & Username
        var from = message.From;
        if (from == null)
            return;

        // Whe don't need bot interaction here :)
        if (from.IsBot)
            return;

        var chatId = message.Chat.Id;
        var userId = from.Id;
        var userName = from.Username;

        if (messageText.ToLower() == "/activame")
        {
            bool userExist = _dbContext.AppUsers.Any(a => a.Id == userId);

            if (userExist)
            {
                AppUser user = _dbContext.AppUsers.Single(a => a.Id == userId);
                user.Id = userId;
                user.chatId = chatId;
                user.Username = userName ?? "";
                _dbContext.AppUsers.Update(user);
            }
            else
            {
                _dbContext.AppUsers.Add(
                    new AppUser { Id = userId, chatId = chatId, Username = userName ?? "" }
                );
            }

            await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
        }

        if (messageText.ToLower() == "/desactivame")
        {
            bool userExist = _dbContext.AppUsers.Any(a => a.Id == userId);

            if (userExist)
            {
                AppUser user = _dbContext.AppUsers.Single(a => a.Id == userId);
                _dbContext.AppUsers.Remove(user);
                await _dbContext.SaveChangesAsync(cancellationToken: cancellationToken);
            }
        }

        if (messageText.ToLower() == "/estado")
        {
            bool userExist = _dbContext.AppUsers.Any(a => a.Id == userId);

            if (userExist)
            {
                await SendTextMessage(chatId, "Usuario actualmente <b>activo</b>.", cancellationToken);
            }
            else
            {
                await SendTextMessage(chatId, "Usuario actualmente <b>inactivo</b>.", cancellationToken);
            }
        }

        _logger.LogInformation("Mensaje '{message}' en chat '{chatId}' recibido del usuario '{username}'", messageText, chatId, userName);
    }

    Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"[Log] Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        Console.WriteLine(ErrorMessage);
        return Task.CompletedTask;
    }

    async Task CheckDbAndSendMessages(CancellationToken stoppingToken) {
        var appUsers = _dbContext.AppUsers.ToArray();
        var appMessages = _dbContext.AppMessages.ToArray();

        Array.ForEach(appUsers, u =>
        {
            Array.ForEach(appMessages, async m =>
            {
                await SendTextMessage(u.chatId, m.Message, stoppingToken);
            });
        });

        // Remove all messages
        _dbContext.AppMessages.RemoveRange(appMessages);
        await _dbContext.SaveChangesAsync(stoppingToken);

        await Task.Delay(2 * 1000, stoppingToken);
    }

    async Task SendTextMessage(ChatId chatId, string messageText, CancellationToken cancellationToken)
    {
        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText,
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken
        );
    }
}
