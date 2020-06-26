using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
                Commands.Pull(repo, new Signature("tom", "tom@dianqing.com", DateTimeOffset.Now), null);

                Execute("/root/build.sh");
            }
            return Ok(payload);
        }

        public void Execute(string commandline)
        {
            var startInfo = new ProcessStartInfo(commandline)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            };

            Process.Start(startInfo);
        }
    }
}
