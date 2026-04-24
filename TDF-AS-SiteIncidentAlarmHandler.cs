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

	using TDFASSiteIncidentAlarmHandler.Models;

	/// <summary>
	/// Represents a DataMiner Automation script.
	/// </summary>
	public class Script
	{

		/// <summary>
		/// Specifies the type of action to perform for the site incident alarm handler.
		/// </summary>
		public enum ActionType
		{
			/// <summary>
			/// Indicates that a new site with an incident has been detected.
			/// </summary>
			NewSiteWithIncident,
			/// <summary>
			/// Indicates that a site already has an incident.
			/// </summary>
			SiteAlreadyHasAnIncident,
			/// <summary>
			/// Indicates that a site no longer has an incident.
			/// </summary>
			SiteNoLongerHasAnIncident,
		}

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
			string actionTypeString = engine.GetScriptParam("ActionType")?.Value ?? string.Empty;
			string targetValue = engine.GetScriptParam("TargetValue")?.Value ?? string.Empty;

			if (string.IsNullOrWhiteSpace(actionTypeString))
			{
				engine.GenerateInformation($"Error: ActionType parameter is required. Valid values: {string.Join(", ", Enum.GetNames(typeof(ActionType)))}");
				return;
			}

			if (string.IsNullOrWhiteSpace(targetValue))
			{
				engine.GenerateInformation($"Error: TargetValue parameter is required for action '{actionTypeString}'.");
				return;
			}

			if (!Enum.TryParse<ActionType>(actionTypeString, true, out ActionType actionType))
			{
				engine.GenerateInformation($"Unknown ActionType: '{actionTypeString}'. Valid values: {string.Join(", ", Enum.GetNames(typeof(ActionType)))}");
				return;
			}

			// Replace the switch expression with a switch statement for C# 7.3 compatibility
			SiteIncident siteIncident;
			switch (actionType)
			{
				case ActionType.NewSiteWithIncident:
					siteIncident = new NewSiteIncident(engine, targetValue);
					break;
				case ActionType.SiteAlreadyHasAnIncident:
					siteIncident = new UpdateSiteIncident(engine, targetValue);
					break;
				case ActionType.SiteNoLongerHasAnIncident:
					siteIncident = new RemoveSiteIncident(engine, targetValue);
					break;
				default:
					throw new ArgumentException($"Unsupported ActionType: '{actionType}'.");
			}
			siteIncident.Process();
		}
	}
}
