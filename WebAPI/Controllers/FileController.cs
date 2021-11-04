using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using WebAPI.Comm;
using WebAPI.Data;
using WebAPI.Utilities;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly long _fileSizeLimit = Program.Settings.FileSizeLimit;
        private readonly ILogger<FileController> _logger;
        private readonly string[] _permittedExtensions = { ".txt", "png", "jpg", "jpeg", "gif" };
        private readonly string _targetFilePath = Program.Settings.StoredFilesPath;

        private static readonly FormOptions _defaultFormOptions = new FormOptions();

        public FileController(ILogger<FileController> logger)
        {
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<ResultMessage> UploadDatabase()
        {
            if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File",
                    $"无法处理该请求。 (Error 1).");
                // Log error
                _logger.LogError("错误请求，非上传请求。");
                return new ResultMessage(500, "无法处理该请求",null);
            }

            var boundary = MultipartRequestHelper.GetBoundary(
                MediaTypeHeaderValue.Parse(Request.ContentType),
                _defaultFormOptions.MultipartBoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();
            while (section != null)
            {
                var hasContentDispositionHeader =
                    ContentDispositionHeaderValue.TryParse(
                        section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader)
                {
                    if (!MultipartRequestHelper
                        .HasFileContentDisposition(contentDisposition))
                    {
                        ModelState.AddModelError("File",
                            $"无法处理该请求 (Error 2).");
                        // Log error
                        _logger.LogError("传输错误");
                        return new ResultMessage(500, "无法处理该请求", ModelState);
                    }
                    else
                    {
                        var trustedFileNameForDisplay = WebUtility.HtmlEncode(
                               contentDisposition.FileName.Value);
                        // 使用 Path.GetRandomFileName(); 可以保证服务器安全性
                        var trustedFileNameForFileStorage = trustedFileNameForDisplay;// Path.GetRandomFileName();

                        //验证
                        var streamedFileContent = await FileHelpers.ProcessStreamedFile(
                            section, contentDisposition, ModelState,
                            _permittedExtensions, _fileSizeLimit);

                        if (!ModelState.IsValid)
                        {
                            return new ResultMessage(500, "无法处理该请求", ModelState);
                        }

                        using (var targetStream = System.IO.File.Create(
                            Path.Combine(_targetFilePath, trustedFileNameForFileStorage)))
                        {
                            await targetStream.WriteAsync(streamedFileContent);

                            _logger.LogInformation(
                                "Uploaded file '{TrustedFileNameForDisplay}' saved to " +
                                "'{TargetFilePath}' as {TrustedFileNameForFileStorage}",
                                trustedFileNameForDisplay, _targetFilePath,
                                trustedFileNameForFileStorage);
                        }
                    }
                }
                section = await reader.ReadNextSectionAsync();
            }
            return new ResultMessage(201, "请求成功","上传成功");
        }
    }
}
