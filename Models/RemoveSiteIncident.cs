namespace TDFASSiteIncidentAlarmHandler.Models
{
	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	public class RemoveSiteIncident : SiteIncidentWithIgCode
	{
		public RemoveSiteIncident(IEngine engine, string igCode) : base(engine, igCode)
		{
		}

		public override void Process()
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmsByIgCode();

			if (alarms.Length == 0)
			{
				engine.GenerateInformation($"[RemoveSiteIncident] No active alarms found for IG Code: '{IgCode}'");
				return;
			}

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (alarm == null)
				{
					engine.GenerateInformation($"[RemoveSiteIncident] Encountered null alarm in collection.");
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, alarm);

				if (currentPropertyValue == null)
				{
					engine.GenerateInformation($"[RemoveSiteIncident] Alarm {FormatAlarmId(alarm)} - Property not accessible (alarm might be cleared).");
					continue;
				}

				if (!TryRemoveIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					engine.GenerateInformation($"[RemoveSiteIncident] Alarm {FormatAlarmId(alarm)} - 'INC' tag not present. Current value: '{currentPropertyValue}'. Skipped.");
					continue;
				}

				if (TrySetAlarmProperty(engine, alarm, newPropertyValue))
				{
					engine.GenerateInformation($"[RemoveSiteIncident] Alarm {FormatAlarmId(alarm)} - Removed 'INC' tag. Changed from '{currentPropertyValue}' to '{newPropertyValue}'");
				}
				else
				{
					engine.GenerateInformation($"[RemoveSiteIncident] Alarm {FormatAlarmId(alarm)} - Failed to update property.");
				}
			}
		}
	}
}
