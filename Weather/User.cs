using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Weather
{
    class User
    {
        public int ID { get; set; }
        public int IDUser { get; set; }
        public string City { get; set; }
        public string Time { get; set; }

        public User()
        {

        }

        public User(IDataReader reader)
        {
            ID = reader.GetInt32(0);
            IDUser = reader.GetInt32(1);
            Time = reader.GetString(2);
            City = reader.GetString(3);
        }
    }
}
