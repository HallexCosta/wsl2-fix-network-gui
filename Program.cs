using System;
using System.Linq;
using System.Management.Automation;
using System.IO;
using System.Text;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Security.Principal;
using System.Collections.Generic;

namespace WSL2FixNetworkGUI
{
    class Business
    {
        private WSL2 wsl;
        private VcxSrv vcxsrv;
        private AutoRun autorun;
        private List<Script> scripts = new List<Script>();

        protected Business()
        {
            Console.WriteLine("> Running Business Rules...");
        }

        public static Business Initialize()
        {
            Business instance = new Business();
            instance._init();
            return instance;
        }

        private void _init()
        {
            this.subscribe(new WSL2());
            this.subscribe(new VcxSrv());
            this.subscribe(new AutoRun());
            return;
        }

        public void subscribe(Script script)
        {
            this.scripts.Add(script);
        }

        public void unsubscribe(int answer)
        {
            this.scripts.RemoveAll(script => script.answer == answer);
        }

        public void publish(int answer)
        {
            Console.WriteLine("> Publishing scripts...");
            foreach (Script script in this.scripts)
            {
                script.notify(answer);
            }
        }
    }

    class DotConfig
    {
        private static string _WSL2_FILEPATH;
        private static string _VCXSRV_FILEPATH;
        private static string _AUTO_RUN;

        public static string currentDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
        public static string filename = ".config";
        public static string filepath = $"{DotConfig.currentDirectory}\\{DotConfig.filename}";
        public static string WSL2_FILEPATH
        {
            set { DotConfig._WSL2_FILEPATH = value; }
            get { return DotConfig._WSL2_FILEPATH; }
        }
        public static string VCXSRV_FILEPATH
        {
            set { DotConfig._VCXSRV_FILEPATH = value; }
            get { return DotConfig._VCXSRV_FILEPATH; }
        }
        public static string AUTO_RUN
        {
            set { DotConfig._AUTO_RUN = value; }
            get { return DotConfig._AUTO_RUN; }
        }

        public void Exists()
        {
            Console.WriteLine($"> Checking {DotConfig.filepath}...");
            Console.Write("> File exists?");
            if (File.Exists(DotConfig.filepath))
            {
                Console.WriteLine(" YES");
            }
            else
            {
                Console.WriteLine(" NO");
                Console.WriteLine($"> Creating file {DotConfig.filename} with default configs");
                this.Create();
            }
        }

        public void Create()
        {
            using (FileStream fs = File.Create(DotConfig.filepath))
            {
                string wsl2FilePath = @"WSL2_FILEPATH=C:\scripts\wsl2bridge.ps1";
                string vcxsrvFilePath = @"VCXSRV_FILEPATH=C:\scripts\xlaunch-auto-run.ps1";
                
                string dotconfig =  wsl2FilePath + "\n" + vcxsrvFilePath;
                byte[] info = new UTF8Encoding(true).GetBytes(dotconfig);
                fs.Write(info, 0, info.Length);
            }
        }

        public void SetEnvironmentVariable()
        {
            Console.WriteLine($"> SetEnvironmentVariable with \"{DotConfig.filepath}\"");
            string dotconfigFile = File.ReadAllText(DotConfig.filepath);
            string[] structure = dotconfigFile.Split("\n");

            int structureSize = structure.Length - 1;

            Console.WriteLine("> Setting Environment Variable...");
            for (int i = 0; i <= structureSize; i++)
            {
                string[] pairs = structure[i].Split("=");
                string propertyName = pairs[0];
                string propertyValue = pairs[1];
                Console.WriteLine("{0}={1}", propertyName, propertyValue);
                DotConfig.SubscribeProperty(propertyName, propertyValue);
            }
        }

        public static bool CheckForThruthyOnAutoRun()
        {
            if (File.Exists(DotConfig.filepath)) {
                Console.WriteLine("CheckForThruthyOnAutoRun File Exists");
                if (!String.IsNullOrWhiteSpace(DotConfig.AUTO_RUN) && DotConfig.AUTO_RUN == "true")
                {
                    Console.WriteLine("CheckForThruthyOnAutoRun File Exists");
                    return true;
                }
            }
            return false;
        }

        public static bool EnableAutoRun()
        {
            Console.WriteLine($"> Add \"AUTO_RUN=true\" in {DotConfig.filepath}");
            using (StreamWriter sw = File.AppendText(DotConfig.filepath))
            {
                byte[] bytes = new byte[1024];
                UTF8Encoding temp = new UTF8Encoding(true);

                string autoRun = "\nAUTO_RUN=true";
                sw.Write(autoRun);
                return true;
            }
        }

        public void Done()
        {
            Console.WriteLine($"> Done: file {DotConfig.filepath} configured");
        }

        private static bool HasDotConfigProperty(string propertyName)
        {
            Type dotConfigType = typeof(DotConfig);
            return dotConfigType.GetProperty(propertyName) != null;
        }

        private static void SubscribeProperty(string propertyName, string propertyValue)
        {
            if (!DotConfig.HasDotConfigProperty(propertyName))
            {
                throw new Exception($"ERROR: property \"{propertyName}\" no is valid");
            }
            DotConfig.SetPropertyValue(propertyName, propertyValue);
        }

        private string GetPropertyValue(string propertyName)
        {
            Type dotConfigType = typeof(DotConfig);
            PropertyInfo piShared = dotConfigType.GetProperty(propertyName);
            string propertyValue = piShared.GetValue(null).ToString();
            return propertyValue;
        }

        private static void SetPropertyValue(string propertyName, string propertyValue)
        {
            Type dotConfigType = typeof(DotConfig);
            PropertyInfo piShared = dotConfigType.GetProperty(propertyName);
            piShared.SetValue(null, propertyValue);
        }
    }

    abstract class Script
    {
        protected PowerShell pipeline = PowerShell.Create();
        protected string scriptDirectory;
        protected string scriptName;
        public int answer = 3;

        public Script(string scriptName)
        {
            this.scriptName = scriptName;
            Console.WriteLine($"> Initialize Script - {scriptName}");
        }

        protected bool RunAsAdmin()
        {
            Console.Write("> Run as Admin");
            try
            {
                this.pipeline
                    .AddCommand("Set-ExecutionPolicy")
                    .AddParameter("Scope", "Process")
                    .AddParameter("ExecutionPolicy", "Bypass")
                    .Invoke();

                Console.WriteLine(": YES");
                return true;
            }
            catch (Exception _)
            {
                Console.WriteLine(": NO");
                return false;
            }
        }

        protected Collection<PSObject> Load(string fileDirectory)
        {
            Console.WriteLine($"> Loading Script on {fileDirectory}");
            string Content = this.ReadFile(fileDirectory);
            return this.pipeline.AddScript(Content, true).Invoke();
        }

        protected void Output(Collection<PSObject> results)
        {
            Console.WriteLine("");
            Console.WriteLine("> OUTPUT: ");
            foreach (var result in results)
            {
                string resultText = result.ToString();
                if (!String.IsNullOrWhiteSpace(resultText))
                {
                    Console.WriteLine(resultText);
                }
            }

            foreach (var Error in this.pipeline.Streams.Error)
            {
                Console.Error.WriteLine("ERROR: " + Error.ToString());
            }
        }

        protected void Done()
        {
            Console.WriteLine($"Done: script execution {this.scriptName}");
            Program.Pause(2);
        }

        public void SetScriptDirectory(string scriptDirectory)
        {
            this.scriptDirectory = scriptDirectory;
        }

        protected string ReadFile(string scriptDirectory)
        {
            return File.ReadAllText(scriptDirectory);
        }

        abstract public void notify(int answer);
    }

    class VcxSrv : Script
    {
        public VcxSrv() : base("XLaunch - VcxSrv")
        {
            this.answer = 2;
        }

        public void OpenVcxSrv()
        {
            Console.WriteLine("> Open XLaunch - VcxSrv...");
            this.Load(this.scriptDirectory);
            return;
        }

        public override void notify(int answer)
        {
            if (answer == this.answer)
            {
                Console.WriteLine($"> Notifying script {this.scriptName}");
                this.OpenVcxSrv();
            }
        }
    }

    class WSL2 : Script
    {
        public WSL2() : base("WSL2 - Bridge Mode")
        {
            this.answer = 1;
        }
        
        public void EnableBridgeMode()
        {
            Console.WriteLine("> Enabling Bridge Mode");
            bool isRunAsAdmin = this.RunAsAdmin();

            if (!isRunAsAdmin)
            {
                Console.Error.WriteLine("Something Wrong: Error to run powershell as admin");
                return;
            }

            Collection<PSObject> output = this.Load(this.scriptDirectory);

            this.Output(output);
            this.Done();
        }

        public override void notify(int answer)
        {
            if (answer == this.answer) {
                Console.WriteLine($"> Notifying script {this.scriptName}");
                this.EnableBridgeMode();
            }
        }
    }

    class AutoRun : Script
    {
        public AutoRun(): base("AutoRun")
        {
            this.answer = 3;
        }

        public override void notify(int answer)
        {
            if (answer == this.answer)
            {
                Console.WriteLine($"> Notifying script {this.scriptName}");

                VcxSrv vcxsrv = new VcxSrv();
                vcxsrv.SetScriptDirectory(DotConfig.VCXSRV_FILEPATH);
                vcxsrv.OpenVcxSrv();

                WSL2 wsl = new WSL2();
                wsl.SetScriptDirectory(DotConfig.WSL2_FILEPATH);
                wsl.EnableBridgeMode();

                DotConfig.EnableAutoRun();
            }
        }
    }

    class ConsoleApplicationInterface
    {
        private int[] _allowAnswers = new int[] { 1, 2, 3 };
        private bool _continueInContext = true;
        public int answer;
        public bool autoRun = false;

        public static ConsoleApplicationInterface Initialize()
        {
            ConsoleApplicationInterface instance = new ConsoleApplicationInterface();
            instance._init();
            return instance;
        }

        private void _init()
        {
            Program.Pause(2);
            this._contextQuestions(this.AskQuestions);
        }

        private void _contextQuestions(Action app)
        {
            do
            {
                app();
            }
            while (this._continueInContext);
        }

        private void AskQuestions()
        {
            bool isAutoRun = this._checkForThruthyOnAutoRun();
            if (!isAutoRun)
            {
                int answer = this.Questions();
                this.answer = answer;
            }
            bool access = this._checkIsValidAnswer(this.answer);
            this._allowAccess(access);
        }

        private bool _checkForThruthyOnAutoRun()
        {
            if (DotConfig.CheckForThruthyOnAutoRun())
            {
                this._continueInContext = false;
                this.answer = 3;
                return true;
            }
            return false;
        }

        private int Questions()
        {
            Console.Clear();
            Console.WriteLine("Choose a program: ");
            Console.WriteLine("1) Enable Bridge Mode from WSL 2");
            Console.WriteLine("2) Open XLaunch VcxSrv with Configs");
            Console.WriteLine("3) Auto Run All Scripts");
            Console.WriteLine("0) Exit");

            Console.WriteLine("");
            Console.Write("Enter Number: ");

            return this._catchAnswer();
        }

        private bool _checkIsValidAnswer(int answer)
        {
            bool isZero = this._checkIsZero(answer);
            if (isZero) {
                Program.Exit();
            }
            return this._allowAnswers.Contains(answer);
        }

        private void _allowAccess(bool access)
        {
            if (access)
                this._continueInContext = false;
        }

        private bool _checkIsZero(int answer)
        {
            if (answer == 0)
            {
                return true;
            }
            return false;
        }

        private int _catchAnswer()
        {
            var answer = Console.ReadLine();
            return this._normalizeAnswer(answer);
        }

        private int _normalizeAnswer(string answer)
        {
            string repeatQuestion = "-1";
            string answerStatement = String.IsNullOrWhiteSpace(answer) ? repeatQuestion : answer.ToString();
            int answerToInt = Int16.Parse(answerStatement);
            return answerToInt;
        }
    }

    class SupportedVersion
    {
        public SupportedVersion() {}

        public void CheckVersion()
        {
            this.checkIsSupportedPlatform();
            this.requireRunAsAdminOnWindows();
        }

        private void requireRunAsAdminOnWindows()
        {
            Console.Write("> Run as Admin on Windows");
            if (this.isPlatformWindows())
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                bool runAsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
                this.checkRunAsAdmin(runAsAdmin);
            }
        }

        private void checkRunAsAdmin(bool runAsAdmin)
        {
            if (!runAsAdmin)
            {
                Console.WriteLine(": NO");
                throw new Exception("ERROR: Software not was Run as Admin");
            }
            Console.WriteLine(": YES");
        }

        private void checkIsSupportedPlatform()
        {
            Console.WriteLine("> Check Supported Platforms");
            if (!this.isPlatformWindows() && !this.isPlatformLinux() && !this.isPlatformMacOSX())
            {
                throw new Exception("ERROR: unsupported system platform.");
            }
        }

        private bool isPlatformMacOSX()
        {
            bool isPlatformMacOSX = 
                Environment.OSVersion.Platform == PlatformID.MacOSX;

            return isPlatformMacOSX;
        }

        private bool isPlatformLinux()
        {
            bool isPlatformLinux = 
                Environment.OSVersion.Platform == PlatformID.Unix;

            return isPlatformLinux;
        }

        private bool isPlatformWindows()
        {
            bool isPlatformWindows = 
                Environment.OSVersion.Platform == PlatformID.Win32Windows
                || Environment.OSVersion.Platform == PlatformID.Win32NT
                || Environment.OSVersion.Platform == PlatformID.Win32S 
                || Environment.OSVersion.Platform == PlatformID.WinCE;

            return isPlatformWindows;
        }
    }

    class Context
    {
        private Context()
        {
            Console.WriteLine("Loading...");
        }

        public static Context Initialize()
        {
            Context instance = new Context();
            return instance;
        }

        public Action<int> appContext()
        {
            return (int answer) => {
                Business business = Business.Initialize();
                business.publish(answer);
            };
        }

        public Action supportedVersionContext()
        {
            return () => {
                SupportedVersion sp = new SupportedVersion();
                sp.CheckVersion();
            };
        }

        public Func<int> questionsContext()
        {
            return () => {
                ConsoleApplicationInterface cai = ConsoleApplicationInterface.Initialize();
                return cai.answer;
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Program program = Program.Initialize();
            program._loadDotConfigFile();

            Context context = program.context();

            Action supportedVersionContext = context.supportedVersionContext();
            Func<int> questionsContext = context.questionsContext();
            Action<int> appContext = context.appContext();

            Program._supportedVersionContext(supportedVersionContext);
            Program._applicationContext(() => {
                int answer = questionsContext();
                appContext(answer);
            });
        }

        private static Program Initialize()
        {
            Program program = new Program();
            return program;
        }

        private void _loadDotConfigFile()
        {
            DotConfig dotconfig = new DotConfig();
            dotconfig.SetEnvironmentVariable();
            Console.WriteLine($"{System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)}\\.config");
            dotconfig.Done();
        }

        private static void _supportedVersionContext(Action supportedVersion)
        {
            Program._context(supportedVersion);
        }

        private static void _applicationContext(Action app)
        {
            Program._context(app);
        }

        private static void _context(Action context)
        {
            try
            {
                context();
                // Console.ReadKey();
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                // Console.ReadKey();
                Program.Exit();
            }
        }

        private Context context()
        {
            return Context.Initialize();
        }

        public static void Pause(int seconds = 1, int ms = 1000)
        {
            System.Threading.Thread.Sleep(ms * seconds);
        }

        public static void Exit()
        {
            Environment.Exit(0);
        }
     }
}
