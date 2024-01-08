using AnnovarWebui.Hubs;
using AnnovarWebui.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Text;

namespace AnnovarWebui.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHubContext<MyHub> _hubContext;
        public HomeController(ILogger<HomeController> logger, IHubContext<MyHub> hubContext)
        {
            _logger = logger;
            _hubContext = hubContext;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpPost]
        public async Task<IActionResult> UploadFile(IFormFile file, string cmdOption)
        {
            if (file != null && file.Length > 0)
            {
                string tempFolderPath = Path.GetTempPath();
                var downloadId = Guid.NewGuid().ToString();
                var workspacePath = Path.Combine(tempFolderPath, downloadId);
                Directory.CreateDirectory(workspacePath);

                var inputFilePath = Path.Combine(workspacePath, file.FileName);

                using (var stream = new FileStream(inputFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var command = $"perl $ANNOVAR_PATH/table_annovar.pl {file.FileName} $ANNOVAR_PATH/humandb/ --outfile {Path.GetFileNameWithoutExtension(file.FileName)} --buildver {cmdOption} --protocol refGene,1000g2015aug_all,1000g2015aug_afr,1000g2015aug_amr,1000g2015aug_eas,1000g2015aug_eur,1000g2015aug_sas,exac03,esp6500siv2_all,esp6500siv2_aa,esp6500siv2_ea,nci60,avsnp147,cosmic70,clinvar_20221231,dbnsfp42a,gnomad400_exome,gnomad400_genome,dbscsnv11,rmsk,ensGene,knownGene,regsnpintron,gene4denovo201907,icgc28 --operation g,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,f,r,g,g,f,f,f --vcfinput --otherinfo --thread 4 --maxgenethread 4";
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = $"-c \"{command}\"",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = workspacePath,
                    }
                };
                process.Start();

#pragma warning disable CS4014
                Task.Run(async () =>
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0)
                    {
                        await _hubContext.Clients.All.SendAsync("JobFinish", downloadId);
                    }
                });
#pragma warning restore CS4014

                return Ok(new { DownloadId = downloadId });
            }

            return BadRequest("File not provided");
        }

        [HttpPost]
        public IActionResult DownloadFile([FromBody] DownloadRequest request)
        {
            string tempFolderPath = Path.GetTempPath();
            var workspacePath = Path.Combine(tempFolderPath, request.DownloadId);
            var resultFilePath = Directory.EnumerateFiles(workspacePath, "*_multianno.txt").First();

            var fs = new FileStream(resultFilePath, FileMode.Open);
            return File(fs, "application/text", Path.GetFileName(resultFilePath));
        }

        [HttpPost]
        public IActionResult DeleteFile([FromBody] DownloadRequest request)
        {
            string tempFolderPath = Path.GetTempPath();
            var workspacePath = Path.Combine(tempFolderPath, request.DownloadId);
            Directory.Delete(workspacePath, true);
            return Ok();
        }
    }

    public class DownloadRequest
    {
        public string DownloadId { get; set; }
    }
}
