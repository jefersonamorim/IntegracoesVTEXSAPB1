using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegracoesVTEX.Util
{
    class ErrorResponseNF
    {
        public Error error { get; set; }
    }
    class Error
    {
        public int code { get; set; }

        public string message { get; set; }
}

}
