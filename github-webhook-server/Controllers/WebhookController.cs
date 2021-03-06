﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace github_webhook_server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ILogger<WebhookController> logger)
        {
            _logger = logger;
        }

        public IActionResult Get()
        {
            return Content("ok");
        }

        [HttpPost]
        public IActionResult Post([FromBody] dynamic payload)
        {
            string workdirPath = @"/root/projects/WebhookTest";
            var isValid = Repository.IsValid(workdirPath);
            if (!isValid)
            {
                string sourceUrl = payload.repository.clone_url.ToString();
                string cloneResult = Repository.Clone(sourceUrl, workdirPath);
                _logger.LogInformation($"cloneResult:{cloneResult}");
            }


            using (var repo = new Repository(workdirPath))
            {
                var mergeResult = Commands.Pull(repo, new Signature("tom", "tom@dianqing.com", DateTimeOffset.Now), null);

                var commitId = mergeResult?.Commit?.Id?.ToString();
                if (string.IsNullOrEmpty(commitId))
                {
                    commitId = DateTime.Now.ToString("yyyyMMddHHmm");
                }
                else
                {
                    commitId = commitId.Substring(0, 8);
                }

                _logger.LogInformation($"CommitId:{commitId}");

                Execute($"/root/projects/WebhookTest/WebhookTest/build.sh", commitId);
            }

            return Ok(payload);
        }

        [NonAction]
        private void Execute(string fileName, string commitId)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"{fileName} {commitId}",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            var process = Process.Start(startInfo);

            process.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogInformation("output>>" + e.Data);
                }
            };

            process.BeginOutputReadLine();

            process.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    _logger.LogError("error>>" + e.Data);
                }
            };

            process.BeginErrorReadLine();

            process.WaitForExit();
            process.Close();

            _logger.LogInformation("发布成功");
        }
    }
}
