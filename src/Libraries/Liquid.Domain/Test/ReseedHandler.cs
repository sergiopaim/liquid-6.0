using Microsoft.AspNetCore.Http;
using System.Net;

namespace Liquid.Domain.Test
{
    internal class ReseedHandler
    {
        internal static bool HandleHttpInvoke(ref HttpContext context, string pathToCheck, bool textFormat)
        {
            WorkBench.ConsoleResultFormat(!textFormat);

            if (pathToCheck.StartsWith("/reseed"))
            {
                if (!WorkBench.IsDevelopmentEnvironment && !WorkBench.IsIntegrationEnvironment && !WorkBench.IsQualityEnvironment)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return false;
                }

                var dataType = pathToCheck.Split('/').Length > 2 ?
                    pathToCheck.Split('/')[2] : null;

                string message;
                context.Response.ContentType = "text/plain";

                if (string.IsNullOrEmpty(dataType))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    message = "Missing dataset name. Call example: '/reseed/Unit'";
                }
                else if (WorkBench.Repository.ReseedDataAsync(dataType, force: true).Result)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    message = "----------------------------------------------------------------------------\n";
                    message += $"Repository was RESEEDED from whole '{dataType}' dataset.";
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    message = "---------------------------\n";
                    message += $"Reseed dataset `{dataType}` was NOT FOUND, was INCONSISTENT or the collection/container is script created only.\nThe database was RESET and left EMPTY or PARTIALLY seeded or not created at all.";
                }
                message += "\n----------------------------------------------------------------------------\n";
                WorkBench.ConsoleWriteHighlightedLine(message);
                WorkBench.WriteLog(context.Response);
                return false;
            }
            else
                return true;
        }
    }
}
