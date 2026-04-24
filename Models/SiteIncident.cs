namespace TDFASSiteIncidentAlarmHandler.Models
{
    using Skyline.DataMiner.Automation;
    public abstract class SiteIncident
    {
        protected readonly IEngine engine;

        protected SiteIncident(IEngine engine)
        {
            this.engine = engine;
		}

		public abstract void Process();
	}
}
