namespace TDFASSiteIncidentAlarmHandler.Models
{
	using System.Collections.Generic;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Messages;

	public class NewSiteIncident : SiteIncidentWithIgCode
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:Field names should not use Hungarian notation", Justification = "Hungarian notation was not intended")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
		public NewSiteIncident(IEngine engine, string igCode) : base(engine, igCode)
		{
			// Constructor delegates to base class - no additional setup needed
		}

		public override void Process()
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmsByIgCode();

			if (alarms.Length == 0)
			{
				return;
			}

			// HashSet to track unique Root Alarm IDs (no duplicates)
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

				if (!TryAddIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					// Already has INC tag - skip
					continue;
				}

				// Update the property
				if (TrySetAlarmProperty(engine, alarm, newPropertyValue))
				{
					string rootAlarmId = FormatRootAlarmId(alarm);
					updatedRootAlarmIds.Add(rootAlarmId);
				}
			}

			// Summary log at the end
			if (updatedRootAlarmIds.Count > 0)
			{
				string alarmList = string.Join("; ", updatedRootAlarmIds);
				engine.GenerateInformation($"[NewSiteIncident] {updatedRootAlarmIds.Count} alarm(s) with IG Code '{IgCode}' have been updated. Root Alarm IDs: {alarmList}");
			}
		}
	}
}
