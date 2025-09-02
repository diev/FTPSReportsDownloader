namespace FTPSReportsDownload
{
    class Program
    {
        static void Main(string[] args)
        {
            Tests.RunAllTests();
            Ftps.Sync();
        }
    }
}