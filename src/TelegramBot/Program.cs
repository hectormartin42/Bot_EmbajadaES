using TelegramBot.Worker;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<TelegramBotWorker>();
    })
    .Build();

host.Run();
