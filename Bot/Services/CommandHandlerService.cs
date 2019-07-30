using Discord;
using Discord.Commands;
using Discord.WebSocket;

using System;
using System.Reflection;
using System.Threading.Tasks;

namespace Bot.Services
{
	internal class CommandHandlerService
	{
		private CommandService command;
		private readonly IServiceProvider service;
		private readonly DiscordShardedClient discord;

		public CommandHandlerService(CommandService commandService, IServiceProvider serviceProvider, DiscordShardedClient shardedClient)
		{
			command = commandService;
			service = serviceProvider;
			discord = shardedClient;
		}
		public async Task ConfigureAsync()
		{
			command = new CommandService(new CommandServiceConfig
			{
				DefaultRunMode = RunMode.Async,
				CaseSensitiveCommands = false
			});

			await command.AddModulesAsync(Assembly.GetEntryAssembly(), service);
		}

		public async Task HandleCommandAsync(SocketMessage arg)
		{
			if (!(arg is SocketUserMessage msg)) return;

			var context = new ShardedCommandContext(discord, msg);

			var argPos = 0;
			// Ignore if not mention this bot or command not start from char ! REMARK: This audio bot allow commands only on Mention.
			if (!msg.HasMentionPrefix(discord.CurrentUser, ref argPos)) return;
			{

				var cmdSearchResult = command.Search(context, argPos);
				if (cmdSearchResult.Commands == null)
				{
					await Logger.Log(new LogMessage(LogSeverity.Warning, "HandleCommand", $"Command {msg.Content} return {cmdSearchResult.Error}"));
					return;
				}

				var executionTask = command.ExecuteAsync(context, argPos, service);

				await executionTask.ContinueWith(task =>
				{
					if (task.Result.IsSuccess || task.Result.Error == CommandError.UnknownCommand) return;
					const string errTemplate = "{0}, {1}.";
					var errMessage = string.Format(errTemplate, context.User.Mention, task.Result.ErrorReason);
					context.Channel.SendMessageAsync(errMessage);
				});
			}
		}
	}
}
