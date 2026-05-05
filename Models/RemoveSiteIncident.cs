namespace TDFASSiteIncidentAlarmHandler.Models
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	public class RemoveSiteIncident : SiteIncidentWithIgCode
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:Field names should not use Hungarian notation", Justification = "Hungarian notation was not intended")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
		public RemoveSiteIncident(IEngine engine, string igCode) : base(engine, igCode)
		{
		}

		public override void Process()
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmsByIgCode();

			if (alarms.Length == 0)
			{
				return;
			}

			var updatedRootAlarmIds = new HashSet<string>();

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (alarm == null)
				{
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, alarm);

				if (currentPropertyValue == null)
				{
					continue;
				}

				if (!TryRemoveIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					continue;
				}

				if (TrySetAlarmProperty(engine, alarm, newPropertyValue))
				{
					string rootAlarmId = FormatRootAlarmId(alarm);
					updatedRootAlarmIds.Add(rootAlarmId);
				}
			}

			if (updatedRootAlarmIds.Count > 0)
			{
				string alarmList = string.Join("; ", updatedRootAlarmIds);
				engine.GenerateInformation($"[RemoveSiteIncident] {updatedRootAlarmIds.Count} alarm(s) with IG Code '{IgCode}' have been updated. Root Alarm IDs: {alarmList}");
			}
		}
	}
}
