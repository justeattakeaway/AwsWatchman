using System.Configuration;
using System.Globalization;
using System.Net.Mail;
using CsvHelper;
using QuarterMaster.Models;

namespace Quartermaster
{
    public class ReportSender
    {
        private string _smtpHost;

        public async Task SendReports(IList<ProvisioningReport> reports)
        {
            ReadSmtpHost();

            foreach (var provisionReport in reports)
            {
                ConsoleReport(provisionReport);
                await SendReport(provisionReport);
            }
        }

        private void ReadSmtpHost()
        {
            _smtpHost = ConfigurationManager.AppSettings.Get("SmtpHost");

            if (string.IsNullOrWhiteSpace(_smtpHost))
            {
                throw new ApplicationException("Cannot find app setting for SmtpHost");
            }
        }

        private static void ConsoleReport(ProvisioningReport provisionReport)
        {
            foreach (var reportRow in provisionReport.Rows)
            {
                Console.WriteLine($"Table: {reportRow.TableName}({reportRow.IndexName})");
                Console.WriteLine(
                    $"Provision: {reportRow.ProvisionedReadCapacityUnits}, {reportRow.ProvisionedWriteCapacityUnits}");
                Console.WriteLine(
                    $"Consumption: {reportRow.MaxConsumedReadPerMinute}, {reportRow.MaxConsumedWritePerMinute}");
            }
            Console.WriteLine($"Report {provisionReport.Name} done");
        }

        private async Task SendReport(ProvisioningReport provisionReport)
        {
            try
            {
                var targets = EmailTargets(provisionReport);

                if (!targets.Any())
                {
                    Console.Error.WriteLine($"No mail targets for report {provisionReport.Name}. Will not send.");
                    return;
                }

                var reportData = WriteCsvToString(provisionReport);
                var reportFile = WriteReportToFile(provisionReport.Name, reportData);
                await MailReport(provisionReport.Name, reportFile.FullName, targets);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to save and mail report {provisionReport.Name}");
                Console.Error.WriteLine(ex.ToString());
                throw;
            }
        }

        private static List<string> EmailTargets(ProvisioningReport provisionReport)
        {
            return provisionReport.Targets
                .Select(x => x.Email)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static string WriteCsvToString(ProvisioningReport provisionReport)
        {
            var reportString = new StringWriter();

            using var csv = new CsvWriter(reportString, CultureInfo.InvariantCulture);
            csv.WriteRecords(provisionReport.Rows);
            return reportString.ToString();
        }

        private static FileInfo WriteReportToFile(string reportName, string reportText)
        {
            var reportDirectory = GetReportDirectory();
            var reportFile = GetReportFile(reportName, reportDirectory);
            File.WriteAllText(reportFile.FullName, reportText);
            return reportFile;
        }

        private async Task MailReport(string reportName, string reportFileName, List<string> targets)
        {
            Console.WriteLine($"Sending report {reportName} to: {string.Join(",", targets)}");

            var client = new SmtpClient(_smtpHost);
            var attachment = new Attachment(reportFileName);
            var mailMessage = new MailMessage
                {
                    From = new MailAddress("quartermaster@just-eat.com"),
                    Body = $"Attached is the {reportName} dynamo provisioning report",
                    Subject = $"{reportName} dynamo provisioning report",
                    Attachments = { attachment }
                };

            foreach (var target in targets)
            {
                mailMessage.To.Add(target);
            }

            await client.SendMailAsync(mailMessage);
        }

        private static DirectoryInfo GetReportDirectory()
        {
            var reportDirectory = new DirectoryInfo(@".\Reports\");
            reportDirectory.Create();
            return reportDirectory;
        }

        private static FileInfo GetReportFile(string reportName, DirectoryInfo reportDirectory)
        {
            var baseFileName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars()));

            var count = 0;
            var proposedName = $"{baseFileName}.csv";
            var reportDirectoryFiles = reportDirectory.GetFiles();

            while (FileExistsWithName(reportDirectoryFiles, proposedName))
            {
                count++;
                proposedName = $"{baseFileName}_{count}.csv";
            }

            return new FileInfo(Path.Combine(reportDirectory.FullName, proposedName));
        }

        private static bool FileExistsWithName(FileInfo[] files, string name)
        {
            return files.Any(file => file.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
