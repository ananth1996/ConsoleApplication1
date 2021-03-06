﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Da;
using System.Xml;
using System.Web.Script.Serialization;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace ConsoleApplication1
{

    class subItems
    {
        public String name;
        public String path;
        public int samplingRate;

        public subItems()
        {
            samplingRate = 0;
            name = null;
            path = null;
        }
    
        
    }
    class subs
    {
        public String name;
        public int updateRate;
        public Boolean active;
        public List<subItems> items;

        public subs()
        {
            name = null;
            updateRate = 0;
            active = true;
            items = new List<subItems>();
        }
        public subs(String XmlFile)    
        {
            //Add the parsing and asigning of group subscriptions after parsing the file
        }


    }

    
    class Program
    {
        private static Opc.Da.Server m_server = null;
       

        public class SQLWriter
        {
            public SQLWriter()
            {

            }

            private string dbName = string.Empty;
            public string DBName
            {
                get { return dbName; }
                set { dbName = value; }
            }
            private string pass = string.Empty;
            public string Password
            {
                get { return pass; }
                set { pass = value; }
            }
            private string uid = string.Empty;
            public string UID
            {
                get { return uid; }
                set { uid = value; }
            }


            private MySqlConnection conn = null;
            public MySqlConnection connection
            {
                get { return conn; }
            }

            private static SQLWriter _instance = null;

            public static SQLWriter Instance()
            {
                if (_instance == null)
                    _instance = new SQLWriter();
                return _instance;
            }

            public bool Connect()
            {
                bool result = true;

                if (conn == null)
                {
                    string connstring = string.Format("server=localhost;UID={0}; password={1}", uid, pass);
                    conn = new MySqlConnection(connstring);
                    conn.Open();
                }
                return result;
            }

            public void Close()
            {
                conn.Close();
            }
            public bool MakeDB(string Database)
            {
                if (String.IsNullOrEmpty(Database))
                    return false;
                if (conn == null)
                    return false;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE DATABASE IF NOT EXISTS `" + Database + "`;";
                    cmd.ExecuteNonQuery();
                }
                return true;
            }
            public bool MakeTable(string Item, string MySqlType)
            {
                if (String.IsNullOrEmpty(Item))
                    return false;
                if (conn == null)
                    return false;
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS `" + Item + "`(ts TIMESTAMP(3) primary key, value " + MySqlType + ");";
                    cmd.ExecuteNonQuery();
                    Debug.Write("Executed the command :{1}", cmd.CommandText);
                    cmd.CommandText = "ALTER TABLE `" + Item + "` MODIFY ts TIMESTAMP(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3);";
                    cmd.ExecuteNonQuery();
                }
                return true;
            }

        }



        public static List<subs> Parser(String inputFile)
        {
            String name, itemPath;
            int updateRate;
            List<subs> data = new List<subs>();
            subs s = null;
            subItems item = null;
            XmlDocument inputfile = new XmlDocument();

            try
            {
                inputfile.Load(inputFile);
            }
            catch (System.IO.FileNotFoundException)
            {
                Console.Write("404");
                return null;

            }
            
            XmlNode root = inputfile.DocumentElement;

            XmlNode group = root.SelectSingleNode("descendant::PSTAliasGroup");

            XmlNodeList grouplist = group.SelectNodes("PSTAliasGroup");

            var db = SQLWriter.Instance();
            db.UID = "ananth";
            db.Password = "admin123";
            if (!db.Connect())
            {
                Console.WriteLine("Cannot Connect to database");
            }
            else
            {
                
                foreach (XmlNode g in grouplist)
                {
                    s = new subs();
                    XmlElement gElement = (XmlElement)g;
                    XmlAttribute attr = gElement.GetAttributeNode("name");
                    name = attr.InnerXml;
                    s.name = name;
                    Console.WriteLine("Group name: {0}", name);

                    db.MakeDB(name);
                    db.connection.ChangeDatabase(name);

                    XmlNodeList aliaslist = g.SelectNodes("PSTAlias");
                    foreach (XmlNode alias in aliaslist)
                    {
                        item = new subItems();
                        XmlElement aElement = (XmlElement)alias;
                        attr = aElement.GetAttributeNode("name");
                        name = attr.InnerXml;
                        attr = aElement.GetAttributeNode("itemPath");
                        itemPath = attr.InnerXml;
                        attr = aElement.GetAttributeNode("updateRate");
                        db.MakeTable(name,"varchar(30)");

                        try
                        {
                            updateRate = Convert.ToInt32(attr.InnerXml);
                        }
                        catch (Exception e)
                        {
                            updateRate = 0;
                        }
                        item.name = name;
                        item.path = itemPath;
                        item.samplingRate = updateRate;
                        Console.WriteLine("\tThe item name: {0} \n\tThe item path: {1}\n\tThe item rate: {2}\n\t", name, itemPath, updateRate);
                        s.items.Add(item);
                    }
                    int min;
                    try
                    {

                        min = s.items.Where(x => x.samplingRate != 0).Min(x => x.samplingRate);
                    }
                    catch (Exception e)
                    {
                        min = 100;
                    }
                    s.updateRate = min;
                    Console.WriteLine("Sub min is : {0}", min);
                    data.Add(s);
                }
            }
            return data;
        }

        private static void addSub(subs subscription)
        {
            Opc.Da.Subscription m_subscirption = null;
            Opc.Da.SubscriptionState state = new SubscriptionState();
            state.ClientHandle = Guid.NewGuid().ToString();
            state.Active = subscription.active;
            state.Name = subscription.name;
            state.UpdateRate = subscription.updateRate;
            m_subscirption = (Subscription)m_server.CreateSubscription(state);
            m_subscirption.DataChanged += new DataChangedEventHandler(GroupRead_DataChanged);


            List<Item> itemList = new List<Item>();
            Item item = null;
            foreach(subItems subItem in subscription.items)
            {
                item = new Item();
                item.ItemName = subItem.path;
                item.ClientHandle = subItem.name;
                if(subItem.samplingRate>0)
                {
                    item.SamplingRate = subItem.samplingRate;
                    item.SamplingRateSpecified = true;
                }
                itemList.Add(item);
            }

            m_subscirption.AddItems(itemList.ToArray());

        }


        


        static void Main(string[] args)
        {
            
            //Console.Write("Hello World\n");
            Opc.URL url = new Opc.URL("opcda://localhost/Matrikon.OPC.Simulation.1");
            //System.Net.NetworkCredential  creds = new System.Net.NetworkCredential();
            //System.Net.WebProxy m_proxy = new System.Net.WebProxy();
            //Opc.ConnectData cdata = new Opc.ConnectData(creds, m_proxy);
            //Type t = Type.GetTypeFromProgID("Matrikon.OPC.Simulation.1");
            //Console.Write(t);
            OpcCom.Factory fact = new OpcCom.Factory();
            m_server = new Server(fact, null);
            m_server.Url = url;
            m_server.Connect();

            List<subs> s = null;
            s = Parser(@"C:\Program Files (x86)\Matrikon\OPC\Simulation\alias.xml");
            string json = JsonConvert.SerializeObject(s, Newtonsoft.Json.Formatting.Indented);
            //Console.WriteLine(json);


            foreach(subs x in s)
            {
                addSub(x);
            }
            var t1 = DateTime.Now;
           

           // Disconnecting 
            Console.Read();
            m_server.Disconnect();

       
        }

        public static void GroupRead_DataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] values)
        {

            var db = SQLWriter.Instance();
            db.UID = "ananth";
            db.Password = "admin123";
            
            if (!db.Connect())
            {
                Console.WriteLine("Cannot Connect to database");
            }

            foreach (Subscription subscription in m_server.Subscriptions)
            {
                if(subscription.ClientHandle.Equals(subscriptionHandle))
                {
                    Console.Write(subscription.Name+"\t");
                    db.connection.ChangeDatabase(subscription.Name);
                    string inserter = "";
                    var cmd = db.connection.CreateCommand();
                    
                    foreach (ItemValueResult res in values)
                    {
                        //Here we add the write to the database
                        Console.Write("  {1}:  {0} {2}\n", res.Value.ToString(),res.ClientHandle,res.Timestamp);
                        
                        inserter = String.Format("Insert into `{0}` values ( '{1}' , '{2}' );", res.ClientHandle, res.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"), res.Value.ToString());
                        
                        cmd.CommandText = inserter;
                        try
                        {
                            
                            cmd.ExecuteNonQuery();
                            Debug.WriteLine("success: " + inserter);
                        }
                        catch(Exception e)
                        {
                            Debug.WriteLine("Failure: " + inserter);
                            return;
                        }
                    }
                    Console.WriteLine();
                }
            }
        }


    }


    
}
