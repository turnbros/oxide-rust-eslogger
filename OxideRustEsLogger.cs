using System.Collections.Generic;
using System;
using System.Reflection;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("HelloWord", "igor", 1.0)]
    class OxideRustEsLogger : RustPlugin
    {
        // Called after the player object is created, but before the player has spawned
        void OnPlayerConnected(BasePlayer player)
        {
            Puts("OnPlayerConnected works!");
        }

        //  Called after the player has disconnected from the server
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            Puts("OnPlayerDisconnected works!");
        }

        // Called when the player sends chat to the server
        object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            Puts("OnPlayerChat works!");
            return null;
        }

        // Called when the player triggers an anti-hack violation
        object OnPlayerViolation(BasePlayer player, AntiHackType type, float amount)
        {
            Puts("OnPlayerViolation works!");
            return null;
        }

        // Useful for modifying an attack before it goes out hitInfo.HitEntity should be the
        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            Puts("OnPlayerAttack works!");
        }

        // Called when the player is about to die. HitInfo may be null sometimes
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            Puts("OnPlayerDeath works!");
            return null;
        }

        // Called when the player starts looting another player
        void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            Puts("OnLootPlayer works!");
        }

        /*
         *  Base Event
            {
              "steam_id": 5555555555555,
              "player_name": "fungus",
              "action": "OnFooAction",
              "data": {},
              "timestamp": 3483476648,
              "location": {
                "x": 324,
                "y": 564,
                "z": 987
              }
            }
         */
        void SendEventLog(BasePlayer player, string action, Hash<string,string>data)
        {

        }
    }
}
