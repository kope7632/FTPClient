using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Security.Cryptography;

namespace ConsoleApp1
{
    class Program
    {
        const string serverIp = "ftp://192.168.2.16";
        const string user = "jiasiang";
        const string password = "jiasiang";
        static void Main(string[] args)
        {
            string listStr = GetList();
            List<ItemInfo> list = GetListInfoes(listStr);

            string cmd;
            int cmdIndex;
            while (true)
            {
                PrintListInfo(list);
                cmd = Console.ReadLine();
                if (int.TryParse(cmd, out cmdIndex))
                {
                    if (cmdIndex < list.Count && cmdIndex >= 0)
                        DownloadFile(list[cmdIndex]);
                    else
                        Console.WriteLine("Invalid choice!!");
                }
                else
                    break;
            }
            Console.WriteLine("Press enter to exit...");
            Console.Read();
        }

        static string GetList()
        {
            string result;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverIp);
            request.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            request.Credentials = new NetworkCredential(user, password);

            FtpWebResponse response = (FtpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();
            StreamReader reader = new StreamReader(responseStream);

            result = reader.ReadToEnd();

            reader.Close();
            response.Close();
            return result;
        }
        static List<ItemInfo> GetListInfoes(string listStr)
        {
            List<ItemInfo> result = new List<ItemInfo>();
            string[] listArry = listStr.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in listArry)
            {
                string[] array = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (array.Length == 9)
                {
                    result.Add(new ItemInfo(array[8], ulong.Parse(array[4])));
                }
            }
            return result;
        }
        static void PrintListInfo(List<ItemInfo> list)
        {
            int index = 0;
            Console.WriteLine("Choose download item:");
            foreach (ItemInfo item in list)
            {
                Console.WriteLine("{0}.\t{1}\r\n\t({2} Byte {3:F} KB {4:F} MB)", index++, item.name, item.size, item.size / 1024, item.size / 1024 / 1024);
            }
            Console.WriteLine("Select :");
        }

        static void DownloadFile(ItemInfo item)
        {
            DateTime first = DateTime.Now;
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(serverIp + "/" + item.name);
            request.Method = WebRequestMethods.Ftp.DownloadFile;
            request.ReadWriteTimeout = 20;
            request.Credentials = new NetworkCredential(user, password);
            FtpWebResponse response = (FtpWebResponse)request.GetResponse();

            Stream responseStream = response.GetResponseStream();
            double totalSize = 0;
            byte[] buffer = new byte[102400];
            int readSize = 1;
            int tryTime = 10;
            string savePath = "D:\\" + item.name;
            using (FileStream fs = new FileStream(savePath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                bool hasError = false;
                do
                {
                    try
                    {
                        readSize = responseStream.Read(buffer, 0, buffer.Length);
                        tryTime = 10;
                    }
                    catch (Exception)
                    {
                        hasError = true;
                        if (--tryTime <= 0)
                            break;
                        else
                        {
                            Console.WriteLine("Retry:" + tryTime.ToString());
                            System.Threading.Thread.Sleep(1000);
                            continue;
                        }
                    }

                    if (hasError)
                    {
                        hasError = false;
                        System.Threading.Thread.Sleep(2000);
                    }
                    else if (readSize > 0)
                    {
                        totalSize += readSize;
                        Console.WriteLine("read:{0} Byte\ttotal:{1} Byte {3:F} KB {4:F} MB\t({2:P}%)", readSize, totalSize, totalSize / item.size, totalSize / 1024, totalSize / 1024 / 1024);
                        fs.Write(buffer, 0, readSize);
                    }
                } while (readSize > 0);
            }
            if (tryTime <= 0)
                Console.WriteLine("Download fail..");
            else
                Console.WriteLine("Download to {0} Complete, spend time {1}", savePath, DateTime.Now - first);
            response.Close();
        }
    }
    class ItemInfo
    {
        public string name { get; set; }
        public double size { get; set; }

        public ItemInfo(string name, double size)
        {
            this.name = name;
            this.size = size;
        }

        public ItemInfo() : this("", 0) { }
    }
}
