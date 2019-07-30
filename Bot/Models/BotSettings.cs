using System;
using System.Collections.Generic;
using System.Text;

namespace Bot.Models
{
	public class BotSettings
	{
		public string BotName { get; set; } = "Neuromatrix";
		public string MusicModuleName { get; set; } = "Neira Audio#1";

		public DiscordSettings DiscordSettings { get; set; } = new DiscordSettings();
	}

	public class DiscordSettings
	{
		public string BotToken { get; set; } = "PUT_YOU_DISCORD_BOT_TOKEN_HERE";
		public int[] ShardIds { get; set; } = { 0, 1 };
	}
}
