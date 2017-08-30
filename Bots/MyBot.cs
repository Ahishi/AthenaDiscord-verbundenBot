using System;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Audio;
using System.Diagnostics;

namespace Bots
{
    public class MyBot : ModuleBase
    {
#region Vars
        private CommandService _service;
        IAudioClient client;
        IVoiceChannel channel;
        public List<string> urls2;

        public MyBot(CommandService service)
        {
            _service = service;
        }
#endregion

        private Process CreateStream(string url)
        {
            Process currentsong = new Process();

            currentsong.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/C youtube-dl.exe -o - {url} | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            currentsong.Start();
            return currentsong;
        }

        [Command("play", RunMode = RunMode.Async)]
        public async Task play()
        {

            if (urls2.FirstOrDefault() != null)
            {
                async Task _play()
                {
                    string url = urls2.First();
                    urls2.Remove(urls2.First());
                    IVoiceChannel _channel = (Context.User as IVoiceState).VoiceChannel;
                    channel = _channel;
                    IAudioClient _client = await _channel.ConnectAsync();
                    client = _client;

                    var output = CreateStream(url).StandardOutput.BaseStream;
                    var stream = _client.CreatePCMStream(AudioApplication.Music, 128 * 1024);
                    output.CopyToAsync(stream);
                    stream.FlushAsync().ConfigureAwait(false);
                }

                if (urls2.FirstOrDefault() != null) _play();
                else
                {
                    await ReplyAsync("Que finished, the bot will leave in 60secs, if no music is qued.");

                }
            }
            else
            {
                await ReplyAsync("You need to que songs before playing them.");
            }

            }


        }


        [Command("que")]
        [Summary("Que Music")]
        [Alias("q")]
        public async Task que(string url)
        {
            urls2.Add(url);
            await ReplyAsync(Context.User.Username + " added " + url + " to the que!");
        }

        public async DiscTimer()
        {
        int i = 0;
            for (urls2.First() != null; ;)
            {
            await Task.Delay(TimeSpan.FromSeconds(5));
            i++;
            if (12 <= i)
            {
                await channel.DisconnectAsync();
            }
            }
        }

        [Command("hello")]
        [Summary("Iz hello.")]
        [Alias("hi")]
        public async Task Hello()
        {
            await ReplyAsync(" Hello... fuckface " + Context.Message.Author.Mention);
        }

        [Command("say")]
        [Summary("Yes, My lord!")]
        public async Task Say([Remainder, Summary(" echo for the bot ")] string echo = null)
        {
            if (echo == null)
            {
                await ReplyAsync(" I didn't hear you MASTER. ");
            }
            else
            {
                await ReplyAsync(echo);
            }
        }


        [Command("purge")]
        [RequireBotPermission(GuildPermission.ManageMessages)]
        [RequireUserPermission(GuildPermission.ManageMessages)]
        [Alias("Exterminate!Exterminate! ", "delete")]
        public async Task Purge([Remainder] int num = 0)
        {
            if (num <= 100)
            {
                var messagesToDelete = await Context.Channel.GetMessagesAsync(num + 1).Flatten();
                await Context.Channel.DeleteMessagesAsync(messagesToDelete);
                if (num == 1)
                {
                    await Context.Channel.SendMessageAsync(Context.User.Username + " deleted a message ");
                }
                else
                {
                    await Context.Channel.SendMessageAsync(Context.User.Username + " deleted " + num + " messages.");
                }
            }
            else
            {
                await ReplyAsync("Top 100 scrub");
            }
        }

        [Command("help")]
        [Summary("Gives help")]
        public async Task HelpAsync([Remainder, Summary("command to retrieve help for")] string command = null)
        {

            string prefix = "!";

            if (command == null)
            {
                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = "Here you go!"
                };

                foreach (var module in _service.Modules)
                {
                    string description = null;
                    foreach (var cmd in module.Commands)
                    {
                        var result = await cmd.CheckPreconditionsAsync(Context);
                        if (result.IsSuccess)
                            description += $"{prefix}{cmd.Aliases.First()}\n";
                    }

                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = false;
                        });
                    }
                }
                await ReplyAsync("", false, builder.Build());
            }
            else
            {
                var result = _service.Search(Context, command);

                if (!result.IsSuccess)
                {
                    await ReplyAsync($"No command like **{command}** in this hellhole.");
                    return;
                }

                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = $"You wondered about **{prefix}{command}**\n\nAliases; "
                };

                foreach (var match in result.Commands)
                {
                    var cmd = match.Command;

                    builder.AddField(x =>
                    {
                        x.Name = string.Join(", ", cmd.Aliases);
                        x.Value =
                            $"Summary: {cmd.Summary}\n" +
                            $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n";
                        x.IsInline = false;
                    });

                }
                await ReplyAsync("", false, builder.Build());
            }
        }

        [Command("Guest")]
        public async Task Guest()
        {
            var unregistered = Context.Guild.Roles.Where(input => input.Name.ToUpper() == "GUEST").FirstOrDefault() as SocketRole;
            var registered = Context.Guild.Roles.Where(input => input.Name.ToUpper() == "REGISTERED").FirstOrDefault() as SocketRole;
            var userList = await Context.Guild.GetUsersAsync();
            var user = userList.Where(input => input.Username == Context.Message.Author.Username).FirstOrDefault() as SocketGuildUser;

            if (user.Roles.Contains(unregistered))
            {
                await user.RemoveRoleAsync(unregistered);
                await user.AddRoleAsync(registered);
            }

        }
    }
}
