using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Core;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Web.Controllers;
using Nop.Web.Models.Customer;
using Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Domain;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.ActionFilters
{
    /// <summary>
    /// Redirect customer to password change page if the custom customer 
    /// attribute "RequirePasswordChange" is set to true.
    /// </summary>
    /// <seealso cref="System.Web.Mvc.ActionFilterAttribute" />
    /// <seealso cref="System.Web.Mvc.IFilterProvider" />
    public class CustomerLoginActionFilter : ActionFilterAttribute, IFilterProvider
    {
        #region Fields

        private const string CustomerControllerName = "Customer";
        private const string LoginActionName = "Login";

        #endregion

        #region Methods

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var filters = new List<Filter>();

            if (IsCustomerLoginAction(actionDescriptor, controllerContext?.HttpContext))
                filters.Add(new Filter(this, FilterScope.Action, 0));

            return filters;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            base.OnActionExecuted(filterContext);
            OnCustomerLoginActionExecuted(filterContext);
        }

        private void OnCustomerLoginActionExecuted(ActionExecutedContext filterContext)
        {
            // get parameters sent to action
            var email = filterContext.Controller.ValueProvider.GetValue("Email")?.AttemptedValue;
            var username = filterContext.Controller.ValueProvider.GetValue("UserName")?.AttemptedValue;
            var returnUrl = filterContext.Controller.ValueProvider.GetValue("returnUrl")?.AttemptedValue;

            // Validate action result indicates successful login
            // On successful login, a redirect action is returned. On an 
            // unsuccessful login, a view is returned.
            bool isExpectedResult = (filterContext.Result as ViewResult) == null;
            if (!isExpectedResult)
                return;

            // Resolve dependencies
            var customerController = (CustomerController)filterContext.Controller;
            var customerAttributeParser = EngineContext.Current.ContainerManager.Resolve<ICustomerAttributeParser>();
            var customerAttributeService = EngineContext.Current.ContainerManager.Resolve<ICustomerAttributeService>();
            var customerService = EngineContext.Current.ContainerManager.Resolve<ICustomerService>();
            var customerSettings = EngineContext.Current.ContainerManager.Resolve<CustomerSettings>();
            var genericAttributeService = EngineContext.Current.ContainerManager.Resolve<IGenericAttributeService>();

            // Get the current customer
            var customer = customerSettings.UsernamesEnabled
                ? customerService.GetCustomerByUsername(username)
                : customerService.GetCustomerByEmail(email);

            if (customer == null)
                return;

            // Get the custom customer attributes for the require password change flag
            var requirePasswordChangeAttributeId = customerAttributeService
                .GetAllCustomerAttributes()
                .FirstOrDefault(attr => attr.Name == RequirePasswordChangePluginCustomerAttributeNames.RequiredPasswordChange)
                ?.Id;

            if (!requirePasswordChangeAttributeId.HasValue)
                return;

            // Get custom customer attributes xml field
            var attributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CustomCustomerAttributes);

            if (string.IsNullOrWhiteSpace(attributesXml))
                return;

            // Check to see if customer requires a password change
            var customerAttributeValues = customerAttributeParser.ParseCustomerAttributeValues(attributesXml);
            var passwordChangeIsRequired = customerAttributeValues
                .Any(x =>
                    x.CustomerAttributeId == requirePasswordChangeAttributeId &&
                    x.Name == RequirePasswordChangePluginCustomerAttributeValueNames.RequiredPasswordChangeYes);

            if (passwordChangeIsRequired)
            {
                // get info customer needs to reset pass
                var token = customer.GetAttribute<string>(SystemCustomerAttributeNames.PasswordRecoveryToken);
                if (string.IsNullOrWhiteSpace(token))
                {
                    token = Guid.NewGuid().ToString();
                    genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.PasswordRecoveryToken, token);
                }

                // log customer out
                customerController.Logout();

                // redirect customer to password recovery page
                filterContext.Result = new RedirectToRouteResult(
                    "PasswordRecoveryConfirm",
                    new RouteValueDictionary(new { token = token, email = email, returnUrl = returnUrl }));
            }
        }

        #endregion

        #region Utilities

        private bool IsCustomerLoginAction(ActionDescriptor actionDescriptor, HttpContextBase httpContext)
        {
            var controllerName = actionDescriptor.ControllerDescriptor.ControllerName;
            var actionName = actionDescriptor.ActionName;
            var method = httpContext?.Request?.HttpMethod;

            bool isCustomerController = String.Equals(controllerName, CustomerControllerName, StringComparison.InvariantCultureIgnoreCase);
            bool isLoginAction = String.Equals(actionName, LoginActionName, StringComparison.InvariantCultureIgnoreCase);
            bool isPost = String.Equals(method, "POST", StringComparison.InvariantCultureIgnoreCase);

            return isCustomerController && isLoginAction && isPost;
        }

        #endregion
    }
}