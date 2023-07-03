using Newtonsoft.Json.Linq;
using SUMS_Agent.Models;

namespace SUMS_Agent.Data
{
    public class RegistryEvent
    {
        public string id;
        public DateTime timeStamp;
        public string action;
        public string appHexDigest;
        public string appName;
        public string versionTag;
        public RegistryEvent(JObject keyValuePairs, TCUContext tcuContext)
        {
            id = keyValuePairs["id"]?.Value<string>() ?? string.Empty;
            var dateString = keyValuePairs["timestamp"]?.Value<string>() ?? string.Empty;
            if (dateString == string.Empty)
                timeStamp = DateTime.Now;
            else
                timeStamp = DateTime.Parse(dateString);


            action = keyValuePairs["action"]?.Value<string>() ?? string.Empty;

            JObject? targetSetings = keyValuePairs["target"]?.Value<JObject>();
            if (targetSetings == null)
            {
                appHexDigest = string.Empty;
                appName = string.Empty;
                versionTag = string.Empty;
            }
            else
            {
                appHexDigest = targetSetings["action"]?.Value<string>() ?? string.Empty;
                appName = targetSetings["repository"]?.Value<string>() ?? string.Empty;
                versionTag = targetSetings["tag"]?.Value<string>() ?? string.Empty;
                if (action == "push")
                {
                    bool isUpdate = (from _app in tcuContext.Apps
                                     where _app.Repo == appName
                                     && _app.Tag == versionTag
                                     select _app).Any();
                    action = isUpdate ? "update" : "push";
                }
            }
        }

        public static List<RegistryEvent> ParseNotification(JObject keyValuePairs, TCUContext tcuContext)
        {
            var eventsJson = keyValuePairs["events"]?.Values<JObject>();
            if (eventsJson == null)
                return new List<RegistryEvent>();
            List<RegistryEvent> events = (from _event in eventsJson select new RegistryEvent(_event, tcuContext)).ToList();
            return events;
        }

        public override string ToString()
        {
            return action switch
            {
                "pull" => "Application " + appName + " version " + versionTag + " Has been downloaded",
                "push" => "Application " + appName + " version " + versionTag + " Has been published",
                "update" => "Application " + appName + " version " + versionTag + " Has been updated",
                _ => "Another action occurred",
            };
        }
    }
}
