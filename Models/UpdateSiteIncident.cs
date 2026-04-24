namespace TDFASSiteIncidentAlarmHandler.Models
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;

	public class UpdateSiteIncident : SiteIncident
	{
		private const string PropertyName = "SiteActivities";
		private const string IncidentTag = "INC";

		public UpdateSiteIncident(IEngine engine, string alarmId) : base(engine)
		{
			AlarmId = alarmId;
		}

		public string AlarmId { get; }

		public override void Process()
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmByAlarmID();

			if (alarms.Length == 0)
			{
				engine.GenerateInformation($"[UpdateSiteIncident] No active alarms found for Alarm ID: '{AlarmId}'");
				return;
			}

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (alarm == null)
				{
					engine.GenerateInformation($"[UpdateSiteIncident] Encountered null alarm in collection.");
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, alarm);

				if (currentPropertyValue == null)
				{
					engine.GenerateInformation($"[UpdateSiteIncident] Alarm {FormatAlarmId(alarm)} - Property not accessible (alarm might be cleared).");
					continue;
				}

				if (!TryAddIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					engine.GenerateInformation($"[UpdateSiteIncident] Alarm {FormatAlarmId(alarm)} - 'INC' tag is already present. Current value: '{currentPropertyValue}'. Skipped.");
					continue;
				}

				if (TrySetAlarmProperty(engine, alarm, newPropertyValue))
				{
					engine.GenerateInformation($"[UpdateSiteIncident] Alarm {FormatAlarmId(alarm)} - Added 'INC' tag. Changed from '{currentPropertyValue}' to '{newPropertyValue}'");
				}
				else
				{
					engine.GenerateInformation($"[UpdateSiteIncident] Alarm {FormatAlarmId(alarm)} - Failed to update property.");
				}
			}
		}

		private static string TryGetAlarmProperty(IEngine engine, AlarmEventMessage alarm)
		{
			if (alarm == null)
			{
				return null;
			}

			try
			{
				return engine.GetAlarmProperty(alarm.DataMinerID, alarm.ElementID, alarm.AlarmID, PropertyName);
			}
			catch (ArgumentException)
			{
				return null;
			}
			catch (Exception)
			{
				return null;
			}
		}

		private static bool TrySetAlarmProperty(IEngine engine, AlarmEventMessage alarm, string newValue)
		{
			if (alarm == null)
			{
				return false;
			}

			try
			{
				engine.SetAlarmProperty(alarm.DataMinerID, alarm.ElementID, alarm.AlarmID, PropertyName, newValue);
				return true;
			}
			catch (ArgumentException)
			{
				return false;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool TryAddIncidentTag(string currentValue, out string newValue)
		{
			newValue = currentValue;

			if (string.IsNullOrWhiteSpace(currentValue))
			{
				newValue = IncidentTag;
				return true;
			}

			var tags = currentValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

			if (tags.Any(t => t.Trim().Equals(IncidentTag, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			newValue = currentValue + ";" + IncidentTag;
			return true;
		}

		private static string FormatAlarmId(AlarmEventMessage alarm)
		{
			if (alarm == null)
			{
				return "null";
			}

			int rootAlarmId = alarm.TreeID?.RootAlarmID ?? 0;
			return $"{alarm.DataMinerID}/{alarm.ElementID}/{rootAlarmId}/{alarm.AlarmID}";
		}

		private AlarmEventMessage[] GetFilteredAlarmByAlarmID()
		{
			if (!int.TryParse(AlarmId, out int targetAlarmId))
			{
				return Array.Empty<AlarmEventMessage>();
			}

			var message = new GetActiveAlarmsMessage(-1);

			if (engine.SendSLNetSingleResponseMessage(message) is ActiveAlarmsResponseMessage alarmsResponse)
			{
				return alarmsResponse.ActiveAlarms
					.WhereNotNull()
					.Where(x => x.AlarmID == targetAlarmId)
					.ToArray();
			}

			return Array.Empty<AlarmEventMessage>();
		}
	}
}
