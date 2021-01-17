using System.Collections;
using System;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Globalization;
using System.Security.Cryptography.X509Certificates;

namespace Oxide.Plugins
{
    [Info("OxideRustEsLogger", "RedSys", 2.7)]
    [Description("Logs player actions.")]
    class OxideRustEsLogger : RustPlugin
    {

        private void LoadVariables()
        {
            LoadConfigVariables();
        }

        private ConfigData configData;
        class ConfigData {
            public string esHost { get; set; }
            public string esPort { get; set; }
            public string esUsername { get; set; }
            public string esPassword { get; set; }
        }
        private void LoadConfigVariables() => configData = Config.ReadObject<ConfigData>();

        // Called after the player object is created, but before the player has spawned
        void OnPlayerConnected(BasePlayer player) {
            try
            {
                PlayerBaseEventLogEntry eventLogEntry = new PlayerBaseEventLogEntry(player, "OnPlayerConnected");
                string eventLogEntryString = JsonConvert.SerializeObject(eventLogEntry);
                SendEventLog(eventLogEntryString);
            }
            catch (Exception error)
            {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR - OnPlayerConnected - {error.Message} - {error.StackTrace}", this);
            }
        }

        //  Called after the player has disconnected from the server
        void OnPlayerDisconnected(BasePlayer player, string reason) {
            try
            {
                PlayerBaseEventLogEntry eventLogEntry = new PlayerBaseEventLogEntry(player, "OnPlayerDisconnected");
                string eventLogEntryString = JsonConvert.SerializeObject(eventLogEntry);
                SendEventLog(eventLogEntryString);
            }
            catch (Exception error)
            {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR - OnPlayerDisconnected - {error.Message} - {error.StackTrace}", this);
            }
        }

        // Called when the player sends chat to the server
        object OnPlayerChat(BasePlayer player, string message, ConVar.Chat.ChatChannel channel) {
            try
            {
                PlayerChatEventLogEntry eventLogEntry = new PlayerChatEventLogEntry(player, message);
                string eventLogEntryString = JsonConvert.SerializeObject(eventLogEntry);
                SendEventLog(eventLogEntryString);
            }
            catch (Exception error)
            {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR - OnPlayerChat - {error.Message} - {error.StackTrace}", this);
            }
            return null;
        }

        // Called when the player starts looting another player
        void OnLootPlayer(BasePlayer player, BasePlayer target) {
            try {
                PlayerLootEventLogEntry eventLogEntry = new PlayerLootEventLogEntry(player, target);
                string eventLogEntryString = JsonConvert.SerializeObject(eventLogEntry);
                SendEventLog(eventLogEntryString);
            } catch (Exception error) {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR - OnLootPlayer - {error.Message} - {error.StackTrace}", this);
            }
        }

        // Useful for modifying an attack before it goes out hitInfo.HitEntity should be the
        void OnPlayerAttack(BasePlayer attacker, HitInfo info) {
            try {
                if(attacker == null)
                {
                    LogToFile("es_logger.log", $"Attacker is null", this);
                }
                if(info == null)
                {
                    LogToFile("es_logger.log", $"info is null", this);
                }
                LogToFile("es_logger_asdf.log", $"OnPlayerAttack - {JsonConvert.SerializeObject(attacker.displayName)}", this);

                PlayerAttackEventLogEntry eventLogEntry = new PlayerAttackEventLogEntry(attacker, info);
                string eventLogEntryString = JsonConvert.SerializeObject(eventLogEntry);
                SendEventLog(eventLogEntryString);
            }
            catch (Exception error)
            {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR - OnPlayerAttack - {error.Message} - {error.StackTrace}", this);
                LogToFile("es_logger.log", error.StackTrace, this);
            }
        }

        // Called when the player is about to die. HitInfo may be null sometimes
        object OnPlayerDeath(BasePlayer player, HitInfo info) {
            try
            {
                PlayerBaseEventLogEntry eventLogEntry = new PlayerDeathEventLogEntry(player, info);
                string eventLogEntryString = JsonConvert.SerializeObject(eventLogEntry);
                SendEventLog(eventLogEntryString);
            }
            catch (Exception error)
            {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR - OnPlayerDeath - {error.Message} - {error.StackTrace}", this);
            }
            return null;
        }

        IEnumerator SendEventLog(string logMsg) {

            LogToFile("es_log_messages.log", logMsg, this);

            string identity = ConVar.Server.identity;
            string suffix = getEsIndexSuffix();
            string esIndex = String.Format("{0}-{1}", identity, suffix);
            string esHost = configData.esHost;
            string esPort = configData.esPort;
            string esUsername = configData.esUsername;
            string esPassword = configData.esPassword;
            string esUri = String.Format("{0}:{1}/{2}",esHost,esPort,esIndex);
            string esAuth = String.Format("{0}:{1}", esUsername, esPassword);

            UnityWebRequest webRequest = UnityWebRequest.Post(esUri, logMsg);
            LogToFile("es_logger.log", $"[{DateTime.Now}] INFO {webRequest}", this);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("Authorization", Base64Encode(esAuth));
            webRequest.certificateHandler = new AcceptPinnedCerts();

            yield return webRequest.SendWebRequest();
            if (webRequest.isNetworkError || webRequest.isHttpError) {
                LogToFile("es_logger.log", $"[{DateTime.Now}] ERROR {webRequest.error}", this);
            } else
            {
                LogToFile("es_logger.log", $"[{DateTime.Now}] INFO {webRequest.responseCode}", this);
            }
            
        }

        public static string Base64Encode(string plainText) {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return Convert.ToBase64String(plainTextBytes);
        }

        string getEsIndexSuffix() {
            DateTime dateTime = DateTime.UtcNow;
            return String.Format("{0}-{1}", dateTime.Year, GetIso8601WeekOfYear(dateTime));
        }

        // Nabbed from: https://stackoverflow.com/questions/11154673/get-the-correct-week-number-of-a-given-date
        public static int GetIso8601WeekOfYear(DateTime time) {
            DayOfWeek day = CultureInfo.InvariantCulture.Calendar.GetDayOfWeek(time);
            if (day >= DayOfWeek.Monday && day <= DayOfWeek.Wednesday) {
                time = time.AddDays(3);
            }
            return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(time, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
        }

        [Serializable]
        public class PlayerDeathEventLogEntry : PlayerBaseEventLogEntry
        {
            public ulong target_steam_id;
            public string target_name;
            public float target_location_x;
            public float target_location_y;
            public float target_location_z;
            public bool hit_info_null;
            public string weapon_name;
            public bool did_gather;
            public bool is_projectile;
            public PlayerDeathEventLogEntry(BasePlayer player, HitInfo info) : base(player, "OnPlayerDeath")
            {
                if (info != null)
                {
                    hit_info_null = false;
                    if(info.Weapon.name != null) {
                        weapon_name = info.Weapon.name;
                    }
                    if (info.DidGather != null) {
                        did_gather = info.DidGather;
                    }
                    if (info.IsProjectile() != null)
                    {
                        is_projectile = info.IsProjectile();
                    }
                }
                else
                {
                    hit_info_null = true;
                    weapon_name = "";
                    did_gather = false;
                    is_projectile = false;
                }
            }
        }

        [Serializable]
        public class PlayerAttackEventLogEntry : PlayerBaseEventLogEntry
        {
            public ulong target_steam_id;
            public string target_name;
            public float target_location_x;
            public float target_location_y;
            public float target_location_z;
            public bool hit_info_null;
            public string weapon_name;
            public bool did_gather;
            public bool is_projectile;
            public PlayerAttackEventLogEntry(BasePlayer player, HitInfo info) : base(player, "OnPlayerAttack")
            {

                BasePlayer targetPlayer = info.HitEntity.GetComponent<BasePlayer>();

                if (targetPlayer != null)
                {
                    target_steam_id = targetPlayer.userID;
                    target_name = targetPlayer.displayName;
                    target_location_x = targetPlayer.transform.position.x;
                    target_location_y = targetPlayer.transform.position.y;
                    target_location_z = targetPlayer.transform.position.z;
                } else
                {
                    target_steam_id = 0;
                    target_name = "";
                    target_location_x = 0;
                    target_location_y = 0;
                    target_location_z = 0;
                }

                if (info != null)
                {
                    hit_info_null = false;
                    if (info.Weapon.name != null)
                    {
                        weapon_name = info.Weapon.name;
                    }
                    if (info.DidGather != null)
                    {
                        did_gather = info.DidGather;
                    }
                    if (info.IsProjectile() != null)
                    {
                        is_projectile = info.IsProjectile();
                    }
                }
                else
                {
                    hit_info_null = true;
                    weapon_name = "";
                    did_gather = false;
                    is_projectile = false;
                }
            }
        }

        [Serializable]
        public class PlayerLootEventLogEntry : PlayerBaseEventLogEntry
        {
            public ulong target_steam_id;
            public string target_name;
            public float target_location_x;
            public float target_location_y;
            public float target_location_z;

            public PlayerLootEventLogEntry(BasePlayer player, BasePlayer targetPlayer) : base(player, "OnLootPlayer")
            {
                target_steam_id = targetPlayer.userID;
                target_name = targetPlayer.displayName;
                target_location_x = targetPlayer.transform.position.x;
                target_location_y = targetPlayer.transform.position.y;
                target_location_z = targetPlayer.transform.position.z;
            }
        }

        [Serializable]
        public class PlayerChatEventLogEntry : PlayerBaseEventLogEntry
        {
            public string message;
            public PlayerChatEventLogEntry(BasePlayer player, string message) : base(player, "OnPlayerChat")
            {
                this.message = message;
            }
        }

        [Serializable]
        public class PlayerBaseEventLogEntry
        {
            // We're using snake_case because this will be serialized to JSON.
            public int timestamp;
            public ulong steam_id;
            public string name;
            public float location_x;
            public float location_y;
            public float location_z;
            public string hook_name;

            public PlayerBaseEventLogEntry(BasePlayer player, string hookName)
            {
                timestamp = getEpoch();
                steam_id = player.userID;
                name = player.displayName;
                location_x = player.transform.position.x;
                location_y = player.transform.position.y;
                location_z = player.transform.position.z;
                hook_name = hookName;
            }

            int getEpoch()
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                return (int)t.TotalSeconds;
            }
        }

        // Based on https://www.owasp.org/index.php/Certificate_and_Public_Key_Pinning#.Net
        class AcceptPinnedCerts : CertificateHandler
        {
            // Encoded RSAPublicKey
            private static string PUB_KEY = "30818902818100C4A06B7B52F8D17DC1CCB47362" +
                "C64AB799AAE19E245A7559E9CEEC7D8AA4DF07CB0B21FDFD763C63A313A668FE9D764E" +
                "D913C51A676788DB62AF624F422C2F112C1316922AA5D37823CD9F43D1FC54513D14B2" +
                "9E36991F08A042C42EAAEEE5FE8E2CB10167174A359CEBF6FACC2C9CA933AD403137EE" +
                "2C3F4CBED9460129C72B0203010001";

            protected override bool ValidateCertificate(byte[] certificateData)
            {
                return true;

                X509Certificate2 certificate = new X509Certificate2(certificateData);
                string pk = certificate.GetPublicKeyString();
                if (pk.Equals(PUB_KEY))
                    return true;

                // Bad dog
                return false;
            }
        }

    }
}
