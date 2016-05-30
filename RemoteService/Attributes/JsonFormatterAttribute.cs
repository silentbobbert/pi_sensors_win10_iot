using System;
using System.Web.Http.Controllers;

namespace RemoteService.Attributes
{
    public class JsonFormatterAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings settings,
            HttpControllerDescriptor descriptor)
        {
            // Clear the formatters list.
            settings.Formatters.Clear();

            // Add a custom media-type formatter.
            settings.Formatters.Add(new BrowserJsonFormatter());
        }
    }
}