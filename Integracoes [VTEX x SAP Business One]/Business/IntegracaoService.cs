using IntegracoesVETX.DAL;
using IntegracoesVETX.Entity;
using IntegracoesVETX.Util;
using IntegracoesVTEX.Util;
using Newtonsoft.Json;
using SAPbobsCOM;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegracoesVETX.Business
{
    public class IntegracaoService
    {
        private Log log;

        public IntegracaoService() {
            this.log = new Log();
        }

        public void IniciarIntegracaoEstoque(SAPbobsCOM.Company oCompany)
        {
            try
            {
                Repositorio repositorio = new Repositorio();

                this.log.WriteLogEstoque("Inicio do Processo de Integração de Estoque");

                WarehouseDAL whsDAL = new WarehouseDAL();

                SAPbobsCOM.Recordset recordset = (SAPbobsCOM.Recordset)oCompany.GetBusinessObject(SAPbobsCOM.BoObjectTypes.BoRecordset);

                recordset = whsDAL.RecuperarSaldoEstoqueSAP(oCompany);

                if (recordset != null && recordset.RecordCount > 0)
                {
                    for (int i = 0; i < recordset.RecordCount; i++)
                    {

                        try
                        {
                            string _itemCode = recordset.Fields.Item("ItemCode").Value.ToString();
                            Int16 _onHand = System.Convert.ToInt16(recordset.Fields.Item("OnHand").Value.ToString());
                            string warehouseId = ConfigurationManager.AppSettings["warehouseId"];

                            if (_itemCode.Equals("003179-055"))
                            {
                                string teste = string.Empty;
                                
                            }

                            Task<HttpResponseMessage> response = repositorio.BuscarItemPorSKU(_itemCode, _onHand, oCompany);

                            if (response.Result.IsSuccessStatusCode)
                            {
                                this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Estoque, _itemCode, _itemCode, EnumStatusIntegracao.Sucesso, "Estoque atualizado com sucesso.");
                                this.log.WriteLogEstoque("Quantidade de estoque do Produto " + _itemCode + " para o depósito " + warehouseId + " atualizada com sucesso.");
                            }
                            if (Convert.ToInt16(response.Result.StatusCode) == 400)
                            {
                                this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Estoque, _itemCode, _itemCode, EnumStatusIntegracao.Erro, response.Result.ReasonPhrase);
                                this.log.WriteLogEstoque("Não foi possível atualizar a quantidade de estoque para o produto " + _itemCode + ". Retorno API Vtex: " + response.Result.ReasonPhrase);
                            }
                        }
                        catch (Exception)
                        {

                            throw;
                        }
                        

                        recordset.MoveNext();

                    }
                    
                }

                if (recordset != null)
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(recordset);
                }
                
                //Log.WriteLog("Atualização controle executação.");
                // Environment.SetEnvironmentVariable("controleExecucao", DateTime.Now.ToUniversalTime().ToString("s") + "Z");

            }
            catch (Exception e)
            {
                this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Estoque, "", "", EnumStatusIntegracao.Erro, e.Message);
                this.log.WriteLogEstoque("Exception IniciarProcessoEstoque "+ e.Message);
                throw;
            }
        }

        public void ProcessarNovosClientes(List<Cliente> clientes, List<Endereco> enderecos) {
            try
            {
                Log.WriteLogCliente("Processando Novos Clientes.");

                //Percorrendo fazendo de para da lista de clientes e endereços para enserir endereço correspondente
                if (clientes.Count > 0)
                {
                    SAPbobsCOM.Company oCompany = CommonConn.InitializeCompany();

                    //Log.WriteLogCliente("oCompany.Connected "+ oCompany.Connected);

                    if (oCompany.Connected)
                    {
                        //List<Cliente> clientesComErro = new List<Cliente>();

                        string document = string.Empty;

                        foreach (Cliente cliente in clientes)
                        {
                            foreach (Endereco endereco in enderecos)
                            {
                                if (cliente.document != null || cliente.corporateDocument != null)
                                {
                                    if (cliente.isCorporate.Equals("true"))
                                    {
                                        document = cliente.corporateDocument;
                                    }
                                    else
                                    {
                                        document = cliente.document;
                                    }
                                }
                                if (document != null)
                                {
                                    //Se o Id do Cliente for igual ao UserId que está em endereço o processo continua
                                    if (cliente.id.Equals(endereco.userId))
                                    {
                                        //this.InserirClientes(oCompany, cliente, endereco);
                                        this.InserirClientes(oCompany, cliente, endereco, null);

                                    }
                                    else
                                    {
                                        /*if (cliente.document == null)
                                        {
                                            this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, cliente.id, "", EnumStatusIntegracao.Erro, "Cliente não cadastrado pois o número do documento VTEX é inválido.");
                                            break;
                                        }
                                        else
                                        {
                                            this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, cliente.document, "", EnumStatusIntegracao.Erro, "Cliente não cadastrado pois não tem endereço cadastrado VTEX.");
                                            break;
                                        }
                                        */
                                        //clientesComErro.Add(cliente);
                                    }
                                }
                                else {
                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, cliente.id, "", EnumStatusIntegracao.Erro, "Cliente não cadastrado pois o número do documento VTEX é inválido.");
                                    break;
                                }
                                document = null;
                            }
                        }

                        /*if (clientesComErro.Count > 0)
                        {
                            string documentoAtual = string.Empty;

                            foreach (Cliente cli in clientesComErro)
                            {
                                if (cli.document == null)
                                {
                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, cli.id, "", EnumStatusIntegracao.Erro, "Cliente não cadastrado pois o Documento cadastrado na VTEX é inválido.");
                                }
                                if (!documentoAtual.Equals(cli.document) && cli.document != null)
                                {
                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, cli.document, "", EnumStatusIntegracao.Erro, "Cliente não cadastrado pois não tem endereço cadastrado VTEX.");
                                }
                                documentoAtual = cli.document;
                            }
                        }*/
                    }
                    //CommonConn.FinalizeCompany();
                }

            }
            catch (Exception e)
            {
                Log.WriteLogCliente("ProcessarNovosClientes Exception:"+e.Message);
                throw;
            }
        }

        private void InserirClientes(SAPbobsCOM.Company company, Cliente cliente, Endereco endereco, Pedido pedido) {
            try
            {
                BusinessPartnersDAL bpDAL = new BusinessPartnersDAL();

                string errorMessage;

                bpDAL.InserirBusinessPartner(company, cliente, endereco, pedido, out errorMessage);
            }
            catch (Exception e)
            {
                Log.WriteLogCliente("Exception inserirClientes "+e.Message);
                throw;
            }
        }

        public void IniciarIntegracaoPedido(SAPbobsCOM.Company oCompany) {
            try
            {
                this.log.WriteLogPedido("Inicio do Processo de Integração de Pedido.");

                //var test = Convert.ToDouble(testIn);
                Repositorio repositorioPedido = new Repositorio();
                List<Feed> listaEnveto = new List<Feed>();
                Pedido pedidoVtex = new Pedido();

                Task<HttpResponseMessage> responsePedido = repositorioPedido.ConsultarFilaDeEventos();

                if (responsePedido.Result.IsSuccessStatusCode)
                {
                    var jsonResponseFeed = responsePedido.Result.Content.ReadAsStringAsync().Result;

                    listaEnveto = JsonConvert.DeserializeObject<List<Feed>>(jsonResponseFeed);

                    if (listaEnveto.Count > 0)
                    {
                        //Validando evento do pedido
                        foreach (Feed evento in listaEnveto)
                        {
                            //Se o evento do pedido for Pronto para Manuseio (ready-for-handling)
                            if (evento.state.Equals("ready-for-handling"))
                            {
                                Task<HttpResponseMessage> responseOrder = repositorioPedido.BuscarPedido(evento.orderId);

                                if (responseOrder.Result.IsSuccessStatusCode)
                                {
                                    var jsonPedido = responseOrder.Result.Content.ReadAsStringAsync().Result;

                                    pedidoVtex = JsonConvert.DeserializeObject<Pedido>(jsonPedido);

                                    if (pedidoVtex.storePreferencesData.countryCode.Equals("BRA"))
                                    {
                                        if (pedidoVtex.origin.Equals("Fulfillment"))
                                        {
                                            Cliente clienteMkt = new Cliente();
                                            Endereco enderecoMkt = new Endereco();

                                            this.InserirClientes(oCompany, clienteMkt, enderecoMkt, pedidoVtex);
                                        }
                                        else
                                        {
                                            //BuscarCliente
                                            List<Cliente> clientels = new List<Cliente>();
                                            List<Endereco> enderecols = new List<Endereco>();

                                            if (!string.IsNullOrEmpty(pedidoVtex.clientProfileData.document))
                                            {
                                                Task<HttpResponseMessage> responseCliente = repositorioPedido.BuscarClientePorDocumento(pedidoVtex.clientProfileData.document);

                                                if (responseCliente.Result.IsSuccessStatusCode)
                                                {
                                                    var jsonCliente = responseCliente.Result.Content.ReadAsStringAsync().Result;

                                                    clientels = JsonConvert.DeserializeObject<List<Cliente>>(jsonCliente);

                                                    foreach (var cliente in clientels)
                                                    {
                                                        Task<HttpResponseMessage> responseEndereco = repositorioPedido.BuscarEnderecoPorUserId(cliente.id);

                                                        if (responseEndereco.Result.IsSuccessStatusCode)
                                                        {
                                                            var jsonEndereco = responseEndereco.Result.Content.ReadAsStringAsync().Result;

                                                            enderecols = JsonConvert.DeserializeObject<List<Endereco>>(jsonEndereco);

                                                        }

                                                        foreach (var endereco in enderecols)
                                                        {
                                                            if (oCompany.Connected)
                                                            {
                                                                string document = string.Empty;

                                                                if (cliente.document != null || cliente.corporateDocument != null)
                                                                {
                                                                    if (cliente.isCorporate.Equals("true"))
                                                                    {
                                                                        document = cliente.corporateDocument;
                                                                    }
                                                                    else
                                                                    {
                                                                        document = cliente.document;
                                                                    }
                                                                }
                                                                if (document != null)
                                                                {
                                                                    this.InserirClientes(oCompany, cliente, endereco, pedidoVtex);
                                                                }
                                                                else
                                                                {
                                                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cliente, cliente.id, "", EnumStatusIntegracao.Erro, "Cliente não cadastrado pois o número do documento VTEX é inválido.");

                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }

                                        }

                                        //Inserir Pedido de venda
                                        this.InserirPedidoVenda(oCompany, pedidoVtex, evento);
                                    }
                                }
                            }
                        }
                    }
                }
                else {
                    this.log.WriteLogPedido("Não foi possível consultar Fila de Enventos Vtex. "+ responsePedido.Result.ReasonPhrase);
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            catch (Exception e)
            {
                this.log.WriteLogPedido("Exception IniciarIntegracaoPedido "+e.Message);
                throw;
            }
        }

        private int InserirPedidoVenda(SAPbobsCOM.Company oCompany, Pedido pedidoVtex, Feed evento) {
            try
            {
                if (oCompany.Connected)
                {
                    OrdersDAL order = new OrdersDAL(oCompany);
                    string messageError = "";
                    int oOrderNum = 0;
                    Boolean inserir = true;

                    foreach (ItemVtex item in pedidoVtex.items)
                    {
                        if (item.refId == null && inserir)
                        {
                            this.log.WriteLogTable(oCompany, EnumTipoIntegracao.PedidoVenda, pedidoVtex.orderId, "", EnumStatusIntegracao.Erro, "Um ou mais item(s) do pedido está com o código de referência inválido.");
                            //throw new ArgumentException("Não foi possível criar o Pedido de Venda para o pedido "+pedidoVtex.orderId+" pois um ou mais item(s) do pedido está com o código de referência inválido.");
                            inserir = false;
                        }
                    }

                    if (inserir)
                    {
                        oOrderNum = order.InsertOrder(pedidoVtex, out messageError);

                        if (oOrderNum == 0)
                        {
                            Repositorio repositorio = new Repositorio();

                            //Pedido inserido no SAP, removendo pedido da fila de enventos(Feed), para não ser mais processado.

                            Task<HttpResponseMessage> response = repositorio.AtualizaFilaEnvetoPedido(evento.handle);

                            if (response.Result.IsSuccessStatusCode)
                            {
                                this.log.WriteLogPedido("Pedido " + pedidoVtex.orderId + " removido da fila de eventos (Feed).");
                            }
                            else
                            {
                                this.log.WriteLogPedido("Não foi possível remover o pedido " + pedidoVtex.orderId + " da fila de eventos (Feed)." + response.Result.ReasonPhrase);
                            }

                            Task<HttpResponseMessage> responseIniciarManuseio = repositorio.InciarManuseio(pedidoVtex.orderId);

                            if (responseIniciarManuseio.Result.IsSuccessStatusCode)
                            {
                                //this.log.WriteLogPedido("Alterado status do pedido "+pedidoVtex.orderId+" para Iniciar Manuseio.");
                            }
                            else
                            {
                                this.log.WriteLogPedido("Não foi possível alterar status do pedido " + pedidoVtex.orderId + " para Iniciar Manuseio." + response.Result.ReasonPhrase);
                            }
                        }
                    }
                }

                return 0;
            }
            catch (Exception e)
            {
                this.log.WriteLogPedido("Exception InserirPedidoVenda "+e.Message);
                throw;
            }
        }

        public void RetornoNotaFiscal(SAPbobsCOM.Company oCompany)
        {
            try
            {
                if (oCompany.Connected)
                {
                    OrdersDAL orders = new OrdersDAL(oCompany);

                    SAPbobsCOM.Recordset recordSet = (Recordset)oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);

                    recordSet = orders.RecuperarNumeroNF();

                    SAPbobsCOM.Recordset tempRecordSet = (Recordset)oCompany.GetBusinessObject(BoObjectTypes.BoRecordset);

                    tempRecordSet = orders.RecuperarNumeroNF();

                    if (recordSet != null && recordSet.RecordCount > 0)
                    {
                        while (!recordSet.EoF)
                        {
                            if (!recordSet.EoF)
                            {
                                Repositorio repositorio = new Repositorio();
                                Invoice invoice = new Invoice();

                                invoice.type = "Output";
                                invoice.issuanceDate = recordSet.Fields.Item("invoiceDate").Value.ToString();
                                CultureInfo provider = CultureInfo.InvariantCulture;
                                DateTime issuanceDateDT = DateTime.ParseExact(invoice.issuanceDate, "dd/MM/yyyy 00:00:00", provider);

                                invoice.issuanceDate = issuanceDateDT.ToString("yyyy-MM-dd");
                                invoice.invoiceNumber = recordSet.Fields.Item("invoiceNumber").Value.ToString();
                                invoice.invoiceKey = recordSet.Fields.Item("nfeKey").Value.ToString();

                                string externalId = string.Empty;
                                string idOrderVtex = string.Empty; 
                                string idOrderVtex2 = string.Empty; 
                                string docSAP = string.Empty;
                                string docNPV = string.Empty;

                                externalId = recordSet.Fields.Item("externalId").Value.ToString();
                                idOrderVtex = recordSet.Fields.Item("idOrderVtex").Value.ToString();
                                idOrderVtex2 = recordSet.Fields.Item("idOrderVtex2").Value.ToString();
                                docSAP = recordSet.Fields.Item("docSAP").Value.ToString();
                                docNPV = recordSet.Fields.Item("docNPV").Value.ToString();

                                invoice.invoiceValue = recordSet.Fields.Item("totalNF").Value.ToString().Replace(",", "");
                                invoice.courier = recordSet.Fields.Item("shippingMethod").Value.ToString();

                                int updatePedidoNum = 0;
                                string idPedidoVTEX = string.Empty;

                                string tempDocNPV = string.Empty;

                                List<ItemNF> listaItem = new List<ItemNF>();

                                for (int j = 0; j < tempRecordSet.RecordCount; j++)
                                {
                                    if (!tempRecordSet.EoF)
                                    {
                                        tempDocNPV = tempRecordSet.Fields.Item("docNPV").Value.ToString();

                                        if (docNPV.Equals(tempDocNPV))
                                        {
                                            ItemNF item = new ItemNF();

                                            item.id = tempRecordSet.Fields.Item("codItem").Value.ToString();
                                            item.price = System.Convert.ToInt32(tempRecordSet.Fields.Item("precoItem").Value.ToString().Replace(",", ""));
                                            item.quantity = System.Convert.ToInt32(tempRecordSet.Fields.Item("qtdItem").Value.ToString());

                                            listaItem.Add(item);
                                        }

                                        if (j >= 10)
                                        {
                                            break; 
                                        }
                                        tempRecordSet.MoveNext();
                                    }

                                }

                                invoice.items = listaItem;

                                if (!string.IsNullOrEmpty(idOrderVtex))
                                {
                                    idPedidoVTEX = idOrderVtex;
                                }
                                else if (!string.IsNullOrEmpty(idOrderVtex2))
                                {
                                    idPedidoVTEX = idOrderVtex2;
                                }

                                if (!string.IsNullOrEmpty(idOrderVtex) && !string.IsNullOrEmpty(idOrderVtex2))
                                {
                                    //recuperar pedido e validar status
                                    Task<HttpResponseMessage> responseOrder = repositorio.BuscarPedido(idOrderVtex);

                                    if (responseOrder.Result.IsSuccessStatusCode)
                                    {
                                        string jsonPedido = responseOrder.Result.Content.ReadAsStringAsync().Result;

                                        var order = JsonConvert.DeserializeObject<Pedido>(jsonPedido);

                                        if (!order.status.Equals("canceled"))
                                        {
                                            Task<HttpResponseMessage> response = repositorio.RetornoNotaFiscal(invoice, idPedidoVTEX);

                                            if (response.Result.IsSuccessStatusCode)
                                            {

                                                this.log.WriteLogTable(oCompany, EnumTipoIntegracao.NF, idPedidoVTEX, docSAP, EnumStatusIntegracao.Sucesso, "Número NF " + invoice.invoiceNumber + " enviado para a Vtex com sucesso.");
                                                this.log.WriteLogPedido("Número NF para o Pedido de Venda " + docSAP + " enviado para a Vtex com sucesso.");

                                                //Atualizando campo de usuário U_EnvioNFVTEX
                                                updatePedidoNum = orders.AtualizarPedidoVenda(oCompany, Convert.ToInt32(externalId));

                                                if (updatePedidoNum != 0)
                                                {
                                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.NF, idPedidoVTEX, docSAP, EnumStatusIntegracao.Erro, "Número NF " + invoice.invoiceNumber + " retornado porém não foi possivél atualizar campo de usuário (U_EnvioNFVTEX) do Pedido de Venda");
                                                    this.log.WriteLogPedido("Falha ao atualizar Pedido de Venda " + docSAP);
                                                }
                                            }
                                            else
                                            {
                                                //serializacao erro:
                                                var stringJSONResp = response.Result.Content.ReadAsStringAsync().Result;

                                                var errorResponse = JsonConvert.DeserializeObject<ErrorResponseNF>(stringJSONResp);

                                                if (errorResponse != null)
                                                {
                                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.NF, idPedidoVTEX, externalId, EnumStatusIntegracao.Erro, errorResponse.error.message);
                                                    this.log.WriteLogPedido("Falha ao retornar número da Nota Fiscal " + externalId + " para a Vtex");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            this.log.WriteLogTable(oCompany, EnumTipoIntegracao.NF, idPedidoVTEX, docSAP, EnumStatusIntegracao.Erro, "Pedido com status de \"cancelado\" na VTEX.");
                                            this.log.WriteLogPedido("Pedido com status de \"cancelado\" na VTEX.");

                                            //Atualizando campo de usuário U_EnvioNFVTEX
                                            updatePedidoNum = orders.AtualizarPedidoVenda(oCompany, Convert.ToInt32(externalId));
                                        }
                                    }

                                }
                                else
                                {
                                    this.log.WriteLogTable(oCompany, EnumTipoIntegracao.NF, idPedidoVTEX, externalId, EnumStatusIntegracao.Erro, "Id do Pedido VTEX (NumAtCard e U_NumPedEXT) do Pedido de Venda " + docNPV + " em branco.");
                                    this.log.WriteLogPedido("Falha ao retornar número da Nota Fiscal " + externalId + " para a Vtex - Id do Pedido VTEX (NumAtCard) do Pedido de Venda " + docNPV + " em branco.");

                                    //Atualizando campo de usuário U_EnvioNFVTEX
                                    updatePedidoNum = orders.AtualizarPedidoVenda(oCompany, Convert.ToInt32(externalId));

                                    if (updatePedidoNum != 0)
                                    {
                                        this.log.WriteLogTable(oCompany, EnumTipoIntegracao.NF, idPedidoVTEX, docSAP, EnumStatusIntegracao.Erro, "Número NF " + invoice.invoiceNumber + " retornado porém não foi possivél atualizar campo de usuário (U_EnvioNFVTEX) do Pedido de Venda");
                                        this.log.WriteLogPedido("Falha ao atualizar Pedido de Venda " + docSAP);
                                    }
                                }
                                //recordSet = orders.RecuperarNumeroNF();
                                recordSet.MoveNext();
                            }
                            
                        }
                    }

                    if (recordSet != null)
                    {
                        System.Runtime.InteropServices.Marshal.ReleaseComObject(recordSet);
                    }
                }
            }
            catch (Exception e)
            {
                this.log.WriteLogPedido("Exception RetornoNotaFiscal "+e.Message);
                //throw;
            }
        }

        public void IniciarIntegracaoCancelamentoPedido(SAPbobsCOM.Company oCompany)
        {
            try
            {
                //var test = Convert.ToDouble(testIn);
                Repositorio repositorioCancelPedido = new Repositorio();
                OrderFiltered orders = new OrderFiltered();

                Task<HttpResponseMessage> responseOrderFiltered = repositorioCancelPedido.PedidosACancelar();

                if (responseOrderFiltered.Result.IsSuccessStatusCode)
                {
                    var jsonListOrderFiltered = responseOrderFiltered.Result.Content.ReadAsStringAsync().Result;

                    orders = JsonConvert.DeserializeObject<OrderFiltered>(jsonListOrderFiltered);

                    if (orders.list.Length > 0)
                    {
                        foreach (List item in orders.list)
                        {
                            if (item.currencyCode.Equals("BRL") && item.status.Equals("payment-pending"))
                            {
                                //string idFormaPagmt = pedido.paymentData.transactions.ElementAt<Transaction>(0).payments.ElementAt<Payment>(0).paymentSystem;

                                TimeSpan date = DateTime.Now - item.creationDate;

                                int qtdDias = date.Days;

                                if (qtdDias > System.Convert.ToInt32(ConfigurationManager.AppSettings["qtdDiasCancelemtno"]))
                                {
                                    //cancelar pedido com mais de 3 dias
                                    Task<HttpResponseMessage> responseCacelPedido = repositorioCancelPedido.CancelarPedido(item.orderId);

                                    if (responseCacelPedido.Result.IsSuccessStatusCode)
                                    {
                                        //pedido cancelado
                                        this.log.WriteLogPedido("Pedido " + item.orderId + " cancelado com sucesso." + responseCacelPedido.Result.ReasonPhrase);
                                        this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cancel, item.orderId,"", EnumStatusIntegracao.Sucesso, "Pedido " + item.orderId + " cancelado com sucesso.");
                                    }
                                    else
                                    {
                                        this.log.WriteLogPedido("Não foi possível cancelar pedido " + item.orderId + "." + responseCacelPedido.Result.ReasonPhrase);
                                        this.log.WriteLogTable(oCompany, EnumTipoIntegracao.Cancel, item.orderId, "", EnumStatusIntegracao.Erro, "Não foi possível cancelar pedido " + item.orderId + "." + responseCacelPedido.Result.ReasonPhrase);
                                    }
                                }

                            }
                        }
                    }
                }
                else
                {
                    log.WriteLogPedido("Nenhum Pedido pendente a ser cancelado.");
                }
            }
            catch (Exception e)
            {
                this.log.WriteLogPedido("Exception IntegracaoCancelamentoPedido " + e.Message);
                throw;
            }
        }

    }
}
