﻿using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.History;
using Wirehome.Core.History.Extract;

namespace Wirehome.Core.HTTP.Controllers
{
    [ApiController]
    public class HistoryController : Controller
    {
        private readonly HistoryService _historyService;

        public HistoryController(HistoryService historyService)
        {
            _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        }

        [HttpGet]
        [Route("api/v1/components/{componentUid}/status/{statusUid}/history")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<ActionResult<HistoryExtract>> GetComponentStatusHistory(
            string componentUid, 
            string statusUid,
            DateTimeOffset? rangeStart,
            DateTimeOffset? rangeEnd,
            TimeSpan? interval,
            HistoryExtractDataType? dataType,
            int maxRowCount = 1000)
        {
            if (rangeEnd == null)
            {
                rangeEnd = DateTimeOffset.UtcNow;
            }

            if (rangeStart == null)
            {
                rangeStart = rangeEnd.Value.AddHours(-1);
            }

            if (dataType == null)
            {
                dataType = HistoryExtractDataType.Text;
            }

            if (dataType == HistoryExtractDataType.Number && interval == null)
            {
                interval = TimeSpan.FromMinutes(5);
            }

            if (dataType == HistoryExtractDataType.Text && interval != null)
            {
                return new ObjectResult(new ProblemDetails
                {
                    Status = (int)HttpStatusCode.BadRequest,
                    Title = "Parameters are invalid.",
                    Detail = "Interval is not supported for data type TEXT."
                });
            }

            var historyExtract = await _historyService.BuildHistoryExtractAsync(
                componentUid,
                statusUid,
                rangeStart.Value.UtcDateTime,
                rangeEnd.Value.UtcDateTime,
                interval,
                dataType.Value,
                maxRowCount,
                HttpContext.RequestAborted);

            return new ObjectResult(historyExtract);
        }

        [HttpGet]
        [Route("api/v1/components/{componentUid}/status/{statusUid}/history/raw")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task<ActionResult<HistoryExtract>> GetComponentStatusHistoryRaw(string componentUid, string statusUid, int year, int month, int day)
        {
            var result = await _historyService.GetComponentStatusValues(
                componentUid,
                statusUid,
                new DateTime(year, month, day),
                HttpContext.RequestAborted);

            return new ObjectResult(result);
        }

        [HttpGet]
        [Route("api/v1/components/{componentUid}/status/{statusUid}/history/size")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task<long> GetComponentStatusHistorySize(string componentUid, string statusUid)
        {
            return _historyService.GetComponentStatusHistorySize(componentUid, statusUid, HttpContext.RequestAborted);
        }

        [HttpGet]
        [Route("api/v1/components/{componentUid}/history/size")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task<long> GetComponentHistorySize(string componentUid)
        {
            return _historyService.GetComponentHistorySize(componentUid, HttpContext.RequestAborted);
        }

        [HttpDelete]
        [Route("api/v1/components/{componentUid}/history")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task DeleteComponentHistory(string componentUid)
        {
            return _historyService.DeleteComponentHistory(componentUid, HttpContext.RequestAborted);
        }

        [HttpDelete]
        [Route("api/v1/components/{componentUid}/status/{statusUid}/history")]
        [ApiExplorerSettings(GroupName = "v1")]
        public Task DeleteComponentStatusHistory(string componentUid, string statusUid)
        {
            return _historyService.DeleteComponentStatusHistory(componentUid, statusUid, HttpContext.RequestAborted);
        }

        [HttpDelete]
        [Route("api/v1/history/statistics")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteStatistics()
        {
            _historyService.ResetStatistics();
        }
    }
}
