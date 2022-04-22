using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.ServiceProcess;
using System.Text;
using System.Timers;

namespace EDRProcessor
{
    public class EDRDIARY
    {
        public string EventId { get; set; }
        public string MemberId { get; set; }
        public string Serial { get; set; }
        public string MemberName { get; set; }
        public string IP { get; set; }
        public string ReportTime { get; set; }
        public string ClientTime { get; set; }
        public string Location { get; set; }
        public string TypeAlert { get; set; }
        public string Detail { get; set; }
    }
    public class Root
    {
        public List<EDRDIARY> EDR_DIARY { get; set; }
    }
    public partial class EDRProcessor : ServiceBase
    {
        private string edr_directory = @"C:\\BEDR Server\\BEDR Server\\LogSOC\\";
        private string event_log_directory = @"C:\\EDRProcessor\\EventLog\\";
        private string service_log_directory = @"C:\\EDRProcessor\\ServiceLog\\";
        private string old_char = @"\";
        private string new_char = @"/";
        private int rotate_time = 604800;   //  7 days
        private FileSystemWatcher fsw;

        public EDRProcessor()
        {
            InitializeComponent();
        }
        private int to_unix_timeseconds(DateTime date)
        {
            DateTime point = new DateTime(1970, 1, 1);
            TimeSpan time = date.Subtract(point);
            return (int)time.TotalSeconds;
        }
        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            List<string> directorys = new List<string>();
            directorys.Add(event_log_directory);
            directorys.Add(service_log_directory);
            int now = to_unix_timeseconds(DateTime.Now);

            foreach (string directory in directorys)
            {
                // Process the list of files found in the directory.
                string[] file_entries = Directory.GetFiles(directory);
                foreach (string file_path in file_entries)
                {
                    int file_creation_time = to_unix_timeseconds(File.GetCreationTime(file_path));
                    if ((now - file_creation_time) > rotate_time)
                    {
                        delete_file(file_path);
                        string message = String.Format("Delete file by logrotate: {0}", file_path);
                        report_generation("INFO", message);
                    }
                }
            }
        }
        private void filesystemwatcher()
        {
            //  Create a FileSystemWatcher to monitor all files in folder.
            fsw = new FileSystemWatcher(edr_directory);

            //  Register a handler that gets called when a file is created
            fsw.Created += new FileSystemEventHandler(OnChanged);

            //  Register a handler that gets called if the
            //  FileSystemWatcher needs to report an error.
            fsw.Error += new ErrorEventHandler(OnError);

            // Monitor only json files.
            fsw.Filter = "*.json";

            //  Unsupervised subdirectories
            fsw.IncludeSubdirectories = false;

            //  Sets the size (in bytes) of the internal buffer to 64KB (maximum).
            fsw.InternalBufferSize = 65536;

            //  Begin watching.
            fsw.EnableRaisingEvents = true;
            report_generation("INFO", "Begin watching");
        }
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            //  Show that a file has been created.
            //WatcherChangeTypes wct = e.ChangeType;

            file_parsing(e.FullPath);
        }
        private void OnError(object source, ErrorEventArgs e)
        {
            //  Show that an error has been detected.
            report_generation("ERROR", "The FileSystemWatcher has detected an error");

            //  Give more information if the error is due to an internal buffer overflow.
            if (e.GetException().GetType() == typeof(InternalBufferOverflowException))
            {
                //  This can happen if Windows is reporting many file system events quickly
                //  and internal buffer of the  FileSystemWatcher is not large enough to handle this
                //  rate of events. The InternalBufferOverflowException error informs the application
                //  that some of the file system events are being lost.
                report_generation("ERROR", ("The file system watcher experienced an internal buffer overflow: " + e.GetException().Message));
            }
        }
        private void file_parsing(string file_path)
        {
            int count = 0;
            int process_status = 0;
        process_start:
            string event_log_file = file_path_generation(event_log_directory);
            string file_content = @"";

            try { file_content = File.ReadAllText(file_path, Encoding.UTF8).Replace(old_char, new_char); }
            catch (IOException exp) { report_generation("ERROR", "File deletion failed: " + exp.Message); }

            if (file_content != "")
            {
                Root items = JsonConvert.DeserializeObject<Root>(file_content);
                foreach (EDRDIARY item in items.EDR_DIARY)
                {
                    if (String.IsNullOrEmpty(item.EventId))
                        continue;
                    string message = JsonConvert.SerializeObject(item);
                    write_to_file(message, event_log_file);
                }

                //  Set process status to success
                process_status = 0;
                //  Delete file when parsing successful.
                delete_file(file_path);
            }
            else
            {
                //  Set process status to failure
                process_status = 1;
                if (count < 1)
                {
                    //  Write error.
                    string message = String.Format("The file could not be loaded. Reload it again: {0}", file_path);
                    report_generation("ERROR", message);

                    //  increase the counter variable and reload the file.
                    count++;
                    System.Threading.Thread.Sleep(100);
                    goto process_start;
                }
            }
            if (process_status == 0 && count == 1)
                report_generation("INFO", "Reparsing successful: " + file_path);
            if (process_status == 1)
            {
                string message = String.Format("File reload failed: {0}", file_path);
                report_generation("ERROR", message);
            }
        }
        private void write_to_file(string content, string file_path)
        {
            FileStream fs = null;
            try
            {
                fs = new FileStream(file_path, FileMode.Append);
                using (StreamWriter writer = new StreamWriter(fs, Encoding.UTF8))
                {
                    writer.Write(content.ToLower());
                }
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }
        }
        private string file_path_generation(string directory)
        {
            string file_name = String.Concat(DateTime.Now.ToString("yyyy-MM-dd"), ".log");
            return String.Concat(directory, file_name);
        }
        private void report_generation(string level, string content)
        {
            string file_path = file_path_generation(service_log_directory);
            string message = String.Format("[{0}] [{1}] {2} ", DateTime.Now.ToString("yyyy-MM-dd-HH:mm:ss"), level, content);
            write_to_file(message, file_path);
        }
        private void delete_file(string file_path)
        {
            try { File.Delete(file_path); }
            catch (IOException exp) { report_generation("ERROR", "File deletion failed: " + exp.Message); }
        }
        protected override void OnStart(string[] args)
        {
            //  Create Directory if it doesn't exist.
            if (!Directory.Exists(event_log_directory))
                System.IO.Directory.CreateDirectory(event_log_directory);
            if (!Directory.Exists(service_log_directory))
                System.IO.Directory.CreateDirectory(service_log_directory);

            // Set up a timer that triggers every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 60000; // 60 seconds

            //  Call function OnTimer() when the interval elapses.
            timer.Elapsed += new ElapsedEventHandler(this.OnTimer);
            timer.Start();

            //  Start processor
            filesystemwatcher();
            report_generation("INFO", "Service started");
        }

        protected override void OnStop()
        {
            //  Finish watching.
            fsw.EnableRaisingEvents = false;
            report_generation("INFO", "Service stopped");
        }
    }
}
