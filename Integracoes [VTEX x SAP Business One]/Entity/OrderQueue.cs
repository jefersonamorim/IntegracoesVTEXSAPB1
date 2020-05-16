﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegracoesVETX.Entity
{
    public class OrderQueue
    {
        public int Total { get; set; }
        public List<Orders> orders { get; set; }
    }

    public class Orders
    {
        public int Id { get; set; }
        public string IdOrder { get; set; }
        public string IdOrderMarketplace { get; set; }
        public DateTime InsertedDate { get; set; }
        public string OrderStatus { get; set; }
    }

}
