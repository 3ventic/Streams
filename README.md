# Streams

A Discord bot to provide stream status and updates from multiple Twitch channels to multiple Discord channels. Messages are edited to update stream status and deleted once the stream is over.

![example embed](https://i.3v.fi/raw/1494968608-9024.png)

# Usage

## Invite to guild

[Here](https://discordapp.com/oauth2/authorize?client_id=314045006551711747&scope=bot&permissions=27648)

## Commands

You require "Manage Channel" permission on the channel to use these commands.

- `=addstream [comma-separated-list of Twitch channel names]` to enable tracking of channels. Example: `=addstream lirik,3v`
- `=liststreams` to list tracked channels with their IDs
- `=delstream [comma-separated-list of Twitch channel IDs]` to disable tracking of channels. Example: `=del 23161357,17089325`
