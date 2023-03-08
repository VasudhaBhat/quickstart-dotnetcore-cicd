using MediatR;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Utilities.Email.EmailSender.Events;

namespace OpenSoftware.OidcTemplate.Auth.Controllers
{
    [Route("error")]
    public class ErrorController : Controller
    {
        private readonly ILogger<ErrorController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IMediator _mediator;
        public ErrorController(ILogger<ErrorController> logger, IConfiguration configuration, IMediator mediator)
        {
            _logger = logger;
            _configuration = configuration;
            _mediator = mediator;
        }

        [Route("500")]
        public async Task<IActionResult> AppError()
        {
            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            _logger.LogCritical($"A critical system exception occurred.\n " +
                    $"Message: {exceptionHandlerPathFeature.Error.Message} \n " +
                    $"Inner Exception: {exceptionHandlerPathFeature.Error.InnerException} \n " +
                    $"Stack trace: {exceptionHandlerPathFeature.Error.StackTrace} \n ");

            try
            {
                var errorToShow = string.Concat("<b>Datetime:</b> " + DateTime.UtcNow + "<br/><br/>"
                                                + "<b>Application Name:</b> " + _configuration.GetValue<string>("ApplicationName") + "<br/><br/>"
                                                + "<b>Environment:</b> " + _configuration.GetValue<string>("ActiveEnvironment") + "<br/><br/>"
                                                + "<b>Lifecycle:</b> " + _configuration.GetValue<string>("Lifecycle") + "<br/><br/>"
                                                + "<b>Application Source:</b> " + _configuration.GetValue<string>("ApplicationSource") + "<br/><br/>"
                                                + "<b>Tenant:</b> " + _configuration.GetValue<string>("Tenant") + "<br/><br/>"
                                                + "<b>Message:</b> " + exceptionHandlerPathFeature.Error.Message + "<br/><br/>"
                                                + "<b>Inner Exception:</b> " + exceptionHandlerPathFeature.Error.InnerException + "<br/><br/>"
                                                + "<b>Stack Trace:</b> " + exceptionHandlerPathFeature.Error.StackTrace + "<br/>");
                var websiteUrlLink = _configuration["Email:WebsiteUrlLink"]; 

                await _mediator.Send(new SendEmailToStraxEvent(_configuration.GetValue<string>("StraxEmailClients"),
                                                               "Critical Exception - 500 Email",
                                                               "Internal Server Error 500 on " + _configuration.GetValue<string>("ApplicationName"),
                                                               errorToShow,websiteUrlLink));
            }
            catch (Exception e)
            {
                // Log failure to send email
                _logger.LogCritical($"A critical system exception occurred.\n " +
                                    $"Message: {e.Message} \n " +
                                    $"Inner Exception: {e.InnerException} \n " +
                                    $"Stack trace: {e.StackTrace} \n ");
            }

            return View($"~/Views/Errors/500.cshtml");
        }

        [Route("404")]
        public IActionResult PageNotFound()
        {            
            return View($"~/Views/Errors/404.cshtml");
        }
    }
}
