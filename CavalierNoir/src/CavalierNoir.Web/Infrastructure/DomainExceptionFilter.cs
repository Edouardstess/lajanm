using CavalierNoir.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CavalierNoir.Web.Infrastructure;

/// <summary>
/// Traduit une <see cref="DomainException"/> en message utilisateur : la
/// violation d'une règle de gestion n'est pas une erreur serveur. Sur une
/// requête classique, le message est replacé dans le formulaire ; sur une
/// requête AJAX, il est renvoyé en JSON avec le code 422.
/// </summary>
public sealed class DomainExceptionFilter(ILogger<DomainExceptionFilter> logger) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        if (context.Exception is not DomainException exception)
        {
            return;
        }

        logger.LogWarning(
            "Règle de gestion {Code} violée : {Message}",
            exception.Code ?? "(sans code)",
            exception.Message);

        context.ExceptionHandled = true;

        var isAjax = context.HttpContext.Request.Headers.XRequestedWith == "XMLHttpRequest"
                     || context.HttpContext.Request.Headers.Accept.ToString().Contains("application/json");

        if (isAjax)
        {
            context.Result = new JsonResult(new
            {
                code = exception.Code,
                message = exception.Message
            })
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            };

            return;
        }

        if (context.Controller is Controller controller)
        {
            controller.TempData["Erreur"] = exception.Message;
            var referer = context.HttpContext.Request.Headers.Referer.ToString();
            context.Result = string.IsNullOrWhiteSpace(referer)
                ? controller.RedirectToAction("Index", "Home")
                : new RedirectResult(referer);

            return;
        }

        context.Result = new StatusCodeResult(StatusCodes.Status422UnprocessableEntity);
    }
}
