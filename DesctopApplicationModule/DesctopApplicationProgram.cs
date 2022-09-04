using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using static InputApplicationProgram;

[Description(@"
    /// <summary>
    /// Программа выполняет поиск проектов dotnet по файловой систему
    /// и формирует ярлыки для запуска программ
    /// </summary>
")]
public class DesctopApplicationProgram
{
    [Label("Переход к выполнению программы")]
    public static void Start(ref string[] args)
    {        
        bool Interactive = Environment.UserInteractive;
        if (Interactive)
        {
            MainMenu(ref args);
        }
        else
        {
            if (args.Length == 0)
            {
                CreateLinks(System.IO.Directory.GetCurrentDirectory(),System.IO.Directory.GetCurrentDirectory());
            }
            else if (args.Length == 2)
            {
                foreach (string arg in args)
                    CreateLinks(DEFAULT_PROJECTS_DIR, arg);
            }
            else
            {
                throw new Exception("Необходимо передать 2 аргумента путь к каталогу с проектами dotnet и путь к избранному");
            }
        }       
    }


    [Label("Переход в главное меню")]
    private static void MainMenu( ref string[] args)
    {
        Clear();
        WriteLine("Консоль рабочего стола");
        switch ( ProgramDialog.SingleSelect( "",new string[]{
            "Выполнить вешнюю программу",
            "Выполнить программу Dotnet",
            "Собрать рабочий стол",
            "Выход" }, ref args))
        {

            case "Выполнить вешнюю программу":
                RunExternalProgram(ref args); break;
            case "Выполнить программу Dotnet":
                RunDotnetProgram(ref args); break;   
            case "Собрать рабочий стол":
                RunDesctopToolBuilder(ref args); break;
            case "Выход":
                Process.GetCurrentProcess().Kill();
                break;
            default:
                throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Выполнить внешнюю программу
    /// </summary> 
    private static void RunExternalProgram(ref string[] args)
    {        
        Clear();
        string path = null;
        var programs = ProgressProgram.Wait("Поиск программ на локальном компьютере", ()=> DirectoryProgram.GetProgramFiles());
        switch (path = ProgramDialog.SingleSelect("", 
            Concat<string>(programs, "Назад"), 
            ref args))
        {           
            case "Выход":
                Process.GetCurrentProcess().Kill();
                break;
            default:
                string command = $"{path.Substring(0, 2)} && cd {path.Substring(0, path.LastIndexOf("\\"))} && {path}";
                Info(SystemProgram.Execute(command));

                break;
        }
    }

    private static void RunDotnetProgram(ref string[] args)
    {
        Clear();
        string program = null;
        switch (program = ProgramDialog.SingleSelect("",
            Concat<string>(GetProgramNames(), "Назад"),
            ref args))
        {
            case "Выход":
                Process.GetCurrentProcess().Kill();
                break;
            default:
                string wrk = System.IO.Directory.GetCurrentDirectory();
                ProcessStartInfo info = new ProcessStartInfo("CMD",
                            $"/K " + @"""" + wrk.Substring(0, 2) + $" && cd {wrk} && dotnet run --project {program}" + @"""");
                info.RedirectStandardError = true;
                info.RedirectStandardOutput = true;
                info.UseShellExecute = false;

                System.Diagnostics.Process process = System.Diagnostics.Process.Start(info);
           
                process.WaitForExit(); 
     
                break;
        }
    }

    public static IEnumerable<T> Concat<T>(IEnumerable<T> l, T r)
    {
        var result = new ConcurrentBag<T>();
        l.ToList().ForEach(result.Add);
        result.Add(r);
        return result;
    }
    public static IEnumerable<T> Concat<T>(IEnumerable<T> l, IEnumerable<T> r)
    {
        var result = new ConcurrentBag<T>();
        l.ToList().ForEach(result.Add);
        r.ToList().ForEach(result.Add);
        return result;
    }
 

    [Label("Переход в меню редактора рабочего стола")]
    private static void RunDesctopToolBuilder(ref string[] args)
    {
        Clear();
        switch (ProgramDialog.SingleSelect("Выберите действие", new string[]{
            "Создать ярлыки","Назад" },ref args))
        {
            case "Создать ярлыки":
                CreateLinks(ref args);
                RunDesctopToolBuilder(ref args);
                break; 
            case "Назад":
                MainMenu(ref args);
                break;
            default: 
                throw new NotImplementedException();
        }
    }


    [Label("Переход в меню редактора рабочего стола")]
    private static void CreateLinks(ref string[] args)
    {
        string text = InputDirPath("Укажите путь к реестру приложений (оставьте поле пустым что исп. значение " + DEFAULT_LINKS_DIR + ").", DEFAULT_LINKS_DIR);
        Console.WriteLine("Вы ввели: " + text);
        CreateLinks(DEFAULT_PROJECTS_DIR, text);
        ConfirmContinue();
    }

    /// <summary>
    /// Каталог хранения ярлыков по-умолчанию
    /// </summary>
    private const int DEFAULT_SEARCH_LEVEL = 2;
    private const string DEFAULT_LINKS_DIR = @"D:\Links";
    private const string DEFAULT_PROJECTS_DIR = @"D:\Projects";
    private const string CS_PROJECT_FILE_EXT = @"*.csproj";

    public static IEnumerable<string> GetProgramNames()
      => FileSearch(DEFAULT_PROJECTS_DIR, CS_PROJECT_FILE_EXT, DEFAULT_SEARCH_LEVEL);
             
    /// <summary>
    /// Создаёт ярлык для исполнения программы
    /// </summary>
    public static string CreateLink(string program, string dir, string link)
    {
        ProcessStartInfo info = new ProcessStartInfo("PowerShell.exe", 
            $"/C "+@""""+dir.Substring(0, 2)+$" && cd {dir} && {program}"+@"""");
        info.RedirectStandardError = true;
        info.RedirectStandardOutput = true;
        info.UseShellExecute = false;

        System.Diagnostics.Process process = System.Diagnostics.Process.Start(info);
        System.IO.StreamReader reader = process.StandardOutput;

        string result = reader.ReadToEnd();

        return result;
    }


    /// <summary>
    /// Создать ярлыки на запуск обнаруженных проектов в заданном каталоге
    /// </summary>
    static int CreateLinks(string ProjectsDir, string LinksDir )
    {
        if(System.IO.Directory.Exists(LinksDir) == false)
        {
            try
            {
                System.IO.Directory.CreateDirectory(LinksDir);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        int n = 0;
        System.IO.Directory.GetFiles(LinksDir,"*.bat").ToList().ForEach(path => {
            
            if (path.EndsWith(".bat"))
            {
                Console.WriteLine("Удаляю "+path);
                System.IO.File.Delete(path);
            }
        });
        foreach( string file in FileSearch(ProjectsDir, "*.csproj", 2))
        {
            Console.WriteLine(file);
            string shortName = file.Substring((int)(Math.Max(file.LastIndexOf("\\"), file.LastIndexOf("/")) + 1));

            string linkPath = System.IO.Path.Combine(LinksDir, shortName.Replace(".csproj", ".bat"));
            Console.WriteLine(linkPath);
            System.IO.File.WriteAllText(linkPath, $"{file.Substring(0,2)} && cd {file.Substring(0,file.Length-shortName.Length)} && dotnet watch run");
            n++;
            //CreateLink(linkPath, @"C:\users\reuser\desctop",shortName);
        }
        return n;
    }





    /// <summary>
    /// Поиск файлов по шаблону для максимальной глубины директорий установленной заданным значением
    /// </summary>
    public static List<string> FileSearch(string root, string pattern, int levels)
    {
        
        var res = new List<string>();
        if(levels > 0)
        {
            if (System.IO.Directory.Exists(root) == false)
            {
                throw new Exception("Путь к каталогу задан неверно: " + root);
            }
            try
            {
                foreach (var dir in System.IO.Directory.GetDirectories(root).ToList())
                {
                    try
                    {
                        res.AddRange(dir.IndexOf("$") == -1 ? FileSearch(dir, pattern, levels - 1) : new List<string>());
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        continue;
                    }
                    finally
                    {

                    }
                }
            }catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                var files = System.IO.Directory.GetFiles(root, pattern);
                res.AddRange(files);
            }catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            
        }
        
        return res;
    }


    
    /// <summary>
    /// Ввод пути к директории
    /// </summary>
    /// <param name="message">сообщение вывод в консоль перед вводом</param>
    /// <param name="defaultValue">значение по умолчанию</param>
    /// <returns></returns>
    private static string InputDirPath(string message="", string defaultValue = "")
    {
        Console.WriteLine(message);
        Console.Write(">");
        string inputed = Console.ReadLine();

        if (String.IsNullOrEmpty(inputed))
            return defaultValue;
        else return inputed;
    }
 
 
}
