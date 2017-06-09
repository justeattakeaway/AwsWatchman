using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading.Tasks;
using QuarterMaster.Models;
using CsvHelper;

namespace Quartermaster
{
    public class ReportSender
    {
        private readonly string _smtpHost;

        public ReportSender()
        {
            _smtpHost = ConfigurationManager.AppSettings.Get("SmtpHost");

            if (string.IsNullOrWhiteSpace(_smtpHost))
            {
                throw new ApplicationException("Cannot find app setting for SmtpHost");
            }
        }


        public async Task SendReports(IList<ProvisioningReport> reports)
        {
            foreach (var provisionReport in reports)
            {
                ConsoleReport(provisionReport);
                await SendReport(provisionReport);
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
                var targets = provisionReport.Targets
                    .Select(x => x.Email)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                if (!targets.Any())
                {
                    Console.Error.WriteLine($"No mail targets for report {provisionReport.Name}. Will not send.");
                    return;
                }

                Console.WriteLine($"Sending report {provisionReport.Name} to: " + string.Join(",", targets));

                var reportString = new StringWriter();
                var csv = new CsvWriter(reportString);
                csv.WriteRecords(provisionReport.Rows);
                var report = reportString.ToString();

                var reportDirectory = GetReportDirectory();
                var reportFile = GetReportFile(provisionReport.Name, reportDirectory);
                File.WriteAllText(reportFile.FullName, report);

                var client = new SmtpClient(_smtpHost);
                var attachment = new Attachment(reportFile.FullName);
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("quartermaster@just-eat.com"),
                    Body = $"Attached is the {provisionReport.Name} dynamo provisioning report",
                    Subject = $"{provisionReport.Name} dynamo provisioning report",
                    Attachments = {attachment}
                };

                foreach (var target in targets)
                {
                    mailMessage.To.Add(target);
                }

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Failed to save and mail report {provisionReport.Name}");
                Console.Error.WriteLine(ex.ToString());
                throw;
            }
        }

        private static DirectoryInfo GetReportDirectory()
        {
            var reportDirectory = new DirectoryInfo(@".\Reports\");
            reportDirectory.Create();
            return reportDirectory;
        }

        private static FileInfo GetReportFile(string reportName, DirectoryInfo reportDirectory)
        {
            var sourceFileName = string.Join("_", reportName.Split(Path.GetInvalidFileNameChars()));

            var suffix = "";
            var count = 0;
            var reportDirectoryFiles = reportDirectory.GetFiles();
            while (
                reportDirectoryFiles.Any(
                    file => file.Name.Equals($"{sourceFileName}{suffix}.csv", StringComparison.InvariantCultureIgnoreCase)))
            {
                suffix = count++.ToString();
            }
            return new FileInfo(Path.Combine(reportDirectory.FullName, $"{sourceFileName}{suffix}.csv"));
        }
    }
}
