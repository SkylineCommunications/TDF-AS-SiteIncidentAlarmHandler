/*
***********************************************
*  Copyright (c), Skyline Communications NV.  *
***********************************************

Revision History:

DATE		VERSION		AUTHOR			COMMENTS

07. 04. 2026	1.0.0.1		LPA, Skyline	Initial version
****************************************************************************
*/

namespace TDFASSiteIncidentAlarmHandler
{
	using System;
	using System.Linq;

	using Skyline.DataMiner.Automation;
	using Skyline.DataMiner.Net.Filters;
	using Skyline.DataMiner.Net.Helper;
	using Skyline.DataMiner.Net.Messages;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
    {

		private static readonly string PropertyName = "SiteActivities";
		private static readonly string PropertyIgCodeNameFilter = "Alarm.IG Code";
		private static readonly string IncidentTag = "INC";

		// ActionType constants
		private static readonly string ActionNewSiteWithIncident = "NewSiteWithIncident";
		private static readonly string ActionSiteAlreadyHasAnIncident = "SiteAlreadyHasAnIncident";
		private static readonly string ActionSiteNoLongerHasAnIncident = "SiteNoLongerHasAnIncident";

		/// <summary>
		/// The script entry point.
		/// </summary>
		/// <param name="engine">Link with SLAutomation process.</param>
		public void Run(IEngine engine)
        {
            try
            {
                RunSafe(engine);
            }
            catch (ScriptAbortException)
            {
                // Catch normal abort exceptions (engine.ExitFail or engine.ExitSuccess)
                throw; // Comment if it should be treated as a normal exit of the script.
            }
            catch (ScriptForceAbortException)
            {
                // Catch forced abort exceptions, caused via external maintenance messages.
                throw;
            }
            catch (ScriptTimeoutException)
            {
                // Catch timeout exceptions for when a script has been running for too long.
                throw;
            }
            catch (InteractiveUserDetachedException)
            {
                // Catch a user detaching from the interactive script by closing the window.
                // Only applicable for interactive scripts, can be removed for non-interactive scripts.
                throw;
            }
            catch (Exception e)
            {
                engine.ExitFail("Run|Something went wrong: " + e);
            }
        }

        private void RunSafe(IEngine engine)
        {
			string actionType = engine.GetScriptParam("ActionType")?.Value ?? string.Empty;
			string targetValue = engine.GetScriptParam("TargetValue")?.Value ?? string.Empty;

			//Validate inputs
			if (string.IsNullOrWhiteSpace(actionType))
			{
				engine.GenerateInformation("Error: ActionType parameter is required. Valid values: 'NewSiteWithIncident', 'SiteAlreadyHasAnIncident', 'SiteNoLongerHasAnIncident'.");
				return;
			}

			if (string.IsNullOrWhiteSpace(targetValue))
			{
				engine.GenerateInformation($"Error: TargetValue parameter is required for action '{actionType}'.");
				return;
			}

			if (actionType.Equals(ActionNewSiteWithIncident, StringComparison.OrdinalIgnoreCase))
			{
				// Scenario 1: New Site with Incidents
				// TargetValue is interpreted as IG Code
				ProcessNewSiteWithIncident(engine, targetValue);
			}
			else if (actionType.Equals(ActionSiteAlreadyHasAnIncident, StringComparison.OrdinalIgnoreCase))
			{
				// Scenario 2: Site Already has an Incident
				// TargetValue is interpreted as AlarmId
				ProcessSiteAlreadyHasAnIncident(engine, targetValue);
			}
			else if (actionType.Equals(ActionSiteNoLongerHasAnIncident, StringComparison.OrdinalIgnoreCase))
			{
				// Scenario 3: Site No Longer has Incidents
				// TargetValue is interpreted as IG Code
				ProcessSiteNoLongerHasAnIncident(engine, targetValue);
			}
			else
			{
				engine.GenerateInformation($"Unknown ActionType: '{actionType}'. Valid values: '{ActionNewSiteWithIncident}', '{ActionSiteAlreadyHasAnIncident}', '{ActionSiteNoLongerHasAnIncident}'.");
			}
		}

		private void ProcessNewSiteWithIncident(IEngine engine, string entryParamIgCode)
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmsByIgCode(engine, entryParamIgCode);

			if (alarms == null || alarms.Length == 0)
			{
				engine.GenerateInformation($"[{ActionNewSiteWithIncident}] No active alarms found for IG Code: '{entryParamIgCode}'");
				return;
			}

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (!TryParseAlarmIdsFromMessage(alarm, out int dmaId, out int elementId, out int rootAlarmId, out int alarmId))
				{
					engine.GenerateInformation($"[{ActionNewSiteWithIncident}] Could not parse alarm IDs from alarm message.");
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, dmaId, elementId, alarmId);

				if (currentPropertyValue == null)
				{
					engine.GenerateInformation($"[{ActionNewSiteWithIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Property not accessible (alarm might be cleared).");
					continue;
				}

				if (!TryAddIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					engine.GenerateInformation($"[{ActionNewSiteWithIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - 'INC' tag is already present. Current value: '{currentPropertyValue}'. Skipped.");
					continue;
				}

				if (TrySetAlarmProperty(engine, dmaId, elementId, alarmId, newPropertyValue))
				{
					engine.GenerateInformation($"[{ActionNewSiteWithIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Added 'INC' tag. Changed from '{currentPropertyValue}' to '{newPropertyValue}'");
				}
				else
				{
					engine.GenerateInformation($"[{ActionNewSiteWithIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Failed to update property.");
				}
			}
		}

		private void ProcessSiteAlreadyHasAnIncident(IEngine engine, string entryParamAlarmId)
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmByAlarmID(engine, entryParamAlarmId);

			if(alarms == null || alarms.Length == 0)
			{
				engine.GenerateInformation($"[{ActionSiteAlreadyHasAnIncident}] No active alarms found for Alarm ID: '{entryParamAlarmId}'");
				return;
			}

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (!TryParseAlarmIdsFromMessage(alarm, out int dmaId, out int elementId, out int rootAlarmId, out int alarmId))
				{
					engine.GenerateInformation($"[{ActionSiteAlreadyHasAnIncident}] Could not parse alarm IDs from alarm message.");
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, dmaId, elementId, alarmId);

				if (currentPropertyValue == null)
				{
					engine.GenerateInformation($"[{ActionSiteAlreadyHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Property not accessible (alarm might be cleared).");
					continue;
				}

				if (!TryAddIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					engine.GenerateInformation($"[{ActionSiteAlreadyHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - 'INC' tag is already present. Current value: '{currentPropertyValue}'. Skipped.");
					continue;
				}

				if (TrySetAlarmProperty(engine, dmaId, elementId, alarmId, newPropertyValue))
				{
					engine.GenerateInformation($"[{ActionSiteAlreadyHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Added 'INC' tag. Changed from '{currentPropertyValue}' to '{newPropertyValue}'");
				}
				else
				{
					engine.GenerateInformation($"[{ActionSiteAlreadyHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Failed to update property.");
				}
			}				
		}

		private void ProcessSiteNoLongerHasAnIncident(IEngine engine, string entryParamIgCode)
		{
			AlarmEventMessage[] alarms = GetFilteredAlarmsByIgCode(engine, entryParamIgCode);

			if (alarms == null || alarms.Length == 0)
			{
				engine.GenerateInformation($"[{ActionSiteNoLongerHasAnIncident}] No active alarms found for IG Code: '{entryParamIgCode}'");
				return;
			}

			foreach (AlarmEventMessage alarm in alarms)
			{
				if (!TryParseAlarmIdsFromMessage(alarm, out int dmaId, out int elementId, out int rootAlarmId, out int alarmId))
				{
					engine.GenerateInformation($"[{ActionSiteNoLongerHasAnIncident}] Could not parse alarm IDs from alarm message.");
					continue;
				}

				string currentPropertyValue = TryGetAlarmProperty(engine, dmaId, elementId, alarmId);

				if (currentPropertyValue == null)
				{
					engine.GenerateInformation($"[{ActionSiteNoLongerHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Property not accessible (alarm might be cleared).");
					continue;
				}

				if (!TryRemoveIncidentTag(currentPropertyValue, out string newPropertyValue))
				{
					engine.GenerateInformation($"[{ActionSiteNoLongerHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - 'INC' tag not present. Current value: '{currentPropertyValue}'. Skipped.");
					continue;
				}

				if (TrySetAlarmProperty(engine, dmaId, elementId, alarmId, newPropertyValue))
				{
					engine.GenerateInformation($"[{ActionSiteNoLongerHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Removed 'INC' tag. Changed from '{currentPropertyValue}' to '{newPropertyValue}'");
				}
				else
				{
					engine.GenerateInformation($"[{ActionSiteNoLongerHasAnIncident}] Alarm {dmaId}/{elementId}/{rootAlarmId}/{alarmId} - Failed to update property.");
				}
			}
		}

		private static AlarmEventMessage[] GetFilteredAlarmsByIgCode(IEngine engine, string igCode)
		{
			var alarmFilterItem = new AlarmFilterItemString(AlarmFilterField.PropertyValue, PropertyIgCodeNameFilter, AlarmFilterCompareType.Equality, new[] { igCode });
			var message = new GetActiveAlarmsMessage(-1)
			{
				Filter = new AlarmFilter(alarmFilterItem),
			};

			var alarmsResponse = engine.SendSLNetSingleResponseMessage(message) as ActiveAlarmsResponseMessage;
			if (alarmsResponse != null)
			{
				return alarmsResponse.ActiveAlarms.WhereNotNull().ToArray();
			}

			return Array.Empty<AlarmEventMessage>();
		}

		private static AlarmEventMessage[] GetFilteredAlarmByAlarmID(IEngine engine, string alarmId)
		{
			//var alarmFilterItem = new AlarmFilterItemString(AlarmFilterField.AlarmID,AlarmFilterCompareType.Equality, new[] { dmaId + "/" + alarmId });
			var message = new GetActiveAlarmsMessage();

			var alarmsResponse = engine.SendSLNetSingleResponseMessage(message) as ActiveAlarmsResponseMessage;

			if (alarmsResponse != null)
			{
				return alarmsResponse.ActiveAlarms.WhereNotNull().Where(x => x.AlarmID.ToString().Contains(alarmId)).ToArray();
			}

			return Array.Empty<AlarmEventMessage>();
		}

		private static string TryGetAlarmProperty(IEngine engine, int dmaId, int elementId, int alarmId)
		{
			try
			{
				return engine.GetAlarmProperty(dmaId, elementId, alarmId, PropertyName);
			}
			catch (ArgumentException)
			{
				// Alarm is no longer there / cleared in the meantime.
				return null;
			}
		}
		private static bool TrySetAlarmProperty(IEngine engine, int dmaId, int elementId, int alarmId, string newValue)
		{
			try
			{
				engine.SetAlarmProperty(dmaId, elementId, alarmId, PropertyName, newValue);
				return true;
			}
			catch (ArgumentException)
			{
				// Alarm is no longer there / already cleared in the meantime.
				return false;
			}
			catch (Exception)
			{
				// Other exceptions
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

		private static bool TryRemoveIncidentTag(string currentValue, out string newValue)
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

		private static bool TryParseAlarmIdsFromMessage(AlarmEventMessage alarm, out int dmaId, out int elementId, out int rootAlarmId, out int alarmId)
		{
			dmaId = 0;
			elementId = 0;
			rootAlarmId = 0;
			alarmId = 0;

			if (alarm == null)
			{
				return false;
			}

			// Directly use properties from AlarmEventMessage object instead of parsing response string
			dmaId = alarm.DataMinerID;
			elementId = alarm.ElementID;
			rootAlarmId = alarm.TreeID.RootAlarmID;
			alarmId = alarm.AlarmID;

			return true;
		}
	}
}
