using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Opc.Da;

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
        private Opc.Da.Subscription m_subscirption = null;

        private void addSub(subs subscription)
        { 
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


            groupState = new SubscriptionState();
            groupState.Name = "myReadGroup2";
            groupState.UpdateRate = 1000; 
            groupState.Active = true;
            groupState.ClientHandle = Guid.NewGuid().ToString();
            group2 = (Subscription)m_server.CreateSubscription(groupState);
            group2.DataChanged += new DataChangedEventHandler(GroupRead_DataChanged);

            

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
            Console.WriteLine(item.SamplingRate);

            groupRead.AddItems(itemlist.ToArray());

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
