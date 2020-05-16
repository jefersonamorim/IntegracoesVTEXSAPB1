using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegracoesVETX.Entity
{
    class UpdateInventory
    {

        public bool unlimitedQuantity { get; set; }
        public string dateUtcOnBalanceSystem { get; set; }
        public int quantity { get; set; }

        internal UpdateInventory()
        {
            this.unlimitedQuantity = false;
            this.dateUtcOnBalanceSystem = null;
        }


    }
}
