using Bot.Services;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Bot.Modules
{
	public class MusicModule : RootModule
	{
		private readonly MusicService music;
		public MusicModule(MusicService musicService)
		{
			music = musicService;
		}
		[Command("join")]
		public async Task MusicJoin()
			=> await ReplyAsync(embed: await music.JoinAsync((SocketGuildUser)Context.User, Context.Guild.Id));

		[Command("play")]
		public async Task MusicPlay([Remainder]string search)
			=> await ReplyAsync(embed: await music.PlayAsync((SocketGuildUser)Context.User, Context.Guild.Id, search));

		[Command("leave")]
		public async Task MusicLeave()
			=> await ReplyAsync(embed: await music.LeaveAsync((SocketGuildUser)Context.User, Context.Guild.Id));

		[Command("queue")]
		public async Task MusicQueue()
			=> await ReplyAsync(embed: await music.ListAsync(Context.Guild.Id));

		[Command("skip")]
		public async Task SkipTrack()
			=> await ReplyAsync(embed: await music.SkipTrackAsync(Context.Guild.Id));

		[Command("volume")]
		public async Task Volume(int volume)
			=> await ReplyAsync(embed: await music.VolumeAsync(Context.Guild.Id, volume));

		[Command("Pause")]
		public async Task Pause()
		   => await ReplyAsync(embed: await music.Pause(Context.Guild.Id));

		[Command("Resume")]
		public async Task Resume()
			=> await ReplyAsync(embed: await music.Pause(Context.Guild.Id));
	}
}
