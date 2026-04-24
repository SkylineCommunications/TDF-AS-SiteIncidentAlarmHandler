using Skyline.DataMiner.Automation;
using Skyline.DataMiner.Net.Messages;

namespace TDFASSiteIncidentAlarmHandler.Models
{
    public class NewSiteIncident : SiteIncidentWithIgCode
	{
		public NewSiteIncident(IEngine engine, string igCode) : base(engine, igCode)
		{
			// Constructor delegates to base class - no additional setup needed
		}

		public override void Process()
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmsByIgCode();

			if (alarms.Length == 0)
			{
				engine.GenerateInformation($"[NewSiteIncident] No active alarms found for IG Code: '{IgCode}'");
				return;
			}

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (alarm == null)
				{
					engine.GenerateInformation($"[NewSiteIncident] Encountered null alarm in collection.");
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, alarm);

				if (currentPropertyValue == null)
				{
					engine.GenerateInformation($"[NewSiteIncident] Alarm {FormatAlarmId(alarm)} - Property not accessible (alarm might be cleared).");
					continue;
				}

				if (!TryAddIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					// Already has INC tag - skip
					engine.GenerateInformation($"[NewSiteIncident] Alarm {FormatAlarmId(alarm)} - 'INC' tag is already present. Current value: '{currentPropertyValue}'. Skipped.");
					continue;
				}

				// Update the property
				if (TrySetAlarmProperty(engine, alarm, newPropertyValue))
				{
					engine.GenerateInformation($"[NewSiteIncident] Alarm {FormatAlarmId(alarm)} - Added 'INC' tag. Changed from '{currentPropertyValue}' to '{newPropertyValue}'");
				}
				else
				{
					engine.GenerateInformation($"[NewSiteIncident] Alarm {FormatAlarmId(alarm)} - Failed to update property.");
				}
			}
		}
	}
}
