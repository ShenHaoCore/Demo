using Demo.ETag.Api.Application;
using Demo.ETag.Api.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace Demo.ETag.Api.Controllers;

[ApiController]
[Route("api/documents")]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentAppService _documentAppService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(IDocumentAppService documentAppService, ILogger<DocumentsController> logger)
    {
        _documentAppService = documentAppService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id)
    {
        var result = await _documentAppService.GetAsync(id);
        if (result is null)
        {
            return NotFound(new { message = $"未找到文档：{id}" });
        }

        Response.Headers.ETag = result.ETag;
        return Ok(result.Document);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateAsync(string id)
    {
        UpdateDocumentDto? body;
        try
        {
            body = await Request.ReadFromJsonAsync<UpdateDocumentDto>();
        }
        catch (Exception)
        {
            return BadRequest(new { message = "请求体不是有效的 JSON" });
        }

        if (body is null)
        {
            return BadRequest(new { message = "标题不能为空，内容字段必须提供" });
        }

        var ifMatch = Request.Headers.IfMatch.ToString();
        var result = await _documentAppService.UpdateAsync(id, body, ifMatch);

        if (result.Status == DocumentUpdateStatus.PreconditionFailed)
        {
            _logger.LogWarning(
                "文档 {DocumentId} 更新冲突：If-Match={IfMatch}，当前 ETag={ETag}",
                id, ifMatch, result.CurrentETag);
        }

        return result.Status switch
        {
            DocumentUpdateStatus.BadRequest => BadRequest(new { message = result.Message }),
            DocumentUpdateStatus.NotFound => NotFound(new { message = $"未找到文档：{id}" }),
            DocumentUpdateStatus.PreconditionFailed => StatusCode(
                StatusCodes.Status412PreconditionFailed,
                new { message = result.Message, currentETag = result.CurrentETag }),
            DocumentUpdateStatus.Success => OkWithETag(id, result),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }

    private IActionResult OkWithETag(string id, DocumentUpdateResult result)
    {
        _logger.LogInformation("已更新文档 {DocumentId}，新 ETag={ETag}", id, result.ETag);
        Response.Headers.ETag = result.ETag;
        return Ok(result.Document);
    }
}
