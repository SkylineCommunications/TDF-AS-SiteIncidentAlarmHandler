namespace TDFASSiteIncidentAlarmHandler.Models
{
	using Skyline.DataMiner.Automation;

	public abstract class SiteIncident
	{
		[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Protected field is required so derived classes can access the IEngine instance.")]
		protected readonly IEngine engine;

		protected SiteIncident(IEngine engine)
		{
			this.engine = engine;
		}

		public abstract void Process();
	}
}
