using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;

namespace RimSense
{
    public enum ClientStatus
    {
        NotFound,
        Connected,
        Unsupported,
    }

    // Talks to SteelSeries GG's local GameSense HTTP API from a background thread.
    // The game thread only calls Report(); all networking happens here, so the game never waits on GG.
    public static class GameSenseClient
    {
        private const string GameId = "RIMSENSE";
        private const string EventId = "ALERT";

        private static readonly TimeSpan LoopInterval = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(10);   // GG releases devices after 15 s of silence
        private static readonly TimeSpan StaleReport = TimeSpan.FromSeconds(3);          // no reports → not in a game → calm
        private static readonly TimeSpan RediscoverInterval = TimeSpan.FromSeconds(10);
        private const int RequestTimeoutMs = 1500;

        // GameSense zone-count device types known to GG (from GG's own device specs).
        private static readonly int[] ZoneCounts = { 1, 2, 3, 4, 5, 8, 10, 12, 17, 24, 52, 103 };

        private static string corePropsPath;
        private static Thread worker;
        private static volatile bool quitting;
        private static volatile int reportedLevel;
        private static long lastReportTicks;

        public static volatile ClientStatus Status = ClientStatus.NotFound;
        public static volatile string Address;

        // Main thread only (Unity APIs).
        public static void Start()
        {
            if (worker != null)
                return;

            corePropsPath = CorePropsPath(Application.platform);
            if (corePropsPath == null)
            {
                Status = ClientStatus.Unsupported;
                return;
            }

            Application.quitting += OnQuitting;
            worker = new Thread(Run) { IsBackground = true, Name = "RimSense GameSense" };
            worker.Start();
        }

        public static void Report(AlertLevel level)
        {
            reportedLevel = (int)level;
            Interlocked.Exchange(ref lastReportTicks, DateTime.UtcNow.Ticks);
        }

        private static string CorePropsPath(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    return "/Library/Application Support/SteelSeries Engine 3/coreProps.json";
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                        "SteelSeries", "SteelSeries Engine 3", "coreProps.json");
                default:
                    return null; // GG doesn't exist on Linux
            }
        }

        private static void Run()
        {
            const int Unknown = -1;
            int sentLevel = (int)AlertLevel.Calm;
            DateTime lastSend = DateTime.MinValue;
            DateTime nextDiscovery = DateTime.MinValue;

            while (!quitting)
            {
                Thread.Sleep(LoopInterval);
                DateTime now = DateTime.UtcNow;

                // Connect eagerly: registers the RimWorld card in GG and lets the settings window show the status.
                if (Address == null)
                {
                    if (now < nextDiscovery)
                        continue;
                    nextDiscovery = now + RediscoverInterval;
                    if (!Connect())
                        continue;
                }

                int wanted = reportedLevel;
                if (now - new DateTime(Interlocked.Read(ref lastReportTicks)) > StaleReport)
                    wanted = (int)AlertLevel.Calm;

                bool calm = wanted == (int)AlertLevel.Calm;
                bool changed = wanted != sentLevel;
                if (!changed && (calm || now - lastSend < HeartbeatInterval))
                    continue;

                bool ok;
                if (calm)
                    ok = Post("stop_game", "{\"game\":\"" + GameId + "\"}");
                else if (changed)
                    ok = Post("game_event", "{\"game\":\"" + GameId + "\",\"event\":\"" + EventId + "\",\"data\":{\"value\":" + wanted + "}}");
                else
                    ok = Post("game_heartbeat", "{\"game\":\"" + GameId + "\"}");

                if (ok)
                {
                    sentLevel = wanted;
                    lastSend = now;
                }
                else
                {
                    // GG stopped or restarted on a new port: find it again and resend the current state.
                    Address = null;
                    Status = ClientStatus.NotFound;
                    sentLevel = Unknown;
                }
            }
        }

        private static bool Connect()
        {
            string address = ReadAddress();
            if (address == null)
                return false;

            Address = address;
            bool ok = Post("game_metadata", "{\"game\":\"" + GameId + "\",\"game_display_name\":\"RimWorld\",\"developer\":\"Serhii Shramko\"}")
                && Post("bind_game_event", BindPayload());
            if (!ok)
            {
                Address = null;
                return false;
            }

            Status = ClientStatus.Connected;
            return true;
        }

        private static string ReadAddress()
        {
            try
            {
                if (!File.Exists(corePropsPath))
                    return null;
                Match match = Regex.Match(File.ReadAllText(corePropsPath), "\"address\"\\s*:\\s*\"([^\"]+)\"");
                return match.Success ? match.Groups[1].Value : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool Post(string endpoint, string json)
        {
            string address = Address;
            if (address == null)
                return false;

            try
            {
                var request = (HttpWebRequest)WebRequest.Create("http://" + address + "/" + endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Proxy = null;
                request.Timeout = RequestTimeoutMs;
                request.ReadWriteTimeout = RequestTimeoutMs;

                byte[] body = Encoding.UTF8.GetBytes(json);
                request.ContentLength = body.Length;
                using (Stream stream = request.GetRequestStream())
                    stream.Write(body, 0, body.Length);

                using (var response = (HttpWebResponse)request.GetResponse())
                    return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void OnQuitting()
        {
            quitting = true;
            Post("stop_game", "{\"game\":\"" + GameId + "\"}");
        }

        // One ALERT event on the "all" zone of every RGB device type GG knows.
        // Per-zone bindings light the device too, but GG's Configure screen then shows the event on zone 1 only.
        // value_optional makes GG replay the effect on every update, so a new threat flashes again.
        private static string BindPayload()
        {
            const string effect =
                "\"mode\":\"color\"," +
                "\"color\":[" +
                    "{\"low\":1,\"high\":2,\"color\":{\"red\":255,\"green\":45,\"blue\":0}}," +
                    "{\"low\":3,\"high\":4,\"color\":{\"red\":255,\"green\":0,\"blue\":0}}]," +
                "\"rate\":{" +
                    "\"frequency\":[{\"low\":2,\"high\":2,\"frequency\":1},{\"low\":4,\"high\":4,\"frequency\":2}]," +
                    "\"repeat_limit\":[{\"low\":2,\"high\":2,\"repeat_limit\":3},{\"low\":4,\"high\":4,\"repeat_limit\":10}]}";

            var sb = new StringBuilder();
            sb.Append("{\"game\":\"").Append(GameId).Append("\",\"event\":\"").Append(EventId)
              .Append("\",\"min_value\":0,\"max_value\":4,\"icon_id\":35,\"value_optional\":true,\"handlers\":[");

            foreach (int count in ZoneCounts)
                AppendHandler(sb, "rgb-" + count + "-zone", "all", effect);
            AppendHandler(sb, "rgb-per-key-zones", "all", effect);

            sb.Length--; // trailing comma
            sb.Append("]}");
            return sb.ToString();
        }

        private static void AppendHandler(StringBuilder sb, string deviceType, string zone, string effect)
        {
            sb.Append("{\"device-type\":\"").Append(deviceType).Append("\",\"zone\":\"").Append(zone).Append("\",")
              .Append(effect).Append("},");
        }
    }
}
