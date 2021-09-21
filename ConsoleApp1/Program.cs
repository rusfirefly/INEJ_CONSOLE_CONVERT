using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Ionic.Zip;
//using System.IO.Compression;


namespace InejConvertConsole
{
    class Program
    {
        static INIManager ini;

        static string global_path;
        static string name_file_bin, function_name;

        static bool F1, F2, state;
        static int count_tests;
        static int cnt_files = 0;
        static private bool Check(string[] files)
        {
            FileInfo fi = null;

            for (int i = 0; i < files.Length; i++)
            {
                fi = new FileInfo(files[i]);
                if (fi.Name.Contains("Страница"))  return true;

                cnt_files++;
            }

            cnt_files = 0;
            WriteConsolt("[НЕТ ФАЙЛОВ]", ConsoleColor.Red);
            Console.WriteLine("------------------------------------------------------------------");
            Environment.ExitCode = 1;

            return false;
        }
        private static void LoadAndConvert(string path)
        {
            //поиск bin файлов

            try
            {
                string[] files = Directory.GetFiles(path, "*.BIN");
           
                if (files.Length > 0)
                {
                    CreatePAS(files[0]);
                }
                else
                {
                    //bin не найден поиск *-N.txt и конверт в bin
                    files = Directory.GetFiles(path, "*.TXT");

                    if (!Check(files))  return;//проверяем есть ли файлы в папке для конвертация

                    //создаем новый bin файл и записываем данные
                    FileStream bin_f = null;//= new FileStream(path + @"\temp.BIN", FileMode.Create);
                    BinaryWriter bw = null;// = new BinaryWriter(bin_f);
                    StreamReader f = null;
                    string[] file_arr = new string[files.Length];

                    bool state = false;
                    if (File.Exists(path + "\\Страница0.TXT") || File.Exists(path + "\\Страница-0.TXT"))
                        state = true;

                    for (int i = 0; i < files.Length - cnt_files; i++)
                    {
                        //StreamReader f = new StreamReader(files[i]);
                        string file = "";
                        
                        if(state)
                            file = path + "\\Страница" + (i).ToString() + ".TXT";
                        else
                            file = path + "\\Страница" + (i + 1).ToString() + ".TXT";

                        if (File.Exists(file))
                            f = new StreamReader(file);
                        else
                        {

                            if(state)
                                file = path + "\\Страница" + (i).ToString() + ".TXT";
                            else
                                file = path + "\\Страница-" + (i + 1).ToString() + ".TXT";

                            if (!File.Exists(file))
                            {
                                WriteConsolt("[НЕТ ФАЙЛОВ]", ConsoleColor.Red);
                                Console.WriteLine("------------------------------------------------------------------");
                                Environment.ExitCode = 1;
                                return;
                            }
                        }

                        f = new StreamReader(file);
                        if (bin_f == null)
                        {
                            bin_f = new FileStream(path + @"\temp.BIN", FileMode.Create);
                            bw = new BinaryWriter(bin_f);
                        }

                        file_arr[i] = file;
                        while (!f.EndOfStream)
                        {
                            string[] s = f.ReadLine().Split(' ');
                             for (int j = 0; j < s.Length; j++)
                             {
                                if (s[j] != "")
                                {
                                    byte v = byte.Parse(s[j], System.Globalization.NumberStyles.HexNumber);
                                    bw.Write(v);
                                }
                             }
                        }
                        f.Close();
                    }
                    bw.Close();

                    string[] name = GetName(path + @"\temp.BIN").Split(' ');

                    if(name[0] == "ошибка404")
                    {
                        //по условию длина максимум 72 символа...если больше 100 явно косяк
                        WriteConsolt("[ФАЙЛ ПОВРЕЖДЕН]", ConsoleColor.Red);
                        Console.WriteLine("------------------------------------------------------------------");
                        if (File.Exists(path + @"\temp.BIN")) File.Delete(path + @"\temp.BIN");
                        Environment.ExitCode = 1;
                        return;
                    }

                    name_file_bin = name[3].Replace("-", "");

                   
                    if (name_file_bin.Length < 4)
                        name_file_bin = name[2].Replace("-", "");

                    File.Move(path + @"\temp.BIN", path + "\\" + name_file_bin + ".BIN");
                    CreatePAS(path + @"\" + name_file_bin + ".bin");//!
                }
            }
            catch(Exception ex)
            {
                WriteConsolt("[ОШИБКА] " + ex.Message, ConsoleColor.Red);
                Console.WriteLine("------------------------------------------------------------------");
                if (File.Exists(path + @"\temp.BIN"))  File.Delete(path + @"\temp.BIN");
                Environment.ExitCode = 1;
                return;
            }
}

        #region Заголовок
        private static string GetName(string bin_file)
        {
            string caption = "";
            using (FileStream fs = new FileStream(bin_file, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    //br.ReadBytes(32);
                    int position = 0;
                    do
                    {
                        position++;
                    } while (br.ReadByte() != 1);

                    br.ReadBytes(5);

                    //получчаем заголовок
                    byte b = 0;

                    do
                    {
                        b = br.ReadByte();
                        caption += GOST_ASCII(b);

                        if(caption.Length > 100)//если bin файл другой структуры
                        {
                            return "ошибка404";
                        }
                    } while (b != 13);//0D
                }
            }
            return caption;
        }
        #endregion
        #region кол-во тестов
        private static int GetCountTests(string bin_file)
        {
            int cntTest = 0;
            state = false;
            using (FileStream fs = new FileStream(bin_file, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    try
                    {
                        //br.ReadBytes(32);
                        int position = 0;
                            do
                            {
                                position++;
                            } while (br.ReadByte() != 1);

                            br.ReadBytes(5);

                            //получчаем заголовок
                            byte b = 0;
                            string caption = "";
                            do
                            {
                                b = br.ReadByte();
                                caption += GOST_ASCII(b);

                            } while (b != 13);//0D

                            if(caption.Length>100)
                            {
                                WriteConsolt("[ФАЙЛ ПОВРЕЖДЕН]", ConsoleColor.Red);
                                Console.WriteLine("------------------------------------------------------------------");
                            if (File.Exists(bin_file)) File.Delete(bin_file);
                                Environment.ExitCode = 1;
                                return -1;
                            }
                            //группа годных
                            byte Group_Good = br.ReadByte();
                            br.ReadByte();

                            //тест N
                            do
                            {
                                byte[] bt;
                                //первые 5 слов, режим измерения
                                for (int i = 0; i < 5; i++)
                                {
                                    bt = br.ReadBytes(2);
                                    if (i == 0) 
                                        cntTest++;
                                    if (bt[0] == 0x2E && bt[1] == 0x80)
                                    {
                                        return cntTest - 1;
                                    }
                                }
                    
                            //6 слово УПГ (условный переход по генерации)
                            bt = null;
                            bt = br.ReadBytes(2);

                            for (int i = 0; i < 48; i++)
                            {
                                bt = br.ReadBytes(2);
                                if (bt[0] == 0x3b && bt[1] == 0x00) 
                                    break;

                                if (bt[0] == 0x00 && bt[1] == 0x3b)
                                {
                                    WriteConsolt("[НАРУШЕНА СТРУКТУРА ТЕСТА] № " + (cntTest-1).ToString() + "", ConsoleColor.Yellow);
                                    Console.WriteLine("------------------------------------------------------------------");
                                    state = true;
                                    br.ReadByte();
                                    break;
                                }
                            }

                        } while (true);

                }
                 catch
                {
                    WriteConsolt("[ФАЙЛ ПОВРЕЖДЕН]", ConsoleColor.Red);
                    Console.WriteLine("------------------------------------------------------------------");
                    if (File.Exists(bin_file)) File.Delete(bin_file);
                    Environment.ExitCode = 1;
                    return -1;
                }
            }
                
            }
        }
        #endregion

        private static void CreateProjectD7(string path, string file_pas)
        {
            string template_path = Directory.GetCurrentDirectory() + "\\Template";
            Directory.CreateDirectory(global_path + "\\D7");

            //Создать идентичное дерево каталогов
            foreach (string dirPath in Directory.GetDirectories(template_path, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(template_path, global_path + "\\D7"));

            //Скопировать все файлы. И перезаписать(если такие существуют)
            string new_file = "";
            string name = Path.GetFileNameWithoutExtension(path);
            foreach (string newPath in Directory.GetFiles(template_path, "*.*", SearchOption.AllDirectories))
            {
                new_file = newPath.Replace("I_TEMPLATE", "I_" + name);
                FileInfo f = new FileInfo(new_file);
                if (f.Name == "uPLAN1.PAS")
                {
                    File.Copy(file_pas, new_file.Replace(template_path, global_path + "\\D7"), true);
                    File.Delete(file_pas);//удаляем временный файл
                }
                else
                    File.Copy(newPath, new_file.Replace(template_path, global_path + "\\D7"), true);
            }

            using (StreamWriter sw = new StreamWriter(global_path + "\\D7\\I_" + name + ".dpr", false, Encoding.GetEncoding("Windows-1251")))
            {

                sw.WriteLine("program I_" + name +";\n\n" +
                "uses\n" +
                "  Forms,\n" +
                "  SysUtils,\n" +
                "  uMain,\n" +
                "  uWP;\n\n" +
                "{$R *.res}\n\n" +
                "begin\n" +
                "  DecimalSeparator := '.';\n" +
                "  WP_INIT;\n\n" +
                "  Application.Initialize;\n" +
                "  Application.Title := 'Иней';\n" +
                "  Application.CreateForm(TfMain, fMain);\n" +
                "  Application.Run;\n" +
                "end.");
            }
            
           using(StreamWriter sw = new StreamWriter(global_path + "\\D7\\uWP.pas", false, Encoding.GetEncoding("Windows-1251")))
            {

                sw.WriteLine("unit uWP;\n\n" +
                            "interface\n\n" +
                            "procedure WP_Init;\n\n" +
                            "implementation\n\n" +
                            "uses\n" +
                            "    uPLAN1;\n\n" +
                            "var\n" +
                            "    PLAN1: TPLAN1;\n\n" +
                            "procedure WP_Init;\n" +
                            "begin\n" +
                            "\tPLAN1.INIT('" + name + "', 1);\nend;\n\nend.");
            }


            //ZipFile

            //ZipFile.CreateFromDirectory(global_path + "\\D7", global_path + "\\D7.zip");

            Encoding encoding = Encoding.GetEncoding("Windows-1251");
            ZipFile zipFile = new ZipFile(encoding);
            zipFile.AddDirectory(global_path + "\\D7");
            zipFile.Save(global_path+"\\D7.zip");
            Directory.Delete(global_path + "\\D7", true);
        }

        private static void CreatePAS(string bin_file)
        {
            string path = Path.GetDirectoryName(bin_file);
            string name = Path.GetFileNameWithoutExtension(bin_file);

            int cnt = GetCountTests(bin_file);
            count_tests = cnt;
            if(cnt == -1)
            {
                Environment.ExitCode = 1;
                return;
            }

            string file_pas = path + "\\" + name + ".pas";
            //создаем pas файл
            StreamWriter swPAS = new StreamWriter(file_pas, false, Encoding.GetEncoding("Windows-1251"));

            string label = "";
            for (int i = 0; i < cnt; i++)
                label += " T" + (i + 1).ToString() + ",";

            swPAS.WriteLine("unit uPLAN1;\n\n" +
                            "interface\n\n" +
                            "uses Inej_Globals, Inej_Functions, Inej_Plans, SysUtils;\n\n" +
                            "type TPLAN1 = object(TPLAN)\n" +
                            "  procedure PLAN; virtual;\n" +
                            "end;\n\n" +
                            "implementation\n\n" +
                            "PROCEDURE TPLAN1.PLAN;\n" +
                            "label \n   " + label + " en, en2;\n" +
                            "BEGIN\n" +
                            "  PROGRAMMA;\n");

            using (FileStream fs = new FileStream(bin_file, FileMode.Open, FileAccess.Read))
            {

                using (BinaryReader br = new BinaryReader(fs))
                {
                    try 
                    { 
                    string caption = "",
                           text = "";

                    #region Шапка
                    //br.ReadBytes(32);

                    int position = 0;
                    do
                    {
                        position++;
                    }
                    while (br.ReadByte() != 1);

                    br.ReadBytes(5);
                    #endregion

                    #region Заголовок
                    //получчаем заголовок
                    byte b = 0;
                    do
                    {
                        b = br.ReadByte();
                        caption += GOST_ASCII(b);

                    } while (b != 13);//0D

                    Console.WriteLine(caption.Replace("\n", ""));
                    swPAS.WriteLine("logwrite('" + caption.Substring(0, caption.Length - 1) + "');\n");
                    #endregion

                    #region Группа Годных
                    //группа годных
                    byte Group_Good = br.ReadByte();
                    swPAS.WriteLine("//ГОДНЫХ ГРУПП = " + Group_Good.ToString()+" \n");
                    br.ReadByte();
                    #endregion

                    #region Режим измерения

                    int numTest = 0;
                    //тест N
                    do
                    {
                        text = "";
                        byte[] bt;
                        double diapazon = 0;
                        byte parametr = 0;
                        string dw1 = "", dw2 = ",", dw3 = ",", dw4 = ",", dw5 = ",";

                        string name_test = "";
                        //первые 5 слов, режим измерения
                        for (int i = 0; i < 5; i++)
                        {
                            bt = br.ReadBytes(2);

                            if (bt[0] == 0x2E && bt[1] == 0x80)
                            {
                                #region End pas
                                swPAS.WriteLine("\nen:\n" +
                                                "  endtest;\n" +
                                                "en2:\n"+
                                                "  ENDPROGRAM;\n" +
                                                "END;\n\n" +
                                                "END.");
                                swPAS.Close();
                                #endregion

                                CreateProjectD7(bin_file, file_pas);
                                if (!state)
                                {
                                    WriteConsolt("[ГОТОВО]", ConsoleColor.Green);
                                    Console.WriteLine("------------------------------------------------------------------");
                                }

                                Environment.ExitCode = 0;
                                return;
                            }

                            if (i == 0) //U первое слово dw1
                            {
                                numTest++;
                                //swPAS.WriteLine("T" + numTest + ":");    //label
                                //swPAS.WriteLine("\ttest('T" + numTest + "','');");
                                parametr = bt[1];

                                #region параметр (Ice,Icbo,Iebo,h21e,Ucesat,Ubesat,Uceo,Uin,h21)
                                switch (parametr)
                                {
                                    case 1:
                                        dw1 = "Ice";
                                        break;
                                    case 2:
                                        dw1 = "Icbo";
                                        break;
                                    case 3:
                                        dw1 = "Iebo";
                                        break;
                                    case 4:
                                        dw1 = "h21e";//1+h21e
                                        break;
                                    case 5:
                                        dw1 = "Ucesat";
                                        break;
                                    case 6:
                                        dw1 = "Ubesat";
                                        break;
                                    case 7:
                                        dw1 = "Uceo";
                                        break;
                                    case 8:
                                        dw1 = "h21";//1+B0
                                        break;
                                    case 9:
                                        dw1 = "Uin";
                                        break;
                                }
                                    #endregion

                                name_test = dw1;
                                #region Rбэ и (pnp/npn)
                                    if ((bt[0] & 0x80) == 0x80) dw1 += ",pnp";
                                else dw1 += ",npn";
                                //r0,r1,r2,r3,r4,r5,r6,r7
                                dw1 += ",r" + (bt[0] & 0x7).ToString();

                                #endregion

                                #region диапазон (В)
                                if (bt[0] == 0x7)
                                {
                                    diapazon = 1.0;     //0-1В
                                    dw1 += ",D1";
                                }
                                else
                                if (bt[0] == 0x8)
                                {
                                    diapazon = 10.0;    //0-10В
                                    dw1 += ",D10";
                                }
                                else
                                if (bt[0] == 0x9)
                                {
                                    diapazon = 100.0;   //0-100В
                                    dw1 += ",D100";
                                }
                                else
                                if (bt[0] == 0x10)
                                {
                                    diapazon = 500.0; //0-500В
                                    dw1 += ",D500";
                                }
                                else
                                {
                                    dw1 += ",D0";
                                }
                                #endregion
                            }

                            if (i == 1) //I A dw2 (nA,uA,mA,A)
                            {
                                #region Значение
                                double zna4 = (bt[1] & 0xF) + ((bt[0] & 0xF0) >> 4) / 10.0 + (bt[0] & 0xF) / 100.0;
                                #endregion

                                #region Множитель
                                double mult = 0;
                                string IA = "uA";

                                switch ((bt[1] & 0xF0) >> 4)
                                {
                                    case 4:
                                        mult = 100;
                                        IA = "nA";
                                        break;
                                    case 5:
                                        mult = 1;
                                        IA = "uA";
                                        break;
                                    case 6:
                                        mult = 10;
                                        IA = "uA";
                                        break;
                                    case 7:
                                        mult = 1;//
                                        IA = "uA";
                                        break;
                                    case 8:
                                        mult = 1;
                                        IA = "mA";
                                        break;
                                    case 9:
                                        mult = 10;
                                        IA = "mA";
                                        break;
                                    case 10:
                                        mult = 100;
                                        IA = "mA";
                                        break;
                                    case 11:
                                        mult = 1;
                                        IA = "A";
                                        break;
                                }
                                zna4 *= mult;
                                #endregion

                                dw2 += " " + zna4.ToString("0.###").Replace(",", ".") + ", " + IA;
                            }

                            if (i == 2) //I Б (nA,uA,mA,A)
                            {
                                #region Значение
                                double zna4 = (bt[1] & 0xF) + ((bt[0] & 0xF0) >> 4) / 10.0 + (bt[0] & 0xF) / 100.0;
                                #endregion

                                #region Множитель
                                double mult = 0;
                                string IA = "uA";
                                switch ((bt[1] & 0xF0) >> 4)
                                {
                                    case 3:
                                        mult = 10;
                                        IA = "nA";
                                        break;
                                    case 4:
                                        mult = 100;
                                        IA = "nA";
                                        break;
                                    case 5:
                                        mult = 1;
                                        IA = "uA";
                                        break;
                                    case 6:
                                        mult = 10;
                                        IA = "uA";
                                        break;
                                    case 7:
                                        mult = 100;//
                                        IA = "uA";
                                        break;
                                    case 8:
                                        mult = 1;
                                        IA = "mA";
                                        break;
                                    case 9:
                                        mult = 10;
                                        IA = "mA";
                                        break;
                                    case 10:
                                        mult = 100;
                                        IA = "mA";
                                        break;
                                }
                                zna4 *= mult;
                                #endregion

                                dw3 += " " + zna4.ToString("0.###").Replace(",", ".") + "," + IA;
                                if (parametr <= 3) diapazon = mult * 10;
                            }

                            if (i == 3)// (В напряжение)
                            {
                                #region Значение
                                double zna4 = (bt[1] & 0xF) + ((bt[0] & 0xF0) >> 4) / 10.0 + (bt[0] & 0xF) / 100.0;
                                #endregion

                                #region Множитель
                                double mult = 0;

                                switch ((bt[1] & 0xF0) >> 4)
                                {
                                    case 0:
                                        mult = 1;
                                        break;
                                    case 7:
                                        mult = 0.1;
                                        break;
                                    case 8:
                                        mult = 1;
                                        break;
                                    case 9:
                                        mult = 10;
                                        break;
                                    case 10:
                                        mult = 100;
                                        break;
                                }
                                #endregion

                                dw4 += " " + (zna4 * mult).ToString().Replace(",", ".") + ",V";
                            }

                            if (i == 4)//Длительность
                            {
                                int T1 = 0;
                                int T2 = 0;

                                T1 = ((bt[1] & 0xF0) >> 4) * 10 + (bt[1] & 0xF);
                                T2 = ((bt[0] & 0xF0) >> 4) * 100 + (bt[0] & 0xF) * 10;

                                dw5 += " " + T1.ToString() + ", " + T2.ToString();

                                string units = "";
                                if (dw1.Split(',')[0] == "h21e" || dw1.Split(',')[0] == "h21") 
                                        units = "";
                                else
                                {
                                    if(dw1.Split(',')[3] == "D0")
                                    {
                                        units = dw3.Split(',')[2];
                                    }
                                    else
                                    {
                                        if (dw1.Split(',')[3] == "D1")
                                           units = "mV";
                                        else
                                           units = "V";
                                        }
                                }
                                 //string sss = "\ttest('T" + numTest + "','"+units+"');";
                                 swPAS.WriteLine("T" + numTest + ":");    //label
                                 swPAS.WriteLine("\t//test('" + name_test + "','" + units + "');");
                                 swPAS.WriteLine("\ttest('T" + numTest + "','');");

                                }

                            if (F1)
                                text += "$" + bt[0].ToString("X2") + "" + bt[1].ToString("X2") + ",";
                            else
                                text += "$" + bt[1].ToString("X2") + "" + bt[0].ToString("X2") + ",";

                        }

                        //или такая запись inpdata(Ice,npn,r2,D0,0,uA,100,uA,30,V,1,10);
                        if (!F2)
                        {
                            swPAS.WriteLine("\t     //inpdata(" + dw1 + dw2 + dw3 + dw4 + dw5 + ");");
                            swPAS.WriteLine("\t     "+ function_name + "(" + text.Substring(0, text.Length - 1) + ");");
                        }
                        else
                        {
                            swPAS.WriteLine("\t     inpdata(" + dw1 + dw2 + dw3 + dw4 + dw5 + ");");
                            swPAS.WriteLine("\t     //" + function_name + "(" + text.Substring(0, text.Length - 1) + ");");
                        }

                        //6 слово УПГ (условный переход по генерации)
                        bt = null;
                        bt = br.ReadBytes(2);

                            //int number = bt[0];// (bt[0]&0xF);

                            if ((bt[1] & 0x8) == 0x8)
                            {
                                swPAS.WriteLine("\t     IF (sign = osc) THEN begin GROUP(" + bt[0].ToString() + ", " + ((bt[0] > Group_Good) ? "FAIL" : "PASS") + "); goto en; end; //УПГ 0x" + BitConverter.ToString(bt).Replace("-", ""));
                                //if(bt[0] > 0xf)
                                //    WriteConsolt("[ГР"+bt[0].ToString()+"\t> 15] №T" + numTest.ToString(), ConsoleColor.Yellow);
                            }
                            else
                            {
                                if (bt[0] != 0)//T0
                                    swPAS.WriteLine("\t     IF (sign = osc) THEN goto T" + bt[0].ToString() + "; //УПГ 0x" + BitConverter.ToString(bt).Replace("-", ""));
                            }

                            byte cntF = 1, count_yp = 1;
                            double norma = 0;
                            for (int i = 0; i < 48; i++)
                            {
                                bt = br.ReadBytes(2);

                                if (bt[0] == 0x3b && bt[1] == 0x00)
                                    break;

                                if (bt[0] == 0x00 && bt[1] == 0x3b)
                                {
                                    //(ТО 092\12913  КТ315И-1КЛ-536\ на 24 тесте лишний байт. (сдвиг)
                                    br.ReadByte();
                                    break;
                                }

                                if (cntF == 1)
                                {
                                    //ПР
                                    if (bt[0] + bt[1] != 0)
                                    {
                                        int a = bt[1] >> 0x4,
                                            d = bt[1] & 0xF,
                                            s = bt[0] >> 0x4,
                                            t = bt[0] & 0xf;

                                        if (diapazon != 0)
                                            norma = ((a) + d / 10.0 + s / 100.0 + t / 1000.0) * (diapazon / 10.0);
                                        else
                                            norma = ((a) + d / 10.0 + s / 100.0 + t / 1000.0) * 100;
                                    }
                                }

                                if (cntF == 2)
                                {
                                    //УПБ
                                    if (bt[0] + bt[1] != 0)
                                    {
                                        if ((bt[1] & 0x8) == 0x8)
                                        {
                                            swPAS.WriteLine("\t     IF (more(" + norma.ToString("0.##########").Replace(",", ".") + ")) THEN begin GROUP(" + bt[0].ToString() + ", " + ((bt[0] > Group_Good) ? "FAIL" : "PASS") + "); goto en; end; // УПБ" + count_yp.ToString() + " 0x" + BitConverter.ToString(bt).Replace("-", ""));
                                            //if (bt[0] > 0xf)
                                            //    WriteConsolt("[ГР" + bt[0].ToString() + "\t > 15] №T" + numTest.ToString(), ConsoleColor.Yellow);

                                        }
                                        else
                                        {
                                            if (bt[0] != 0)//T0
                                                swPAS.WriteLine("\t     IF (more(" + norma.ToString("0.##########").Replace(",", ".") + ")) THEN goto T" + bt[0].ToString() + ";// УПБ" + count_yp.ToString() + " 0x" + BitConverter.ToString(bt).Replace("-", ""));
                                        }
                                    }
                                }

                                if (cntF == 3)
                                {
                                    //УПМ
                                    if (bt[0] + bt[1] != 0)
                                    {
                                        if ((bt[1] & 0x8) == 0x8)
                                        {
                                            swPAS.WriteLine("\t     IF (less(" + norma.ToString("0.##########").Replace(",", ".") + ")) THEN begin GROUP(" + bt[0].ToString() + "," + ((bt[0] > Group_Good) ? "FAIL" : "PASS") + "); goto en; end;//УПМ" + count_yp.ToString() + " 0x" + BitConverter.ToString(bt).Replace("-", ""));
                                            //if (bt[0] > 0xf)
                                            //    WriteConsolt("[ГР" + bt[0].ToString() + "\t > 15] №T" + numTest.ToString(), ConsoleColor.Yellow);

                                        }
                                        else
                                        {
                                            if (bt[0] != 0)//T0
                                                swPAS.WriteLine("\t     IF (less(" + norma.ToString("0.##########").Replace(",", ".") + ")) THEN goto T" + bt[0].ToString() + ";//УПМ" + count_yp.ToString() + " 0x" + BitConverter.ToString(bt).Replace("-", ""));
                                        }
                                    }
                                    count_yp++;
                                    cntF = 0;
                                }
                                
                                cntF++;
                            }

                        if(numTest == count_tests)
                            swPAS.WriteLine("\tendtest;\n\tgoto en2;\n");
                        else
                            swPAS.WriteLine("\tendtest;\n");
                        #endregion

                    } while (true);

                    }
                    catch 
                    {
                        WriteConsolt("[ФАЙЛ ПОВРЕЖДЕН]", ConsoleColor.Red);
                        Console.WriteLine("------------------------------------------------------------------");
                        Environment.ExitCode = 1; 
                        return;
                    }
                
                }
            }
        }

        #region GOAST_ASCII
        private static char GOST_ASCII(byte ch)
        {
            char b = Convert.ToChar(ch);

            switch (ch)
            {
                case 64:    b = '@'; break;
                case 80:    b = 'P'; break;
                case 96:    b = 'Ю'; break;
                case 112:   b = 'П'; break;
                case 65:    b = 'A'; break;
                case 81:    b = 'Q'; break;
                case 97:    b = 'А'; break;
                case 113:   b = 'Я'; break;
                case 66:    b = 'B'; break;
                case 82:    b = 'R'; break; 
                case 98:    b = 'Б'; break;
                case 114:   b = 'Р'; break;
                case 67:    b = 'C'; break;
                case 83:    b = 'S'; break;
                case 99:    b = 'Ц'; break;
                case 115:   b = 'С'; break;
                case 68:    b = 'D'; break;
                case 84:    b = 'T'; break;
                case 100:   b = 'Д'; break;
                case 116:   b = 'Т'; break;
                case 69:    b = 'E'; break;
                case 85:    b = 'U'; break;
                case 101:   b = 'Е'; break;
                case 117:   b = 'У'; break;
                case 70:    b = 'F'; break;
                case 86:    b = 'V'; break;
                case 102:   b = 'Ф'; break;
                case 118:   b = 'Ж'; break;
                case 71:    b = 'G'; break;
                case 87:    b = 'W'; break;
                case 103:   b = 'Г'; break;
                case 119:   b = 'В'; break;
                case 72:    b = 'H'; break;
                case 88:    b = 'X'; break;
                case 104:   b = 'Х'; break;
                case 120:   b = 'Ь'; break;
                case 73:    b = 'I'; break; 
                case 89:    b = 'Y'; break;
                case 105:   b = 'И'; break;
                case 121:   b = 'Ы'; break;
                case 74:    b = 'J'; break;
                case 90:    b = 'Z'; break;
                case 106:   b = 'Й'; break;
                case 122:   b = 'З'; break;
                case 75:    b = 'K'; break; 
                case 91:    b = '['; break;
                case 107:   b = 'К'; break;
                case 123:   b = 'Ш'; break;
                case 76:    b = 'L'; break; 
                case 92:    b = '\\'; break;
                case 108:   b = 'Л'; break;
                case 124:   b = 'Э'; break;
                case 77:    b = 'M'; break; 
                case 93:    b = ']'; break;
                case 109:   b = 'М'; break;
                case 125:   b = 'Щ'; break;
                case 78:    b = 'N'; break; 
                case 94:    b = '-'; break;
                case 110:   b = 'Н'; break;
                case 126:   b = 'Ч'; break;
                case 79:    b = 'O'; break; 
                case 95:    b = '_'; break;
                case 111:   b = 'О'; break;
                case 127:   b = '-'; break;
            }

            return b;
        }
        #endregion
        private static void WriteConsolt(string msg, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        static void Main(string[] args)
        {

            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            string path_ini = Directory.GetCurrentDirectory() + "\\options.ini";
            ini = new INIManager(path_ini);

            function_name = ini.GetPrivateString("options", "function_name");

            F1 = F2 = false;
            count_tests = 0;

            string path = "";
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    if (args[1] == "f1") F1 = true;
                    else
                    if (args[1] == "f2") F2 = true;
                }

                path = args[0];
                if (File.Exists(path))
                {
                    global_path = Path.GetDirectoryName(path);
                    CreatePAS(path);
                }
                else
                {
                    /*
                     функция LoadAndConvert(путь) проверяет что в папке, если есть BIN, то выполняет преобразование всех найденных файлов в pas
                     если нет bin файлов, то конвертирует все найденные TXT в bin, а потом преобразует их в pas
                    */
                    global_path = path;
                    LoadAndConvert(path);
                }
            }
            else
            {
                WriteConsolt("[НЕВЕРНЫЙ ПУТЬ]", ConsoleColor.Red);
                Console.WriteLine("------------------------------------------------------------------");
                Environment.ExitCode = 1;
            }

            Environment.ExitCode = 0;

            //Console.Write("\nPress <Enter> to exit... ");
            //while (Console.ReadKey().Key != ConsoleKey.Enter) { }
        }

    }
}
