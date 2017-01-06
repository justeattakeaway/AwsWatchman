using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using QuarterMaster.Models;
using CsvHelper;

namespace Quartermaster
{
    public class ReportSender
    {
        public void SendReports(IList<ProvisioningReport> reports)
        {
            foreach (var provisionReport in reports)
            {
                ConsoleReport(provisionReport);
                SendReport(provisionReport);
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
            Console.WriteLine("Report done");
        }

        private static void SendReport(ProvisioningReport provisionReport)
        {
            try
            {
                var targets = provisionReport.Targets.Select(x => x.Email).ToList();

                var reportString = new StringWriter();
                var csv = new CsvWriter(reportString);
                csv.WriteRecords(provisionReport.Rows);
                var report = reportString.ToString();

                var reportDirectory = GetReportDirectory();
                var reportFile = GetReportFile(provisionReport.Name, reportDirectory);
                File.WriteAllText(reportFile.FullName, report);

                if (!targets.Any()) return;

                var smtpHost = System.Configuration.ConfigurationManager.AppSettings.Get("SmtpHost");
                var client = new SmtpClient(smtpHost);
                var attachment = new Attachment(reportFile.FullName);
                var mailMessage = new MailMessage
                {
                    From = new MailAddress("quartermaster@just-eat.com"),
                    Body = $"Attached is the {provisionReport.Name} dynamo provisioning report",
                    Subject = $"{provisionReport.Name} dynamo provisioning report",
                    Attachments = {attachment}
                };
                mailMessage.To.Add(string.Join(",", targets));
                client.Send(mailMessage);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Failed to save and mail report");
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
