using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Nop.Web.Framework.Themes;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Infrastructure
{
    public class RequirePasswordChangeViewEngine : ThemeableRazorViewEngine
    {
        public RequirePasswordChangeViewEngine()
        {
            ViewLocationFormats = new[] { "~/Plugins/Misc.RequirePasswordChange/Views/{0}.cshtml" };
            PartialViewLocationFormats = new[] { "~/Plugins/Misc.RequirePasswordChange/Views/{0}.cshtml" };
        }
    }
}