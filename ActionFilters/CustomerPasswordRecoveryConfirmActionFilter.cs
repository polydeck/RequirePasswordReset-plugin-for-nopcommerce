using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Nop.Core.Domain.Customers;
using Nop.Core.Infrastructure;
using Nop.Services.Common;
using Nop.Services.Customers;
using Nop.Services.Localization;
using Nop.Web.Controllers;
using Nop.Web.Models.Customer;
using Polydeck.Nop.Plugin.Misc.RequirePasswordChange.Domain;

namespace Polydeck.Nop.Plugin.Misc.RequirePasswordChange.ActionFilters
{
    public class CustomerPasswordRecoveryConfirmActionFilter : ActionFilterAttribute, IFilterProvider
    {
        #region Fields

        // see Nop.Web.Controllers.ShoppingCartController.cs
        private const string CustomerControllerName = "Customer";
        private const string PasswordRecoveryConfirmActionName = "PasswordRecoveryConfirm";

        // Initializing services per ActionFilter causes them to throw DbContext
        // not initialized exceptions. Instead, initialize them in the methods 
        // where they are to be used. E.g.
        // var invoiceItemService = EngineContext.Current.ContainerManager.Resolve<IInvoiceItemService>();
        // Don't do this at the class Level:
        // private readonly IOrderService _orderService = EngineContext.Current.Resolve<IOrderService>();
        // private readonly IInvoiceItemService _invoiceItemService = EngineContext.Current.Resolve<IInvoiceItemService>();

        #endregion

        #region Methods

        public IEnumerable<Filter> GetFilters(ControllerContext controllerContext, ActionDescriptor actionDescriptor)
        {
            var filters = new List<Filter>();

            if (IsPasswordRecoveryConfirmAction(actionDescriptor, controllerContext?.HttpContext))
                filters.Add(new Filter(this, FilterScope.Action, 0));

            return filters;
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (IsPasswordRecoveryConfirmAction(filterContext.ActionDescriptor, filterContext.HttpContext))
                OnPasswordRecoveryConfirmActionExecuted(filterContext);
        }

        public void OnPasswordRecoveryConfirmActionExecuted(ActionExecutedContext filterContext)
        {
            // Resolve dependencies
            var customerController = (CustomerController)filterContext.Controller;
            var localizationService = EngineContext.Current.ContainerManager.Resolve<ILocalizationService>();
            var customerService = EngineContext.Current.ContainerManager.Resolve<ICustomerService>();
            var customerAttributeService = EngineContext.Current.ContainerManager.Resolve<ICustomerAttributeService>();
            var customerAttributeParser = EngineContext.Current.ContainerManager.Resolve<ICustomerAttributeParser>();
            var genericAttributeService = EngineContext.Current.ContainerManager.Resolve<IGenericAttributeService>();

            #region Validate action result indicates successful password change

            var validResult = localizationService.GetResource("Account.PasswordRecovery.PasswordHasBeenChanged");
            var viewResult = filterContext.Result as ViewResult;
            var resultModel = viewResult?.Model as PasswordRecoveryConfirmModel;
            var isExpectedResult = String.Equals(resultModel?.Result, validResult);
            if (!isExpectedResult)
                return;

            #endregion

            // get parameters sent to action
            var customerEmail = filterContext.Controller.ValueProvider.GetValue("email")?.AttemptedValue;
            var newPassword = filterContext.Controller.ValueProvider.GetValue("NewPassword")?.AttemptedValue;
            var returnUrl = filterContext.Controller.ValueProvider.GetValue("returnUrl")?.AttemptedValue;

            // Get customer
            var customer = customerService.GetCustomerByEmail(customerEmail);
            if (customer == null)
                throw new InvalidOperationException($"Coud not find customer with email \"{customerEmail}\"");

            #region Set custom customer attribute xml

            // Get customer attributes & attribute values
            var requirePasswordChangeAttribute = customerAttributeService
                .GetAllCustomerAttributes()
                .First(attr => attr.Name == RequirePasswordChangePluginCustomerAttributeNames.RequiredPasswordChange);

            var requirePasswordChangeNoAttributeValue = requirePasswordChangeAttribute
                .CustomerAttributeValues
                .First(attrValue => attrValue.Name == RequirePasswordChangePluginCustomerAttributeValueNames.RequiredPasswordChangeNo);

            // Update customer's attributes
            var attributesXml = customer.GetAttribute<string>(SystemCustomerAttributeNames.CustomCustomerAttributes);
            attributesXml = customerAttributeParser.AddOrUpdateCustomerAttribute(
                attributesXml,
                requirePasswordChangeAttribute,
                requirePasswordChangeNoAttributeValue.Id.ToString());

            genericAttributeService.SaveAttribute<string>(customer, SystemCustomerAttributeNames.CustomCustomerAttributes, attributesXml);

            #endregion

            // log customer in
            var loginActionResult = customerController.Login(
                model: new LoginModel()
                {
                    Email = customer.Email,
                    Username = customer.Username,
                    Password = newPassword,
                    RememberMe = false,
                },
                returnUrl: returnUrl,
                captchaValid: true);

            filterContext.Result = loginActionResult;
        }

        #endregion Methods

        #region Utilities

        private bool IsPasswordRecoveryConfirmAction(ActionDescriptor actionDescriptor, HttpContextBase httpContext)
        {
            var controllerName = actionDescriptor.ControllerDescriptor.ControllerName;
            var actionName = actionDescriptor.ActionName;
            var method = httpContext?.Request?.HttpMethod;

            bool isCustomerController = String.Equals(controllerName, CustomerControllerName, StringComparison.InvariantCultureIgnoreCase);
            bool isPasswordRecoveryConfirmPOSTAction = String.Equals(actionName, PasswordRecoveryConfirmActionName, StringComparison.InvariantCultureIgnoreCase);
            bool isPost = String.Equals(method, "POST", StringComparison.InvariantCultureIgnoreCase);

            return isCustomerController && isPasswordRecoveryConfirmPOSTAction && isPost;
        }

        #endregion


    }
}