using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using System.Data;
using CsvHelper;
using System.IO;
using Microsoft.CSharp.RuntimeBinder;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Configuration;

namespace ConsoleApplication1
{
    class Program
    {
        //static string connstr = @"Server=172.16.0.168;Database=ddp;User Id=bbp_user;Password=bbp_user;"; //伺服器地址與登入資訊
        //static string filename = @"C:\db\convertcsv.csv"; //讀取檔案位置
        #region Dynamic
        //dynamic比較乾脆，直接串上.propName或.fieldName就對了
        //編譯一定會過(因為不檢查)，若Property或Field不存在，
        //則丟出RuntimeBinderException
        static void ShowAnythingByDynamic(dynamic dyna)
        {
              
            foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(dyna))
            {
                try
                {
                    Console.WriteLine(prop.Name + "==" + prop.GetValue(dyna));

                }
                catch (RuntimeBinderException ex) {
                    Console.WriteLine("Error:" + ex.Message);
                }


            }

        }
        #endregion
        private static void getCsvData(out DataTable dt)
        {
            using (StreamReader reader = File.OpenText(ConfigurationManager.AppSettings["FL"])) //用app.config
            {
                //char[] result = new char[reader.BaseStream.Length];
                //await reader.ReadAsync(result, 0, (int)reader.BaseStream.Length);
                //有關DataTable 可參閱 https://dotblogs.com.tw/chjackiekimo/2014/04/03/144606
                var csv = new CsvReader(reader);
                csv.Configuration.HasHeaderRecord = false;
                csv.Configuration.RegisterClassMap<CustomerMap>();
                dt = new DataTable();
                ///建立Header列(水平)
                dt.Columns.Add("INT_seq", typeof(String));
                dt.Columns.Add("STR_first", typeof(String));
                dt.Columns.Add("STR_last", typeof(String));
                dt.Columns.Add("INT_age", typeof(String));
                dt.Columns.Add("STR_street", typeof(String));
                dt.Columns.Add("STR_city", typeof(String));
                dt.Columns.Add("STR_state", typeof(String));
                dt.Columns.Add("int_zip", typeof(String));
                dt.Columns.Add("STR_dollar", typeof(String));
                dt.Columns.Add("STR_pick", typeof(String));
                dt.Columns.Add("STR_date", typeof(String));
                //一列一列的讀取
                while (csv.Read())
                {
                    var record = csv.GetRecord<Customer>();
                    DataRow row = dt.NewRow();
                    row["INT_seq"] = "'"+record.seq+"',";
                    row["STR_first"] = "'" + record.first + "',";
                    row["STR_last"] = "'" + record.last + "',";
                    row["INT_age"] = "'" + record.age + "',";
                    row["STR_street"] = "'" + record.street + "',";
                    row["STR_city"] = "'" + record.city + "',";
                    row["STR_state"] = "'" + record.state + "',";
                    row["int_zip"] = "'" + record.zip + "',";
                    row["STR_dollar"] = "'" + record.dollar + "',";
                    row["STR_pick"] = "'" + record.pick + "',";
                    row["STR_date"] = "'" + record.date + "'";
                    dt.Rows.Add(row);          
                }
                Console.WriteLine();
            }
        }
        // md5驗證參考
        // https://stackoverflow.com/questions/10520048/calculate-md5-checksum-for-a-file
        static string CalculateMD5() //md5比對
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead((ConfigurationManager.AppSettings["FL"]))) //用app.config
                {
                    System.IO.StreamReader stream2 = new System.IO.StreamReader((ConfigurationManager.AppSettings["MD5"]));
                    String md5file = stream2.ReadLine();
                    var hash = md5.ComputeHash(stream);
                    string temp = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                    if (temp == md5file)
                    {
                        return "驗證成功";
                    }
                    else {
                        return "驗證失敗";
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            //取得本地資料
            DataTable dt;
            getCsvData(out dt);

            //進行MD5檔的比對
            Console.WriteLine(CalculateMD5());
            Console.WriteLine();

            //執行目前test筆數
            string SQL = @"select * from public.test_db"; // 選擇TABLE上所有欄位
            Console.WriteLine("日前DB上資料筆數:" + GetAll(SQL).Rows.Count); // 計算資料筆數

            //開始將本地資料匯入DB
            Console.WriteLine("ENTER鍵開始匯入資料");
            Console.ReadLine();
            Console.WriteLine("一共新增"+ ExecNonQuery(dt) + "個資料");
                                    
            //測試新增資料
            //若成功會回傳執行筆數
            //SQL = "insert into test(c1 )values ('c2');";
            //Console.WriteLine("新增結果:" + ExecNonQuery(SQL));
            //Console.ReadLine();
            //刪除修改也是類似的寫法呼叫
        }

        //資料庫 為本機資料庫
        //DataBase:SampeDB
        //TableName:test
        //ColumnNmae:c1

        /// <summary>
        /// 建立取得資料方式
        /// </summary>
        /// <param name="__SQLCommand"></param>
        /// <returns></returns>
        static DataTable GetAll(string _SQLCommand) // 建立資料庫連線 
        {
            DataTable oDataTable = new DataTable();
            using (NpgsqlConnection conn = new NpgsqlConnection(ConfigurationManager.AppSettings["DB"])) //連線
            {
                conn.Open(); //建立連線

                NpgsqlDataAdapter oAdapter = new NpgsqlDataAdapter(_SQLCommand, conn);
                oAdapter.Fill(oDataTable);
                if (oAdapter != null) //檢查是否有資料
                    oAdapter.Dispose();
            }
            return oDataTable;
        }

        static int ExecNonQuery(DataTable dt)
        {
            int result = 0; //計算處裡的資料
            using (NpgsqlConnection connection = new NpgsqlConnection(ConfigurationManager.AppSettings["DB"])) //連線 用app.config
            {
                connection.Open(); //建立連線
                bool notfirst = false;
                foreach (DataRow row in dt.Rows) //將資料庫以每列(水平)進行導出
                {
                    if(notfirst) //跳過第一筆header資料
                    { 
                        string SQL = "INSERT INTO public.test_db VALUES ";
                        SQL = SQL + "(" + row["INT_seq"] + row["STR_first"] + row["STR_last"] + row["INT_age"] + row["STR_street"] + row["STR_city"] + row["STR_state"] + row["int_zip"] + row["STR_dollar"] + row["STR_pick"] + row["STR_date"] + ");";
                        NpgsqlCommand cmd = new NpgsqlCommand(SQL, connection); //建立連線與發送命令
                        result++; //累計匯入筆數次數
                        cmd.CommandType = CommandType.Text;
                        Console.WriteLine(SQL+" code: "+ cmd.ExecuteNonQuery()); //輸出單筆匯入結果
                        cmd.Dispose(); //釋放資源
                    }
                    notfirst = true;
                }
                connection.Close(); //斷開連線
            }
            return result;
        }
    }
}
