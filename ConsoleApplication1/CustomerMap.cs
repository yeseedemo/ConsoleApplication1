using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;

namespace ConsoleApplication1
{
    public class Customer
    {
        public String seq { get; set; }
        public String first { get; set; }
        public String last { get; set; }
        public String age { get; set; }
        public String street { get; set; }
        public String city { get; set; }
        public String state { get; set; }
        public String zip { get; set; }
        public String dollar { get; set; }
        public String pick { get; set; }
        public String date { get; set; }
    }

    public sealed class CustomerMap : ClassMap<Customer>
    {
        public CustomerMap()
        {
            Map(m => m.seq).Index(0);
            Map(m => m.first).Index(1);
            Map(m => m.last).Index(2);
            Map(m => m.age).Index(3);
            Map(m => m.street).Index(4);
            Map(m => m.city).Index(5);
            Map(m => m.state).Index(6);
            Map(m => m.zip).Index(7);
            Map(m => m.dollar).Index(8);
            Map(m => m.pick).Index(9);
            Map(m => m.date).Index(10);
        }
    }
}
