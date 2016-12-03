using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using RobotDotNet.FRC_Extension.MonoCode;

namespace RobotDotNet.FRC_Extension.RoboRIOCode
{
    public static class CachedFileHelper
    {
        public static async Task<bool> CheckAndDeployNativeLibraries(string remoteDirectory, string propertiesName, string dirToUpload, IList<string> ignoreFiles)
        {
            MemoryStream stream = new MemoryStream();
            bool readFile =
                    await
                        RoboRIOConnection.ReceiveFile($"{remoteDirectory}/{propertiesName}.properties", stream,
                            ConnectionUser.LvUser);

            string nativeLoc = dirToUpload;

            var fileMd5List = await GetMD5ForFiles(nativeLoc, ignoreFiles);
            if (!readFile)
            {
                // Libraries definitely do not exist, deploy
                return await DeployNativeLibraries(fileMd5List, remoteDirectory, propertiesName);
            }

            stream.Position = 0;

            bool foundError = false;
            int readCount = 0;
            StreamReader reader = new StreamReader(stream);
            string line = null;
            while (!string.IsNullOrWhiteSpace(line = reader.ReadLine()))
            {
                // Split line at =
                string[] split = line.Split('=');
                if (split.Length < 2) continue;
                readCount++;
                foreach (Tuple<string, string> tuple in fileMd5List)
                {
                    if (split[0] == tuple.Item1)
                    {
                        // Found a match file name
                        if (split[1] != tuple.Item2)
                        {
                            foundError = true;
                        }
                        break;
                    }
                }
                if (foundError) break;
            }

            reader.Dispose();

            if (foundError || readCount != fileMd5List.Count)
            {
                return await DeployNativeLibraries(fileMd5List, remoteDirectory, propertiesName);
            }

            OutputWriter.Instance.WriteLine("Native libraries exist. Skipping deploy");
            return true;
        }

        public static async Task<List<Tuple<string, string>>> GetMD5ForFiles(string fileLocation, IList<string> ignoreFiles)
        {
            if (Directory.Exists(fileLocation))
            {
                string[] files = Directory.GetFiles(fileLocation);
                List<Tuple<string, string>> retList = new List<Tuple<string, string>>(files.Length);
                await Task.Run(() =>
                {
                    foreach (var file in files)
                    {
                        bool skip = false;
                        foreach (string ignoreFile in ignoreFiles)
                        {
                            if (file.Contains(ignoreFile))
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip) continue;
                        string md5 = MD5Helper.Md5Sum(file);
                        retList.Add(new Tuple<string, string>(file, md5));
                    }
                });
                return retList;
            }
            else
            {
                return null;
            }
        }

        public static async Task<bool> DeployNativeLibraries(List<Tuple<string, string>> files, string remoteDirectory, string propertiesName)
        {
            List<string> fileList = new List<string>(files.Count);

            MemoryStream memStream = new MemoryStream();
            StreamWriter writer = new StreamWriter(memStream);

            foreach (Tuple<string, string> tuple in files)
            {
                fileList.Add(tuple.Item1);
                await writer.WriteLineAsync($"{tuple.Item1}={tuple.Item2}");
            }

            writer.Flush();

            memStream.Position = 0;

            OutputWriter.Instance.WriteLine("Deploying native files");
            bool nativeDeploy = await RoboRIOConnection.DeployFiles(fileList, remoteDirectory, ConnectionUser.Admin);
            bool md5Deploy = await RoboRIOConnection.DeployFile(memStream, $"{remoteDirectory}/{propertiesName}.properties", ConnectionUser.Admin);

            writer.Dispose();
            memStream.Dispose();

            return nativeDeploy && md5Deploy;
        }
    }
}
