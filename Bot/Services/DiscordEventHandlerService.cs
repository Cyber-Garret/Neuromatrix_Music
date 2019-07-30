using Discord;
using Discord.WebSocket;

using System;
using System.Threading.Tasks;
using Victoria;

namespace Bot.Services
{
	internal class DiscordEventHandlerService
	{
		private int loadedShards = 0;
		private readonly DiscordShardedClient discord;
		private readonly CommandHandlerService command;
		private readonly LavaShardClient lavaShard;
		private readonly MusicService music;
		public DiscordEventHandlerService(DiscordShardedClient shardedClient, CommandHandlerService commandService, LavaShardClient lavaShardClient, MusicService musicService)
		{
			discord = shardedClient;
			command = commandService;
			lavaShard = lavaShardClient;
			music = musicService;
		}

		internal void Configure()
		{
			discord.ShardReady += Discord_ShardReady;
			discord.ShardDisconnected += Discord_ShardDisconnected;
			discord.MessageReceived += Discord_MessageReceived;
		}

		private async Task Discord_ShardReady(DiscordSocketClient arg)
		{
			loadedShards++;
			if (loadedShards == discord.Shards.Count)
			{
				await Task.Delay(500);
				await lavaShard.StartAsync(discord);
				lavaShard.Log += Logger.Log;
				lavaShard.OnTrackFinished += music.OnTrackFinished;
				loadedShards = 0;
			}
		}

		private Task Discord_ShardDisconnected(Exception ex, DiscordSocketClient client)
		{
			Logger.Log(new LogMessage(LogSeverity.Warning, $"Shard {client.ShardId} Disconnected", ex.Message));
			return Task.CompletedTask;
		}
		private async Task Discord_MessageReceived(SocketMessage message)
		{
			//Ignore all bots
			if (message.Author.IsBot) return;
			await command.HandleCommandAsync(message);
		}
	}
}
