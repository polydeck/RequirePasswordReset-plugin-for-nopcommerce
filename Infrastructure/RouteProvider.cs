using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        public int Priority { get { return -1; } }

        public void RegisterRoutes(RouteCollection routes)
        {
            // use custom view engine
            ViewEngines.Engines.Insert(0, new RequirePasswordChangeViewEngine());
        }
    }
}