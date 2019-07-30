using Bot.Helper;
using Bot.Models;

using Discord;
using Discord.WebSocket;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Victoria;
using Victoria.Entities;

namespace Bot.Services
{
	public class MusicService
	{
		private readonly LavaShardClient lavaShard;
		private readonly LavaRestClient lavaRest;
		private LavaPlayer lavaPlayer;

		public MusicService(LavaRestClient lavaRestClient, LavaShardClient lavaShardClient)
		{
			lavaShard = lavaShardClient;
			lavaRest = lavaRestClient;
		}

		private readonly Lazy<ConcurrentDictionary<ulong, MusicSettings>> LazySettings = new Lazy<ConcurrentDictionary<ulong, MusicSettings>>();

		private ConcurrentDictionary<ulong, MusicSettings> Options => LazySettings.Value;

		public async Task<Embed> JoinAsync(SocketGuildUser user, ulong guildId)
		{
			if (user.VoiceChannel == null)
				return await EmbedHelper.CreateErrorEmbed("Music Join", "You must first join a voice channel!");

			if (Options.TryGetValue(user.Guild.Id, out var options) && options.Master.Id != user.Id)
				return await EmbedHelper.CreateErrorEmbed("Music, Join", $"I can't join another voice channel until {options.Master} disconnects me.");
			try
			{
				lavaPlayer = lavaShard.GetPlayer(guildId);
				if (lavaPlayer == null)
				{

					await lavaShard.ConnectAsync(user.VoiceChannel);
					Options.TryAdd(user.Guild.Id, new MusicSettings
					{
						Master = user
					});
					lavaPlayer = lavaShard.GetPlayer(guildId);
				}
				return await EmbedHelper.CreateBasicEmbed("Music Join", $"Joined to {user.VoiceChannel}");
			}
			catch (Exception e)
			{
				return await EmbedHelper.CreateErrorEmbed("Music, Join", e.Message);
			}
		}
		public async Task<Embed> PlayAsync(SocketGuildUser user, ulong guildId, string query = null)
		{
			if (user.VoiceChannel == null)
				return await EmbedHelper.CreateErrorEmbed("Music Play", "You must first join a voice channel!");

			if (Options.TryGetValue(user.Guild.Id, out var options) && options.Master.Id != user.Id)
				return await EmbedHelper.CreateErrorEmbed("Music, Play", $"I can't join another voice channel until {options.Master} disconnects me.");
			try
			{

				LavaTrack track;
				var search = await lavaRest.SearchYouTubeAsync(query);

				if (search.LoadType == LoadType.NoMatches && query != null)
					return await EmbedHelper.CreateErrorEmbed("Music", $"I wasn't able to find anything for {query}.");
				if (search.LoadType == LoadType.LoadFailed && query != null)
					return await EmbedHelper.CreateErrorEmbed("Music", $"I failed to load {query}.");

				track = search.Tracks.FirstOrDefault();

				if (lavaPlayer.CurrentTrack != null && lavaPlayer.IsPlaying || lavaPlayer.IsPaused)
				{
					lavaPlayer.Queue.Enqueue(track);
					return await EmbedHelper.CreateBasicEmbed("Music", $"{track.Title} has been added to queue.");
				}
				await lavaPlayer.PlayAsync(track);
				return await EmbedHelper.CreateMusicEmbed("Music", $"Now Playing: {track.Title}\nUrl: {track.Uri}");
			}
			catch (Exception e)
			{
				return await EmbedHelper.CreateErrorEmbed("Music, Play", e.Message);
			}
		}

		public async Task<Embed> LeaveAsync(SocketGuildUser user, ulong guildId)
		{
			if (Options.TryGetValue(user.Guild.Id, out var options) && options.Master.Id != user.Id)
				return await EmbedHelper.CreateErrorEmbed("Music, Leave", $"I can't leave voice channel until {options.Master} disconnects me.");
			try
			{
				var player = lavaShard.GetPlayer(guildId);

				if (player.IsPlaying)
					await player.StopAsync();
				Options.TryRemove(user.Guild.Id, out var musicSettings);
				var channelName = player.VoiceChannel.Name;
				await lavaShard.DisconnectAsync(user.VoiceChannel);
				return await EmbedHelper.CreateBasicEmbed("Music", $"Disconnected from {channelName}.", $"Bye, bye {musicSettings.Master}");
			}

			catch (InvalidOperationException e)
			{
				return await EmbedHelper.CreateErrorEmbed("Leaving Music Channel", e.Message);
			}
		}

		public async Task<Embed> ListAsync(ulong guildId)
		{
			try
			{
				var descriptionBuilder = new StringBuilder();

				var player = lavaShard.GetPlayer(guildId);
				if (player == null)
					return await EmbedHelper.CreateErrorEmbed("Music Queue", $"Could not aquire music player.\nAre you using the music service right now?");

				if (player.IsPlaying)
				{

					if (player.Queue.Count < 1 && player.CurrentTrack != null)
					{
						return await EmbedHelper.CreateBasicEmbed($"Now Playing: {player.CurrentTrack.Title}", "There are no other items in the queue.");
					}
					else
					{
						var trackNum = 2;
						foreach (LavaTrack track in player.Queue.Items)
						{
							if (trackNum == 2) { descriptionBuilder.Append($"Up Next: [{track.Title}]({track.Uri})\n"); trackNum++; }
							else { descriptionBuilder.Append($"#{trackNum}: [{track.Title}]({track.Uri})\n"); trackNum++; }
						}
						return await EmbedHelper.CreateBasicEmbed("Music Playlist", $"Now Playing: [{player.CurrentTrack.Title}]({player.CurrentTrack.Uri})\n{descriptionBuilder.ToString()}");
					}
				}
				else
				{
					return await EmbedHelper.CreateErrorEmbed("Music Queue", "Player doesn't seem to be playing anything right now. If this is an error, Please contact Stage in the Kaguya support server.");
				}
			}
			catch (Exception ex)
			{
				return await EmbedHelper.CreateErrorEmbed("Music, List", ex.Message);
			}

		}

		public async Task<Embed> SkipTrackAsync(ulong guildId)
		{
			try
			{
				var player = lavaShard.GetPlayer(guildId);
				if (player == null)
					return await EmbedHelper.CreateErrorEmbed("Music, List", $"Could not aquire player.\nAre you using the bot right now?");
				if (player.Queue.Count == 1)
					return await EmbedHelper.CreateMusicEmbed("Music Skipping", "This is the last song in the queue, so I have stopped playing."); await player.StopAsync();
				if (player.Queue.Count == 0)
					return await EmbedHelper.CreateErrorEmbed("Music Skipping", "There are no songs to skip!");
				else
				{
					try
					{
						var currentTrack = player.CurrentTrack;
						await player.SkipAsync();
						return await EmbedHelper.CreateBasicEmbed("Music Skip", $"Successfully skipped {currentTrack.Title}");
					}
					catch (Exception ex)
					{
						return await EmbedHelper.CreateErrorEmbed("Music Skipping Exception:", ex.ToString());
					}

				}
			}
			catch (Exception ex)
			{
				return await EmbedHelper.CreateErrorEmbed("Music Skip", ex.ToString());
			}
		}

		public async Task<Embed> VolumeAsync(ulong guildId, int volume)
		{
			if (volume >= 150 || volume <= 0)
			{
				return await EmbedHelper.CreateErrorEmbed($"Music Volume", $"Volume must be between 1 and 149.");
			}
			try
			{
				var player = lavaShard.GetPlayer(guildId);
				await player.SetVolumeAsync(volume);
				return await EmbedHelper.CreateBasicEmbed($"🔊 Music Volume", $"Volume has been set to {volume}.");
			}
			catch (InvalidOperationException ex)
			{
				return await EmbedHelper.CreateErrorEmbed("Music Volume", $"{ex.Message}", "Please contact Stage in the support server if this is a recurring issue.");
			}
		}

		public async Task<Embed> Pause(ulong guildId)
		{
			try
			{
				var player = lavaShard.GetPlayer(guildId);
				if (player.IsPaused)
				{
					await player.ResumeAsync();
					return await EmbedHelper.CreateMusicEmbed("▶️ Music", $"**Resumed:** Now Playing {player.CurrentTrack.Title}");
				}
				else
				{
					await player.PauseAsync();
					return await EmbedHelper.CreateMusicEmbed("⏸️ Music", $"**Paused:** {player.CurrentTrack.Title}");
				}
			}
			catch (InvalidOperationException e)
			{
				return await EmbedHelper.CreateErrorEmbed("Music Play/Pause", e.Message);
			}
		}

		public async Task OnTrackFinished(LavaPlayer player, LavaTrack track, TrackEndReason reason)
		{
			if (!reason.ShouldPlayNext())
				return;

			if (!player.Queue.TryDequeue(out var item) || !(item is LavaTrack nextTrack))
			{
				await player.TextChannel?.SendMessageAsync($"There are no more songs left in queue.");
				return;
			}

			await player.PlayAsync(nextTrack);

			EmbedBuilder embed = new EmbedBuilder();
			embed.WithDescription($"**Finished Playing: `{track.Title}`\nNow Playing: `{nextTrack.Title}`**");
			embed.WithColor(Color.Red);
			await player.TextChannel.SendMessageAsync(embed: embed.Build());
			await player.TextChannel.SendMessageAsync(player.ToString());
		}
	}
}
