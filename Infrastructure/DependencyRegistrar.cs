using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Autofac;
using Nop.Core.Configuration;
using Nop.Core.Infrastructure;
using Nop.Core.Infrastructure.DependencyManagement;
using Polydeck.Nop.Plugin.Misc.RequirePasswordChange.ActionFilters;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Infrastructure
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
        public int Order { get { return 1; } }

        public void Register(ContainerBuilder builder, ITypeFinder typeFinder, NopConfig config)
        {
            #region register action filters

            builder.RegisterType<CustomerInfoActionFilter>()
                .As<IFilterProvider>()
                .InstancePerRequest();

            builder.RegisterType<CustomerLoginActionFilter>()
                .As<IFilterProvider>()
                .InstancePerRequest();

            builder.RegisterType<CustomerPasswordRecoveryConfirmActionFilter>()
                .As<IFilterProvider>()
                .InstancePerRequest();

            #endregion
        }
    }
}