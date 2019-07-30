using Discord;

using System;
using System.Threading.Tasks;

namespace Bot
{
	internal class Logger
	{
		private static ConsoleColor SeverityToConsoleColor(LogSeverity severity)
		{
			switch (severity)
			{
				case LogSeverity.Critical:
					return ConsoleColor.Red;
				case LogSeverity.Debug:
					return ConsoleColor.Blue;
				case LogSeverity.Error:
					return ConsoleColor.Yellow;
				case LogSeverity.Info:
					return ConsoleColor.Blue;
				case LogSeverity.Verbose:
					return ConsoleColor.Green;
				case LogSeverity.Warning:
					return ConsoleColor.Magenta;
				default:
					return ConsoleColor.White;
			}
		}

		internal static Task Log(LogMessage logMessage)
		{
			Console.ForegroundColor = SeverityToConsoleColor(logMessage.Severity);
			string message = $"[{DateTime.Now.ToLongTimeString()} | Source: {logMessage.Source}] Message: {logMessage.Message}.";
			Console.WriteLine(message);
			Console.ResetColor();
			return Task.CompletedTask;
		}
	}
}
