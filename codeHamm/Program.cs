using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace codeHamm
{
    class Program
    {
        /// <summary>
        /// Интро без проверки на корректность данных и прочего
        /// </summary>
        /// <returns>Строку из 0 и 1</returns>
        static string Intro()
        {
            Console.WriteLine("-------------------------\n");
            Console.Write("Введите(dec) число: ");
            var str = string.Empty;
            Console.ForegroundColor = ConsoleColor.Cyan;
            str = Console.ReadLine();
            Console.ResetColor();
            str = TakeSTRBinary(str);
            Console.Write("Входные данные(bin)11 бит: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(str);
            Console.WriteLine();
            Console.ResetColor();
            return str;

        }
        static string TakeSTRBinary(string str)
        {
            var y = 0;

            int.TryParse(str, out y);

            var binarychar = Convert.ToString(y, 2).PadLeft(11, '0').ToArray();
            return new string(binarychar);
        }
        static void Main(string[] args)
        {
            //Естсественно, всё можно было сделать намного проще) //          
            while (true)
            {
                var edata = Intro();
                Hamming_1511 hm = new Hamming_1511();
                ShowCodeHelpers show = new ShowCodeHelpers();
                ProxyResivbyHardware transfer = new ProxyResivbyHardware();
                //прижелании можно отключить подписку
                hm.Bin += show.showBinary;
                hm.Hex += show.showHex;
                hm.Dec += show.showDecimal;
                hm.Transfer += transfer.bitError;
                hm.errBit += show.ShowPoiledBit;
                hm.sucsec += show.ShowSuccess;
                hm.Run(edata);

            }
        }
        static private bool IsTextValid(string s)
        {
            Regex regex = new Regex(@"^[0-9 ]+$");
            return !regex.IsMatch(s);
        }

    }
    
    #region Аппаратнаая передача данных по зашумленному канала
    class ProxyResivbyHardware
    {
        public string bitError(string s)
        {
            Console.Write("Какой № бита инвертировать? ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            var bit = Convert.ToInt32(Console.ReadLine());
            Console.ResetColor();
            Console.Write("Значение бита: ");
            var binarychar = s.ToArray();
            Console.ForegroundColor = ConsoleColor.Cyan;
            binarychar[bit-1] = Convert.ToChar(Console.ReadLine());
            Console.ResetColor();
            return new string(binarychar);
        }
       
    }
    #endregion

    /// <summary>
    /// Класс кодирования и декодирования по Хеммингу(15, 11)
    /// 
    /// </summary>
    class Hamming_1511
    {
        public delegate string Broadcast(string s);
        public delegate void GG();
        public delegate void GuiShowCode(string code);
        public delegate void ShowSpoiledBit(int index,string Code);
        public event GuiShowCode Bin;
        public event GuiShowCode Dec;
        public event GuiShowCode Hex;
        public event Broadcast Transfer;
        public event ShowSpoiledBit errBit;
        public event GG sucsec;
        private int[,] Position;
        private string syndrome;
        private  List<string> BDerrors;
        public string Code { get; protected set; }
        private string Data { get; set; }
        /// <summary>
        /// Кодирует систематическим кодом 
        /// Работает в автоматическом режиме метод- Run()
        /// по подписке устанавливается отражение процесса кодирования и декодирования,
        /// а так же инвертирование бита
        /// </summary>
        /// <param name="data">Двоичное 11 битное число</param>
        public Hamming_1511()
        {
            //позиции проверочных битов(не Gmatrix!)
            Position = new int[4, 8] {
            {7,6,5,4,3,2,1,0 },
            {11,10,9,8,3,2,1,0 },
            {13,12,9,8,5,4,1,0 },
            {14,12,10,8,6,4,2,0 }};
            BDerrors = new List<string>(new string[] {
               "1111","1110","1101","1100","1011","1010","1001",
               "0111","0110","0101","0100","0011","0010","0001" });
        }
        public void Run(string data)
        {
            if (StatusOk(data) == true) {          
            
                Data = data;                
                Encoding();
                Console.WriteLine();
                Bin?.Invoke(Code);
                Dec?.Invoke(Code);
                Hex?.Invoke(Code);
                if (Transfer != null)
                {
                    Code = Transfer.Invoke(Code);
                    Decoding();
                    DeleteError();
                }
            }
            else
            {
                Non("Входные данные > 11 бит!");
            }
        }
        private bool StatusOk(string code)
        {
            if (code.Length > 11)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        /// <summary>
        /// Вызывает исключение
        /// </summary>
        /// <param name="s">строка исключения</param>
        private void Non(string s)
        {            
            throw new Exception(s);
        }
        /// <summary>
        /// Преобразует 11 битное слово в 16 битное, приэтом 16(0) == 0
        /// </summary>      
        public void Encoding()
        {
            var code = new List<char>(new char[16]);
            var c = 10;uint t = 0;
            for (int i = 15; i >= 0; i--, t++)
            {
                if (IsPowTwo(t) == true)
                {
                    code[i] = '0';
                }
                else
                {
                    code[i] = Data[c--];                    
                }
            }           
            //15,14,12,7
            Code = new string(code.ToArray());
            Xor();                  
        }
        /// <summary>
        /// Ручное кодирование
        /// </summary>
        /// <param name="edata">11 битное слово</param>
        /// <returns> 16 битное слово</returns>
        public string Encoding(string edata)
        {
            var code = new List<char>(new char[16]);
            var c = 10; uint t = 0;
            for (int i = 15; i >= 0; i--, t++)
            {
                if (IsPowTwo(t) == true)
                {
                    code[i] = '0';
                }
                else
                {
                    code[i] = Data[c--];
                }
            }          
            Code = new string(code.ToArray());
            Xor();
            return Code;
        }

        /// <summary>
        /// Самая простая реализация кодирования слова
        /// систематическая(1 2 4 8) бит
        /// </summary>
        /// <returns>Синдром ошибки</returns>
        private string Xor()
        {
            var codeint = Code.ToCharArray().
                Select((x, y) => x == '1' ? 1 : 0).
                ToArray();
            var synd = new int[4];

            for (int i = 0; i < synd.Length; i++)
            {
                for (int j = 0; j < Position.GetLength(1); j++)
                {
                    synd[i] ^= codeint[Position[i, j]];
                }
            }

            codeint[Position[0, 0]] = synd[0];
            codeint[Position[1, 0]] = synd[1];
            codeint[Position[2, 0]] = synd[2];
            codeint[Position[3, 0]] = synd[3];

            var v = codeint.Select(x => x.ToString()).ToArray();
            Code = string.Join("", v);
            var a = synd.Select(x => x.ToString()).ToArray();
            var Ssynd = string.Join("", a);

            return Ssynd;
        }     

        /// <summary>
        /// Определяет является ли число степенью двойки
        /// </summary>
        /// <param name="f">целое число</param>
        /// <returns>возвращает "true", если число стпень двойки, "false"-если не стпень или 0 бит</returns>
        private bool IsPowTwo(uint f)
        {
            if ((f & (f - 1)) == 0)//Например,0010(0010-0001) ->                
            {                     // 0010&0001=0000
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// Ручное кодирование
        /// </summary>
        /// <param name="str">11 битное число</param>
        /// <returns> синдром ошибки</returns>
        public string Decoding(string str)
        {
            if (StatusOk(str) == true)
            {
                var codeint = Code.ToCharArray().
                   Select((x, y) => x == '1' ? 1 : 0).
                   ToArray();
                var synd = new int[4];

                for (int i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 8; j++)
                    {
                        synd[i] ^= codeint[Position[i, j]];
                    }
                }

                var a = synd.Select(x => x.ToString()).ToArray();
                syndrome = string.Join("", a);
                Console.WriteLine(syndrome);
            }
            return syndrome;

        }      
        private void Decoding()
        {
            var codeint = Code.ToCharArray().
                Select((x, y) => x == '1' ?  1 : 0).
                ToArray();
            var synd = new int[4];

            for (int i = 0; i <4 ; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    synd[i] ^= codeint[Position[i, j]];
                }
            }

            var a = synd.Select(x => x.ToString()).ToArray();
            syndrome = string.Join("", a);
            Console.WriteLine(syndrome);                       

        }
       
        private void DeleteError()
        {
            if (syndrome != "0000")
            {                       
                int v = BDerrors.FindIndex(sf => syndrome == sf);
                Console.WriteLine("Ошибка в {0} бите ",v+1);
                var c = Code.ToArray();
                c[v] = Invertion(c[v]);
                Code = new string(c);
                errBit?.Invoke(v - 1, Code);
                Hex?.Invoke(Code);
                Dec?.Invoke(Code);
            }
            else
            {
                sucsec?.Invoke();
                Bin?.Invoke(Code);
                Dec?.Invoke(Code);
                Hex?.Invoke(Code);
            }

            }        
        private char Invertion(char g)
        {
            return g == '1' ? '0' : '1';
        }
    }
    #region Класс-Helper GUI Hamming15_11
    public class ShowCodeHelpers
    {
        private string str_1;
        private string str_2;
        private int t1;
        private int t2;

        public ShowCodeHelpers(string code)
        {
            Init(code);
        }
        public ShowCodeHelpers() { }
        private void Init(string code)
        {
             str_2 = code.Substring(8);
             str_1 = code.Substring(0, 8);
        }
        public void showBinary(string code)
        {
            Init(code);
            Console.Write("\tCodeHamming in Binary- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str_1);
            Console.ResetColor();
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str_2 + "\n");
        }
        public void ShowBinary()
        {
            Console.Write("\tCodeHamming in Binary- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str_2);
            Console.ResetColor();
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str_1 + "\n");
        }
        public void showHex(string code)
        {
            Init(code);
            converttoInt();
            str_1 = Convert.ToString(t2, 16);
            str_2 = Convert.ToString(t1, 16);
            Console.Write("\tCodeHamming in Hex- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str_1.ToUpper() + " " + str_2.ToUpper() + "\n");
            Console.ResetColor();
        }
        public void showHex()
        {
            converttoInt();
            str_1 = Convert.ToString(t2, 16);
            str_2 = Convert.ToString(t1, 16);
            Console.Write("\tCodeHamming in Hex- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(str_1.ToUpper() + " " + str_2.ToUpper() + "\n");
            Console.ResetColor();
        }
        private void converttoInt()
        {
            t1 = Convert.ToInt32(str_1, 2);
            t2 = Convert.ToInt32(str_2, 2);
        }
        public void showDecimal(string code)
        {
            Init(code);
            converttoInt();
            Console.ResetColor();
            Console.Write("\tCodeHamming in Decimal- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(t2 + " " + t1 + "\n");
            Console.ResetColor();
        }
        public void showDecimal()
        {
            converttoInt();
            Console.ResetColor();
            Console.Write("\tCodeHamming in Decimal- ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write(t2 + " " + t1 + "\n");
            Console.ResetColor();
        }

        public void ShowSuccess()
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ошибки нет!");
            Console.ResetColor();
        }
        public void ShowPoiledBit(int lightposition, string Code)
        {
            Console.Write("CodeHamming in Binary- ");
            for (int i = 0; i < Code.Length; i++)
            {
                if (lightposition == i)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("{0}", Code[i]);
                    Console.ResetColor();
                }
               
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("{0}", Code[i]);
                    Console.ResetColor();
                }
            }
            Console.WriteLine();
        }

        public  void ShowArr(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {

                Console.Write("{0}", s[i]);
            }
        }

        

    }
    #endregion

}




