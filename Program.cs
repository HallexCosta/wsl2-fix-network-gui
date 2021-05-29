using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace WSL2FixNetworkGUI
{
    class Business
    {
        private WSL2 WSL;
        private VcxSrv VcxSrv;

        public static void Initiliaze()
        {
            Business instance = new Business();
            instance._init();
            return;
        }

        private void _init()
        {
            this.WSL = new WSL2();
            this.WSL.SetFileDirectory(@"C:\scripts\wsl2bridge.ps1");
            this.WSL.EnableBridgeMode();

            return;
        }
    }

    class RuleApp
    {
        private static string currentDirectory = Directory.GetCurrentDirectory();
        private string filepath = $"{RuleApp.currentDirectory}/config.json";
        public RuleApp()
        {
            Console.WriteLine("Start Applicatin...");
        }

        public static void Initialize()
        {
            RuleApp instance = new RuleApp();
            instance._init();
        }

        public void _init()
        {
            this.CheckIsFile(this.filepath);
        }

        private void CheckIsFile(string filepath)
        {
            Console.WriteLine("> Checking config.json...");
            Console.Write("> File exists?");
            if (File.Exists(filepath))
            {
                Console.WriteLine(" YES");
            }
            else
            {
                Console.WriteLine(" NO");
                throw new Exception("ERROR: File config.json not found");
            }
        }
        
        private void Read()
        {

        }
    }

    abstract class Script
    {
        protected PowerShell pipeline = PowerShell.Create();
        protected string fileDirectory;

        public Script(string message)
        {
            Console.WriteLine("Loading...");
            Console.WriteLine(message);
            Program.SystemPause(5);
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

        protected Collection<PSObject> Load(string FileDirectory)
        {
            Console.WriteLine($"> Loading Script on {FileDirectory}");
            string Content = this.ReadFile(FileDirectory);
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
            Console.WriteLine("");
            Console.WriteLine("Done!!");
            Program.SystemPause(2);
        }

        public void SetFileDirectory(string fileDirectory)
        {
            this.fileDirectory = fileDirectory;
        }

        protected string ReadFile(string fileDirectory)
        {
            return File.ReadAllText(fileDirectory);
        }
    }

    class VcxSrv : Script
    {
        public VcxSrv() : base("> Initialize Script XLaunch - VcxSrv") { }
    }

    class WSL2 : Script
    {
        public WSL2() : base("> Initialize Script WSL2 - Bridge Mode") { }
        
        public void EnableBridgeMode()
        {
            Console.WriteLine("> Enabling Bridge Mode");
            bool isRunAsAdmin = this.RunAsAdmin();

            if (!isRunAsAdmin)
            {
                Console.Error.WriteLine("Something Wrong: Error to run powershell as admin");
                return;
            }

            Collection<PSObject> output = this.Load(this.fileDirectory);

            this.Output(output);
            this.Done();
        }
    }
    
    class Program
    {
        static void Main(string[] args)
        {
            Program.App(Program._App);

            RuleApp.Initialize();
            Business.Initiliaze();
        }

        private static bool _App(bool inLoop)
        {
            int answer = Program.Questions();
            bool isAnswer = Program.AllowAnswers(answer);
            if (isAnswer)
            {
                inLoop = false;
            }
            return inLoop;
        }

        private static void App(Func<bool, bool> _App)
        {
            bool inLoop = true;
            do
            {
                inLoop = Program._App(inLoop);
            }
            while (inLoop);
        }

        private static int Questions()
        {
            Console.Clear();
            Console.WriteLine("Choose a program: ");
            Console.WriteLine("1) Enable Bridge Mode from WSL 2");
            Console.WriteLine("2) Open XLaunch VcxSrv with Configs");
            Console.WriteLine("0) Exit");

            Console.WriteLine("");
            Console.Write("Enter Number: ");

            var answer = Console.ReadLine();
            string repeatQuestion = "-1";
            string answerStatement = String.IsNullOrWhiteSpace(answer) ? repeatQuestion : answer.ToString();
            int answerToInt = Int16.Parse(answerStatement);
            return answerToInt;
        }

        private static bool AllowAnswers(int answer)
        {
            Program.LeaveProgram(answer);
            int[] answers = new int[] { 1, 2 };
            return answers.Contains(answer);
        }

        public static void SystemPause(int seconds = 1, int ms = 1000)
        {
            System.Threading.Thread.Sleep(ms * seconds);
        }

        public static void LeaveProgram(int command)
        {
            if (command.CompareTo(0) == 0)
            {
                Environment.Exit(0);
            }
        }
    }
}
