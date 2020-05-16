using IntegracoesVETX.Business;
using IntegracoesVETX.DAL;
using IntegracoesVETX.Util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace IntegracoesVTEX
{
    public partial class Scheduler : ServiceBase
    {
        private Timer timerEstoque = null;

        private Timer timerPedidos = null;

        private Timer timerRetNF = null;

        private Timer timerCancelPedido = null;

        private string _path = ConfigurationManager.AppSettings["Path"];

        private Boolean jobIntegracaoEstoque = Convert.ToBoolean(ConfigurationManager.AppSettings["jobIntegracaoEstoque"]);

        private Boolean jobIntegracaoPedido = Convert.ToBoolean(ConfigurationManager.AppSettings["jobIntegracaoPedido"]);

        private Boolean jobIntegracaoRetNF = Convert.ToBoolean(ConfigurationManager.AppSettings["jobIntegracaoRetornoNF"]);

        private Boolean jobIntegracaoCancelPedido = Convert.ToBoolean(ConfigurationManager.AppSettings["jobIntegracaoCancelPedido"]);

        private Log log;

        private SAPbobsCOM.Company oCompany;

        public Scheduler()
        {

            InitializeComponent();
            this.log = new Log();
            this.oCompany = CommonConn.InitializeCompany();
        }

        public void TesteWriter()
        {
            ///Log.WriteLogEstoque("Modo DEBUG inicializado.");

            //Log.WriteLogPedido("Modo DEBUG inicializado.");
            //string intervaloExecucaoEstoque = ConfigurationManager.AppSettings["intervaloExecucaoEstoque"] + ",10";
            //this.timerEstoque = new Timer();

            //this.timerEstoque.Interval = TimeSpan.FromHours(Convert.ToDouble(intervaloExecucaoEstoque)).TotalMilliseconds;

            IntegracaoService integracaoService = new IntegracaoService();
            //integracaoService.IniciarIntegracaoCancelamentoPedido(oCompany);
            //integracaoService.IniciarIntegracaoEstoque(oCompany);

            integracaoService.RetornoNotaFiscal(this.oCompany);

            //integracaoService.IniciarIntegracaoPedido(null);



        }
        protected override void OnStart(string[] args)
        {
            try
            {
                this.log.WriteLogEstoque("#### Integração Inicializada.");
                this.log.WriteLogPedido("#### Integração Inicializada.");

                if (jobIntegracaoEstoque)
                {
                    this.timerEstoque = new Timer();

                    string intervaloExecucaoEstoque = ConfigurationManager.AppSettings["intervaloExecucaoEstoque"] + ",01";
                    //double teste = Convert.ToDouble(intervaloExecucaoEstoque);
                    //this.timerEstoque.Interval = Convert.ToDouble(intervaloExecucaoEstoque);
                    this.timerEstoque.Interval = TimeSpan.FromHours(Convert.ToDouble(intervaloExecucaoEstoque)).TotalMilliseconds;

                    timerEstoque.Enabled = true;

                    this.timerEstoque.Elapsed += new System.Timers.ElapsedEventHandler(this.IntegracaoEstoque);

                }
                if (jobIntegracaoPedido)
                {
                    this.timerPedidos = new Timer();

                    string intervaloExecucaoPedido = ConfigurationManager.AppSettings["intervaloExecucaoPedido"];

                    this.timerPedidos.Interval = Convert.ToInt32(intervaloExecucaoPedido);

                    timerPedidos.Enabled = true;

                    this.timerPedidos.Elapsed += new System.Timers.ElapsedEventHandler(this.IntegracaoPedido);
                }
                if (jobIntegracaoRetNF)
                {
                    this.timerRetNF = new Timer();

                    string intervaloExecucaoRetNF = ConfigurationManager.AppSettings["intervaloExecucaoRetNF"];

                    this.timerRetNF.Interval = Convert.ToInt32(intervaloExecucaoRetNF);

                    timerRetNF.Enabled = true;

                    this.timerRetNF.Elapsed += new System.Timers.ElapsedEventHandler(this.IntegracaoRetornoNF);

                }
                if (jobIntegracaoCancelPedido)
                {
                    this.timerCancelPedido = new Timer();

                    string intervaloExecucaoCancelPedido = ConfigurationManager.AppSettings["intervaloExecucaoCancelPedido"];

                    this.timerCancelPedido.Interval = Convert.ToInt32(intervaloExecucaoCancelPedido);

                    timerCancelPedido.Enabled = true;

                    this.timerCancelPedido.Elapsed += new System.Timers.ElapsedEventHandler(this.IntegracaoCancelPedido);
                }

            }
            catch (Exception)
            {
                throw;
            }
        }

        private void IntegracaoPedido(object sender, ElapsedEventArgs e)
        {
            try
            {
                timerPedidos.Enabled = false;
                timerPedidos.AutoReset = false;

                this.log.WriteLogPedido("#### INTEGRAÇÃO DE PEDIDOS INICIALIZADA");

                IntegracaoService integracaoService = new IntegracaoService();

                integracaoService.IniciarIntegracaoPedido(this.oCompany);

                timerPedidos.Enabled = true;

                //System.Runtime.InteropServices.Marshal.ReleaseComObject(oCompany);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                this.log.WriteLogPedido("Exception IntegracaoPedido " + ex.Message);
                throw;
            }
        }

        private void IntegracaoEstoque(object sender, ElapsedEventArgs e)
        {
            try
            {
                timerEstoque.Enabled = false;
                timerEstoque.AutoReset = false;

                this.log.WriteLogEstoque("#### INTEGRAÇÃO DE ESTOQUE INICIALIZADA");

                IntegracaoService integracaoService = new IntegracaoService();

                integracaoService.IniciarIntegracaoEstoque(this.oCompany);

                timerEstoque.Enabled = true;

                //System.Runtime.InteropServices.Marshal.ReleaseComObject(oCompanyEstoque);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

            }
            catch (Exception ex)
            {
                this.log.WriteLogEstoque("Exception IntegracaoEstoque " + ex.Message);
                throw;
            }
        }

        private void IntegracaoRetornoNF(object sender, ElapsedEventArgs e)
        {
            try
            {
                timerRetNF.Enabled = false;
                timerRetNF.AutoReset = false;

                this.log.WriteLogPedido("#### INTEGRAÇÃO RETORNO NF INICIALIZADA");

                IntegracaoService integracaoService = new IntegracaoService();

                integracaoService.RetornoNotaFiscal(this.oCompany);

                timerRetNF.Enabled = true;

                //System.Runtime.InteropServices.Marshal.ReleaseComObject(oCompanyRetNF);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                this.log.WriteLogPedido("Exception IntegracaoRetornoNF " + ex.Message);
                throw;
            }
        }

        private void IntegracaoCancelPedido(object sender, ElapsedEventArgs e)
        {
            try
            {
                timerCancelPedido.Enabled = false;
                timerCancelPedido.AutoReset = false;

                this.log.WriteLogPedido("#### INTEGRAÇÃO CANCELAMENTO DE PEDIDO INICIADA");

                IntegracaoService integracaoService = new IntegracaoService();

                integracaoService.IniciarIntegracaoCancelamentoPedido(this.oCompany);

                timerCancelPedido.Enabled = true;

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception ex)
            {
                this.log.WriteLogPedido("Exception IntegracaoCancelamento " + ex.Message);
                throw;
            }
        }
        protected override void OnStop()
        {
            timerEstoque.Stop();
            timerPedidos.Stop();
            timerRetNF.Stop();
            timerCancelPedido.Stop();

        }
    }
}
