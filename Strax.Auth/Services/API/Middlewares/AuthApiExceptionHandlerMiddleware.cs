using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Threading.Tasks;
using Utilities.ApiResponse;
using Utilities.Email.EmailSender.Events;

namespace API.Middlewares
{
    public class AuthApiExceptionHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<AuthApiExceptionHandlerMiddleware> _logger;
        private readonly DefaultContractResolver _contractResolver;
        private readonly IConfiguration _configuration;

        public AuthApiExceptionHandlerMiddleware(RequestDelegate next, ILogger<AuthApiExceptionHandlerMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _contractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            };
        }

        public async Task Invoke(HttpContext httpContext, IMediator _mediator)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                // logging beforehand
                _logger.LogCritical($"A critical system exception occurred.\n " +
                    $"Message: {ex.Message} \n " +
                    $"Inner Exception: {ex.InnerException} \n " +
                    $"Stack trace: {ex.StackTrace} \n ");

                if (httpContext.Response.HasStarted)
                {
                    Console.Write("Bigger problem, response already on the way back");
                    throw;
                }


                try
                {
                    var errorToShow = string.Concat("<b>Datetime:</b> " + DateTime.UtcNow + "<br/><br/>"
                                                    + "<b>Application Name:</b> " + _configuration.GetValue<string>("ApplicationName") + "<br/><br/>"
                                                    + "<b>Environment:</b> " + _configuration.GetValue<string>("ActiveEnvironment") + "<br/><br/>"
                                                    + "<b>Lifecycle:</b> " + _configuration.GetValue<string>("Lifecycle") + "<br/><br/>"
                                                    + "<b>Application Source:</b> " + _configuration.GetValue<string>("ApplicationSource") + "<br/><br/>"
                                                    + "<b>Tenant:</b> " + _configuration.GetValue<string>("Tenant") + "<br/><br/>"
                                                    + "<b>Message:</b> " + ex.Message + "<br/><br/>"
                                                    + "<b>Inner Exception:</b> " + ex.InnerException + "<br/><br/>"
                                                    + "<b>Stack Trace:</b> " + ex.StackTrace + "<br/>");
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

                httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                httpContext.Response.ContentType = @"application/json";

                var statusCode = (int)HttpStatusCode.InternalServerError;
                var error = SiApiResponse.FromGlobalException(ex, statusCode.ToString());
                var json = JsonConvert.SerializeObject(error, new JsonSerializerSettings
                {
                    ContractResolver = _contractResolver,
                    Formatting = Formatting.Indented
                });


                await httpContext.Response.WriteAsync(json);
            }
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class AuthApiExceptionHandlerExtensions
    {
        public static IApplicationBuilder UseAuthApiExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthApiExceptionHandlerMiddleware>();
        }
    }
}
