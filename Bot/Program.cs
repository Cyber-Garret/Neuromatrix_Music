using Bot.Models;
using Bot.Services;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

using Nett;

using System;
using System.IO;
using System.Threading.Tasks;
using Victoria;

namespace Bot
{
	class Program
	{
		private const string userPath = "UserData";
		private const string fileName = "config.toml";

		private IServiceProvider service;
		private static BotSettings config;

		static void Main()
		{
			config = GetConfiguration();

			Console.Title = $"{config.BotName} Discord bot (Library Discord.NET v{DiscordConfig.Version})";
			try
			{
				new Program().StartAsync().GetAwaiter().GetResult();
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Press any key to exit...");
				Console.ResetColor();
				Console.ReadLine();
			}
		}
		private async Task StartAsync()
		{
			service = BuildServices();

			var bot = service.GetRequiredService<DiscordShardedClient>();
			bot.Log += Logger.Log;

			service.GetRequiredService<DiscordEventHandlerService>().Configure();
			await service.GetRequiredService<CommandHandlerService>().ConfigureAsync();

			await bot.LoginAsync(TokenType.Bot, config.DiscordSettings.BotToken);
			await bot.StartAsync();
			await bot.SetStatusAsync(UserStatus.Online);

			await Task.Delay(-1);
		}

		public ServiceProvider BuildServices()
		{
			return new ServiceCollection()
				.AddSingleton(new DiscordShardedClient(config.DiscordSettings.ShardIds, new DiscordSocketConfig
				{
					AlwaysDownloadUsers = true,
					LogLevel = LogSeverity.Verbose,
					DefaultRetryMode = RetryMode.AlwaysRetry,
					MessageCacheSize = 100,
					TotalShards = config.DiscordSettings.ShardIds.Length
				}))
				.AddSingleton<CommandService>()
				.AddSingleton<CommandHandlerService>()
				.AddSingleton<DiscordEventHandlerService>()
				.AddSingleton<LavaRestClient>()
				.AddSingleton<LavaShardClient>()
				.AddSingleton<MusicService>()
				.BuildServiceProvider();
		}

		private static BotSettings GetConfiguration()
		{
			try
			{
				return Toml.ReadFile<BotSettings>(Path.Combine(Directory.GetCurrentDirectory(), userPath, fileName));
			}
			catch
			{
				var initializeConfig = new BotSettings();
				Toml.WriteFile(initializeConfig, Path.Combine(userPath, fileName));
				return Toml.ReadFile<BotSettings>(Path.Combine(Directory.GetCurrentDirectory(), userPath, fileName));
			}
		}
	}
}
