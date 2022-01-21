using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Web.Mvc.Framework.ActionFilters;
using DotNetNuke.Web.Mvc.Framework.Controllers;
using System;
using System.Web.Mvc;
using System.Collections.Generic;
using DotNetNuke.Services.Sitemap;
using DotNetNuke.Entities.Portals;
using System.Globalization;
using Localization = DotNetNuke.Services.Localization.Localization;

namespace WireMayr.Modules.HtmlSitemap.Controllers
{
    [DnnHandleError]
    public class HtmlSitemapController : DnnController
    {

        public ActionResult Index()
        {
            var allUrls = new List<SitemapUrl>();
            var smBuilder = new ReadableSitemapBuilder(this.PortalSettings);
            allUrls = smBuilder.BuildSiteMap();
            return View(allUrls);
        }
    }

    public class ReadableSitemapBuilder : SitemapBuilder
    {
        private const string SITEMAP_VERSION = "0.9";

        private readonly PortalSettings PortalSettings;


        public ReadableSitemapBuilder(PortalSettings ps) : base(ps)
        {
            this.PortalSettings = ps;
        }

        public List<SitemapUrl> BuildSiteMap()
        {
            var allUrls = new List<SitemapUrl>();

            // excluded urls by priority
            float excludePriority = 0;
            excludePriority = float.Parse(PortalController.GetPortalSetting("SitemapExcludePriority", PortalSettings.PortalId, "0"), NumberFormatInfo.InvariantInfo);

            // get all urls
            bool isProviderEnabled = false;
            bool isProviderPriorityOverrided = false;
            float providerPriorityValue = 0;

            foreach (SitemapProvider _provider in this.Providers)
            {
                isProviderEnabled = bool.Parse(PortalController.GetPortalSetting(_provider.Name + "Enabled", this.PortalSettings.PortalId, "True"));

                if (isProviderEnabled)
                {
                    // check if we should override the priorities
                    isProviderPriorityOverrided = bool.Parse(PortalController.GetPortalSetting(_provider.Name + "Override", this.PortalSettings.PortalId, "False"));

                    // stored as an integer (pr * 100) to prevent from translating errors with the decimal point
                    providerPriorityValue = float.Parse(PortalController.GetPortalSetting(_provider.Name + "Value", this.PortalSettings.PortalId, "50")) / 100;

                    // Get all urls from provider
                    List<SitemapUrl> urls = new List<SitemapUrl>();
                    try
                    {
                        urls = _provider.GetUrls(this.PortalSettings.PortalId, this.PortalSettings, SITEMAP_VERSION);
                    }
                    catch (Exception ex)
                    {
                        DotNetNuke.Services.Exceptions.Exceptions.LogException(new Exception(Localization.GetExceptionMessage(
                            "SitemapProviderError",
                            "URL sitemap provider '{0}' failed with error: {1}",
                            _provider.Name, ex.Message)));
                    }

                    foreach (SitemapUrl url in urls)
                    {
                        if (isProviderPriorityOverrided)
                        {
                            url.Priority = providerPriorityValue;
                        }

                        if (url.Priority > 0 && url.Priority >= excludePriority) // #RS# a valid sitemap needs priorities larger then 0, otherwise the sitemap will be rejected by google as invalid
                        {
                            allUrls.Add(url);
                        }
                    }
                }
            }

            if (allUrls.Count > 0)
            {
                return allUrls;
            }
            else return null;
        }
    }
}