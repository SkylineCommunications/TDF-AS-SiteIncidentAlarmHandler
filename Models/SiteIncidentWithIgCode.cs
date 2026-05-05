namespace TDFASSiteIncidentAlarmHandler.Models
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Filters;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
	public abstract class SiteIncidentWithIgCode : SiteIncident
	{
		protected const string PropertyName = "SiteActivities";
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
		protected const string PropertyIgCodeNameFilter = "Alarm.IG Code";
		protected const string IncidentTag = "INC";

		[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.NamingRules", "SA1305:Field names should not use Hungarian notation", Justification = "Hungarian notation was not intended")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
		protected SiteIncidentWithIgCode(IEngine engine, string igCode) : base(engine)
		{
			IgCode = igCode;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
		public string IgCode { get; }

		protected static string TryGetAlarmProperty(IEngine engine, AlarmEventMessage alarm)
		{
			try
			{
				return engine.GetAlarmProperty(alarm.DataMinerID, alarm.ElementID, alarm.AlarmID, PropertyName);
			}
			catch (ArgumentException)
			{
				// Alarm is no longer there / cleared in the meantime.
				return null;
			}
			catch (Exception)
			{
				// Other exceptions
				return null;
			}
		}

		protected static bool TryAddIncidentTag(string currentValue, out string newValue)
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

		protected static bool TrySetAlarmProperty(IEngine engine, AlarmEventMessage alarm, string newValue)
		{
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

		protected static bool TryRemoveIncidentTag(string currentValue, out string newValue)
		{
			newValue = currentValue;

			if (string.IsNullOrWhiteSpace(currentValue))
			{
				return false;
			}

			var tags = currentValue
				.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(t => t.Trim())
				.ToArray();

			if (!tags.Any(t => t.Equals(IncidentTag, StringComparison.OrdinalIgnoreCase)))
			{
				return false;
			}

			var filtered = tags
				.Where(t => !t.Equals(IncidentTag, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			newValue = filtered.Length > 0 ? string.Join(";", filtered) : string.Empty;
			return true;
		}

		protected static string FormatRootAlarmId(AlarmEventMessage alarm)
		{
			if (alarm == null)
				return "null";

			int rootAlarmId = alarm.TreeID?.RootAlarmID ?? 0;
			return $"{alarm.DataMinerID}/{alarm.ElementID}/{rootAlarmId}";
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "VSSpell001:Spell Check", Justification = "Ig is a domain-specific abbreviation and is intentionally named as such.")]
		protected AlarmEventMessage[] GetFilteredAlarmsByIgCode()
		{
			var alarmFilterItem = new AlarmFilterItemString(AlarmFilterField.PropertyValue, PropertyIgCodeNameFilter, AlarmFilterCompareType.Equality, new[] { IgCode });
			var message = new GetActiveAlarmsMessage(-1)
			{
				Filter = new AlarmFilter(alarmFilterItem),
			};

			if (engine.SendSLNetSingleResponseMessage(message) is ActiveAlarmsResponseMessage alarmsResponse)
			{
				return alarmsResponse.ActiveAlarms.WhereNotNull().ToArray();
			}

			return Array.Empty<AlarmEventMessage>();
		}

	}
}
