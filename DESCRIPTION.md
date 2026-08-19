Project Fika is a mod designed to enable co-op multiplayer game play with your friends in SPT. You can progress through quests, share items and quest progression, fight AI bots (PMCs, SCAVs, and bosses) together and much more.
-
# Information {.tabset}

## What is Fika? ❓

Fika is a cooperative multiplayer mod for [SPT](https://sp-tarkov.com/). Fika adds a lobby interface to join other players' raids and provide in-game networking capabilities, along with additional features that do not alter the standard Escape From Tarkov experience by default.

You can find our installation guide [HERE](https://project-fika.gitbook.io/wiki/)

We do not provide support on the Forge, use our [discord](http://project-fika.com/discord) for support. Please try to minimize the burden on SPT moderators and use our discord for questions.

In summary:

* SPT is the back-end server that allows you to start the game without using BSG servers — it provides the player profile, inventory, flea market, and many other Escape From Tarkov features.
* Fika is the mod component that adds cooperative multiplayer capabilities which allow players to host or join a raid to play together.

## Specifications 📃

* Fika is a combination of a [BepInEx plugin](https://github.com/project-fika/Fika-Plugin) and a [SPT server mod](https://github.com/project-fika/Fika-Server).
* Fika uses SPT (with the Fika-Server plugin) as the back-end server for profile and lobby management.
* Fika uses a Server <-> Client UDP networking model for game play. While Fika is still in development, the general consensus is that network performance is better than Escape From Tarkov live (provided the host has proper networking capabilities). Fika 2.0 (coming with SPT 4.0) will have *much better* net code than live, outperforming both speed, bandwidth usage and accuracy.

## Main features ⭐

* Host your own server or join a local/external server.
* Create your own raid (or play solo).
* Join someone else's raid.
* Play together against bots, share items, progress quests together in the same raid.
* Preserve character, quest, inventory, and hideout progression.
* Use client/server mods from [SPT](https://hub.sp-tarkov.com/) (some mods are not compatible - see **Limitations**).

## Other features 🌟

* Item Sending
  * Right-click an item in your stash to send it to another account
  * Can be customized in the [server](https://github.com/project-fika/Fika-Documentation/wiki/6.-Fika-configuration) config
* Free cam (default to `F9` key)
  * In free cam you can teleport to the cam position by pressing `T`
  * You can jump to another player by `Left/Right` clicking
  * You can snap to their head by holding `SPACE` when jumping
  * You can snap to their back in a 3rd position view by holding `CTRL` when jumping
  * You can press the `HOME` key to temporarily toggle free cam controls
* In-game chat system
* In-raid VOIP
* Online player list
* Culling system to increase performance
* Custom notifications (teammate died, boss got killed by a player, etc.)
* Pinging system to ping an area in the game for your teammates
* Player health bars for your teammates
* Quest progress sharing in raids
* Optional/Advanced Headless client to offload AI and gain performance (more info [here](https://project-fika.gitbook.io/wiki/advanced-features/headless-client))
* Network interpolation for smoother gameplay
* UPnP and NAT Punching

## Limitations ❌

* You cannot play Fika without owning a legitimate copy of Escape From Tarkov.
* There is no protection against abuse or cheating. Fika is designed to be played with trusted friends. **Hosting a public server is strongly discouraged**. We will NEVER support public servers.
* Fika does not include any PvP mechanisms. Supporting PvP is not the goal of this project.
* Fika does not offer a global matchmaking service. Only players connected to the same server can create or join raids.
* Certain SPT mods are incompatible with Fika. SPT mods are typically designed for a standard SPT installation, which does not include multiplayer functionality. It is the responsibility of the mod's author to make their mod compatible with Fika, if they choose to do so.

## Modding Fika🛠️
Please find our wiki page for modding [here](https://wiki.project-fika.com/modding-fika).

## Support Me 💖
You can support me on Ko-Fi

 [Click](https://ko-fi.com/lacyway)

{.endtabset}
