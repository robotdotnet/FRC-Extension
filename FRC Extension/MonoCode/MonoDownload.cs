using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoDownload
    {
        public static async Task DownloadMono(string fileName, IProgress<int> progress = null)
        {
            string target = DeployProperties.MonoURL + DeployProperties.MonoVersion;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        progress?.Report(e.ProgressPercentage);
                    };
                    await client.DownloadFileTaskAsync(new Uri(target), fileName);

                }
            }
            catch (Exception)
            {
                var writer = OutputWriter.Instance;
                writer.WriteLine($"Could not download file: {DeployProperties.MonoVersion}");
            }
        }
    }
}
