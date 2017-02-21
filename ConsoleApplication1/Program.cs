using System;
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
            active = false;
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

            foreach (XmlNode g in grouplist)
            {
                s = new subs();
                XmlElement gElement = (XmlElement)g;
                XmlAttribute attr = gElement.GetAttributeNode("name");
                name = attr.InnerXml;
                s.name = name;
                Console.WriteLine("Group name: {0}", name);

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
                    try
                    {
                        updateRate = Convert.ToInt32( attr.InnerXml);
                    }
                    catch(Exception e)
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


        public class SQLWriter
        {
            private SQLWriter()
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
                    string connstring = string.Format("server=localhost;UID={1}; password={2}", uid, passw);
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
                    cmd.CommandText = "CREATE TABLE IF NOT EXISTS `" + Item + "`(ts TIMESTAMP primary key, value " + MySQLtype + ");";
                    cmd.ExecuteNonQuery();
                }
                return true;
            }

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
            Console.WriteLine(json);


            foreach(subs x in s)
            {
                addSub(x);
            }

            foreach (Subscription i in m_server.Subscriptions)
            {
                Console.WriteLine(JsonConvert.SerializeObject(i, Newtonsoft.Json.Formatting.Indented));
            }

            //Console.Write(m_server+"\n");

            /*
            Subscription groupRead,group2;
            SubscriptionState groupState;

            List<Item> itemlist = new List<Item>();
            groupState = new SubscriptionState();

            groupState.Name = "myReadGroup";
            groupState.UpdateRate = 1000; //should be min of all the items in the sub, this allows slower data to be read when required
            groupState.Active = true;
            groupState.ClientHandle = Guid.NewGuid().ToString();
            groupRead = (Subscription)m_server.CreateSubscription(groupState);
            groupRead.DataChanged += new DataChangedEventHandler(GroupRead_DataChanged);


            Item item = new Item();
            item.ItemName = "Random.Real4";
            item.SamplingRate = 10000;
            item.ClientHandle = "Vlaue A";
            item.SamplingRateSpecified = true;
            itemlist.Add(item);





            item = new Item();
            item.ItemName = "Random.Real4";
            item.ClientHandle = "Integer";
            itemlist.Add(item);
            groupRead.AddItems(itemlist.ToArray());
            */




            /*

            groupState = new SubscriptionState();
            groupState.Name = "myReadGroup2";
            groupState.UpdateRate = 1000; 
            groupState.Active = true;
            groupState.ClientHandle = Guid.NewGuid().ToString();
            group2 = (Subscription)m_server.CreateSubscription(groupState);
            group2.DataChanged += new DataChangedEventHandler(GroupRead_DataChanged);

            

            

            itemlist = null;
            itemlist = new List<Item>();
            item = new Item();
            item.ItemName = "Random.Boolean";
            itemlist.Add(item);
            item = new Item();
            item.ItemName = "Random.Time";
            itemlist.Add(item);
                        
            group2.AddItems(itemlist.ToArray());

              */
            Console.Read(); 
            m_server.Disconnect();

       
        }

        public static void GroupRead_DataChanged(object subscriptionHandle, object requestHandle, ItemValueResult[] values)
        {
           foreach(Subscription subscription in m_server.Subscriptions)
            {
                if(subscription.ClientHandle.Equals(subscriptionHandle))
                {
                    Console.Write(subscription.Name+"\t");
                    foreach(ItemValueResult res in values)
                    {
                        //Here we add the write to the database
                        Console.Write("  {1}:  {0}\t", res.Value.ToString(),res.ClientHandle);
                    }
                    Console.WriteLine();
                }
            }
        }


    }


    
}
