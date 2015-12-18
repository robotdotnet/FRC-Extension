using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using RobotDotNet.FRC_Extension.WPILibFolder;

namespace RobotDotNet.FRC_Extension.MonoCode
{
    public class MonoFile
    {
        public string FileName { get; set; }

        private readonly string m_extractPath;

        public MonoFile(string fileName)
        {
            FileName = fileName;

            string monoFolder = WPILibFolderStructure.CreateMonoFolder();

            m_extractPath = monoFolder + Path.DirectorySeparatorChar + "temp";
        }

        public void ResetToDefaultDirectory()
        {
            string monoFolder = WPILibFolderStructure.CreateMonoFolder();
            FileName = monoFolder + Path.DirectorySeparatorChar + DeployProperties.MonoVersion;
        }

        private string Md5Sum()
        {
            byte[] fileMd5Sum = null;
            

            if (File.Exists(FileName))
            {
                using (FileStream stream = new FileStream(FileName, FileMode.Open))
                {
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        fileMd5Sum =  md5.ComputeHash(stream);
                    }
                }
            }

            if (fileMd5Sum == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();
            foreach (var b in fileMd5Sum)
            {
                builder.Append(b);
            }
            return builder.ToString();
        }

        public static string SelectMonoFile()
        {
            OpenFileDialog dialog = new OpenFileDialog();

            dialog.Filter = "Zip Files(*.zip)|*.zip";

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                return dialog.FileName;
            }
            return null;
        }

        public void SaveMonoFile()
        {
            if (!CheckFileValid()) return;

            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "Zip Files(*.zip)|*.zip|All files (*.*)|*.*";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    string fileName = dialog.FileName;
                    try
                    {
                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }
                        File.Copy(FileName, fileName, true);

                    }
                    catch
                    {
                        MessageBox.Show("File not saved successfully.");
                    }
                    MessageBox.Show("File saved successfully.");
                }
            }
        }

        public bool CheckFileValid()
        {
            string fileSum = Md5Sum();

            return fileSum != null && fileSum.Equals(DeployProperties.MonoMd5);
        }

        public async Task DownloadMono(IProgress<int> progress = null)
        {
            string target = DeployProperties.MonoUrl + DeployProperties.MonoVersion;

            try
            {
                using (WebClient client = new WebClient())
                {
                    client.Credentials = CredentialCache.DefaultNetworkCredentials;
                    client.DownloadProgressChanged += (sender, e) =>
                    {
                        progress?.Report(e.ProgressPercentage);
                    };
                    await client.DownloadFileTaskAsync(new Uri(target), FileName);
                }
            }
            catch (Exception)
            {
                var writer = OutputWriter.Instance;
                writer.WriteLine($"Could not download file: {DeployProperties.MonoVersion}");
            }
        }

        public List<string> GetUnzippedFileList()
        {
            return Directory.GetFiles(m_extractPath).ToList();
        } 

        public async Task<bool> UnzipMonoFile()
        {
            CleanupMonoFile();

            if (!CheckFileValid())
                return false;


            try
            {
                await Task.Run(() => ZipFile.ExtractToDirectory(FileName, m_extractPath));
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        public bool CleanupMonoFile()
        {
            try
            {
                if (Directory.Exists(m_extractPath))
                {
                    Directory.Delete(m_extractPath, true);
                }
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

    }
}
