﻿using System.Threading.Tasks;
using Discord.WebSocket;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Discord;
using Newtonsoft.Json;

namespace Streams
{
    internal class Bot
    {
        private static readonly DateTime startTime = DateTime.Now;
        private static readonly RequestOptions reqOpt = new RequestOptions()
        {
            RetryMode = RetryMode.RetryRatelimit
        };
        private static readonly EmbedFooterBuilder footer = new EmbedFooterBuilder()
        {
            Text = "service by 3v.fi/l/streams"
        };

        private DiscordSocketClient client = new DiscordSocketClient();
        private Http.Requester request = new Http.Requester(new Uri("https://api.twitch.tv/v5/"));
        private CancellationTokenSource stopswitch = new CancellationTokenSource();
        private Timer streamlooker;
        private ConcurrentDictionary<ulong, (SocketTextChannel Channel, List<Http.Models.Twitch.User> UserList)> streamChannels = new ConcurrentDictionary<ulong, (SocketTextChannel, List<Http.Models.Twitch.User>)>();
        private ConcurrentDictionary<string, IUserMessage> storedMessages = new ConcurrentDictionary<string, IUserMessage>();

        public Bot()
        {

        }

        public async Task Run(string token)
        {
            client.Connected += Client_Connected;
            client.MessageReceived += Client_MessageReceived;
            client.GuildAvailable += Client_GuildAvailable;
            client.Log += msg => Task.Run(() => Console.WriteLine($"Discord.Net log: {msg}"));

            Console.WriteLine($"Connecting using token \"{token}\"");
            await client.LoginAsync(TokenType.Bot, token, true);
            await client.StartAsync();

            Console.WriteLine("Starting update loop");
            streamlooker = new Timer(UpdateStreams, null, 20000, 300000);

            try
            {
                await Task.Delay(-1, stopswitch.Token);
            }
            catch (TaskCanceledException) { /* expected when closing down program via ^C */ }
        }

        private Task Client_GuildAvailable(SocketGuild arg) => Task.Run(() =>
        {
            // Load channel data
            foreach (var channel in arg.TextChannels)
            {
                string channelPath = $"data/{channel.Id}.dat";
                if (File.Exists(channelPath))
                {
                    streamChannels[channel.Id] = (channel, JsonConvert.DeserializeObject<List<Http.Models.Twitch.User>>(File.ReadAllText(channelPath)));
                }
            }
        });

        private void SaveChannelData(ulong channelId, List<Http.Models.Twitch.User> data)
        {
            List<Http.Models.Twitch.User> copy = new List<Http.Models.Twitch.User>(data.Count);
            lock (data)
            {
                copy.AddRange(data);
            }
            File.WriteAllText($"data/{channelId}.dat", JsonConvert.SerializeObject(copy));
        }

        public async Task Stop()
        {
            Console.WriteLine("Cleaning up...");

            // Clean and logout
            streamlooker.Dispose();

            foreach (var message in storedMessages.Values)
            {
                await message.DeleteAsync();
            }

            Console.WriteLine("Logging out...");
            await client.LogoutAsync();
            await client.StopAsync();
            Console.WriteLine("Exited");
            stopswitch.Cancel();
        }

        private Task Client_Connected() => Task.Run(async () =>
        {
            try
            {
                await client.SetGameAsync("Monitoring streams", "https://www.twitch.tv/directory/following/live", StreamType.Twitch);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting game: {ex.Message}");
            }
            Console.WriteLine("Connected");
        });


        private Task Client_MessageReceived(SocketMessage arg) => Task.Run(async () =>
        {
            // Ensure manage channel permission
            SocketTextChannel channel = arg.Channel as SocketTextChannel;
            SocketGuildUser dUser = arg.Author as SocketGuildUser;
            if (dUser?.GetPermissions(channel).ManageChannel == true && arg.Content.Length > 0 && arg.Content[0] == '=')
            {
                string[] words = arg.Content.Split(' ');
                try
                {
                    switch (words[0].Substring(1))
                    {
                        case "addstream":
                            Task deleteCommand = DeleteMessage(arg);
                            if (words.Length >= 2)
                            {
                                Http.Models.Twitch.UsersByLogin users;
                                using (channel.EnterTypingState())
                                {
                                    try
                                    {
                                        users = await request.GetObjectAsync<Http.Models.Twitch.UsersByLogin>($"users/?limit=100&login={Uri.EscapeDataString(words[1])}");
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Error: {ex.Message}");
                                        await DeleteMessage(await channel.SendMessageAsync("Twitch API returned an error. Please try again later."));
                                        await deleteCommand;
                                        break;
                                    }

                                    (SocketTextChannel, List<Http.Models.Twitch.User> UserList) tUsers;
                                    try
                                    {
                                        tUsers = streamChannels[channel.Id];
                                    }
                                    catch (KeyNotFoundException)
                                    {
                                        tUsers = (channel, new List<Http.Models.Twitch.User>());
                                        streamChannels[channel.Id] = tUsers;
                                    }

                                    foreach (var tUser in users.Users)
                                    {
                                        lock (tUsers.UserList)
                                        {
                                            if (!tUsers.UserList.Contains(tUser))
                                            {
                                                tUsers.UserList.Add(tUser);
                                            }
                                        }
                                    }
                                    SaveChannelData(channel.Id, tUsers.UserList);

                                    int trackedChannels = tUsers.UserList.Count;
                                    if (trackedChannels > 100)
                                    {
                                        await channel.SendMessageAsync($":warning: You have enabled tracking for {trackedChannels}. Behavior has not been tested when over a 100 tracked channels are live at once. Consider creating another channel to track the additional streams.");
                                    }
                                }
                                await DeleteMessage(await channel.SendMessageAsync($"Added tracking for {users.Users.Length} channels: " + string.Join(", ", (users.Users.Select(tUser => tUser.DisplayName)))));
                            }
                            else
                            {
                                await DeleteMessage(await channel.SendMessageAsync("Usage: =addstream [username]"));
                            }
                            await deleteCommand;
                            break;
                        case "liststreams":
                        case "list":
                            try
                            {
                                List<Http.Models.Twitch.User> tUsers = streamChannels[channel.Id].UserList;
                                await channel.SendMessageAsync($"Tracked channels:\n- " + string.Join("\n- ", tUsers.Select(u => $"{u.DisplayName} ({u.Name}) - {u.Id}")));
                            }
                            catch (KeyNotFoundException)
                            {
                                await channel.SendMessageAsync("Tracking no streams for this channel.");
                            }
                            break;
                        case "delstream":
                            deleteCommand = DeleteMessage(arg);
                            if (words.Length >= 2)
                            {
                                List<Http.Models.Twitch.User> tUsers = null;
                                try
                                {
                                    tUsers = streamChannels[channel.Id].UserList;
                                }
                                catch (KeyNotFoundException) { /* key not found, tUsers stays null and is ignored */ }

                                if (tUsers != null)
                                {
                                    string[] ids = words[1].Split(',');
                                    foreach (string id in ids)
                                    {
                                        DeleteStoredMessage($"{channel.Id}:{id}");

                                        lock (tUsers)
                                        {
                                            tUsers.Remove(tUsers.Where(u => u.Id == id).First());
                                        }
                                    }
                                    SaveChannelData(channel.Id, tUsers);
                                }

                                await DeleteMessage(await channel.SendMessageAsync("Removed the requested channel IDs from tracking."));
                            }
                            await deleteCommand;
                            break;
                        case "bot":
                        case "status":
                            EmbedBuilder eb = new EmbedBuilder()
                            {
                                Author = new EmbedAuthorBuilder()
                                {
                                    IconUrl = "https://i.3v.fi/raw/3logo.png",
                                    Name = "3v",
                                    Url = "https://3v.fi/l/streams"
                                },
                                Title = "Bot Status"
                            }
                            .AddInlineField("RAM Usage (GC)", $"{(Math.Ceiling(GC.GetTotalMemory(true) / 1024.0))} KB")
                            .AddInlineField("Uptime", (DateTime.UtcNow - startTime).ToString(@"d\ \d\a\y\s\,\ h\ \h\o\u\r\s"))
                            .AddInlineField("Guilds", client.Guilds.Count)
                            .AddInlineField("Users", GetUserCount())
                            .AddInlineField("Streams Tracked", GetTrackedStreamCount())
                            .AddInlineField("Streams Live", storedMessages.Count);
                            await channel.SendMessageAsync("", embed: eb.Build());
                            break;
                    }
                }
                catch (Exception ex) when (ex is Discord.Net.HttpException || ex is Discord.Net.RateLimitedException || ex is Discord.Net.WebSocketClosedException)
                {
                    // don't crash on library internal errors
                }
            }
        });

        private int GetTrackedStreamCount()
        {
            int streams = 0;
            foreach (var tUsers in streamChannels.Values)
            {
                streams += tUsers.UserList.Count;
            }
            return streams;
        }

        private int GetUserCount()
        {
            int users = 0;
            foreach (var guild in client.Guilds)
            {
                users += guild.MemberCount;
            }
            return users;
        }

        private void DeleteStoredMessage(string storedMessageKey)
        {
            if (storedMessages.TryRemove(storedMessageKey, out IUserMessage message))
            {

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

                try
                {
                    message.DeleteAsync(reqOpt);
                }
                catch (Exception ex) when (ex is Discord.Net.HttpException || ex is Discord.Net.RateLimitedException || ex is Discord.Net.WebSocketClosedException)
                {
                    // don't crash on library internal errors
                }

#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            }
        }

        private async Task DeleteMessage(IMessage message)
        {
            await Task.Delay(5000);
            await message.DeleteAsync(reqOpt);
        }


        private Embed GetEmbedObject(Http.Models.Twitch.Stream stream)
        {
            EmbedBuilder eb = new EmbedBuilder()
            {
                Author = new EmbedAuthorBuilder()
                {
                    IconUrl = stream.Channel.Logo,
                    Name = stream.Channel.DisplayName,
                    Url = stream.Channel.Url
                },
                Color = new Color(100, 65, 164),
                Title = stream.Channel.Status,
                Url = stream.Channel.Url,
                Timestamp = DateTimeOffset.Now,
                ImageUrl = stream.Previews["large"] + $"?{DateTime.Now.Ticks}",
                Footer = footer
            }.AddField("Category", stream.Game).AddInlineField("Viewers", stream.Viewers).AddInlineField("Uptime", GetUptime(stream.CreatedAt));
            return eb.Build();
        }

        private string GetUptime(DateTime createdAt) => (DateTime.Now - createdAt).ToString(@"h\h\ m\m");

        private async void UpdateStreams(object state)
        {
            Console.WriteLine("Running UpdateStreams");
            foreach (var tUsers in streamChannels.Values)
            {
                Console.WriteLine($"Checking updates for channel {tUsers.Channel.Name} in {tUsers.Channel.Guild.Name}");
                using (tUsers.Channel.EnterTypingState())
                {
                    string idlist = string.Join(",", tUsers.UserList.Select(tUser => tUser.Id));
                    Http.Models.Twitch.Streams streams;
                    try
                    {
                        streams = await request.GetObjectAsync<Http.Models.Twitch.Streams>($"streams/?limit=100&channel={idlist}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        continue;
                    }

                    IEnumerable<string> foundIds = streams.StreamList.Select(stream => stream.Channel.Id);
                    foreach (var tUser in tUsers.UserList)
                    {
                        string storedMessageKey = $"{tUsers.Channel.Id}:{tUser.Id}";
                        if (!foundIds.Contains(tUser.Id))
                        {
                            // Offline, remove if message exists, else nothing to do
                            DeleteStoredMessage(storedMessageKey);
                        }
                        else
                        {
                            try
                            {
                                var stream = streams.StreamList.Where(s => s.Channel.Id == tUser.Id).First();
                                if (storedMessages.TryGetValue(storedMessageKey, out IUserMessage message))
                                {
                                    Console.WriteLine($"Editing {storedMessageKey} on {tUsers.Channel.Id}, {tUsers.Channel.Name} in {tUsers.Channel.Guild.Name}");
                                    // Message exists, edit it
                                    await message.ModifyAsync(properties =>
                                    {
                                        properties.Embed = GetEmbedObject(stream);
                                    }, reqOpt);
                                }
                                else
                                {
                                    // Message doesn't exist, create it
                                    storedMessages[storedMessageKey] = await tUsers.Channel.SendMessageAsync("", embed: GetEmbedObject(stream), options: reqOpt);
                                }
                            }
                            catch (Exception ex) when (ex is Discord.Net.HttpException || ex is Discord.Net.RateLimitedException || ex is Discord.Net.WebSocketClosedException)
                            {
                                // don't crash on library internal errors
                            }
                        }
                    }
                }
            }
        }
    }
}